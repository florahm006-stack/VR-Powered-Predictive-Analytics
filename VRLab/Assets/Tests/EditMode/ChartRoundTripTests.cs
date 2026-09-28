using NUnit.Framework;
using UnityEngine;
using VRLab.Charts;
using VRLab.Data;
using VRLab.ML;

namespace VRLab.Tests.EditMode
{
    /// <summary>Chart rebuild + offline prediction round-trip (edit-mode friendly).</summary>
    public class ChartRoundTripTests
    {
        private const string Csv =
            "Date,Close,Volume\n2025-01-01,100,900000\n2025-01-02,105,850000\n2025-01-03,103,870000\n";

        private static LineChart3D MakeChart()
        {
            var proto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = proto.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(proto);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var go = new GameObject("chart");
            var chart = go.AddComponent<LineChart3D>();
            chart.Configure(mesh, new Material(shader) { enableInstancing = true });
            return chart;
        }

        [Test]
        public void LineChart_RendersSeriesFromDataset()
        {
            var ds = DatasetLoader.ParseText("test", Csv);
            var chart = MakeChart();
            try
            {
                chart.SetDataset(ds);
                Assert.Greater(chart.PointCount, 0, "Line chart must draw points for a non-empty dataset.");
            }
            finally { Object.DestroyImmediate(chart.gameObject); }
        }

        [Test]
        public void LineChart_LiveAppend_GrowsCount()
        {
            var chart = MakeChart();
            try
            {
                chart.AppendLiveValue(10f);
                int first = chart.PointCount;
                chart.AppendLiveValue(12f);
                Assert.Greater(chart.PointCount, first, "Live-append must add points.");
            }
            finally { Object.DestroyImmediate(chart.gameObject); }
        }

        [Test]
        public void Prediction_OfflineBackend_RoundTrips()
        {
            var task = new OnnxPredictionBackend().PredictAsync(
                ModelId.DemandForecast, new float[] { 25f, 5000f, 0.5f }, 4);
            Assert.IsTrue(task.Result.Success);
            Assert.AreEqual(4, task.Result.Predictions.Length);
            Assert.AreEqual(680f, task.Result.Predictions[0], 1e-3f);
        }
    }
}
