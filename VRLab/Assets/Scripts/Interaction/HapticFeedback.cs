using UnityEngine;
using UnityEngine.XR;

namespace VRLab.Interaction
{
    /// <summary>Simple haptic pulse helper for XR controllers (safe no-op on PC).</summary>
    public static class HapticFeedback
    {
        public static void Pulse(XRNode node, float amplitude = 0.5f, float durationSeconds = 0.1f)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, durationSeconds));
        }

        public static void PulseBothControllers(float amplitude = 0.5f, float durationSeconds = 0.1f)
        {
            Pulse(XRNode.LeftHand, amplitude, durationSeconds);
            Pulse(XRNode.RightHand, amplitude, durationSeconds);
        }
    }
}
