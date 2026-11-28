using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.0f;
        [SerializeField] private float rotationSpeed = 10.0f;
        [SerializeField] private float jumpPower = 5.0f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Control Mode")]
        [SerializeField] private bool isFirstPerson = false;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float lookXLimit = 80.0f;

        private CharacterController characterController;
        private Vector3 velocity;
        private Camera mainCamera;
        private Vector3 spawnPosition;
        private float rotationX = 0f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            spawnPosition = transform.position;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }

            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                Destroy(cap);
            }

            if (characterController != null)
            {
                characterController.center = new Vector3(0, 1.0f, 0);
                characterController.height = 2.0f;
                characterController.radius = 0.5f;
                characterController.stepOffset = 0.3f;
                characterController.slopeLimit = 45f;
            }

            if (!isFirstPerson && GetComponentInChildren<Camera>() != null)
            {
                isFirstPerson = true;
            }

            if (isFirstPerson && cameraTransform == null)
            {
                Camera childCam = GetComponentInChildren<Camera>();
                if (childCam != null)
                {
                    cameraTransform = childCam.transform;
                }
                else if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                }
            }
        }

        private void Start()
        {
            if (isFirstPerson)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
            if (isFirstPerson)
            {
                UpdateCameraHeight();
                HandleMouseLook();
                HandleCursorToggle();
                HandleFirstPersonMovement();
            }
            else
            {
                HandleThirdPersonMovement();
            }

            CheckVoidFall();
        }

        private void UpdateCameraHeight()
        {
            if (cameraTransform != null)
            {
                if (Mathf.Abs(cameraTransform.localPosition.y - eyeHeight) > 0.001f)
                {
                    cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, eyeHeight, cameraTransform.localPosition.z);
                }
            }
        }

        private void CheckVoidFall()
        {
            float voidLimit = isFirstPerson ? -20f : -4f;
            if (transform.position.y < voidLimit)
            {
                characterController.enabled = false;
                GameObject cp = GameObject.Find("Checkpoint");
                if (cp != null)
                {
                    transform.position = cp.transform.position + Vector3.up * 1.5f;
                }
                else
                {
                    transform.position = spawnPosition + Vector3.up * 1.5f;
                }
                ResetVelocity();
                characterController.enabled = true;
                Debug.LogWarning("[PlayerController] Karakter jatuh ke void! Otomatis dikembalikan ke atas lantai.");
            }
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = 0f;
            float mouseY = 0f;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * 0.1f);
                mouseX = delta.x;
                mouseY = delta.y;
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            }

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            }

            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleFirstPersonMovement()
        {
            float moveX = 0f;
            float moveZ = 0f;
            bool isSprinting = false;
            bool jumpPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;

                if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) isSprinting = true;
                if (keyboard.spaceKey.wasPressedThisFrame) jumpPressed = true;
            }
            else
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) isSprinting = true;
                if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
            }

            bool hasMovementInput = (Mathf.Abs(moveX) > 0.05f || Mathf.Abs(moveZ) > 0.05f);

            if (hasMovementInput)
            {
                Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
                float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
                characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));
            }

            if (characterController.isGrounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y = -0.5f;
                }

                if (jumpPressed)
                {
                    velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
                }
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            if (!characterController.isGrounded || velocity.y > 0)
            {
                characterController.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
            }
        }

        private void HandleThirdPersonMovement()
        {
            float moveX = 0f;
            float moveZ = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            }
            else
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;
            }

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

        private void HandleCursorToggle()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            bool anyClick = (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

            if (anyClick && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
