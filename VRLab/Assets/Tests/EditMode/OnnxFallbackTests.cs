using System.Threading.Tasks;
using NUnit.Framework;
using VRLab.ML;

namespace VRLab.Tests.EditMode
{
    /// <summary>
    /// Validates the offline fallback math so scenarios stay sensible when the
    /// ML service is offline. The demand model mirrors train_model.py's ground
    /// truth coefficients (500 - 4*price + 0.05*marketing + 60*seasonality).
    /// </summary>
    public class OnnxFallbackTests
    {
        private OnnxPredictionBackend backend;

        [SetUp] public void Setup() => backend = new OnnxPredictionBackend();

        [Test]
        public async Task DemandForecast_MatchesGroundTruth()
        {
            // price=25, marketing=5000, season=0.5 -> 500 -100 +250 +30 = 680
            var r = await backend.PredictAsync(ModelId.DemandForecast, new float[] { 25f, 5000f, 0.5f }, 1);
            Assert.IsTrue(r.Success);
            Assert.AreEqual(680f, r.Predictions[0], 1e-3f);
        }

        [Test]
        public async Task DemandForecast_NeverNegative()
        {
            var r = await backend.PredictAsync(ModelId.DemandForecast, new float[] { 500f, 0f, -1f }, 1);
            Assert.GreaterOrEqual(r.Predictions[0], 0f);
        }

        [Test]
        public async Task StockForecast_NaiveDrift_IsLinear()
        {
            // history 10,20,30 -> drift 10 -> next steps 40,50,60
            var r = await backend.PredictAsync(ModelId.StockForecast, new float[] { 10f, 20f, 30f }, 3);
            Assert.AreEqual(new[] { 40f, 50f, 60f }, r.Predictions);
        }
    }
}
