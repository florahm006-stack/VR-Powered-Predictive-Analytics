"""
Train the demo models used by the VR lab and save them to models/.

  - demand_model.joblib : GradientBoosting/linear model mapping
      [price, marketing_spend, seasonality] -> demand
      (trained on the synthetic sales dataset schema).
  - linear_model.joblib : simple LinearRegression for /predict/linear.

Also exports ONNX copies (models/*.onnx) used by the Unity offline
fallback (Unity Sentis) once wired in.

Run:  python train_model.py
"""
from __future__ import annotations

from pathlib import Path

import joblib
import numpy as np
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.linear_model import LinearRegression

MODELS_DIR = Path(__file__).parent / "models"


def make_demand_training_data(n: int = 2000, seed: int = 42) -> tuple[np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    price = rng.uniform(5, 200, n)
    marketing = rng.uniform(0, 10_000, n)
    seasonality = rng.uniform(-1, 1, n)
    # Ground truth mirrors the Unity fallback coefficients (with noise).
    demand = np.maximum(
        0,
        500 - 4.0 * price + 0.05 * marketing + 60.0 * seasonality + rng.normal(0, 12, n),
    )
    return np.column_stack([price, marketing, seasonality]), demand


def main() -> None:
    MODELS_DIR.mkdir(parents=True, exist_ok=True)

    # Demand forecasting model ------------------------------------------------
    X, y = make_demand_training_data()
    demand_model = GradientBoostingRegressor(random_state=42).fit(X, y)
    joblib.dump(demand_model, MODELS_DIR / "demand_model.joblib")
    print(f"demand_model R^2 on training data: {demand_model.score(X, y):.3f}")

    # Generic linear demo model ------------------------------------------------
    xl = rng_x = np.linspace(0, 100, 500)
    yl = 2.5 * xl + 7 + np.random.default_rng(1).normal(0, 5, xl.size)
    linear_model = LinearRegression().fit(xl.reshape(-1, 1), yl)
    joblib.dump(linear_model, MODELS_DIR / "linear_model.joblib")

    # ONNX exports (for Unity Sentis offline inference) ------------------------
    try:
        from skl2onnx import to_onnx

        to_onnx(demand_model, X[:1].astype(np.float32), target_opset=17).SerializeToString()
        (MODELS_DIR / "demand_model.onnx").write_bytes(
            to_onnx(demand_model, X[:1].astype(np.float32), target_opset=17).SerializeToString()
        )
        (MODELS_DIR / "linear_model.onnx").write_bytes(
            to_onnx(linear_model, xl[:1].reshape(-1, 1).astype(np.float32), target_opset=17).SerializeToString()
        )
        print("ONNX exports written.")
    except Exception as exc:  # ONNX export is optional
        print(f"(skipping ONNX export: {exc})")

    print(f"Models saved to {MODELS_DIR}")


if __name__ == "__main__":
    main()
