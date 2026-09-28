using System.Threading.Tasks;

namespace VRLab.ML
{
    /// <summary>
    /// Abstraction over prediction sources. Implementations:
    ///  - HttpPredictionBackend: Python FastAPI service (scikit-learn / TensorFlow)
    ///  - OnnxPredictionBackend: local inference (offline fallback)
    /// </summary>
    public interface IPredictionBackend
    {
        string Name { get; }
        Task<PredictionResult> PredictAsync(ModelId model, float[] features, int horizon = 1);
    }
}
