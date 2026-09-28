using UnityEngine;
using VRLab.Data;

namespace VRLab.Charts
{
    /// <summary>
    /// 3D scatter plot: maps three dataset columns to X/Y/Z.
    /// </summary>
    public class ScatterPlot3D : Chart3D
    {
        [Header("Axis columns")]
        [SerializeField] private string xColumn = "Open";
        [SerializeField] private string yColumn = "Close";
        [SerializeField] private string zColumn = "Volume";
        [SerializeField] private float pointScale = 0.015f;

        protected override void BuildPoints()
        {
            if (dataset == null) return;
            var sx = dataset.GetSeries(xColumn);
            var sy = dataset.GetSeries(yColumn);
            var sz = dataset.GetSeries(zColumn);
            if (sx == null || sy == null || sz == null) return;

            int count = Mathf.Min(sx.Values.Count, sy.Values.Count, sz.Values.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 local = new Vector3(
                    Mathf.Lerp(-size.x / 2, size.x / 2, Normalize(sx.Values[i], sx.Min, sx.Max)),
                    Mathf.Lerp(0, size.y, Normalize(sy.Values[i], sy.Min, sy.Max)),
                    Mathf.Lerp(-size.z / 2, size.z / 2, Normalize(sz.Values[i], sz.Min, sz.Max)));
                AddPoint(local, pointScale);
            }
        }
    }
}
