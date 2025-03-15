using System.Collections.Generic;
using UnityEngine;
using System;

namespace KeyboardSim.Core.Interaction
{
    /// <summary>
    /// Responsible for detecting interactable objects in the game world.
    /// This class follows SRP by only handling object detection logic.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [Tooltip("Maximum distance for interaction detection")]
        [SerializeField] private float _interactionDistance = 3f;
        public float InteractionDistance => _interactionDistance;
        
        [Tooltip("Layer mask for interaction detection")]
        [SerializeField] private LayerMask _interactionLayer;
        public LayerMask InteractionLayer => _interactionLayer;
        
        [Tooltip("Show debug ray in the Scene view")]
        [SerializeField] private bool _showDebugRay = true;
        
        // Events - changed from event fields to delegate fields for direct assignment
        public Action<InteractableObject> OnInteractableDetected;
        public Action OnInteractableLost;
        
        // State tracking
        private Camera _playerCamera;
        private InteractableObject _currentInteractable;
        private bool _hasTarget;
        
        /// <summary>
        /// Initialize the detector with a camera reference
        /// </summary>
        public void Initialize(Camera camera)
        {
            _playerCamera = camera;
        }
        
        private void Update()
        {
            if (_playerCamera == null) return;
            
            // Detect interaction
            RaycastHit primaryHit;
            InteractableObject interactable;
            bool hitSomething = DetectInteraction(out primaryHit, out interactable);
            
            // Handle interactable changes
            if (interactable != null)
            {
                if (_currentInteractable != interactable)
                {
                    // Clear previous interactable's outline
                    if (_currentInteractable != null)
                    {
                        _currentInteractable.ShowOutline(false);
                    }
                    
                    _currentInteractable = interactable;
                    OnInteractableDetected?.Invoke(_currentInteractable);
                }
                _hasTarget = true;
            }
            else if (_hasTarget)
            {
                // Clear outline when losing target
                if (_currentInteractable != null)
                {
                    _currentInteractable.ShowOutline(false);
                }
                
                _currentInteractable = null;
                _hasTarget = false;
                OnInteractableLost?.Invoke();
            }
        }
        
        /// <summary>
        /// Detect interactable objects in front of the camera
        /// </summary>
        /// <param name="primaryHit">First hit information</param>
        /// <param name="interactable">Detected interactable object, if any</param>
        /// <returns>True if something was hit</returns>
        public bool DetectInteraction(out RaycastHit primaryHit, out InteractableObject interactable)
        {
            interactable = null;
            primaryHit = new RaycastHit();
            
            // Cast ray from camera center
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (_showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * _interactionDistance, Color.red);
            }
            
            // Use a single SphereCast with optimal radius for better selection
            RaycastHit[] hits = Physics.SphereCastAll(ray, 0.03f, _interactionDistance, _interactionLayer);
            
            if (hits.Length == 0)
            {
                // As a last resort, try a regular raycast
                if (Physics.Raycast(ray, out primaryHit, _interactionDistance, _interactionLayer))
                {
                    return true; // Hit something but no interactable
                }
                return false;
            }
            
            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            // Store the first hit for surface detection
            primaryHit = hits[0];
            
            // Process hits to find interactable objects
            List<InteractableObject> potentialInteractables = FindPotentialInteractables(hits);
            
            // Choose the best interactable based on priority
            if (potentialInteractables.Count > 0)
            {
                interactable = potentialInteractables[0];
                return true;
            }
            
