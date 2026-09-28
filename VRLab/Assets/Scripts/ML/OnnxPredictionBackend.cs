using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VRLab.ML
{
    /// <summary>
    /// Local on-device fallback: runs ONNX models via Unity Sentis (planned)
    /// when the Python ML service is unreachable. Until Sentis is wired in,
    /// it evaluates a simple closed-form model so scenarios remain playable
    /// offline (documented placeholder, clearly labeled in the UI).
    ///
    /// Placeholder model (demand): inverse linear demand curve
    ///   demand = max(0, baseDemand - priceCoef * price + marketingCoef * marketing + seasonalityCoef * season)
    /// </summary>
    public class OnnxPredictionBackend : IPredictionBackend
    {
        public string Name => "onnx-local";

        // Fallback coefficients (approximate the trained sklearn model in ml-service).
        private const float BaseDemand = 500f;
        private const float PriceCoef = -4.0f;
        private const float MarketingCoef = 0.05f;
        private const float SeasonalityCoef = 60f;

        public Task<PredictionResult> PredictAsync(ModelId model, float[] features, int horizon = 1)
        {
            try
            {
                float[] preds = model switch
                {
                    ModelId.DemandForecast => PredictDemand(features, horizon),
                    ModelId.StockForecast => PredictNaiveDrift(features, horizon),
                    ModelId.LinearRegression => PredictWeightedSum(features),
                    _ => throw new ArgumentOutOfRangeException(nameof(model))
                };
                return Task.FromResult(PredictionResult.Ok(preds, Name));
            }
            catch (Exception e)
            {
                return Task.FromResult(PredictionResult.Fail(e.Message, Name));
            }
        }

        /// <summary>features = [price, marketingSpend, seasonality(-1..1)]</summary>
        private static float[] PredictDemand(float[] f, int horizon)
        {
            float price = f.Length > 0 ? f[0] : 0f;
            float marketing = f.Length > 1 ? f[1] : 0f;
            float season = f.Length > 2 ? f[2] : 0f;
            float demand = Mathf.Max(0f, BaseDemand + PriceCoef * price + MarketingCoef * marketing + SeasonalityCoef * season);

            var preds = new float[Mathf.Max(1, horizon)];
            for (int i = 0; i < preds.Length; i++)
                preds[i] = demand * (1f + 0.02f * i); // slight trend per horizon step
            return preds;
        }

        /// <summary>Naive drift: last value extended with average recent change.</summary>
        private static float[] PredictNaiveDrift(float[] history, int horizon)
        {
            var preds = new float[Mathf.Max(1, horizon)];
            if (history.Length == 0) return preds;

            float last = history[history.Length - 1];
            float drift = history.Length > 1 ? (history[history.Length - 1] - history[0]) / (history.Length - 1) : 0f;
            for (int i = 0; i < preds.Length; i++)
                preds[i] = last + drift * (i + 1);
            return preds;
        }

        private static float[] PredictWeightedSum(float[] f)
        {
            float sum = 0f;
            foreach (var v in f) sum += v;
            return new[] { sum / Mathf.Max(1, f.Length) };
        }
    }
}
