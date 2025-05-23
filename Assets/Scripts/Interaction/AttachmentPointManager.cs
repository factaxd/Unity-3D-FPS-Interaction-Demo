using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttachmentPointManager : MonoBehaviour
{
    [Tooltip("List of attachment points on this object")]
    public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();

    [Tooltip("Automatically select nearest attachment point")]
    public bool autoSelectNearestPoint = true;

    private AttachmentPoint lastHighlightedPoint = null;

    private void Start()
    {
        if (attachmentPoints.Count == 0)
        {
            attachmentPoints.AddRange(GetComponentsInChildren<AttachmentPoint>());
        }
    }

    public List<AttachmentPoint> GetAvailablePoints(string attachableTag)
    {
        List<AttachmentPoint> availablePoints = new List<AttachmentPoint>();

        foreach (AttachmentPoint point in attachmentPoints)
        {
            if (!point.IsOccupied() && (string.IsNullOrEmpty(point.acceptableTag) || point.acceptableTag == attachableTag))
            {
                availablePoints.Add(point);
            }
        }

        return availablePoints;
    }

    public void HighlightAvailablePoints(GameObject obj, bool highlight)
    {
        if (!highlight)
        {
            foreach (AttachmentPoint point in attachmentPoints)
            {
                point.HighlightAttachmentPoint(false);
            }
            lastHighlightedPoint = null;
            return;
        }

        string objTag = obj.tag;
        foreach (AttachmentPoint point in attachmentPoints)
        {
            if (point.CanAttach(obj))
            {
                point.HighlightAttachmentPoint(true);
                lastHighlightedPoint = point;
            }
            else
            {
                point.HighlightAttachmentPoint(false);
            }
        }
    }

    public AttachmentPoint GetLastHighlightedPoint()
    {
        return lastHighlightedPoint;
    }

    public AttachmentPoint GetNearestAttachmentPoint(GameObject obj, Vector3 position)
    {
        AttachmentPoint nearestPoint = null;
        float minDistance = float.MaxValue;

        foreach (AttachmentPoint point in attachmentPoints)
        {
            if (point.CanAttach(obj))
            {
                float distance = Vector3.Distance(point.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = point;
                }
            }
        }

        return nearestPoint;
    }
}