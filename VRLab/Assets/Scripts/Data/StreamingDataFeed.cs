using System;
using UnityEngine;

namespace VRLab.Data
{
    /// <summary>
    /// Replays a dataset series on a timer to simulate a "real-time" feed
    /// (e.g. a live stock ticker). Subscribers receive one value per tick.
    /// Loops by default so demos never run dry.
    /// </summary>
    public class StreamingDataFeed : MonoBehaviour
    {
        [SerializeField] private float ticksPerSecond = 2f;
        [SerializeField] private bool loop = true;

        public event Action<int, float> OnTick; // (index, value)

        private DataSeries _series;
        private int _index;
        private float _accum;

        public void SetSeries(DataSeries series, bool restart = true)
        {
            _series = series;
            if (restart) ResetFeed();
        }

        public void ResetFeed()
        {
            _index = 0;
            _accum = 0f;
        }

        private void Update()
        {
            if (_series == null || _series.Values.Count == 0) return;

            _accum += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.01f, ticksPerSecond);

            while (_accum >= interval)
            {
                _accum -= interval;

                if (_index >= _series.Values.Count)
                {
                    if (!loop) return;
                    _index = 0;
                }

                OnTick?.Invoke(_index, _series.Values[_index]);
                _index++;
            }
        }
    }
}
