using System.Collections.Generic;
using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private bool showDebugRay = true;
    
    private Camera playerCamera;
    
    public void Initialize(Camera camera)
    {
        playerCamera = camera;
    }
    
    public bool DetectInteraction(out RaycastHit primaryHit, out InteractableObject interactable)
    {
        interactable = null;
        primaryHit = new RaycastHit();
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
        }
        
        // Get all hits along the ray
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance);
        
        if (hits.Length > 0)
        {
            // Store the first hit for surface detection
            primaryHit = hits[0];
            
            // Process hits to find interactable objects
            foreach (RaycastHit hit in hits)
            {
                // Check for direct interactable
                InteractableObject directInteractable = hit.collider.GetComponent<InteractableObject>();
                if (directInteractable != null)
                {
                    interactable = directInteractable;
                    return true;
                }
                
                // Check attachment points
                AttachmentPoint attachPoint = hit.collider.GetComponent<AttachmentPoint>();
                if (attachPoint != null && attachPoint.IsOccupied())
                {
                    GameObject attachedObj = attachPoint.GetAttachedObject();
                    if (attachedObj != null)
                    {
                        InteractableObject attachedInteractable = attachedObj.GetComponent<InteractableObject>();
                        if (attachedInteractable != null)
                        {
                            interactable = attachedInteractable;
                            return true;
                        }
                    }
                }
                
                // Check hierarchy
                InteractableObject interactableInHierarchy = FindInteractableInHierarchy(hit.transform);
                if (interactableInHierarchy != null)
                {
                    interactable = interactableInHierarchy;
                    return true;
                }
            }
            
            // No interactable found but we have a hit
            return true;
        }
        
        // No hits at all
        return false;
    }
    
    // Find an interactable object in the transform hierarchy
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
    
    public float GetInteractionDistance()
    {
        return interactionDistance;
    }
    
    public LayerMask GetInteractionLayer()
    {
        return interactionLayer;
    }
} 