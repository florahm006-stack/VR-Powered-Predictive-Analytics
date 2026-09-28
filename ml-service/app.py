"""
FastAPI ML microservice for the VR-Powered Predictive Analytics Lab.

Endpoints
---------
GET  /health            -> liveness probe (Unity PredictionClient health-check)
POST /predict/linear    -> generic linear regression inference
POST /predict/forecast  -> demand / time-series forecast

Models are trained by train_model.py and loaded from models/ at startup.
If a model file is missing, the service returns a clear 503 so Unity can
fall back to its local ONNX/closed-form backend.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

MODELS_DIR = Path(__file__).parent / "models"

app = FastAPI(title="VR Lab ML Service", version="1.0.0")

_models: dict[str, object] = {}


class PredictRequest(BaseModel):
    features: list[float] = Field(..., description="Input feature vector")
    horizon: int = Field(1, ge=1, le=64, description="Forecast steps ahead")


class PredictResponse(BaseModel):
    predictions: list[float]


def _load_models() -> None:
    """Load joblib models if present (service stays up without them)."""
    import joblib  # local import so /health works even without sklearn data

    for name in ("demand_model.joblib", "linear_model.joblib"):
        path = MODELS_DIR / name
        if path.exists():
            _models[name] = joblib.load(path)


@app.on_event("startup")
def startup() -> None:
    _load_models()


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "models_loaded": sorted(_models.keys())}


@app.post("/predict/linear", response_model=PredictResponse)
def predict_linear(req: PredictRequest) -> PredictResponse:
    model = _models.get("linear_model.joblib")
    if model is None:
        raise HTTPException(503, "linear_model.joblib not trained yet — run train_model.py")
    x = np.asarray(req.features, dtype=float)
    y = model.predict(x.reshape(-1, 1) if x.ndim == 1 else x)
    return PredictResponse(predictions=[float(v) for v in np.atleast_1d(y)])


@app.post("/predict/forecast", response_model=PredictResponse)
def predict_forecast(req: PredictRequest) -> PredictResponse:
    """
    Two modes:
      - Demand model: features = [price, marketing_spend, seasonality]
        -> demand per horizon step (with small trend).
      - Stock forecast (no demand_model scenario): naive drift over the
        provided history window.
    """
    model = _models.get("demand_model.joblib")

    if model is not None and len(req.features) == 3:
        x = np.asarray(req.features, dtype=float).reshape(1, -1)
        base = float(model.predict(x)[0])
        preds = [max(0.0, base * (1.0 + 0.02 * i)) for i in range(req.horizon)]
        return PredictResponse(predictions=preds)

    # Fallback: naive drift on a history window (stock prices).
    hist = np.asarray(req.features, dtype=float)
    if hist.size == 0:
        raise HTTPException(400, "features must not be empty")
    drift = float((hist[-1] - hist[0]) / (hist.size - 1)) if hist.size > 1 else 0.0
    preds = [float(hist[-1]) + drift * (i + 1) for i in range(req.horizon)]
    return PredictResponse(predictions=preds)
