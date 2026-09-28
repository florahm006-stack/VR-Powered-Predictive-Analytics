using UnityEngine;

namespace VRLab.Core
{
    /// <summary>
    /// Simple first-person keyboard/mouse controller for the PC fallback rig.
    /// Used automatically when no XR headset is detected, satisfying the
    /// multi-device accessibility requirement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PCFallbackController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float lookSensitivity = 2.0f;

        private CharacterController _cc;
        private float _pitch;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float mx = Input.GetAxis("Mouse X") * lookSensitivity;
            float my = Input.GetAxis("Mouse Y") * lookSensitivity;
            transform.Rotate(0f, mx, 0f);
            _pitch = Mathf.Clamp(_pitch - my, -80f, 80f);
            if (Camera.main != null)
                Camera.main.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Vector3 move = transform.TransformDirection(input) * moveSpeed;
            _cc.SimpleMove(move);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
