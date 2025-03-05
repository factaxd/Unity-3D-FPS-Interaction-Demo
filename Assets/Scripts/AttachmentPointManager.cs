using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttachmentPointManager : MonoBehaviour
{
    [Tooltip("Bu nesnenin tüm takma noktalarını içeren liste")]
    public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
    
    [Tooltip("Otomatik olarak en yakın takma noktasını seç")]
    public bool autoSelectNearestPoint = true;
    
    // En son highlight edilen takma noktası
    private AttachmentPoint lastHighlightedPoint = null;
    
    private void Start()
    {
        // Eğer attachmentPoints boşsa, otomatik olarak alt nesnelerdeki AttachmentPoint'leri bul
        if (attachmentPoints.Count == 0)
        {
            attachmentPoints.AddRange(GetComponentsInChildren<AttachmentPoint>());
        }
    }
    
    // Kullanabilir takma noktalarını döndürür
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

    // Bu metot bir obje yaklaştığında uygun takma noktalarını vurgular
    public void HighlightAvailablePoints(GameObject obj, bool highlight)
    {
        // Eğer highlight kapatılıyorsa, tüm noktaların vurgusunu kaldır
        if (!highlight)
        {
            foreach (AttachmentPoint point in attachmentPoints)
            {
                point.HighlightAttachmentPoint(false);
            }
            lastHighlightedPoint = null;
            return;
        }
        
        // Objenin tag'ine uygun takma noktalarını vurgula
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
    
    // En yakın uygun takma noktasını bulur
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