using UnityEngine;
using VRLab.Charts;
using VRLab.Interaction;
using VRLab.ML;

namespace VRLab.Scenarios
{
    /// <summary>
    /// Scenario 2: Business forecasting. Students adjust price, marketing spend,
    /// and seasonality sliders; the predicted demand updates in real time on a
    /// chart. Accurate guesses earn points/badges (gamification).
    /// </summary>
    public class ForecastingScenario : ScenarioBase
    {
        [Header("Sliders")]
        [SerializeField] private ParameterSlider priceSlider;      // e.g. 1..200
        [SerializeField] private ParameterSlider marketingSlider;  // e.g. 0..10000
        [SerializeField] private ParameterSlider seasonSlider;     // -1..1

        [Header("Display")]
        [SerializeField] private LineChart3D forecastChart;
        [SerializeField] private TMPro.TextMeshProUGUI demandReadout;
        [SerializeField] private TMPro.TextMeshProUGUI scoreReadout;

        [Header("Gamification")]
        [SerializeField] private float challengeTargetDemand = 400f;
        [SerializeField] private float accuracyTolerance = 0.05f; // 5%
        [SerializeField] private int challengeHorizon = 4;

        private readonly System.Collections.Generic.List<float> recentPredictions = new();
        private int score;
        private Debouncer debouncer;

        protected override void OnEnter()
        {
            debouncer = GetComponent<Debouncer>() ?? gameObject.AddComponent<Debouncer>();
            recentPredictions.Clear();
            score = 0;
            UpdateScoreReadout();

            priceSlider.OnValueChanged.AddListener(OnParameterChanged);
            marketingSlider.OnValueChanged.AddListener(OnParameterChanged);
            seasonSlider.OnValueChanged.AddListener(OnParameterChanged);

            OnParameterChanged(0f); // initial prediction with defaults
        }

        protected override void OnExit()
        {
            priceSlider.OnValueChanged.RemoveListener(OnParameterChanged);
            marketingSlider.OnValueChanged.RemoveListener(OnParameterChanged);
            seasonSlider.OnValueChanged.RemoveListener(OnParameterChanged);
        }

        private void OnParameterChanged(float value) =>
            debouncer.Schedule(async () => await RefreshPredictionAsync());

        private async System.Threading.Tasks.Task RefreshPredictionAsync()
        {
            if (PredictionClient.Instance == null) return;

            float[] features = { priceSlider.Value, marketingSlider.Value, seasonSlider.Value };
            var result = await PredictionClient.Instance.PredictAsync(ModelId.DemandForecast, features, challengeHorizon);
            if (!result.Success) return;

            recentPredictions.Clear();
            recentPredictions.AddRange(result.Predictions);

            if (forecastChart != null)
                foreach (var p in result.Predictions)
                    forecastChart.AppendLiveValue(p);

            if (demandReadout != null)
                demandReadout.text = $"Predicted demand: {result.Predictions[0]:F0} units  ({result.Backend})";

            CheckChallenge(result.Predictions[0]);
        }

        /// <summary>Badge logic: points when prediction lands within tolerance of the challenge target.</summary>
        private void CheckChallenge(float demand)
        {
            float error = Mathf.Abs(demand - challengeTargetDemand) / Mathf.Max(1f, challengeTargetDemand);
            if (error <= accuracyTolerance)
            {
                score += 10;
                HapticFeedback.PulseBothControllers(0.4f, 0.15f);
                UpdateScoreReadout();
            }
        }

        private void UpdateScoreReadout()
        {
            if (scoreReadout != null)
                scoreReadout.text = $"Score: {score}";
        }
    }
}
