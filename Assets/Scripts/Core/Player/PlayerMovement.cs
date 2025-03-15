using UnityEngine;
using KeyboardSim.Core.Input;

namespace KeyboardSim.Core.Player
{
    /// <summary>
    /// Handles player movement and camera controls.
    /// This class follows SRP by focusing only on movement mechanics.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base movement speed")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Running speed multiplier")]
        [SerializeField] private float runSpeedMultiplier = 1.5f;

        [Tooltip("Jump force")]
        [SerializeField] private float jumpForce = 5f;

        [Tooltip("Gravity multiplier")]
        [SerializeField] private float gravityMultiplier = 2.5f;

        [Header("Mouse Settings")]
        [Tooltip("Mouse sensitivity")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Tooltip("Maximum camera look angle on Y axis")]
        [SerializeField] private float maxLookAngle = 80f;

        // Private variables
        private CharacterController _controller;
        private Camera _playerCamera;
        private float _xRotation = 0f;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isRunning;
        private InputManager _inputManager;

        private void Start()
        {
            // Get required components
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                Debug.LogError("PlayerMovement requires a CharacterController component!");
                this.enabled = false;
                return;
            }

            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }

            // Lock cursor
            LockCursor(true);

            // Get InputManager reference
            _inputManager = InputManager.Instance;
            if (_inputManager == null)
            {
                Debug.LogError("InputManager not found in scene!");
                return;
            }

            // Subscribe to input events
            _inputManager.OnMovementInput += HandleMovementInput;
            _inputManager.OnMouseLookInput += HandleMouseLookInput;
            _inputManager.OnJumpInput += HandleJumpInput;
            _inputManager.OnRunInput += () => _isRunning = true;
            _inputManager.OnRunCancelInput += () => _isRunning = false;
            _inputManager.OnCursorToggleInput += ToggleCursor;
        }

        private void Update()
        {
            // Apply movement from stored input values
            ApplyMovement();
        }

        private void OnDestroy()
        {
            // Unsubscribe from input events
            if (_inputManager != null)
            {
                _inputManager.OnMovementInput -= HandleMovementInput;
                _inputManager.OnMouseLookInput -= HandleMouseLookInput;
                _inputManager.OnJumpInput -= HandleJumpInput;
                _inputManager.OnRunInput -= () => _isRunning = true;
                _inputManager.OnRunCancelInput -= () => _isRunning = false;
                _inputManager.OnCursorToggleInput -= ToggleCursor;
            }
        }

        private void HandleMouseLookInput(Vector2 mouseInput)
        {
            // Apply mouse sensitivity
            float mouseX = mouseInput.x * mouseSensitivity;
            float mouseY = mouseInput.y * mouseSensitivity;

            // Calculate rotation for X and Y axes
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

            // Rotate camera up and down (X rotation)
            _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            // Rotate character left and right (Y rotation)
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovementInput(Vector2 movementInput)
        {
            // Store input for use in ApplyMovement
            _isGrounded = _controller.isGrounded;
        }

        private void HandleJumpInput()
        {
            if (_isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            }
        }

        private void ApplyMovement()
        {
            // Check if grounded
            _isGrounded = _controller.isGrounded;

            // Reset vertical velocity when grounded
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small negative value works better than zero
            }

            // Get movement input from InputManager
            Vector2 movementInput = _inputManager.MovementInput;

            // Calculate movement direction
            Vector3 move = transform.right * movementInput.x + transform.forward * movementInput.y;

            // Normalize diagonal movement
            if (move.magnitude > 1f)
            {
                move.Normalize();
            }

            // Adjust speed based on run state
            float currentSpeed = _isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;

            // Move character
            _controller.Move(move * currentSpeed * Time.deltaTime);

            // Apply gravity
            _velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void ToggleCursor()
        {
            LockCursor(Cursor.lockState == CursorLockMode.None);
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Expose camera for other components to use
        public Camera GetPlayerCamera()
        {
            return _playerCamera;
        }
    }
} 