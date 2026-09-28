using UnityEngine;
using VRLab.Data;

namespace VRLab.Charts
{
    /// <summary>
    /// Bar chart: one bar per row of a named series (e.g. trade volume per day).
    /// </summary>
    public class BarChart3D : Chart3D
    {
        [SerializeField] private string seriesName = "Volume";

        protected override void BuildPoints()
        {
            var series = dataset?.GetSeries(seriesName);
            if (series == null || series.Values.Count == 0) return;

            int count = series.Values.Count;
            float barWidth = size.x / count;

            for (int i = 0; i < count; i++)
            {
                float x = -size.x / 2 + barWidth * (i + 0.5f);
                float h = Mathf.Lerp(0.01f, size.y, Normalize(series.Values[i], 0, series.Max));
                // Represent each bar as a stretched cube: center at half height.
                if (pointCount >= maxPoints) break;
                matrices[pointCount++] = Matrix4x4.TRS(
                    transform.TransformPoint(new Vector3(x, h / 2f, 0)),
                    transform.rotation,
                    new Vector3(barWidth * 0.8f * transform.lossyScale.x,
                                h * transform.lossyScale.y,
                                size.z * 0.2f * transform.lossyScale.z));
            }
        }
    }
}
