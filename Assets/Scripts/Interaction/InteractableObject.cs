using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Tooltip("Name of the object")]
    public string objectName;

    [Tooltip("Can this object be attached to other objects when picked up")]
    public bool canAttach = false;

    [Tooltip("Tag of objects this can be attached to")]
    public string attachableToTag;

    [Header("Outline Settings")]
    [Tooltip("Outline color")]
    public Color outlineColor = new Color(0.18f, 0.8f, 1f, 1f); // Bright blue

    [Tooltip("Outline width")]
    public float outlineWidth = 4f;

    private Rigidbody rb;
    private Collider objectCollider;
    private Outline outline;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    [HideInInspector]
    public bool isAttached = false;

    private Transform attachedTo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        objectCollider = GetComponent<Collider>();
        if (objectCollider == null)
        {
            Debug.LogError("InteractableObject - " + gameObject.name + " has no collider!");
        }

        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
        }
        else
        {
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
        }

        outline.enabled = false;
    }

    public void Pickup()
    {
        if (isAttached)
        {
            Detach();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }

        gameObject.layer = LayerMask.NameToLayer("Held");

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    public void Drop()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        if (objectCollider != null)
        {
            objectCollider.isTrigger = false;
        }

        gameObject.layer = LayerMask.NameToLayer("Interactable");

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Attach(Transform parent)
    {
        if (!canAttach) return;

        transform.SetParent(parent);

        // Use attachment point's position and rotation
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        isAttached = true;
        attachedTo = parent;

        rb.isKinematic = true;
        rb.useGravity = false;

        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    public void Detach()
    {
        if (!isAttached) return;

        Vector3 attachmentPosition = transform.position;
        Vector3 attachmentForward = transform.parent ? transform.parent.forward : Vector3.forward;
        Vector3 attachmentUp = transform.parent ? transform.parent.up : Vector3.up;

        AttachmentPoint attachmentPoint = transform.parent?.GetComponent<AttachmentPoint>();
        if (attachmentPoint != null)
        {
            attachmentPoint.DetachObject();
        }

        transform.SetParent(null);

        // Improved detachment positioning - move object more clearly away from attachment point
        // and slightly towards the camera if possible
        Camera mainCamera = Camera.main;
        Vector3 cameraDirection = Vector3.zero;
        
        if (mainCamera != null)
        {
            cameraDirection = (mainCamera.transform.position - attachmentPosition).normalized;
        }
        
        // Place object in a better visible position by moving it away from attachment point
        // and slightly towards the camera direction for better visibility
        transform.position = attachmentPosition + 
                             (attachmentForward * 0.1f) +      // Move forward more
                             (attachmentUp * 0.05f) +      // Move up more
                             (cameraDirection * 0.05f);    // Move slightly towards camera

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (objectCollider != null)
        {
            objectCollider.isTrigger = false;
        }

        gameObject.layer = LayerMask.NameToLayer("Interactable");

        isAttached = false;
        attachedTo = null;
    }

    public void ShowOutline(bool show)
    {
        if (outline != null)
        {
            outline.enabled = show;
        }
    }

    public bool CanAttachTo(GameObject target)
    {
        return canAttach && target.CompareTag(attachableToTag);
    }
}