# Klavye Modlama Simülasyonu

Unity ile geliştirilmiş klavye modlama ve buildleme simülasyonu.

## Tag ve Layer Sistemi Hakkında

Bu proje için tag ve layer sistemi, nesneler arasındaki etkileşimi ve ilişkileri kontrol etmek için kullanılır. Bu sayede parçaların nereye takılabileceği, neye bırakılabileceği gibi kurallar belirlenebilir.

### Unity Tags (Etiketler)

Unity'de tag'ler GameObject'leri kategorize etmek için kullanılır. Bu projede kullanmanız gereken tag'ler:

1. **Surface**: Nesneleri bırakabileceğiniz herhangi bir yüzey (masa, tezgah vb.)
2. **KeyboardCase**: Klavye kasası
3. **PCB**: Devre kartı
4. **Stabilizer**: Stabilizer parçaları
5. **Switch**: Klavye switchleri
6. **Keycap**: Tuş kapakları

Bu tag'leri "Edit > Project Settings > Tags and Layers" menüsünden ekleyebilirsiniz.

### Tag Sistemi Nasıl Çalışır?

1. **SurfaceTag**: Nesneleri bırakabileceğiniz yüzeyleri işaretlemek için kullanılır. Unity Inspector kısmında tag'i "Surface" olarak ayarlamalısınız.

2. **InteractableObject**: Her etkileşimli nesneye eklenen bu bileşen, bir nesnenin hangi tür nesnelere takılabileceğini belirler:
   - `canAttach`: Nesne diğer nesnelere takılabilir mi?
   - `attachableToTag`: Bu nesne hangi tag'e sahip nesnelere takılabilir? Örneğin PCB parçası "KeyboardCase" tag'ine sahip nesnelere takılabilir.

3. **AttachmentPoint**: Takılma noktalarını belirler:
   - `acceptableTag`: Bu noktaya hangi tag'e sahip nesneler takılabilir? Örneğin klavye kasasındaki PCB yuvası için "PCB" tag'i belirlenmelidir.

### Örnek:

- Klavye kasasına (KeyboardCase tag) PCB'yi takmak istiyorsanız:
  1. Klavye kasasına bir AttachmentPoint ekleyin ve acceptableTag'i "PCB" olarak ayarlayın
  2. PCB nesnesine InteractableObject bileşeni ekleyin, canAttach = true ve attachableToTag = "KeyboardCase" olarak ayarlayın
  3. PCB'nin tag'ini "PCB" olarak ayarlayın

Bu şekilde, PCB'yi tutup klavye kasasına yaklaştırdığınızda, PCB'nin takılabileceği noktayı göreceksiniz ve E tuşuna basarak takabileceksiniz.

### Layer Sistemi

Layers, nesnelerin hangi gruba ait olduğunu belirler ve genellikle Raycast işlemleri için kullanılır:

- **Interactable**: Etkileşime girebileceğiniz nesneler için (tüm parçalar bu layer'da olmalı)
- **Default**: Etkileşime giremeyeceğiniz nesneler
- **Held**: Taşınan nesneler için (collision sorunlarını önlemek için)

Held layerini oluşturmak için:
1. Unity Editor'da "Edit > Project Settings > Tags and Layers" menüsüne gidin
2. Layers bölümünde boş bir slot'a "Held" ekleyin
3. Diğer layerlarla çakışmaması için Physics ayarlarına gitmelisiniz:
   - Unity Editor'da "Edit > Project Settings > Physics" menüsüne gidin
   - Layer Collision Matrix'te "Held" layerı ile "Interactable" layerı arasındaki işareti kaldırın

## Kontroller

- **WASD**: Hareket
- **Mouse**: Kamera kontrolü
- **E**: Nesne alma/bırakma/takma/çıkarma
- **Sol Shift**: Koşma
- **Space**: Zıplama
- **Mouse Scroll Wheel**: Tutulan nesneyi mevcut seçili eksende döndürme
- **X, Y, Z Tuşları**: Döndürme eksenini değiştirme

Varsayılan olarak nesneler Y ekseni etrafında döner. Döndürme eksenini X, Y veya Z tuşlarına basarak değiştirebilirsiniz.

## Yeni Özellikler

### 1. Nesne Rotasyonu Düzeltildi
- Artık nesneleri döndürdüğünüzde rotasyonları korunuyor
- Taşırken nesneler otomatik olarak kamera yönüne dönmüyor
- Mouse scroll ile döndürme daha doğal çalışıyor

### 2. Yerleştirilen Nesneleri Çıkarma
- Takılı nesnelere bakıp E tuşuna basarak onları tekrar alabilirsiniz
- Takılı nesneler outline ile vurgulanıyor ve "Press E to detach" metni görüntüleniyor

### 3. Otomatik Takma Noktası Seçimi
- Bir obje üzerinde birden fazla takma noktası olabilir
- En yakın uygun takma noktası artık otomatik olarak seçiliyor
- Tab tuşu ile elle seçim yapmaya gerek kalmadı

## Takma Noktaları Sistemi

Bir nesne üzerine birden fazla obje takabilmek için:

1. Nesnenizin (örneğin klavye kasası) GameObject'ine `AttachmentPointManager` bileşeni ekleyin
2. Nesne içine boş GameObject'ler ekleyip bunlara `AttachmentPoint` bileşeni ekleyin
3. Her `AttachmentPoint` için:
   - `pointName`: Takma noktasının adı (PCB Slot, Switch Socket 1, vb.)
   - `acceptableTag`: Bu noktaya hangi tag'li nesneler takılabilir?
   - `attachRotationOffset`: Nesnenin takılınca alacağı rotasyon açıları
   - Gizmo ayarlarını düzenleyerek takma noktasının görünümünü değiştirebilirsiniz

Takma işlemi için:
1. Nesneyi tutup takma noktalarına sahip başka bir nesneye yaklaştırın
2. E tuşuna basarak nesneyi en yakın uygun takma noktasına takın

Çıkartma işlemi için:
1. Takılı nesneye bakın (outline ile vurgulanacaktır)
2. E tuşuna basarak nesneyi çıkartın ve tutun

### AttachmentPointManager Ayarları

- **autoSelectNearestPoint**: Etkinleştirildiğinde, en yakın uygun takma noktası otomatik seçilir (varsayılan: true)
- **enableTabCycling**: Etkinleştirildiğinde, Tab tuşu ile takma noktaları arasında geçiş yapılabilir (varsayılan: false) 