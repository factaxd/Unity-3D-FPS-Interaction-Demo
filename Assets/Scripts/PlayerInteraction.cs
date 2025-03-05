using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Etkileşim mesafesi")]
    public float interactionDistance = 3f;
    
    [Tooltip("Tuttuğumuz nesnenin bizden uzaklığı")]
    public float holdDistance = 2f;
    
    [Tooltip("Nesne sorunsuz hareket etmesi için ne kadar hızlı takip etsin")]
    public float moveSpeed = 10f;
    
    [Tooltip("Crosshair'in merkezde olduğu Layerlar")]
    public LayerMask interactionLayer;
    
    [Header("UI References")]
    [Tooltip("Etkileşim metni için UI elemanı")]
    public TextMeshProUGUI interactionText;
    
    [Tooltip("Crosshair UI elemanı")]
    public Image crosshair;
    
    [Header("Debug")]
    public bool showDebugRay = true;
    
    [Header("Rotation Settings")]
    [Tooltip("Fare tekerleğiyle döndürme hızı")]
    public float rotationSpeed = 100f;
    
    [Tooltip("Hangi eksende döndürüleceği")]
    public RotationAxis currentRotationAxis = RotationAxis.Y;
    
    public enum RotationAxis
    {
        X, Y, Z
    }
    
    // Private variables
    private Camera playerCamera;
    private InteractableObject currentInteractable;
    private InteractableObject heldObject;
    private bool isHolding = false;
    private Vector3 targetPosition;
    
    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // UI referansları control et
        if (interactionText == null)
        {
            Debug.LogWarning("Interaction text is not assigned!");
        }
        
        if (crosshair == null)
        {
            Debug.LogWarning("Crosshair image is not assigned!");
        }
        
        // Başlangıçta etkileşim metni yok
        if (interactionText != null)
        {
            interactionText.text = "";
        }
    }
    
    private void Update()
    {
        HandleRaycast();
        HandleInput();
        MoveHeldObject();
        RotateHeldObject();
    }
    
    private void HandleRaycast()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
        }
        
        // ÖNEMLİ DEĞİŞİKLİK: Önce tüm katmanlarda raycast yap
        // Bu, takılı objelerin (farklı layer'da olsalar bile) seçilebilmesini sağlar
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // 1. Önce direkt hit eden objeyi kontrol et
            InteractableObject directInteractable = hit.collider.GetComponent<InteractableObject>();
            if (directInteractable != null)
            {
                // Direkt hit edilen obje interactable ise, onu seç
                UpdateCurrentInteractable(directInteractable);
                return;
            }
            
            // 2. AttachmentPoint'e bağlı objeleri kontrol et
            AttachmentPoint attachPoint = hit.collider.GetComponent<AttachmentPoint>();
            if (attachPoint != null && attachPoint.IsOccupied())
            {
                GameObject attachedObj = attachPoint.GetAttachedObject();
                if (attachedObj != null)
                {
                    InteractableObject interactable = attachedObj.GetComponent<InteractableObject>();
                    if (interactable != null)
                    {
                        UpdateCurrentInteractable(interactable);
                        return;
                    }
                }
            }
            
            // 3. Hit edilen nesnenin tüm parent ve child objelerini kontrol et (derinlemesine)
            InteractableObject foundInteractable = FindInteractableInHierarchy(hit.transform);
            if (foundInteractable != null)
            {
                UpdateCurrentInteractable(foundInteractable);
                return;
            }
                
            // 4. AttachmentPointManager ve yüzey kontrolleri
            if (isHolding)
            {
                ProcessNonInteractableHit(hit);
            }
            else
            {
                ClearInteraction();
            }
        }
        else
        {
            // Hiçbir şeye bakmıyoruz
            if (!isHolding)
            {
                ClearInteraction();
            }
            else if (interactionText != null)
            {
                interactionText.text = "Press E to drop " + heldObject.objectName;
            }
        }
    }
    
    // Hiyerarşide interactable bir obje arar
    private InteractableObject FindInteractableInHierarchy(Transform startTransform)
    {
        if (startTransform == null) return null;
        
        // Önce kendisini kontrol et
        InteractableObject interactable = startTransform.GetComponent<InteractableObject>();
        if (interactable != null) return interactable;
        
        // 1. Yukarı doğru (parent'lara) bak
        Transform parent = startTransform.parent;
        while (parent != null)
        {
            interactable = parent.GetComponent<InteractableObject>();
            if (interactable != null) return interactable;
            
            // Kardeş objeleri de kontrol et (aynı parent altındaki diğer objeler)
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling != startTransform)
                {
                    interactable = sibling.GetComponent<InteractableObject>();
                    if (interactable != null) return interactable;
                    
                    // Kardeşin child'larını da kontrol et (1 seviye derinlik)
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
        
        // 2. Aşağı doğru (child'lara) bak
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
    
    // InteractableObject olmayan bir hit durumunda çağrılır (tutma durumunda)
    private void ProcessNonInteractableHit(RaycastHit hit)
    {
        // AttachmentPointManager kontrolü
        AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
        if (pointManager == null)
        {
            // Belki parent objede olabilir
            pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
        }
        
        if (pointManager != null)
        {
            pointManager.HighlightAvailablePoints(heldObject.gameObject, true);
            
            if (interactionText != null)
            {
                interactionText.text = "Press E to attach " + heldObject.objectName;
            }
        }
        // Yüzey kontrolü
        else if (hit.collider.CompareTag("Surface"))
        {
            if (interactionText != null)
            {
                interactionText.text = "Press E to place " + heldObject.objectName;
            }
        }
        else
        {
            if (interactionText != null)
            {
                interactionText.text = "Press E to drop " + heldObject.objectName;
            }
        }
    }
    
    // Mevcut etkileşimli nesneyi günceller
    private void UpdateCurrentInteractable(InteractableObject interactable)
    {
        // Eğer zaten tutmuyorsak
        if (!isHolding)
        {
            // Eğer nesne bir takma noktasına takılıysa, highlight et ve çıkarma seçeneği göster
            if (interactable.isAttached)
            {
                // Önceki outline'ı kapat
                if (currentInteractable != null && currentInteractable != interactable)
                {
                    currentInteractable.ShowOutline(false);
                }
                
                // Takılı olan nesneyi highlight et
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);
                
                // UI'ı güncelle - çıkartma seçeneği göster
                if (interactionText != null)
                {
                    interactionText.text = "Press E to detach " + currentInteractable.objectName;
                }
            }
            // Normal interactable nesnesi ise, normal highlight işlemine devam et
            else if (currentInteractable != interactable)
            {
                // Önceki outline'ı kapat
                if (currentInteractable != null)
                {
                    currentInteractable.ShowOutline(false);
                }
                
                // Yeni nesneyi highlight et
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);
                
                // UI'ı güncelle
                if (interactionText != null)
                {
                    interactionText.text = "Press E to pick up " + currentInteractable.objectName;
                }
            }
        }
        // Eğer bir nesne tutuyorsak ve baktığımız nesne tuttuğumuz nesneden farklıysa
        else if (interactable != heldObject)
        {
            // Normal interactable nesneler için
            if (currentInteractable != interactable)
            {
                // Önceki outline'ı kapat
                if (currentInteractable != null && currentInteractable != heldObject)
                {
                    currentInteractable.ShowOutline(false);
                }
                
                // Yeni nesneyi highlight et
                currentInteractable = interactable;
                currentInteractable.ShowOutline(true);
                
                // Bu nesnenin AttachmentPointManager'ını kontrol et
                AttachmentPointManager pointManager = interactable.GetComponent<AttachmentPointManager>();
                if (pointManager != null)
                {
                    // Uygun takma noktalarını vurgula
                    pointManager.HighlightAvailablePoints(heldObject.gameObject, true);
                    
                    // UI'ı güncelle
                    if (interactionText != null)
                    {
                        interactionText.text = "Press E to attach " + heldObject.objectName;
                    }
                }
                else
                {
                    // UI'ı güncelle
                    if (interactionText != null)
                    {
                        interactionText.text = "Press E to attach " + heldObject.objectName + " to " + currentInteractable.objectName;
                    }
                }
            }
        }
    }
    
    private void HandleInput()
    {
        // E tuşu ile etkileşim
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Eğer bir nesne tutmuyorsak ve etkileşimli bir nesneye bakıyorsak
            if (!isHolding && currentInteractable != null)
            {
                // Eğer nesne bir yere takılıysa, çıkar
                if (currentInteractable.isAttached)
                {
                    // Takılı olduğu yerden çıkar ve eline al
                    currentInteractable.Detach();
                    PickupObject(currentInteractable);
                }
                else
                {
                    // Normal olarak nesneyi al
                    PickupObject(currentInteractable);
                }
            }
            // Eğer bir nesne tutuyorsak
            else if (isHolding)
            {
                RaycastHit hit;
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                
                // Raycast yaparak bırakabileceğimiz bir yere mi bakıyoruz kontrol et
                if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
                {
                    // Direkt AttachmentPoint'e bakıyorsak
                    AttachmentPoint attachmentPoint = hit.collider.GetComponent<AttachmentPoint>();
                    if (attachmentPoint != null && attachmentPoint.CanAttach(heldObject.gameObject))
                    {
                        // Tutulan nesneyi takma noktasına tak
                        AttachToPoint(attachmentPoint);
                        return;
                    }
                    
                    // AttachmentPointManager kontrol et
                    AttachmentPointManager pointManager = hit.collider.GetComponent<AttachmentPointManager>();
                    if (pointManager == null)
                    {
                        // Belki parent objede olabilir
                        pointManager = hit.collider.GetComponentInParent<AttachmentPointManager>();
                    }
                    
                    if (pointManager != null)
                    {
                        // Otomatik olarak en yakın takma noktasını seç
                        if (pointManager.autoSelectNearestPoint)
                        {
                            // En yakın uygun takma noktasını bul
                            AttachmentPoint nearestPoint = pointManager.GetNearestAttachmentPoint(heldObject.gameObject, heldObject.transform.position);
                            
                            if (nearestPoint != null)
                            {
                                // Tutulan nesneyi en yakın takma noktasına tak
                                AttachToPoint(nearestPoint);
                                return;
                            }
                        }
                    }
                    
                    // Normal bir obje, AttachmentPoint veya AttachmentPointManager değilse
                    if (hit.collider.CompareTag("Surface"))
                    {
                        PlaceObject(hit.point);
                    }
                    else
                    {
                        DropObject();
                    }
                }
                else
                {
                    // Hiçbir şeye bakmıyorsak nesneyi düşür
                    DropObject();
                }
            }
        }
        
        // Rotasyon ekseni değiştirme
        if (Input.GetKeyDown(KeyCode.X))
        {
            currentRotationAxis = RotationAxis.X;
        }
        else if (Input.GetKeyDown(KeyCode.Y))
        {
            currentRotationAxis = RotationAxis.Y;
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            currentRotationAxis = RotationAxis.Z;
        }
    }
    
    private void MoveHeldObject()
    {
        if (isHolding && heldObject != null)
        {
            // Kamera önüne doğru belirli mesafede bir pozisyon belirle
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            
            // Smooth hareket için lerp kullan
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * moveSpeed);
            
            // Objeyi SADECE ilk tutulduğunda kamera yönüne çevir, sonra elle döndürülebilsin
            // Sürekli kamera rotasyonunu takip etmeyi kaldırdık
            // Böylece scroll ile döndürme sorunları çözülecek
        }
    }
    
    private void RotateHeldObject()
    {
        if (isHolding && heldObject != null)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (scrollInput != 0f)
            {
                Vector3 rotationAxis = Vector3.up; // Varsayılan olarak Y ekseni
                
                // Mevcut seçili eksende döndür
                switch (currentRotationAxis)
                {
                    case RotationAxis.X:
                        rotationAxis = heldObject.transform.right; // Objenin kendi X ekseninde döndür
                        break;
                    case RotationAxis.Y:
                        rotationAxis = heldObject.transform.up; // Objenin kendi Y ekseninde döndür
                        break;
                    case RotationAxis.Z:
                        rotationAxis = heldObject.transform.forward; // Objenin kendi Z ekseninde döndür
                        break;
                }
                
                // Mouse scroll değerine göre döndür - world space yerine local space kullan
                heldObject.transform.Rotate(rotationAxis, scrollInput * rotationSpeed, Space.Self);
            }
        }
    }
    
    private void PickupObject(InteractableObject obj)
    {
        heldObject = obj;
        heldObject.Pickup();
        
        // Nesneyi kameraya yerleştir
        heldObject.transform.SetParent(null); // Önce herhangi bir parent varsa temizle
        
        // İlk tutulduğunda rotasyonu kamera ile aynı yöne ayarla (opsiyonel)
        // Ancak orijinal rotasyonu korumak da mümkün
        // heldObject.transform.rotation = playerCamera.transform.rotation;
        
        isHolding = true;
        
        // UI güncelle
        if (interactionText != null)
        {
            interactionText.text = "Press E to place " + heldObject.objectName;
        }
    }
    
    private void DropObject()
    {
        if (heldObject != null)
        {
            // Nesnelerin çakışmasını önlemek için ilk önce uygun pozisyona getir
            Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            
            // Raycast ile kontrol et, nesnelerin içinden geçiyor mu?
            RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, holdDistance * 1.5f);
            bool canPlaceHere = true;
            
            foreach (RaycastHit hit in hits)
            {
                // Kendisi hariç başka bir interactable nesne ile çakışıyor mu?
                if (hit.collider.gameObject != heldObject.gameObject && 
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable"))
                {
                    canPlaceHere = false;
                    break;
                }
            }
            
            // Eğer uygun bir yer ise bırak, değilse biraz daha ileri pozisyona bırak
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
        ClearInteraction();
    }
    
    private void PlaceObject(Vector3 position)
    {
        if (heldObject != null)
        {
            // Nesneyi belirli pozisyona yerleştir, ancak oyuncunun önünde tutma mesafesini koru
            Vector3 placementPosition = position;
            // Eğer herhangi bir yüzeye çarpmadan bırakıyorsa, kamera önündeki mevcut pozisyonu kullan
            if (position == Vector3.zero)
            {
                placementPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            }
            
            // Raycast ile kontrol et, bu pozisyonda nesne başka bir nesne ile çakışıyor mu?
            Collider[] colliders = Physics.OverlapSphere(placementPosition, heldObject.GetComponent<Collider>().bounds.extents.magnitude * 0.8f);
            bool canPlaceHere = true;
            
            foreach (Collider col in colliders)
            {
                // Kendisi hariç başka bir interactable nesne ile çakışıyor mu?
                if (col.gameObject != heldObject.gameObject && 
                    col.gameObject.layer == LayerMask.NameToLayer("Interactable"))
                {
                    canPlaceHere = false;
                    break;
                }
            }
            
            // Eğer uygun bir yer ise bırak, değilse tutmaya devam et
            if (canPlaceHere)
            {
                heldObject.transform.position = placementPosition;
                heldObject.Drop();
                heldObject.ShowOutline(false);
                heldObject = null;
                isHolding = false;
                ClearInteraction();
            }
            else
            {
                // Eğer bırakılamıyorsa, kullanıcıya bildir
                if (interactionText != null)
                {
                    interactionText.text = "Cannot place here, try another spot";
                }
            }
        }
        else
        {
            isHolding = false;
            ClearInteraction();
        }
    }
    
    private void AttachObject(Transform parent)
    {
        if (heldObject != null)
        {
            heldObject.Attach(parent);
            heldObject.ShowOutline(false);
            heldObject = null;
        }
        
        isHolding = false;
        ClearInteraction();
    }
    
    private void AttachToPoint(AttachmentPoint point)
    {
        heldObject.Attach(point.transform);
        point.AttachObject(heldObject.gameObject);
        heldObject.ShowOutline(false);
        heldObject = null;
        isHolding = false;
        ClearInteraction();
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