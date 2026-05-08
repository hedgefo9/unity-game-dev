using UnityEngine;

namespace ReactorTechnician
{
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private float distance = 7f;
        [SerializeField] private float height = 3.2f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = 18f;
        [SerializeField] private float maxPitch = 62f;
        [SerializeField] private float followSmoothTime = 0.05f;

        private Vector3 velocity;
        private float yaw;
        private float pitch = 38f;
        private bool cursorLocked = true;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            SetCursorLocked(true);
        }

        private void LateUpdate()
        {
            if (ReactorInput.CancelPressed)
            {
                SetCursorLocked(!cursorLocked);
            }

            if (cursorLocked)
            {
                Vector2 look = ReactorInput.LookDelta;
                yaw += look.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, minPitch, maxPitch);
            }

            if (target == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + targetOffset;
            Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance + Vector3.up * height;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);
            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
