using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLab.Data
{
    /// <summary>A named series of float values with optional timestamps.</summary>
    [Serializable]
    public class DataSeries
    {
        public string Name;
        public List<float> Values = new List<float>();
        public List<DateTime> Timestamps = new List<DateTime>(); // optional, same length as Values or empty

        public float Min { get; private set; }
        public float Max { get; private set; }

        public void RecalculateBounds()
        {
            if (Values.Count == 0) { Min = 0; Max = 0; return; }
            float min = float.MaxValue, max = float.MinValue;
            foreach (var raw in Values)
            {
                float v = Sanitize(raw, min);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Min = min; Max = max;
        }

        /// <summary>Clamp NaN/Infinity to a safe value so chart scales never break.</summary>
        public static float Sanitize(float value, float fallback = 0f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return value;
        }
    }

    /// <summary>A dataset = named columns of floats, e.g. parsed from CSV.</summary>
    public class Dataset
    {
        public string Name;
        public List<DataSeries> Series = new List<DataSeries>();

        public int RowCount => Series.Count > 0 ? Series[0].Values.Count : 0;

        public DataSeries GetSeries(string name)
        {
            foreach (var s in Series)
                if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }
    }
}
