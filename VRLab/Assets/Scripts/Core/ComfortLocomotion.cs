using UnityEngine;

namespace VRLab.Core
{
    /// <summary>
    /// Comfort options: toggles between teleport (default) and smooth locomotion,
    /// and drives a vignette during smooth motion to reduce motion sickness.
    /// Attach to the XR Origin. Works with XRI's TeleportationProvider /
    /// ContinuousMoveProvider component references.
    /// </summary>
    public class ComfortLocomotion : MonoBehaviour
    {
        public enum LocomotionMode { Teleport, Smooth }

        [Header("Providers (XRI components on the rig)")]
        [SerializeField] private MonoBehaviour teleportProvider;      // TeleportationProvider
        [SerializeField] private MonoBehaviour continuousMoveProvider; // ContinuousMoveProvider

        [Header("Vignette (optional)")]
        [SerializeField] private GameObject vignette;
        [SerializeField] private float vignetteSpeedThreshold = 0.1f;

        public LocomotionMode Mode { get; private set; } = LocomotionMode.Teleport;

        private void Start() => ApplyMode();

        public void SetMode(LocomotionMode mode)
        {
            Mode = mode;
            ApplyMode();
        }

        public void ToggleMode() =>
            SetMode(Mode == LocomotionMode.Teleport ? LocomotionMode.Smooth : LocomotionMode.Teleport);

        private void ApplyMode()
        {
            if (teleportProvider != null)
                teleportProvider.enabled = Mode == LocomotionMode.Teleport;
            if (continuousMoveProvider != null)
                continuousMoveProvider.enabled = Mode == LocomotionMode.Smooth;
        }

        private void Update()
        {
            if (vignette == null || Mode != LocomotionMode.Smooth)
            {
                if (vignette != null && vignette.activeSelf) vignette.SetActive(false);
                return;
            }

            // Show vignette while actually moving.
            var cc = GetComponent<CharacterController>();
            float speed = cc != null ? cc.velocity.magnitude : 0f;
            bool show = speed > vignetteSpeedThreshold;
            if (vignette.activeSelf != show) vignette.SetActive(show);
        }
    }
}
