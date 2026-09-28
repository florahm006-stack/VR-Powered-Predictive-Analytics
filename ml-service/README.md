# VR Lab ML Service

Python FastAPI microservice providing predictions to the Unity app.

## Setup

```powershell
cd ml-service
python -m venv .venv
.venv\Scripts\Activate
pip install -r requirements.txt
python train_model.py        # trains + saves models/ (joblib + ONNX exports via skl2onnx)
uvicorn app:app --reload --port 8000
```

`train_model.py` saves:
- `models/demand_model.joblib` (R² ≈ 0.996 on synthetic training data)
- `models/linear_model.joblib`
- `models/demand_model.onnx`, `models/linear_model.onnx` — ONNX twins used by
  the Unity offline backend (`SentisPredictionBackend`). Copy them to
  `VRLab/Assets/StreamingAssets/Models/` after re-training.

## Endpoints

| Method | Path | Body | Description |
|---|---|---|---|
| GET | `/health` | — | Liveness probe; lists loaded models. |
| POST | `/predict/linear` | `{"features":[x,...], "horizon":1}` | Linear regression demo. |
| POST | `/predict/forecast` | `{"features":[...], "horizon":N}` | Demand forecast if 3 features `[price, marketing, seasonality]`; otherwise naive-drift forecast over a history window (stock prices). |

Unity's `PredictionClient` health-checks `/health`; if the service is down the
app automatically falls back to its local ONNX/closed-form backend and shows a
notice in the UI.

## Example

```powershell
curl -X POST http://127.0.0.1:8000/predict/forecast `
     -H "Content-Type: application/json" `
     -d '{"features":[25, 5000, 0.5], "horizon":4}'
```
