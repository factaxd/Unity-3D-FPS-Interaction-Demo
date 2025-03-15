using UnityEngine;
using KeyboardSim.Core.Interaction;
using KeyboardSim.Core.Input;
using System;

namespace KeyboardSim.Core.Player
{
    /// <summary>
    /// Manages the holding, positioning, and rotation of objects picked up by the player.
    /// This class follows SRP by focusing only on object holding mechanics.
    /// </summary>
    public class ObjectHolder : MonoBehaviour
    {
        [Header("Holding Settings")]
        [Tooltip("Transform where held objects will be positioned")]
        [SerializeField] private Transform _holdPoint;

        [Tooltip("Minimum distance from camera to hold objects")]
        [SerializeField] private float _minHoldDistance = 0.5f;
        
        [Tooltip("Maximum distance from camera to hold objects")]
        [SerializeField] private float _maxHoldDistance = 3f;
        
        [Tooltip("Default distance from camera to hold objects")]
        [SerializeField] private float _defaultHoldDistance = 1.5f;
        
        [Tooltip("How fast to zoom in/out with mouse scroll")]
        [SerializeField] private float _zoomSpeed = 0.5f;

        [Tooltip("How smoothly objects move to the hold position")]
        [SerializeField] private float _moveSpeed = 10f;

        [Tooltip("How smoothly objects rotate to the hold orientation")]
        [SerializeField] private float _rotationSpeed = 5f;

        [Tooltip("How fast objects rotate around Y axis with R key")]
        [SerializeField] private float _autoRotationSpeed = 100f;

        [Tooltip("Maximum distance to place objects from")]
        [SerializeField] private float _maxPlaceDistance = 3f;

        // Component references
        private Camera _playerCamera;
        private InputManager _inputManager;
        
        // State tracking
        private InteractableObject _heldObject;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isRotatingObject;
        private float _currentHoldDistance;
        
        // Events
        public event Action<InteractableObject> OnObjectPickup;
        public event Action<InteractableObject> OnObjectDrop;

        /// <summary>
        /// Initialize the object holder with a camera reference
        /// </summary>
        public void Initialize(Camera camera)
        {
            _playerCamera = camera;
            _currentHoldDistance = _defaultHoldDistance;
            
            // Still maintain hold point for compatibility
            if (_holdPoint == null)
            {
                GameObject holdPointObj = new GameObject("HoldPoint");
                holdPointObj.transform.SetParent(camera.transform);
                holdPointObj.transform.localPosition = new Vector3(0, -0.2f, _defaultHoldDistance);
                holdPointObj.transform.localRotation = Quaternion.identity;
                _holdPoint = holdPointObj.transform;
            }
            
            // Get input manager reference
            _inputManager = InputManager.Instance;
            if (_inputManager != null)
            {
                _inputManager.OnRotateObjectInput += () => _isRotatingObject = true;
                _inputManager.OnRotateObjectCancelInput += () => _isRotatingObject = false;
            }
            else
            {
                Debug.LogWarning("InputManager not found. Object rotation will not work.");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from input events
            if (_inputManager != null)
            {
                _inputManager.OnRotateObjectInput -= () => _isRotatingObject = true;
                _inputManager.OnRotateObjectCancelInput -= () => _isRotatingObject = false;
            }
        }

        /// <summary>
        /// Update position and rotation of held object
        /// </summary>
        public void Update()
        {
            if (!IsHolding()) return;
            
            // Handle zoom with mouse scroll
            HandleZooming();
            
            // Update target position based on camera and crosshair
            UpdateCrosshairPosition();
            
            // Handle rotation input
            if (_isRotatingObject)
            {
                AutoRotateHeldObject();
            }
            
            // Move held object to target position and rotation
            MoveObjectToTarget();
        }
        
        private void HandleZooming()
        {
            float scrollInput = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                // Adjust hold distance based on scroll input
                _currentHoldDistance = Mathf.Clamp(
                    _currentHoldDistance - scrollInput * _zoomSpeed,
                    _minHoldDistance,
                    _maxHoldDistance
                );
            }
        }
        

