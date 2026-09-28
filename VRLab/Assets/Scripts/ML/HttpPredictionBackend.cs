using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VRLab.ML
{
    /// <summary>Calls the Python FastAPI ML service over HTTP.</summary>
    public class HttpPredictionBackend : IPredictionBackend
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public string Name => "http";

        public HttpPredictionBackend(string baseUrl = "http://127.0.0.1:8000", int timeoutSeconds = 10)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<PredictionResult> PredictAsync(ModelId model, float[] features, int horizon = 1)
        {
            string url = baseUrl + model.ToEndpoint();
            var dto = new PredictRequestDto { features = features, horizon = horizon };
            string json = JsonUtility.ToJson(dto);

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;

            try
            {
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return PredictionResult.Fail($"{request.responseCode}: {request.error}", Name);

                var resp = JsonUtility.FromJson<PredictResponseDto>(request.downloadHandler.text);
                if (resp?.predictions == null || resp.predictions.Length == 0)
                    return PredictionResult.Fail("Empty prediction response.", Name);

                return PredictionResult.Ok(resp.predictions, Name);
            }
            catch (Exception e)
            {
                return PredictionResult.Fail(e.Message, Name);
            }
        }

        /// <summary>Quick connectivity probe (used by PredictionClient fallback logic).</summary>
        public async Task<bool> IsHealthyAsync()
        {
            using var request = UnityWebRequest.Get(baseUrl + "/health");
            request.timeout = 3;
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            return request.result == UnityWebRequest.Result.Success;
        }
    }
}
