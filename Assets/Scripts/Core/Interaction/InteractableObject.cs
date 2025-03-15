using UnityEngine;
using System;
using KeyboardSim.Core.Player;

namespace KeyboardSim.Core.Interaction
{
    /// <summary>
    /// Base class for all interactable objects in the game.
    /// This class follows SRP by handling only the core interaction behavior.
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        [Header("Basic Properties")]
        [Tooltip("Display name of the object")]
        [SerializeField] private string _objectName = "KeyboardPart";
        public string objectName => _objectName;

        [Tooltip("Can this object be attached to other objects when picked up")]
        [SerializeField] private bool _canAttach = false;
        public bool CanAttach => _canAttach;

        [Tooltip("Tag of objects this can be attached to")]
        [SerializeField] private string _attachableToTag = "Keyboard";

        [Header("Outline Settings")]
        [Tooltip("Outline color when highlighted")]
        [SerializeField] private Color _outlineColor = new Color(0.18f, 0.8f, 1f, 1f);

        [Tooltip("Outline width when highlighted")]
        [SerializeField] private float _outlineWidth = 4f;

        // Component references
        private Rigidbody _rigidbody;
        private Collider _collider;
        private Outline _outline;
        private PlayerInteraction _playerInteraction;

        // State tracking
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Transform _attachedTo;
        
        [SerializeField] private bool _isAttached = false;
        public bool isAttached => _isAttached;

        // Events
        public event Action<bool> OnAttachmentStateChanged;
        public event Action OnPickup;
        public event Action OnDrop;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Get or add rigidbody
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            // Get collider
            _collider = GetComponent<Collider>();
            if (_collider == null)
            {
                Debug.LogError($"InteractableObject - {gameObject.name} has no collider!");
            }

            // Setup outline component
            SetupOutline();
            