        private void UpdateCrosshairPosition()
        {
            if (_playerCamera == null) return;
            
            // Cast ray from screen center (where crosshair is)
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            // Set target position along this ray at the current hold distance
            _targetPosition = ray.origin + ray.direction * _currentHoldDistance;
            
            // Maintain the same rotation as the camera by default, but only if we don't have a held object
            // or if we're not currently rotating and haven't rotated before
            if (_heldObject == null || (_targetRotation == Quaternion.identity))
            {
                _targetRotation = Quaternion.LookRotation(ray.direction, Vector3.up);
            }
        }

        private void MoveObjectToTarget()
        {
            if (_heldObject == null) return;
            
            // Smoothly move object to target position
            _heldObject.transform.position = Vector3.Lerp(
                _heldObject.transform.position, 
                _targetPosition, 
                _moveSpeed * Time.deltaTime
            );
            
            // Smoothly rotate object to target rotation if not manually rotating
            if (!_isRotatingObject)
            {
                _heldObject.transform.rotation = Quaternion.Slerp(
                    _heldObject.transform.rotation,
                    _targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

        private void AutoRotateHeldObject()
        {
            if (_heldObject == null) return;
            
            // Automatically rotate around Y axis when R is pressed
            _heldObject.transform.Rotate(Vector3.up, _autoRotationSpeed * Time.deltaTime, Space.World);
            
            // Update target rotation to match current rotation to prevent snapping back
            _targetRotation = _heldObject.transform.rotation;
        }

        /// <summary>
        /// Pick up an interactable object
        /// </summary>
        public void PickupObject(InteractableObject obj)
        {
            if (IsHolding())
            {
                // If already holding something, drop it first
                DropObject();
            }
            
            // Call object's pickup method
            obj.Pickup();
            
            // Set as held object
            _heldObject = obj;
            
            // Reset hold distance to default
            _currentHoldDistance = _defaultHoldDistance;
            
            // Update target position and rotation
            UpdateCrosshairPosition();
            
            // Initial position snap
            _heldObject.transform.position = _targetPosition;
            
            // Trigger event
            OnObjectPickup?.Invoke(_heldObject);
        }

        /// <summary>
        /// Drop currently held object
        /// </summary>
        public void DropObject()
        {
            if (!IsHolding()) return;
            
            // Cache held object reference
            InteractableObject droppedObject = _heldObject;
            
            // Ensure outline is disabled
            droppedObject.ShowOutline(false);
            
            // Call object's drop method
            droppedObject.Drop();
            
            // Clear reference
            _heldObject = null;
            
            // Trigger event
            OnObjectDrop?.Invoke(droppedObject);
        }

        /// <summary>
        /// Place held object on a surface
        /// </summary>
        public bool PlaceObject(RaycastHit surfaceHit)
        {
            if (!IsHolding()) return false;
            
            // Check if surface is within placement distance
            if (surfaceHit.distance > _maxPlaceDistance) return false;
            
            // Get correct position on surface
            Vector3 placePosition = surfaceHit.point;
            
            // Get appropriate rotation (align with surface normal)
            Quaternion placeRotation = Quaternion.FromToRotation(Vector3.up, surfaceHit.normal);
            
            // Place object on surface
            _heldObject.transform.position = placePosition;
            _heldObject.transform.rotation = placeRotation * Quaternion.Euler(0, _heldObject.transform.eulerAngles.y, 0);
            
            // Drop object
            DropObject();
            
            return true;
        }

        /// <summary>
        /// Try to attach held object to the target
        /// </summary>
        public bool TryAttachObject(Transform attachPoint)
        {
            if (!IsHolding() || attachPoint == null) return false;
            
            // Cache held object reference
            InteractableObject objectToAttach = _heldObject;
            
            // Try to attach
            objectToAttach.Attach(attachPoint);
            
            // If now attached, clear held reference
            if (objectToAttach.isAttached)
            {
                _heldObject = null;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Check if currently holding an object
        /// </summary>
        public bool IsHolding()
        {
            return _heldObject != null;
        }

        /// <summary>
        /// Get the currently held object
        /// </summary>
        public InteractableObject GetHeldObject()
        {
            return _heldObject;
        }
    }
} 