using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    [Tooltip("Tag of objects that can be attached to this point")]
    public string acceptableTag;

    [Tooltip("Should attached object be visible")]
    public bool makeAttachedVisible = true;

    [Tooltip("Name of this attachment point")]
    public string pointName = "Attachment Point";

    [Tooltip("Should attached object be rotated")]
    public bool rotateAttached = true;

    [Header("Gizmo Settings")]
    [Tooltip("Gizmo color")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);

    [Tooltip("Gizmo size")]
    public float gizmoSize = 0.05f;

    [Tooltip("Expected size of object (for gizmo)")]
    public Vector3 expectedObjectSize = new Vector3(0.05f, 0.05f, 0.05f);

    [Tooltip("Expected rotation of object (Euler)")]
    public Vector3 expectedObjectRotation = Vector3.zero;

    private bool isOccupied = false;
    private GameObject attachedObject;

    public bool IsOccupied()
    {
        return isOccupied;
    }

    public void AttachObject(GameObject obj)
    {
        if (isOccupied || obj == null)
            return;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Set layer to default
        obj.layer = LayerMask.NameToLayer("Default");

        // Parent the object
        obj.transform.SetParent(transform);

        // Set position and rotation
        obj.transform.localPosition = Vector3.zero;

        if (rotateAttached)
        {
            // Snap to the correct rotation immediately
            obj.transform.localRotation = Quaternion.Euler(expectedObjectRotation);
        }

        // Temporarily set collider to trigger
        Collider objCollider = obj.GetComponent<Collider>();
        if (objCollider != null)
        {
            objCollider.isTrigger = true;
        }

        // Set visibility
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = makeAttachedVisible;
        }

        isOccupied = true;
        attachedObject = obj;

        // Play attachment sound if available
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Update attached state in InteractableObject
        InteractableObject interactable = obj.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            interactable.isAttached = true;
        }
    }

    public GameObject DetachObject()
    {
        if (!isOccupied || attachedObject == null)
        {
            return null;
        }

        GameObject obj = attachedObject;
        obj.transform.parent = null;

        // Always make visible when detached
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        isOccupied = false;
        attachedObject = null;

        return obj;
    }

    public GameObject GetAttachedObject()
    {
        return attachedObject;
    }

    public bool CanAttach(GameObject obj)
    {
        if (isOccupied)
        {
            return false;
        }

        // Check tag
        if (string.IsNullOrEmpty(acceptableTag) || obj.CompareTag(acceptableTag))
        {
            InteractableObject interactable = obj.GetComponent<InteractableObject>();
            if (interactable != null && interactable.canAttach)
            {
                return true;
            }
        }

        return false;
    }

    public void HighlightAttachmentPoint(bool highlight)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                renderer.material.color = Color.green;
            }
            else
            {
                renderer.material.color = gizmoColor;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw attachment point gizmo
        Gizmos.color = isOccupied ? Color.red : gizmoColor;

        // Draw center point
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.5f);

        // Show expected object size
        if (!isOccupied)
        {
            // Draw wire cube with rotation
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Matrix4x4 newMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation * Quaternion.Euler(expectedObjectRotation),
                Vector3.one
            );
            Gizmos.matrix = newMatrix;

            // Draw wire cube
            Gizmos.DrawWireCube(Vector3.zero, expectedObjectSize);
            
            // Restore matrix
            Gizmos.matrix = oldMatrix;
        }
    }
}