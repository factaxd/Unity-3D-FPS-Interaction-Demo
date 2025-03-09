using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private InteractionDetector detector;
    [SerializeField] private ObjectHolder objectHolder;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Image crosshair;
    
    [Header("Text Data")]
    [SerializeField] private InteractionTextData textData;
    
    private Camera playerCamera;
    private InteractableObject currentInteractable;

    private void Start()
    {
        // Get camera reference
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Initialize components if not set in inspector
        if (detector == null)
        {
            detector = GetComponent<InteractionDetector>();
            if (detector == null)
            {
                detector = gameObject.AddComponent<InteractionDetector>();
            }
        }
        
        if (objectHolder == null)
        {
            objectHolder = GetComponent<ObjectHolder>();
            if (objectHolder == null)
            {
                objectHolder = gameObject.AddComponent<ObjectHolder>();
            }
        }
        
        // Initialize components
        detector.Initialize(playerCamera);
        objectHolder.Initialize(playerCamera);
        
        // Check UI references
        if (interactionText == null)
        {
            Debug.LogWarning("Interaction text is not assigned!");
        }

        if (crosshair == null)
        {
            Debug.LogWarning("Crosshair image is not assigned!");
        }
        
        // Create default text data if not assigned
        if (textData == null)
        {
            textData = ScriptableObject.CreateInstance<InteractionTextData>();
            Debug.LogWarning("Text data not assigned. Using default values.");
        }
        
        // Clear interaction text
        if (interactionText != null)
        {
            interactionText.text = "";
        }
    }

    private void Update()
    {
        // Process interaction detection
        ProcessInteraction();
        
        // Handle input
        HandleInput();
        
        // Update held object (delegate to ObjectHolder)
        objectHolder.Update();
    }

    private void ProcessInteraction()
    {
        // Use detector to find interactables
        RaycastHit hit;
        InteractableObject interactable;
        
        bool hitSomething = detector.DetectInteraction(out hit, out interactable);
        
        if (hitSomething)
        {
            if (interactable != null)
            {
                // Found an interactable object
                UpdateCurrentInteractable(interactable);
            }
            else if (objectHolder.IsHolding())
            {
                // Looking at a non-interactable while holding an object
                ProcessNonInteractableHit(hit);
            }
            else
            {
                // Looking at a non-interactable with nothing held
                ClearInteraction();
            }
        }
        else
        {
            // Not looking at anything
            if (!objectHolder.IsHolding())
            {
                ClearInteraction();
            }
            else if (interactionText != null)
            {
                // Show drop instructions while holding
                InteractableObject heldObj = objectHolder.GetHeldObject();
                interactionText.text = string.Format(textData.dropText, heldObj.objectName);
            }
        }
    }

    private void ProcessNonInteractableHit(RaycastHit hit)
    {
        InteractableObject heldObj = objectHolder.GetHeldObject();
        
        // Check for AttachmentPointManager
        AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
        if (pointManager == null)
        {
            pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
        }

        if (pointManager != null)
        {
            pointManager.HighlightAvailablePoints(heldObj.gameObject, true);

            if (interactionText != null)
            {
                interactionText.text = string.Format(textData.attachText, heldObj.objectName);
            }
        }
        // Surface check
        else if (hit.collider.CompareTag("Surface"))
        {
            if (interactionText != null)
            {
                interactionText.text = string.Format(textData.placeText, heldObj.objectName);
            }
        }
        else
        {
            if (interactionText != null)
            {
                interactionText.text = string.Format(textData.dropText, heldObj.objectName);
            }
        }
    }

    private void UpdateCurrentInteractable(InteractableObject interactable)
    {
        InteractableObject heldObj = objectHolder.GetHeldObject();
        
        // If not holding anything
        if (!objectHolder.IsHolding())
        {
            // Check for attached objects
            if (interactable.isAttached)
            {
                // Turn off previous outline
                if (currentInteractable != null && currentInteractable != interactable)
                {
                    currentInteractable.ShowOutline(false);
                }

                // Highlight the attached object
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);

                // Show detach option
                if (interactionText != null)
                {
                    interactionText.text = string.Format(textData.detachText, currentInteractable.objectName);
                }
            }
            // For normal interactable objects
            else if (currentInteractable != interactable)
            {
                // Turn off previous outline
                if (currentInteractable != null)
                {
                    currentInteractable.ShowOutline(false);
                }

                // Highlight new object
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);

                // Show pickup option
                if (interactionText != null)
                {
                    interactionText.text = string.Format(textData.pickupText, currentInteractable.objectName);
                }
            }
        }
        // If holding an object and looking at a different object
        else if (interactable != heldObj)
        {
            if (currentInteractable != interactable)
            {
                // Turn off previous outline
                if (currentInteractable != null && currentInteractable != heldObj)
                {
                    currentInteractable.ShowOutline(false);
                }

                // Highlight new object
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);

                // Check for attachment points
                AttachmentPointManager pointManager = interactable.GetComponent<AttachmentPointManager>();
                if (pointManager != null)
                {
                    // Highlight available points
                    pointManager.HighlightAvailablePoints(heldObj.gameObject, true);

                    if (interactionText != null)
                    {
                        interactionText.text = string.Format(textData.attachText, heldObj.objectName);
                    }
                }
                else
                {
                    if (interactionText != null)
                    {
                        interactionText.text = string.Format(textData.attachToObjectText, heldObj.objectName, currentInteractable.objectName);
                    }
                }
            }
        }
    }

    private void HandleInput()
    {
        // Interaction with E key
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Not holding anything but looking at an interactable
            if (!objectHolder.IsHolding() && currentInteractable != null)
            {
                // Detach if attached
                if (currentInteractable.isAttached)
                {
                    currentInteractable.Detach();
                    objectHolder.PickupObject(currentInteractable);
                }
                else
                {
                    // Normal pickup
                    objectHolder.PickupObject(currentInteractable);
                }
                
                // Update UI after pickup
                if (interactionText != null)
                {
                    interactionText.text = string.Format(textData.dropText, currentInteractable.objectName);
                }
            }
            // Holding an object
            else if (objectHolder.IsHolding())
            {
                InteractableObject heldObj = objectHolder.GetHeldObject();
                
                // Try placement
                RaycastHit hit;
                if (Physics.Raycast(playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), 
                                   out hit, detector.GetInteractionDistance(), detector.GetInteractionLayer()))
                {
                    // Check for attachment point
                    AttachmentPoint attachmentPoint = hit.collider.GetComponent<AttachmentPoint>();
                    if (attachmentPoint != null && attachmentPoint.CanAttach(heldObj.gameObject))
                    {
                        objectHolder.AttachToPoint(attachmentPoint);
                        ClearInteraction();
                        return;
                    }

                    // Check for attachment point manager
                    AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
                    if (pointManager == null)
                    {
                        pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
                    }

                    if (pointManager != null && pointManager.autoSelectNearestPoint)
                    {
                        AttachmentPoint nearestPoint = pointManager.GetNearestAttachmentPoint(heldObj.gameObject, heldObj.transform.position);

                        if (nearestPoint != null)
                        {
                            objectHolder.AttachToPoint(nearestPoint);
                            ClearInteraction();
                            return;
                        }
                    }

                    // Try to place on surface
                    if (hit.collider.CompareTag("Surface"))
                    {
                        bool placed = objectHolder.PlaceObject(hit.point);
                        if (!placed && interactionText != null)
                        {
                            interactionText.text = textData.cannotPlaceText;
                        }
                        else
                        {
                            ClearInteraction();
                        }
                    }
                    else
                    {
                        objectHolder.DropObject();
                        ClearInteraction();
                    }
                }
                else
                {
                    // Not looking at anything, just drop
                    objectHolder.DropObject();
                    ClearInteraction();
                }
            }
        }
    }

    private void ClearInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.ShowOutline(false);
            currentInteractable = null;
        }

        if (interactionText != null)
        {
            interactionText.text = "";
        }
    }
}