            // Find player interaction component
            _playerInteraction = FindObjectOfType<KeyboardSim.Core.Player.PlayerInteraction>();
            if (_playerInteraction == null)
            {
                Debug.LogWarning($"InteractableObject - {gameObject.name} could not find PlayerInteraction component!");
            }
        }

        private void SetupOutline()
        {
            _outline = GetComponent<Outline>();
            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
                _outline.OutlineMode = Outline.Mode.OutlineAll;
            }
            
            _outline.OutlineColor = _outlineColor;
            _outline.OutlineWidth = _outlineWidth;
            _outline.enabled = false;
        }

        /// <summary>
        /// Pick up the object
        /// </summary>
        public virtual void Pickup()
        {
            // If attached, detach first
            if (_isAttached)
            {
                Detach();
            }

            // Make physics kinematic
            SetPhysicsState(true);

            // Save original transform
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;

            // Trigger event
            OnPickup?.Invoke();
        }

        /// <summary>
        /// Drop the object
        /// </summary>
        public virtual void Drop()
        {
            // Restore physics
            SetPhysicsState(false);

            // Reset velocity
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // Trigger event
            OnDrop?.Invoke();
        }

        /// <summary>
        /// Attach object to specified parent transform
        /// </summary>
        public virtual void Attach(Transform parent)
        {
            if (!_canAttach) return;

            transform.SetParent(parent);

            // Use attachment point's position and rotation
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Update state
            _isAttached = true;
            _attachedTo = parent;

            // Make physics kinematic while preserving selection capability
            SetPhysicsState(true);

            // Ensure the collider is configured for selection
            if (_collider != null)
            {
                _collider.enabled = true;
                _collider.isTrigger = false; // Keep as non-trigger to be selectable
                
                // If the collider is too small, create a slightly larger selection collider
                if (_collider is BoxCollider boxCollider && boxCollider.size.magnitude < 0.1f)
                {
                    // Create a slightly larger trigger collider for easier selection
                    BoxCollider selectionCollider = gameObject.AddComponent<BoxCollider>();
                    selectionCollider.size = boxCollider.size * 1.2f; // 20% larger
                    selectionCollider.center = boxCollider.center;
                    selectionCollider.isTrigger = true;
                }
            }

            // Ensure we're on the interactable layer
            gameObject.layer = LayerMask.NameToLayer("Interactable");

            // Play sound if available
            PlayAttachSound();

            // Trigger event
            OnAttachmentStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Detach object from its parent
        /// </summary>
        public virtual void Detach()
        {
            if (!_isAttached) return;

            // Store current position and orientation before detaching
            Vector3 attachmentPosition = transform.position;
            Vector3 attachmentForward = transform.parent ? transform.parent.forward : Vector3.forward;
            Vector3 attachmentUp = transform.parent ? transform.parent.up : Vector3.up;

            // Notify attachment point if there is one
            AttachmentPoint attachmentPoint = _attachedTo?.GetComponent<AttachmentPoint>();
            if (attachmentPoint != null)
            {
                attachmentPoint.DetachObject();
            }

            // Detach from parent
            transform.SetParent(null);

            // Update state
            _isAttached = false;
            _attachedTo = null;

            // Ensure outline is disabled before pickup
            ShowOutline(false);

            // Position the object properly after detachment
            PositionAfterDetach(attachmentPosition, attachmentForward, attachmentUp);

            // Trigger event
            OnAttachmentStateChanged?.Invoke(false);
            
            // Use the PlayerInteraction component to pick up the object directly
            if (_playerInteraction != null)
            {
                _playerInteraction.PickupDetachedObject(this);
            }
            else
            {
                // Fallback to the regular Pickup method if PlayerInteraction is not available
                Pickup();
            }
        }

        private void PositionAfterDetach(Vector3 attachmentPosition, Vector3 attachmentForward, Vector3 attachmentUp)
        {
            // Find main camera
            Camera mainCamera = Camera.main;
            Vector3 cameraDirection = Vector3.zero;

            if (mainCamera != null)
            {
                cameraDirection = (mainCamera.transform.position - attachmentPosition).normalized;
            }

            // Move object away from attachment point for better visibility
            // Increased offset values to ensure better separation
            transform.position = attachmentPosition +
                            (attachmentForward * 0.2f) +   // Move forward (increased from 0.1f)
                            (attachmentUp * 0.1f) +       // Move up (increased from 0.05f)
                            (cameraDirection * 0.15f);     // Move towards camera (increased from 0.05f)
        }

        private void SetPhysicsState(bool isKinematic)
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = isKinematic;
                _rigidbody.useGravity = !isKinematic;
            }

            if (_collider != null)
            {
                // If attached, keep the collider as non-trigger to ensure it can be selected
                _collider.isTrigger = isKinematic && !_isAttached;
                
                // Always keep the collider enabled
                _collider.enabled = true;
            }

            // Set layer based on state, ensuring attached objects remain interactable
            if (_isAttached)
            {
                gameObject.layer = LayerMask.NameToLayer("Interactable");
            }
            else
            {
                gameObject.layer = isKinematic ? 
                    LayerMask.NameToLayer("Held") : 
                    LayerMask.NameToLayer("Interactable");
            }
        }

        private void PlayAttachSound()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// Show or hide the outline effect
        /// </summary>
        public void ShowOutline(bool show)
        {
            if (_outline != null)
            {
                // Only change state if needed to avoid unnecessary updates
                if (_outline.enabled != show)
                {
                    _outline.enabled = show;
                    
                    // Force an update of the outline material properties
                    if (show)
                    {
                        _outline.OutlineColor = _outlineColor;
                        _outline.OutlineWidth = _outlineWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Check if this object can attach to the target
        /// </summary>
        public bool CanAttachTo(GameObject target)
        {
            return _canAttach && target.CompareTag(_attachableToTag);
        }

        /// <summary>
        /// Get the transform this object is attached to
        /// </summary>
        public Transform GetAttachedTo()
        {
            return _attachedTo;
        }
    }
} 