            // No interactable found but we have a hit
            return true;
        }
        
        private List<InteractableObject> FindPotentialInteractables(RaycastHit[] hits)
        {
            List<InteractableObject> potentialInteractables = new List<InteractableObject>();
            
            foreach (RaycastHit hit in hits)
            {
                // First check attachment points - this is critical for selecting attached objects
                AttachmentPoint attachPoint = hit.collider.GetComponent<AttachmentPoint>();
                if (attachPoint != null && attachPoint.IsOccupied())
                {
                    GameObject attachedObj = attachPoint.GetAttachedObject();
                    if (attachedObj != null)
                    {
                        InteractableObject attachedInteractable = attachedObj.GetComponent<InteractableObject>();
                        if (attachedInteractable != null)
                        {
                            // Give attached objects the highest priority
                            potentialInteractables.Insert(0, attachedInteractable);
                            continue;
                        }
                    }
                }
                
                // Check for direct interactable
                InteractableObject directInteractable = hit.collider.GetComponent<InteractableObject>();
                if (directInteractable != null)
                {
                    // If this is a recently detached object, give it priority
                    if (directInteractable.isAttached == false && 
                        !potentialInteractables.Contains(directInteractable))
                    {
                        potentialInteractables.Add(directInteractable);
                        continue;
                    }
                    else if (!potentialInteractables.Contains(directInteractable))
                    {
                        potentialInteractables.Add(directInteractable);
                        continue;
                    }
                }
                
                // Check for attached objects on the hit object
                InteractableObject childAttachedInteractable = CheckForAttachedObjects(hit.transform);
                if (childAttachedInteractable != null && 
                    !potentialInteractables.Contains(childAttachedInteractable))
                {
                    potentialInteractables.Insert(0, childAttachedInteractable);
                    continue;
                }
                
                // Check hierarchy
                InteractableObject interactableInHierarchy = FindInteractableInHierarchy(hit.transform);
                if (interactableInHierarchy != null && 
                    !potentialInteractables.Contains(interactableInHierarchy))
                {
                    potentialInteractables.Add(interactableInHierarchy);
                }
            }
            
            return potentialInteractables;
        }
        
        /// <summary>
        /// Specifically check for attached objects on a transform
        /// </summary>
        private InteractableObject CheckForAttachedObjects(Transform transform)
        {
            if (transform == null) return null;
            
            // Check all children for attached interactable objects
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                InteractableObject interactable = child.GetComponent<InteractableObject>();
                
                // If this is an attached interactable, return it with priority
                if (interactable != null && interactable.isAttached)
                {
                    return interactable;
                }
                
                // Recurse into grandchildren (limited depth to avoid performance issues)
                InteractableObject childAttached = CheckForAttachedObjects(child);
                if (childAttached != null)
                {
                    return childAttached;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find an interactable object in the transform hierarchy
        /// </summary>
        private InteractableObject FindInteractableInHierarchy(Transform startTransform)
        {
            if (startTransform == null) return null;

            // Check the object itself
            InteractableObject interactable = startTransform.GetComponent<InteractableObject>();
            if (interactable != null) return interactable;

            // Look upward (parents)
            Transform parent = startTransform.parent;
            while (parent != null)
            {
                interactable = parent.GetComponent<InteractableObject>();
                if (interactable != null) return interactable;

                // Check siblings
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling != startTransform)
                    {
                        interactable = sibling.GetComponent<InteractableObject>();
                        if (interactable != null) return interactable;

                        // Check sibling's children (1 level)
                        for (int j = 0; j < sibling.childCount; j++)
                        {
                            Transform siblingChild = sibling.GetChild(j);
                            interactable = siblingChild.GetComponent<InteractableObject>();
                            if (interactable != null) return interactable;
                        }
                    }
                }

                parent = parent.parent;
            }

            // Look downward (children)
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(startTransform);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();

                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);

                    interactable = child.GetComponent<InteractableObject>();
                    if (interactable != null) return interactable;

                    queue.Enqueue(child);
                }
            }

            return null;
        }
        
        /// <summary>
        /// Get the currently detected interactable object, if any
        /// </summary>
        public InteractableObject GetCurrentInteractable()
        {
            return _currentInteractable;
        }
        
        /// <summary>
        /// Check if there is a currently detected interactable object
        /// </summary>
        public bool HasInteractable()
        {
            return _currentInteractable != null;
        }
    }
} 