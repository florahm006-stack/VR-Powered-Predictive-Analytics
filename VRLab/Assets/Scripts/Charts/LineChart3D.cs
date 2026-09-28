using UnityEngine;
using VRLab.Data;

namespace VRLab.Charts
{
    /// <summary>
    /// Time-series line chart: plots a named series across X (index), Y (value).
    /// Supports live appending for streaming feeds (stock ticker).
    /// </summary>
    public class LineChart3D : Chart3D
    {
        [Header("Series")]
        [SerializeField] private string seriesName = "Close";
        [SerializeField] private float pointScale = 0.02f;
        [SerializeField] private float lineConnectorScale = 0.01f;

        [Header("Live mode")]
        [SerializeField] private int windowSize = 120;
        private readonly System.Collections.Generic.Queue<float> liveWindow = new();

        public void AppendLiveValue(float value)
        {
            liveWindow.Enqueue(DataSeries.Sanitize(value));
            while (liveWindow.Count > windowSize) liveWindow.Dequeue();
            dataset = null; // render from live window
            Rebuild();
        }

        protected override void BuildPoints()
        {
            Vector3 prev = default;
            bool hasPrev = false;
            int i = 0;

            System.Action<Vector3> addWithConnector = p =>
            {
                AddPoint(p, pointScale);
                if (hasPrev)
                {
                    Vector3 mid = (p + prev) * 0.5f;
                    // Connector cube stretched along the segment (approximation using vertical stack).
                    float dist = Vector3.Distance(p, prev);
                    AddPoint(mid, Mathf.Max(lineConnectorScale, dist * 0.5f));
                }
                prev = p; hasPrev = true;
            };

            if (liveWindow.Count > 0)
            {
                int n = liveWindow.Count;
                float min = float.MaxValue, max = float.MinValue;
                foreach (var v in liveWindow) { if (v < min) min = v; if (v > max) max = v; }

                foreach (var v in liveWindow)
                {
                    float x = Mathf.Lerp(-size.x / 2, size.x / 2, n < 2 ? 0.5f : (float)i / (n - 1));
                    float y = Mathf.Lerp(0, size.y, Normalize(v, min, max));
                    addWithConnector(new Vector3(x, y, 0));
                    i++;
                }
                return;
            }

            var series = dataset?.GetSeries(seriesName);
            if (series == null || series.Values.Count == 0) return;

            int count = series.Values.Count;
            for (i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-size.x / 2, size.x / 2, count < 2 ? 0.5f : (float)i / (count - 1));
                float y = Mathf.Lerp(0, size.y, Normalize(series.Values[i], series.Min, series.Max));
                addWithConnector(new Vector3(x, y, 0));
            }
        }
    }
}
