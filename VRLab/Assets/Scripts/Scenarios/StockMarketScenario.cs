using System.Linq;
using UnityEngine;
using VRLab.Charts;
using VRLab.Data;
using VRLab.ML;

namespace VRLab.Scenarios
{
    /// <summary>
    /// Scenario 1: Stock market simulation. Streams a price series live into a
    /// line chart, shows volume bars, and overlays a next-N-step forecast.
    /// Students analyze trends and make predictions (scored for accuracy).
    /// </summary>
    public class StockMarketScenario : ScenarioBase
    {
        [Header("Data")]
        [SerializeField] private string csvFile = "Data/stocks.csv";
        [SerializeField] private string priceColumn = "Close";
        [SerializeField] private int forecastHorizon = 10;

        [Header("Scene references")]
        [SerializeField] private StreamingDataFeed feed;
        [SerializeField] private LineChart3D priceChart;
        [SerializeField] private BarChart3D volumeChart;
        [SerializeField] private LineChart3D forecastOverlay;

        private Dataset dataset;
        private float[] priceHistory = new float[32]; // rolling window for forecasts
        private int historyCount;

        protected override void OnEnter()
        {
            try { dataset = DatasetLoader.Load(csvFile); }
            catch (System.Exception e)
            {
                Debug.LogError($"[StockMarketScenario] {e.Message}");
                return;
            }

            volumeChart?.SetDataset(dataset);
            feed?.SetSeries(dataset.GetSeries(priceColumn));
            feed.OnTick += HandleTick;
        }

        protected override void OnExit()
        {
            feed.OnTick -= HandleTick;
        }

        private void HandleTick(int index, float value)
        {
            priceChart?.AppendLiveValue(value);

            priceHistory[historyCount % priceHistory.Length] = value;
            historyCount++;

            // Refresh the forecast every few ticks using the rolling window.
            if (historyCount % 5 == 0 && PredictionClient.Instance != null)
                _ = UpdateForecastAsync();
        }

        private async System.Threading.Tasks.Task UpdateForecastAsync()
        {
            int n = Mathf.Min(historyCount, priceHistory.Length);
            var window = new float[n];
            for (int i = 0; i < n; i++)
                window[i] = priceHistory[i]; // ordered enough for naive/ML models when window fills

            var result = await PredictionClient.Instance.PredictAsync(ModelId.StockForecast, window, forecastHorizon);
            if (!result.Success || result.Predictions.Length == 0) return;

            foreach (var p in result.Predictions)
                forecastOverlay?.AppendLiveValue(p);
        }
    }
}
