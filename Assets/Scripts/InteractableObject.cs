using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Tooltip("Nesnenin ismi")]
    public string objectName;
    
    [Tooltip("Bu nesne alındığında diğer nesnelere takılabilir mi?")]
    public bool canAttach = false;
    
    [Tooltip("Bu nesne hangi tür nesnelere takılabilir?")]
    public string attachableToTag;
    
    [Tooltip("Nesne takıldığında yerleşeceği pozisyon offseti")]
    public Vector3 attachPositionOffset;
    
    [Tooltip("Nesne takıldığında alacağı rotasyon")]
    public Vector3 attachRotationOffset;
    
    [Header("Outline Settings")]
    [Tooltip("Outline rengi")]
    public Color outlineColor = new Color(0.18f, 0.8f, 1f, 1f); // Parlak mavi
    
    [Tooltip("Outline kalınlığı")]
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
            // Eğer zaten varsa, belirlenen renk ve kalınlığı uygula
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
        }
        
        // Başlangıçta outline'ı kapalı tut
        outline.enabled = false;
    }
    
    public void Pickup()
    {
        // Eğer nesne bir yere takılıysa, önce çıkar
        if (isAttached)
        {
            Detach();
        }
        
        // Fizik işlemlerini durdur
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Collider'ı trigger yap ki diğer objelerle çakışmasın
        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }
        
        // Tutulan nesneyi başka nesnelerin layer'ından farklı bir layer'a geçirebiliriz
        // Bu sayede taşırken ignore collision kullanabiliriz
        gameObject.layer = LayerMask.NameToLayer("Held"); // "Held" adında bir layer oluşturmanız gerekiyor
        
        // Orijinal pozisyon ve rotasyonu kaydet
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }
    
    public void Drop()
    {
        // Fizik işlemlerini devam ettir
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Trigger'ı kapat
        if (objectCollider != null)
        {
            objectCollider.isTrigger = false;
        }
        
        // Nesneyi tekrar Interactable layer'ına geri al
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        
        // Velocity'yi sıfırla ki düşerken anormal hareketler yapmasın
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    public void Attach(Transform parent)
    {
        if (!canAttach) return;
        
        // Değişik offsetleri önlemek için önce parent'ı ayarlamadan önce dünya koordinatlarındaki pozisyonu kaydet
        Vector3 worldPosition = parent.position;
        Quaternion worldRotation = parent.rotation;
        
        // Nesneyi parent'a takla
        transform.SetParent(parent);
        
        // Takılma noktasına snap et - anında geçiş yap
        transform.localPosition = attachPositionOffset;
        transform.localRotation = Quaternion.Euler(attachRotationOffset);
        
        isAttached = true;
        attachedTo = parent;
        
        // Fizik işlemleri durdur
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Fizik çakışmalarını önlemek için trigger kullan
        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }
        
        // Takılma sesi oynatmak için (opsiyonel)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
    
    public void Detach()
    {
        if (!isAttached) return;
        
        // Takılı olduğumuz yerin pozisyonunu ve yönünü hatırla
        Vector3 attachmentPosition = transform.position;
        Vector3 attachmentForward = transform.parent ? transform.parent.forward : Vector3.forward;
        Vector3 attachmentUp = transform.parent ? transform.parent.up : Vector3.up;
        
        // Takılı olduğu bir AttachmentPoint varsa, ona boşaldığını bildir
        AttachmentPoint attachmentPoint = transform.parent?.GetComponent<AttachmentPoint>();
        if (attachmentPoint != null)
        {
            attachmentPoint.DetachObject();
        }
        
        // Nesneyi parent'tan ayır
        transform.SetParent(null);
        
        // Çıkan objeyi hafif öne ve yukarı konumlandır (daha kolay seçilebilmesi için)
        transform.position = attachmentPosition + (attachmentForward * 0.05f) + (attachmentUp * 0.02f);
        
        // Fizik özelliklerini yeniden aktif et
        if (rb != null)
        {
            // ÖNEMLİ: İlk kinematic/gravity durumunu değiştir, sonra velocity işlemleri yap (sıralama önemli)
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        // Collider'ı yeniden aktif et
        if (objectCollider != null)
        {
            objectCollider.isTrigger = false;
        }
        
        // Layer'ı interactable olarak değiştir
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