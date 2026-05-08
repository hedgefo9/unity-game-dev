using UnityEngine;

namespace ReactorTechnician
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -22f;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            Vector2 input = ReactorInput.Move;
            Vector3 move = GetCameraRelativeMove(input);

            float speed = ReactorInput.SprintHeld ? sprintSpeed : walkSpeed;
            Vector3 horizontalVelocity = move * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1.5f;
            }

            if (characterController.isGrounded && ReactorInput.JumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private Vector3 GetCameraRelativeMove(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = cameraTransform == null ? Vector3.forward : cameraTransform.forward;
            Vector3 right = cameraTransform == null ? Vector3.right : cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
