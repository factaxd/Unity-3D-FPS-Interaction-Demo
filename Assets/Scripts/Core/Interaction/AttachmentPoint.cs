using UnityEngine;
using System.Collections.Generic;
using System;

namespace KeyboardSim.Core.Interaction
{
    /// <summary>
    /// Represents a point where objects can be attached.
    /// </summary>
    public class AttachmentPoint : MonoBehaviour
    {
        [Header("Attachment Settings")]
        [Tooltip("Type of components that can be attached here")]
        [SerializeField] private string _acceptedType = "KeyboardPart";
        public string AcceptedType => _acceptedType;

        [Tooltip("Tag of objects that can be attached here")]
        [SerializeField] private string _acceptedTag = "";
        
        [Tooltip("Whether this attachment point can only accept one specific object")]
        [SerializeField] private bool _isSpecificAttachment = false;

        [Tooltip("Specific object prefab that can be attached (if isSpecificAttachment is true)")]
        [SerializeField] private GameObject _specificAttachmentPrefab;

        [Header("Gizmo Settings")]
        [Tooltip("Color of the gizmo when point is available")]
        [SerializeField] private Color _availableColor = Color.green;

        [Tooltip("Color of the gizmo when point is invalid")]
        [SerializeField] private Color _invalidColor = Color.red;

        [Tooltip("Color of the gizmo when point is occupied")]
        [SerializeField] private Color _occupiedColor = Color.blue;
        
        [Tooltip("Size of the gizmo sphere")]
        [SerializeField] private float _gizmoSize = 0.05f;
        
        [Tooltip("Expected size of the attached object (for preview)")]
        [SerializeField] private Vector3 _expectedObjectSize = new Vector3(0.05f, 0.05f, 0.05f);
        
        [Tooltip("Expected rotation of the attached object (for preview)")]
        [SerializeField] private Vector3 _expectedObjectRotation = Vector3.zero;

        // State tracking
        private bool _isOccupied = false;
        private GameObject _attachedObject;
        private bool _shouldHighlight = false;
        private bool _isHighlightValid = true;
        
        // Events
        public event Action<GameObject> OnObjectAttached;
        public event Action<GameObject> OnObjectDetached;

        /// <summary>
        /// Check if an object can be attached to this point
        /// </summary>
        public bool CanAttach(GameObject obj)
        {
            // Already occupied
            if (_isOccupied) return false;
            
            // Specific attachment check
            if (_isSpecificAttachment && _specificAttachmentPrefab != null)
            {
                return obj.name.Contains(_specificAttachmentPrefab.name);
            }
            
            // Tag check
            if (!string.IsNullOrEmpty(_acceptedTag))
            {
                if (!obj.CompareTag(_acceptedTag))
                {
                    return false;
                }
            }
            
            // Type check (using components)
            if (!string.IsNullOrEmpty(_acceptedType))
            {
                // First check for a match with the object's name (for identifying the type)
                if (obj.name.Contains(_acceptedType))
                {
                    return true;
                }
                
                // Then check for InteractableObject component with matching type
                InteractableObject interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    return interactable.objectName.Contains(_acceptedType);
                }
                
                return false;
            }
            
            // If no specific restrictions, any object can be attached
            return true;
        }

        /// <summary>
        /// Attach an object to this point
        /// </summary>
        public bool AttachObject(GameObject obj)
        {
            if (!CanAttach(obj)) return false;
            
            // Store reference to attached object
            _attachedObject = obj;
            _isOccupied = true;
            
            // Make sure the attached object's collider stays enabled for selection
            Collider objCollider = obj.GetComponent<Collider>();
            if (objCollider != null)
            {
                objCollider.enabled = true;
                objCollider.isTrigger = false; // Ensure it can be ray-cast against
            }
            
            // Ensure the object stays on the interactable layer
            obj.layer = LayerMask.NameToLayer("Interactable");
            
            // Set all children to the interactable layer too (to ensure they're detectable)
            foreach (Transform child in obj.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Interactable");
            }
            
            // Trigger event
            OnObjectAttached?.Invoke(obj);
            
            return true;
        }

        /// <summary>
        /// Detach the currently attached object
        /// </summary>
        public GameObject DetachObject()
        {
            if (!_isOccupied || _attachedObject == null) return null;
            
            // Store reference before clearing
            GameObject detachedObject = _attachedObject;
            
            // Clear state
            _isOccupied = false;
            _attachedObject = null;
            
            // Ensure visualizer is hidden
            ShowVisualizer(false);
            
            // Trigger event
            OnObjectDetached?.Invoke(detachedObject);
            
            return detachedObject;
        }

        /// <summary>
        /// Show or hide the attachment point highlighting
        /// </summary>
        public void ShowVisualizer(bool show, bool isValidAttachment = true)
        {
            _shouldHighlight = show;
            _isHighlightValid = isValidAttachment;
        }

        /// <summary>
        /// Draw the gizmo in the editor and during runtime
        /// </summary>
        private void OnDrawGizmos()
        {
            // Set color based on state (runtime) or default to available color (editor)
            Color gizmoColor = _availableColor;
            
            if (Application.isPlaying)
            {
                if (_isOccupied)
                {
                    gizmoColor = _occupiedColor;
                }
                else if (_shouldHighlight)
                {
                    gizmoColor = _isHighlightValid ? _availableColor : _invalidColor;
                }
                else
                {
                    // Don't draw the sphere if we're not highlighting and not occupied during runtime
                    return;
                }
            }
            
            // Draw attachment point
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoSize);
            
            // Draw expected object size and rotation box
            if (!_isOccupied && (_shouldHighlight || !Application.isPlaying))
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
                
                // Set matrix to reflect expected rotation
                Matrix4x4 originalMatrix = Gizmos.matrix;
                Quaternion rotation = Quaternion.Euler(_expectedObjectRotation);
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * rotation, Vector3.one);
                
                // Draw wireframe cube for expected object size
                Gizmos.DrawWireCube(Vector3.zero, _expectedObjectSize);
                
                // Restore matrix
                Gizmos.matrix = originalMatrix;
            }
        }

        /// <summary>
        /// Check if the attachment point is currently occupied
        /// </summary>
        public bool IsOccupied()
        {
            return _isOccupied;
        }

        /// <summary>
        /// Get the currently attached object
        /// </summary>
        public GameObject GetAttachedObject()
        {
            return _attachedObject;
        }
        
        /// <summary>
        /// Get the expected size of objects that will be attached
        /// </summary>
        public Vector3 GetExpectedObjectSize()
        {
            return _expectedObjectSize;
        }
        
        /// <summary>
        /// Get the expected rotation of objects that will be attached
        /// </summary>
        public Vector3 GetExpectedObjectRotation()
        {
            return _expectedObjectRotation;
        }
    }
} 