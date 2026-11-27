using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotationSpeed = 10.0f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController characterController;
        private Vector3 velocity;
        private Camera mainCamera;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            ResetVelocity();
        }

        public void ResetVelocity()
        {
            velocity = Vector3.zero;
        }

        private void Update()
        {
            HandleMovement();

            if (transform.position.y < -4f)
            {
                characterController.enabled = false;
                GameObject cp = GameObject.Find("Checkpoint");
                if (cp != null)
                {
                    transform.position = cp.transform.position + Vector3.up * 1.5f;
                }
                else
                {
                    transform.position = new Vector3(0, 1.5f, 0);
                }
                ResetVelocity();
                characterController.enabled = true;
                Debug.LogWarning("[PlayerController] Karakter jatuh ke void! Otomatis dikembalikan ke atas lantai.");
            }
        }

        private void HandleMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float moveX = 0f;
            float moveZ = 0f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;

            Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                Vector3 moveDir = inputDirection;
                if (mainCamera != null)
                {
                    Vector3 camForward = mainCamera.transform.forward;
                    Vector3 camRight = mainCamera.transform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    moveDir = (camForward * inputDirection.z + camRight * inputDirection.x).normalized;
                }

                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                characterController.Move(moveDir * (moveSpeed * Time.deltaTime));
            }

            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
    }
}
