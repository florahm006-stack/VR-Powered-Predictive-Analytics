using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Unity.InferenceEngine;

namespace VRLab.ML
{
    /// <summary>
    /// On-device ONNX inference via Unity Sentis. Loads demand_model.onnx /
    /// linear_model.onnx from StreamingAssets/Models (exported by
    /// ml-service/train_model.py). Falls back gracefully (returns Failure) if a
    /// model file is missing, letting PredictionClient degrade to closed-form.
    /// </summary>
    public class SentisPredictionBackend : IPredictionBackend, IDisposable
    {
        public string Name => "sentis-onnx";

        private Model demandModel;
        private Model linearModel;

        private static Model TryLoad(string fileName)
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Models", fileName);
                if (!File.Exists(path)) return null;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                return ModelLoader.Load(stream);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Sentis] Failed to load {fileName}: {e.Message}");
                return null;
            }
        }

        public async Task<PredictionResult> PredictAsync(ModelId model, float[] features, int horizon = 1)
        {
            try
            {
                switch (model)
                {
                    case ModelId.DemandForecast:
                        demandModel ??= TryLoad("demand_model.onnx");
                        if (demandModel == null)
                            return PredictionResult.Fail("demand_model.onnx missing", Name);
                        float baseDemand = await RunAsync(demandModel, features);
                        var preds = new float[Mathf.Max(1, horizon)];
                        for (int i = 0; i < preds.Length; i++)
                            preds[i] = Mathf.Max(0f, baseDemand * (1f + 0.02f * i));
                        return PredictionResult.Ok(preds, Name);

                    case ModelId.LinearRegression:
                        linearModel ??= TryLoad("linear_model.onnx");
                        if (linearModel == null)
                            return PredictionResult.Fail("linear_model.onnx missing", Name);
                        float v = await RunAsync(linearModel, features);
                        return PredictionResult.Ok(new[] { v }, Name);

                    default:
                        return PredictionResult.Fail($"Model {model} has no ONNX export", Name);
                }
            }
            catch (Exception e)
            {
                return PredictionResult.Fail(e.Message, Name);
            }
        }

        private static Task<float> RunAsync(Model model, float[] features)
        {
            using var worker = new Worker(model, BackendType.CPU);
            using var input = new Tensor<float>(new TensorShape(1, features.Length), features);
            worker.Schedule(input);
            var output = worker.PeekOutput() as Tensor<float>;       // do not dispose peeked tensors
            using var cpu = output.ReadbackAndClone();
            var data = cpu.DownloadToArray();
            return Task.FromResult(data.Length > 0 ? data[0] : 0f);
        }

        public void Dispose()
        {
            demandModel = null; // Models are ScriptableObject-free data; workers are disposed per-call.
            linearModel = null;
        }
    }
}
