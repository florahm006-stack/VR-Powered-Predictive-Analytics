using System;

namespace VRLab.ML
{
    public enum ModelId
    {
        LinearRegression = 0,   // generic feature -> value regression
        DemandForecast = 1,     // business forecasting (price, marketing, seasonality -> demand)
        StockForecast = 2       // next-N closing price forecast
    }

    /// <summary>Maps to the Python service endpoint path.</summary>
    public static class ModelIdExtensions
    {
        public static string ToEndpoint(this ModelId id) => id switch
        {
            ModelId.LinearRegression => "/predict/linear",
            ModelId.DemandForecast => "/predict/forecast",
            ModelId.StockForecast => "/predict/forecast",
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
    }

    [Serializable]
    public class PredictionRequest
    {
        public string model;
        public float[] features;
        public int horizon = 1; // number of future steps for forecast models
    }

    [Serializable]
    public class PredictionResult
    {
        public bool Success;
        public float[] Predictions = Array.Empty<float>();
        public string Backend;   // "http" or "onnx"
        public string Error;

        public static PredictionResult Fail(string error, string backend) =>
            new PredictionResult { Success = false, Error = error, Backend = backend };

        public static PredictionResult Ok(float[] predictions, string backend) =>
            new PredictionResult { Success = true, Predictions = predictions, Backend = backend };
    }

    // JSON DTOs matching the FastAPI response schema.
    [Serializable]
    internal class PredictResponseDto
    {
        public float[] predictions;
    }

    [Serializable]
    internal class PredictRequestDto
    {
        public float[] features;
        public int horizon;
    }
}
