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

    // En son highlight edilen takma noktası
    private AttachmentPoint lastHighlightedPoint = null;
    
    // Alt nesnelerdeki AttachmentPointManager'lar listesi
    private List<AttachmentPointManager> childManagers = new List<AttachmentPointManager>();
    
    // Yeni bir alt nesne AttachmentPointManager eklendiğinde haberdar olmak için event
    public System.Action<AttachmentPointManager> OnChildManagerAdded;

    private void Start()
    {
        // Eğer attachmentPoints boşsa, otomatik olarak alt nesnelerdeki AttachmentPoint'leri bul
        if (attachmentPoints.Count == 0)
        {
            attachmentPoints.AddRange(GetComponentsInChildren<AttachmentPoint>());
        }
        
        // Alt nesnelerdeki AttachmentPointManager'ları bul ve kaydet
        RefreshChildManagers();
    }
    
    // Yeni bir nesne attach edildiğinde çağrılacak metod
    public void OnObjectAttached(GameObject attachedObject)
    {
        // Yeni attach edilen nesne üzerinde AttachmentPointManager varsa, onu alt manager olarak ekle
        AttachmentPointManager childManager = attachedObject.GetComponent<AttachmentPointManager>();
        if (childManager != null && !childManagers.Contains(childManager))
        {
            childManagers.Add(childManager);
            if (OnChildManagerAdded != null)
            {
                OnChildManagerAdded.Invoke(childManager);
            }
        }
        
        // Attach edilen nesnenin içinde doğrudan AttachmentPoint varsa, bunları da listeme ekle
        AttachmentPoint[] childPoints = attachedObject.GetComponentsInChildren<AttachmentPoint>();
        foreach (AttachmentPoint point in childPoints)
        {
            if (!attachmentPoints.Contains(point))
            {
                attachmentPoints.Add(point);
            }
        }
    }
    
    // Alt nesnelerdeki tüm AttachmentPointManager'ları yeniden bul
    public void RefreshChildManagers()
    {
        childManagers.Clear();
        
        // Bu objeye bağlı tüm child objeleri kontrol et
        foreach (Transform child in transform)
        {
            AttachmentPointManager childManager = child.GetComponent<AttachmentPointManager>();
            if (childManager != null)
            {
                childManagers.Add(childManager);
            }
        }
    }

    // Kullanabilir takma noktalarını döndürür
    public List<AttachmentPoint> GetAvailablePoints(string attachableTag)
    {
        List<AttachmentPoint> availablePoints = new List<AttachmentPoint>();

        // Kendi attachment pointlerini kontrol et
        foreach (AttachmentPoint point in attachmentPoints)
        {
            if (!point.IsOccupied() && (string.IsNullOrEmpty(point.acceptableTag) || point.acceptableTag == attachableTag))
            {
                availablePoints.Add(point);
            }
        }
        
        // Alt managerlardaki attachment pointleri de kontrol et
        foreach (AttachmentPointManager childManager in childManagers)
        {
            availablePoints.AddRange(childManager.GetAvailablePoints(attachableTag));
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
            
            // Alt managerlardaki noktaların da vurgusunu kaldır
            foreach (AttachmentPointManager childManager in childManagers)
            {
                childManager.HighlightAvailablePoints(obj, false);
            }
            
            lastHighlightedPoint = null;
            return;
        }

        // Objenin tag'ine uygun takma noktalarını vurgula
        string objTag = obj.tag;
        bool foundHighlight = false;
        
        foreach (AttachmentPoint point in attachmentPoints)
        {
            if (point.CanAttach(obj))
            {
                point.HighlightAttachmentPoint(true);
                lastHighlightedPoint = point;
                foundHighlight = true;
            }
            else
            {
                point.HighlightAttachmentPoint(false);
            }
        }
        
        // Alt managerlardaki noktaları da vurgula
        foreach (AttachmentPointManager childManager in childManagers)
        {
            childManager.HighlightAvailablePoints(obj, highlight);
            
            // Eğer alt manager'da highlight edilmiş bir nokta varsa, onu da kontrol et
            AttachmentPoint childHighlightedPoint = childManager.GetLastHighlightedPoint();
            if (childHighlightedPoint != null)
            {
                lastHighlightedPoint = childHighlightedPoint;
                foundHighlight = true;
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

        // Kendi noktalarımı kontrol et
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
        
        // Alt managerlardaki noktaları da kontrol et
        foreach (AttachmentPointManager childManager in childManagers)
        {
            AttachmentPoint childNearestPoint = childManager.GetNearestAttachmentPoint(obj, position);
            if (childNearestPoint != null)
            {
                float distance = Vector3.Distance(childNearestPoint.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = childNearestPoint;
                }
            }
        }

        return nearestPoint;
    }
}