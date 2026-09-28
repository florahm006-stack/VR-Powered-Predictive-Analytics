using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRLab.Interaction
{
    /// <summary>
    /// VR drag for ParameterSlider: makes the handle an XR grab interactable and
    /// maps the handle's local X position onto the slider value while grabbed.
    /// PC fallback keeps using mouse-scroll (ParameterSlider.OnMouseOver).
    /// </summary>
    [RequireComponent(typeof(ParameterSlider))]
    public class SliderGrabDriver : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable grab;      // on the handle
        [SerializeField] private float handleTravel = 0.5f;    // must match ParameterSlider

        private Transform handleRoot; // grab attaches here; we move a pivot instead
        private ParameterSlider slider;
        private Transform pivot;
        private bool grabbing;
        private Transform interactorAttach;

        private void Awake()
        {
            slider = GetComponent<ParameterSlider>();
        }

        private void OnEnable()
        {
            if (grab == null) return;
            grab.selectEntered.AddListener(OnGrabStart);
            grab.selectExited.AddListener(OnGrabEnd);
        }

        private void OnDisable()
        {
            if (grab == null) return;
            grab.selectEntered.RemoveListener(OnGrabStart);
            grab.selectExited.RemoveListener(OnGrabEnd);
        }

        private void OnGrabStart(SelectEnterEventArgs args)
        {
            grabbing = true;
            HapticFeedback.Pulse(UnityEngine.XR.XRNode.RightHand, 0.2f, 0.05f);
        }

        private void OnGrabEnd(SelectExitEventArgs args) => grabbing = false;

        private void Update()
        {
            if (!grabbing || grab == null) return;

            // The grab moves the interactable; read its local X relative to the track.
            float localX = grab.transform.localPosition.x;
            float t = Mathf.InverseLerp(-handleTravel / 2f, handleTravel / 2f, localX);
            slider.SetFromNormalizedPosition(t);
        }
    }
}
