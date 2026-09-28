using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VRLab.ML
{
    /// <summary>
    /// Facade used by scenarios/UI. Tries the HTTP ML service first; if unhealthy,
    /// silently degrades to the local ONNX/closed-form fallback and raises
    /// OnBackendChanged so UI can show a notice.
    /// Debounce-friendly: safe to call every slider change.
    /// </summary>
    public class PredictionClient : MonoBehaviour
    {
        public static PredictionClient Instance { get; private set; }

        [Header("Service")]
        [SerializeField] private string serviceUrl = "http://127.0.0.1:8000";
        [SerializeField] private float healthCheckIntervalSeconds = 30f;

        public event Action<string> OnBackendChanged; // "http" | "onnx-local"

        private HttpPredictionBackend http;
        private SentisPredictionBackend sentis;   // true on-device ONNX models
        private OnnxPredictionBackend local;      // closed-form coefficients (last resort)
        private IPredictionBackend active;
        private float nextHealthCheck;

        public string ActiveBackendName => active?.Name ?? "none";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            http = new HttpPredictionBackend(serviceUrl);
            sentis = new SentisPredictionBackend();
            local = new OnnxPredictionBackend();
            active = local; // pessimistic default until first health check

            // Prefer real on-device ONNX models over closed-form when available.
            if (!System.IO.File.Exists(
                    System.IO.Path.Combine(Application.streamingAssetsPath, "Models", "demand_model.onnx")))
                sentis = null;
        }

        private async void Start() => await RefreshBackendAsync();

        public async Task<PredictionResult> PredictAsync(ModelId model, float[] features, int horizon = 1)
        {
            if (Time.time >= nextHealthCheck)
                await RefreshBackendAsync();

            var result = await active.PredictAsync(model, features, horizon);

            // If HTTP failed mid-session, try failing over once.
            if (!result.Success && active == http)
            {
                await RefreshBackendAsync();
                if (active != http)
                    result = await active.PredictAsync(model, features, horizon);
            }
            return result;
        }

        private async Task RefreshBackendAsync()
        {
            nextHealthCheck = Time.time + healthCheckIntervalSeconds;
            bool healthy = await http.IsHealthyAsync();
            // Priority: online service > on-device ONNX (Sentis) > closed-form fallback.
            var chosen = healthy ? (IPredictionBackend)http : (sentis ?? (IPredictionBackend)local);
            if (chosen != active)
            {
                active = chosen;
                OnBackendChanged?.Invoke(active.Name);
                Debug.Log($"[PredictionClient] Backend switched to: {active.Name}");
            }
        }
    }
}
