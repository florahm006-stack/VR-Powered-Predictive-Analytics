using UnityEngine;

namespace VRLab.UI
{
    /// <summary>Keeps labels facing the user's head/camera.</summary>
    public class Billboard : MonoBehaviour
    {
        private Transform cam;

        private void Start() =>
            cam = Camera.main != null ? Camera.main.transform : null;

        private void LateUpdate()
        {
            if (cam == null) { cam = Camera.main != null ? Camera.main.transform : null; return; }
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
        }
    }
}
