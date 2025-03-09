using UnityEngine;

public class ObjectHolder : MonoBehaviour
{
    [Header("Hold Settings")]
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private float moveSpeed = 10f;
    
    
    [Header("Scroll Settings")]
    [Tooltip("How sensitive the mouse scroll wheel is for adjusting distance")]
    [SerializeField] private float scrollSensitivity = 5f;
    [Tooltip("Minimum distance objects can be held from camera")]
    [SerializeField] private float minHoldDistance = 1f;
    [Tooltip("Maximum distance objects can be held from camera")]
    [SerializeField] private float maxHoldDistance = 5f;
    
    private Camera playerCamera;
    private InteractableObject heldObject;
    private bool isHolding = false;
    private Vector3 targetPosition;
    
    public void Initialize(Camera camera)
    {
        playerCamera = camera;
    }
    
    public void Update()
    {
        if (isHolding && heldObject != null)
        {
            // Adjust hold distance with mouse scroll
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                holdDistance -= scrollInput * scrollSensitivity;
                holdDistance = Mathf.Clamp(holdDistance, minHoldDistance, maxHoldDistance);
            }

            // Update position based on camera
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * moveSpeed);
        }
    }
    
    public void PickupObject(InteractableObject obj)
    {
        heldObject = obj;
        heldObject.Pickup();
        heldObject.transform.SetParent(null);
        isHolding = true;
    }
    
    public void DropObject()
    {
        if (heldObject != null)
        {
            Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

            // Check for collision with other objects
            RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, holdDistance * 1.5f);
            bool canPlaceHere = true;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject != heldObject.gameObject &&
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable"))
                {
                    canPlaceHere = false;
                    break;
                }
            }

            // Adjust position if needed
            if (!canPlaceHere)
            {
                dropPosition = playerCamera.transform.position + playerCamera.transform.forward * (holdDistance + 0.5f);
            }

            heldObject.transform.position = dropPosition;
            heldObject.Drop();
            heldObject.ShowOutline(false);
            heldObject = null;
        }

        isHolding = false;
    }
    
    public bool PlaceObject(Vector3 position)
    {
        if (heldObject != null)
        {
            // Get placement position
            Vector3 placementPosition = (position == Vector3.zero) 
                ? playerCamera.transform.position + playerCamera.transform.forward * holdDistance 
                : position;

            // Check for collisions
            Collider[] colliders = Physics.OverlapSphere(placementPosition, heldObject.GetComponent<Collider>().bounds.extents.magnitude * 0.8f);
            bool canPlaceHere = true;

            foreach (Collider col in colliders)
            {
                if (col.gameObject != heldObject.gameObject &&
                    col.gameObject.layer == LayerMask.NameToLayer("Interactable"))
                {
                    canPlaceHere = false;
                    break;
                }
            }

            // Place object if possible
            if (canPlaceHere)
            {
                heldObject.transform.position = placementPosition;
                heldObject.Drop();
                heldObject.ShowOutline(false);
                heldObject = null;
                isHolding = false;
                return true;
            }
            
            // Cannot place here
            return false;
        }

        isHolding = false;
        return true;
    }
    
    public void AttachToPoint(AttachmentPoint point)
    {
        if (heldObject != null)
        {
            heldObject.Attach(point.transform);
            point.AttachObject(heldObject.gameObject);
            heldObject.ShowOutline(false);
            heldObject = null;
            isHolding = false;
        }
    }
    
    public InteractableObject GetHeldObject()
    {
        return heldObject;
    }
    
    public bool IsHolding()
    {
        return isHolding;
    }
    
    public float GetHoldDistance()
    {
        return holdDistance;
    }
} 