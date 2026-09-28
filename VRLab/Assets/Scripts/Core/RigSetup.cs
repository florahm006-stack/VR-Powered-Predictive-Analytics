using UnityEngine;
using UnityEngine.XR.Management;

namespace VRLab.Core
{
    /// <summary>
    /// Detects whether an XR headset is active and toggles between the XR rig
    /// (XR Origin + controllers) and the PC fallback rig (FPS camera + mouse/keyboard).
    /// The same scenes and interactions work in both modes.
    /// </summary>
    public class RigSetup : MonoBehaviour
    {
        public static RigSetup Instance { get; private set; }

        [Header("Rigs (assign in scene)")]
        [SerializeField] private GameObject xrRig;
        [SerializeField] private GameObject pcFallbackRig;

        public bool IsXRActive { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            IsXRActive = DetectXR();
            ApplyRigMode();
        }

        private static bool DetectXR()
        {
            var settings = XRGeneralSettings.Instance;
            if (settings == null || settings.Manager == null)
                return false;

            var loader = settings.Manager.activeLoader;
            return loader != null;
        }

        private void ApplyRigMode()
        {
            if (xrRig != null) xrRig.SetActive(IsXRActive);
            if (pcFallbackRig != null) pcFallbackRig.SetActive(!IsXRActive);
            Debug.Log($"[RigSetup] Mode: {(IsXRActive ? "XR headset" : "PC fallback")}");
        }
    }
}
