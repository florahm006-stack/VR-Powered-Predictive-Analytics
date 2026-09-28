using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRLab.Interaction
{
    /// <summary>
    /// A world-space parameter slider for VR: drag the handle along a local axis
    /// (grabbed via XRI interactable) or adjust with mouse scroll in PC fallback.
    /// Emits ValueChanged (debounce at the consumer level — see Debouncer).
    /// </summary>
    public class ParameterSlider : MonoBehaviour
    {
        [Header("Range")]
        [SerializeField] private float min = 0f;
        [SerializeField] private float max = 100f;
        [SerializeField] private float value = 50f;
        [SerializeField] private string label = "Parameter";

        [Header("Visuals")]
        [SerializeField] private Transform handle;
        [SerializeField] private float handleTravel = 0.5f; // meters along local X

        [Serializable] public class ValueChangedEvent : UnityEvent<float> { }
        public ValueChangedEvent OnValueChanged = new ValueChangedEvent();

        public float Min => min;
        public float Max => max;
        public string Label => label;

        public float Value
        {
            get => value;
            set
            {
                float v = Mathf.Clamp(value, min, max);
                if (Mathf.Approximately(v, this.value)) return;
                this.value = v;
                UpdateHandleVisual();
                OnValueChanged.Invoke(v);
            }
        }

        /// <summary>Normalized [0,1] position of the value.</summary>
        public float Normalized => Mathf.InverseLerp(min, max, value);

        private void Start() => UpdateHandleVisual();

        /// <summary>Set from a normalized grab position (0..1 along the track).</summary>
        public void SetFromNormalizedPosition(float t) => Value = Mathf.Lerp(min, max, Mathf.Clamp01(t));

        private void UpdateHandleVisual()
        {
            if (handle == null) return;
            handle.localPosition = new Vector3(Mathf.Lerp(-handleTravel / 2f, handleTravel / 2f, Normalized), 0f, 0f);
        }

        // PC fallback: scroll wheel while hovering (simple raycast uGUI-free approach).
        private void OnMouseOver()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
                Value = value + scroll * (max - min) * 0.1f;
        }
    }
}
