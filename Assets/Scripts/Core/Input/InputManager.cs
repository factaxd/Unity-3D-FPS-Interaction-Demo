using UnityEngine;
using System;

namespace KeyboardSim.Core.Input
{
    /// <summary>
    /// Manages player input for the game.
    /// This class follows the Single Responsibility Principle by handling only input detection.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // Singleton instance
        private static InputManager _instance;
        public static InputManager Instance => _instance;

        // Input action events
        public event Action<Vector2> OnMovementInput;
        public event Action<Vector2> OnMouseLookInput;
        public event Action OnJumpInput;
        public event Action OnInteractInput;
        public event Action OnDropInput;
        public event Action OnRunInput;
        public event Action OnRunCancelInput;
        public event Action OnRotateObjectInput;
        public event Action OnRotateObjectCancelInput;
        public event Action OnCursorToggleInput;

        // Input state properties
        public Vector2 MovementInput { get; private set; }
        public Vector2 MouseInput { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsRotatingObject { get; private set; }

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Get movement input
            Vector2 movementInput = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
            if (movementInput != MovementInput)
            {
                MovementInput = movementInput;
                OnMovementInput?.Invoke(MovementInput);
            }

            // Get mouse look input
            Vector2 mouseInput = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
            if (mouseInput != MouseInput)
            {
                MouseInput = mouseInput;
                OnMouseLookInput?.Invoke(MouseInput);
            }

            // Jump input
            if (UnityEngine.Input.GetButtonDown("Jump"))
            {
                OnJumpInput?.Invoke();
            }

            // Interact input - now using only Fire1 (left mouse button)
            if (UnityEngine.Input.GetButtonDown("Fire1"))
            {
                OnInteractInput?.Invoke();
            }

            // Drop input - now using only Fire2 (right mouse button)
            if (UnityEngine.Input.GetButtonDown("Fire2"))
            {
                OnDropInput?.Invoke();
            }

            // Run input
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift))
            {
                IsRunning = true;
                OnRunInput?.Invoke();
            }
            
            if (UnityEngine.Input.GetKeyUp(KeyCode.LeftShift))
            {
                IsRunning = false;
                OnRunCancelInput?.Invoke();
            }

            // Rotate held object input - only around Y axis with R key
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                IsRotatingObject = true;
                OnRotateObjectInput?.Invoke();
            }
            
            if (UnityEngine.Input.GetKeyUp(KeyCode.R))
            {
                IsRotatingObject = false;
                OnRotateObjectCancelInput?.Invoke();
            }

            // Cursor toggle
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                OnCursorToggleInput?.Invoke();
            }
        }
    }
} 