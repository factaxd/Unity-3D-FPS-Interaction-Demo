using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KeyboardSim.Core.Interaction;
using KeyboardSim.Core.Input;
using KeyboardSim.Data;

namespace KeyboardSim.Core.Player
{
    /// <summary>
    /// Manages player interaction with objects in the game world.
    /// This class coordinates between input, interaction detection, and object holding.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private InteractionDetector _detector;
        [SerializeField] private ObjectHolder _objectHolder;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _interactionText;
        [SerializeField] private Image _crosshair;
        
        [Header("Data References")]
        [SerializeField] private InteractionTextData _textData;
        
        // Private references
        private Camera _playerCamera;
        private InputManager _inputManager;
        private InteractableObject _currentInteractable;
        
        private void Start()
        {
            InitializeComponents();
            SetupReferences();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeComponents()
        {
            // Get camera reference
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
            
            // Initialize detector if not set in inspector
            if (_detector == null)
            {
                _detector = GetComponent<InteractionDetector>();
                if (_detector == null)
                {
                    _detector = gameObject.AddComponent<InteractionDetector>();
                }
            }
            
            // Initialize object holder if not set in inspector
            if (_objectHolder == null)
            {
                _objectHolder = GetComponent<ObjectHolder>();
                if (_objectHolder == null)
                {
                    _objectHolder = gameObject.AddComponent<ObjectHolder>();
                }
            }
            
            // Check UI references
            if (_interactionText == null)
            {
                Debug.LogWarning("Interaction text is not assigned!");
            }

            if (_crosshair == null)
            {
                Debug.LogWarning("Crosshair image is not assigned!");
            }
            
            // Create default text data if not assigned
            if (_textData == null)
            {
                _textData = ScriptableObject.CreateInstance<InteractionTextData>();
                Debug.LogWarning("Text data not assigned. Using default values.");
            }
            
            // Initialize components
            _detector.Initialize(_playerCamera);
            _objectHolder.Initialize(_playerCamera);
            
            // Clear interaction text
            if (_interactionText != null)
            {
                _interactionText.text = "";
            }
        }
        
        private void SetupReferences()
        {
            // Get InputManager reference
            _inputManager = InputManager.Instance;
            if (_inputManager == null)
            {
                Debug.LogError("InputManager not found in scene!");
                this.enabled = false;
                return;
            }
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to detector events
            if (_detector != null)
            {
                _detector.OnInteractableDetected = HandleInteractableDetected;
                _detector.OnInteractableLost = HandleInteractableLost;
            }
            
            // Subscribe to input events
            if (_inputManager != null)
            {
                _inputManager.OnInteractInput += HandleInteractInput;
                _inputManager.OnDropInput += HandleDropInput;
            }
            
            // Subscribe to object holder events
            if (_objectHolder != null)
            {
                _objectHolder.OnObjectPickup += HandleObjectPickup;
                _objectHolder.OnObjectDrop += HandleObjectDrop;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from detector events
            if (_detector != null)
            {
                _detector.OnInteractableDetected = null;
                _detector.OnInteractableLost = null;
            }
            
            // Unsubscribe from input events
            if (_inputManager != null)
            {
                _inputManager.OnInteractInput -= HandleInteractInput;
                _inputManager.OnDropInput -= HandleDropInput;
            }
            
            // Unsubscribe from object holder events
            if (_objectHolder != null)
            {
                _objectHolder.OnObjectPickup -= HandleObjectPickup;
                _objectHolder.OnObjectDrop -= HandleObjectDrop;
            }
        }

        private void Update()
        {
            // Process additional interaction info based on what we're looking at
            ProcessInteractionDisplay();
        }
        
        private void ProcessInteractionDisplay()
        {
            // Get current information from detector
            RaycastHit hit;
            InteractableObject interactable;
            bool hitSomething = _detector.DetectInteraction(out hit, out interactable);
            
            // Not looking at anything
            if (!hitSomething)
            {
                // If holding an object, show drop text
                if (_objectHolder.IsHolding())
                {
                    InteractableObject heldObj = _objectHolder.GetHeldObject();
                    UpdateInteractionText(string.Format(_textData.dropText, heldObj.objectName));
                }
                else
                {
                    ClearInteractionText();
                }
                return;
            }
            
            // If looking at a non-interactable while holding something
            if (interactable == null && _objectHolder.IsHolding())
            {
                ProcessNonInteractableHit(hit);
                return;
            }
        }
        
        private void ProcessNonInteractableHit(RaycastHit hit)
        {
            InteractableObject heldObj = _objectHolder.GetHeldObject();
            
            // Check for AttachmentPointManager
            AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
            if (pointManager == null)
            {
                pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
            }

            if (pointManager != null)
            {
                // Highlight attachment points
                pointManager.HighlightAvailablePoints(heldObj.gameObject, true);
                
                // Show attachment instruction
                UpdateInteractionText(string.Format(_textData.attachText, heldObj.objectName));
            }
            // Surface check for placement
            else if (hit.collider.CompareTag("Surface"))
            {
                UpdateInteractionText(string.Format(_textData.placeText, heldObj.objectName));
            }
            // Regular drop
            else
            {
                UpdateInteractionText(string.Format(_textData.dropText, heldObj.objectName));
            }
        }
        
        private void HandleInteractableDetected(InteractableObject interactable)
        {
            // If we're already looking at this interactable, don't do anything
            if (_currentInteractable == interactable) return;
            
            // Clear previous interactable's outline if any
            if (_currentInteractable != null)
            {
                _currentInteractable.ShowOutline(false);
            }
            
            // Store reference and highlight
            _currentInteractable = interactable;
            _currentInteractable.ShowOutline(true);
            
            if (_objectHolder.IsHolding())
            {
                // Holding an object and looking at a different interactable
                InteractableObject heldObj = _objectHolder.GetHeldObject();
                if (interactable != heldObj)
                {
                    // Check for attachment points
                    AttachmentPointManager pointManager = interactable.GetComponent<AttachmentPointManager>();
                    if (pointManager != null)
                    {
                        // Clear any previous highlights first
                        ClearAttachmentPointHighlights();
                        
                        // Highlight available attachment points
                        pointManager.HighlightAvailablePoints(heldObj.gameObject, true);
                        
                        // Show attachment prompt
                        UpdateInteractionText(string.Format(_textData.attachText, heldObj.objectName));
                    }
                    else
                    {
                        // Show generic attachment prompt
                        UpdateInteractionText(string.Format(_textData.attachToObjectText, 
                            heldObj.objectName, interactable.objectName));
                    }
                }
            }
            else
            {
                // Not holding anything
                if (interactable.isAttached)
                {
                    // Show detach prompt
                    UpdateInteractionText(string.Format(_textData.detachText, interactable.objectName));
                }
                else
                {
                    // Show pickup prompt
                    UpdateInteractionText(string.Format(_textData.pickupText, interactable.objectName));
                }
            }
        }
        
        private void HandleInteractableLost()
        {
            // Clear highlight
            if (_currentInteractable != null)
            {
                _currentInteractable.ShowOutline(false);
                _currentInteractable = null;
            }
            
            // Clear any attachment point highlights
            ClearAttachmentPointHighlights();
            
            // Clear text if not holding anything
            if (!_objectHolder.IsHolding())
            {
                ClearInteractionText();
            }
        }
        
        private void ClearAttachmentPointHighlights()
        {
            // Find all attachment point managers in the scene
            AttachmentPointManager[] managers = FindObjectsOfType<AttachmentPointManager>();
            foreach (var manager in managers)
            {
                manager.HighlightAvailablePoints(null, false);
            }
        }
        
        private void HandleInteractInput()
        {
            // If looking at an interactable
            if (_currentInteractable != null)
            {
                if (_objectHolder.IsHolding())
                {
                    // Try to attach held object to the interactable
                    HandleAttachAttempt();
                }
                else
                {
                    // Try to pick up or detach the interactable
                    if (_currentInteractable.isAttached)
                    {
                        // Store reference to the interactable before detaching
                        InteractableObject detachingObject = _currentInteractable;
                        
                        // Clear current interactable reference to prevent outline issues
                        _currentInteractable = null;
                        
                        // Detach the object - this will trigger PickupDetachedObject
                        detachingObject.Detach();
                    }
                    else
                    {
                        _objectHolder.PickupObject(_currentInteractable);
                    }
                }
            }
            // If not looking at an interactable but holding something
            else if (_objectHolder.IsHolding())
            {
                // Try to place on surface
                TryPlaceOnSurface();
            }
        }
        
        private void HandleAttachAttempt()
        {
            // Get current hit information
            RaycastHit hit;
            InteractableObject interactable;
            _detector.DetectInteraction(out hit, out interactable);
            
            // Check for attachment point manager
            AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
            if (pointManager == null)
            {
                pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
            }
            
            if (pointManager != null)
            {
                // Try to attach to nearest valid point
                AttachmentPoint attachPoint = pointManager.GetNearestValidPoint(
                    _objectHolder.GetHeldObject().gameObject,
                    hit.point
                );
                
                if (attachPoint != null)
                {
                    _objectHolder.TryAttachObject(attachPoint.transform);
                }
                else
                {
                    // No valid attachment point
                    UpdateInteractionText(_textData.cannotAttachText);
                }
            }
            else
            {
                // No attachment point, try to place instead
                TryPlaceOnSurface();
            }
        }
        
        private void TryPlaceOnSurface()
        {
            // Get current hit information
            RaycastHit hit;
            InteractableObject interactable;
            bool hitSomething = _detector.DetectInteraction(out hit, out interactable);
            
            if (hitSomething && hit.collider.CompareTag("Surface"))
            {
                _objectHolder.PlaceObject(hit);
            }
            else
            {
                // Just drop if not looking at a valid surface
                _objectHolder.DropObject();
            }
        }
        
        private void HandleDropInput()
        {
            if (_objectHolder.IsHolding())
            {
                _objectHolder.DropObject();
            }
        }
        
        private void HandleObjectPickup(InteractableObject obj)
        {
            // Object was picked up
            UpdateCrosshairState(true);
        }
        
        private void HandleObjectDrop(InteractableObject obj)
        {
            // Object was dropped
            UpdateCrosshairState(false);
            ClearInteractionText();
        }
        
        private void UpdateCrosshairState(bool holding)
        {
            // Update crosshair appearance based on if holding an object
            if (_crosshair != null)
            {
                Color color = _crosshair.color;
                color.a = holding ? 0.8f : 0.5f;
                _crosshair.color = color;
            }
        }
        
        private void UpdateInteractionText(string text)
        {
            if (_interactionText != null)
            {
                _interactionText.text = text;
            }
        }
        
        private void ClearInteractionText()
        {
            if (_interactionText != null)
            {
                _interactionText.text = "";
            }
        }

        /// <summary>
        /// Public method to pick up an object directly
        /// </summary>
        public void PickupDetachedObject(InteractableObject obj)
        {
            if (obj != null && _objectHolder != null)
            {
                _objectHolder.PickupObject(obj);
            }
        }
    }
} 