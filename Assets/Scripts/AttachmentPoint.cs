using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    [Tooltip("Bu noktaya hangi tür nesneler takılabilir?")]
    public string acceptableTag;
    
    [Tooltip("Takılan nesne burada görünür mü?")]
    public bool makeAttachedVisible = true;
    
    [Tooltip("Bu takma noktasının ismi")]
    public string pointName = "Attachment Point";
    
    [Tooltip("Takılan nesne rotate edilecek mi?")]
    public bool rotateAttached = true;
    
    [Header("Gizmo Settings")]
    [Tooltip("Gizmo rengi")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    [Tooltip("Gizmo boyutu")]
    public float gizmoSize = 0.05f;
    
    [Tooltip("Takma noktasının yönünü gösterir ok")]
    public bool showDirectionArrow = true;
    
    [Tooltip("Takılacak objenin tahmini boyutu (gizmo için)")]
    public Vector3 expectedObjectSize = new Vector3(0.05f, 0.05f, 0.05f);
    
    [Tooltip("Takılacak objenin olması beklenen rotasyonu (Euler)")]
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

        // Nesneyi parent olarak ayarla
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // ÖNEMLİ: Önce kinematic yap, sonra velocity işlemleri yapma
            rb.isKinematic = true;
            rb.useGravity = false;
            // Kinematic bir cismin velocity değerini değiştirmek hata veriyor
            // Bu satırları kaldırıyoruz
        }

        // Layer'ı interactable olmaktan çıkar
        obj.layer = LayerMask.NameToLayer("Default");
        
        // Nesneyi parent yap
        obj.transform.SetParent(transform);
        
        // Pozisyon ve rotasyonu ayarla
        obj.transform.localPosition = Vector3.zero;
        
        if (rotateAttached)
        {
            // Anında doğru rotasyona snap et
            obj.transform.localRotation = Quaternion.Euler(expectedObjectRotation);
        }
        
        // Collider'ı geçici olarak kapat, diğer objelerle çakışmasını önle
        Collider objCollider = obj.GetComponent<Collider>();
        if (objCollider != null)
        {
            // Parent objemiz varsa, onun collider'ı ile çakışmayı önlemek için
            // geçici olarak trigger moduna al
            objCollider.isTrigger = true;
        }
        
        // Nesneyi görünür/görünmez yap
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = makeAttachedVisible;
        }
        
        isOccupied = true;
        attachedObject = obj;
        
        // Başarılı takılma sesi çal (isteğe bağlı, sesi sisteminize ekleyebilirsiniz)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // InteractableObject bileşenini bul ve attached durumunu güncelle
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
        
        // Nesneyi ayrıştır
        GameObject obj = attachedObject;
        obj.transform.parent = null;
        
        // Nesneyi herzaman görünür yap
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
        
        // Tag kontrolü yap
        if (string.IsNullOrEmpty(acceptableTag) || obj.CompareTag(acceptableTag))
        {
            // Burada nesnenin takılabilir olup olmadığını kontrol et
            InteractableObject interactable = obj.GetComponent<InteractableObject>();
            if (interactable != null && interactable.canAttach)
            {
                return true;
            }
        }
        
        return false;
    }

    // Bu metot takma noktasına yaklaşıldığında çağrılabilir
    public void HighlightAttachmentPoint(bool highlight)
    {
        // Unity Editor'da çalışırken takma noktalarını highlight etmek
        // için özel bir materyal ataması yapılabilir
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                // Highlight rengi
                renderer.material.color = Color.green;
            }
            else
            {
                // Normal renk
                renderer.material.color = gizmoColor;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Takma noktası gizmosu çiz
        Gizmos.color = isOccupied ? Color.red : gizmoColor;
        
        // Önce merkez noktayı çiz
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.5f);
        
        // Takılacak objenin boyutunu göster
        if (!isOccupied)
        {
            // Rotasyonu hesaba katarak wire küp çiz
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Matrix4x4 newMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation * Quaternion.Euler(expectedObjectRotation),
                Vector3.one
            );
            Gizmos.matrix = newMatrix;
            
            // Wire küp çiz
            Gizmos.DrawWireCube(Vector3.zero, expectedObjectSize);
            
            // Z-ekseni (forward) yönünü belirt
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (expectedObjectSize.z * 0.6f));
            Gizmos.DrawLine(Vector3.forward * (expectedObjectSize.z * 0.6f), 
                           Vector3.forward * (expectedObjectSize.z * 0.5f) + Vector3.right * (expectedObjectSize.x * 0.1f));
            Gizmos.DrawLine(Vector3.forward * (expectedObjectSize.z * 0.6f), 
                           Vector3.forward * (expectedObjectSize.z * 0.5f) + Vector3.left * (expectedObjectSize.x * 0.1f));
            
            // X-ekseni (right) yönünü belirt
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.right * (expectedObjectSize.x * 0.6f));
            
            // Y-ekseni (up) yönünü belirt
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * (expectedObjectSize.y * 0.6f));
            
            // Matrix'i geri yükle
            Gizmos.matrix = oldMatrix;
        }
        
        // Takma noktasının yönünü göster
        if (showDirectionArrow)
        {
            Gizmos.color = Color.blue;
            Vector3 direction = transform.forward * gizmoSize;
            Gizmos.DrawRay(transform.position, direction);
            // Ok başını çiz
            Vector3 right = transform.right * (gizmoSize * 0.25f);
            Vector3 up = transform.up * (gizmoSize * 0.25f);
            Vector3 arrowPos = transform.position + direction;
            Gizmos.DrawRay(arrowPos, -direction * 0.25f + right);
            Gizmos.DrawRay(arrowPos, -direction * 0.25f - right);
            Gizmos.DrawRay(arrowPos, -direction * 0.25f + up);
            Gizmos.DrawRay(arrowPos, -direction * 0.25f - up);
        }
    }
} 