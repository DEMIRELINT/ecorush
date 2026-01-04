using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Eco-Rush - Oyuncu Kontrol Scripti
/// Bu script, oyuncu karakterinin hareketini, dönüşünü, ateş etmesini ve arkasına çim bırakmasını yönetir.
/// Gereksinimler: Rigidbody, FixedUpdate, Quaternion, Instantiate, DeltaTime
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [Tooltip("Karakterin hareket hızı")]
    public float moveSpeed = 5f;

    [Header("Prefab Referansları")]
    [Tooltip("Ateş edilecek arı mermisi prefab'ı")]
    public GameObject bulletPrefab;
    
    [Tooltip("Arkaya bırakılacak çim prefab'ı")]
    public GameObject grassPrefab;
    
    [Tooltip("Yerleştirilecek ağaç prefab'ı")]
    public GameObject treePrefab;

    [Header("Ateş Ayarları (Fare Kontrolü)")]
    [Tooltip("Merminin karakterin ne kadar önünden çıkacağı")]
    public float bulletSpawnOffset = 1.5f;
    
    [Tooltip("Merminin başlangıç hızı")]
    public float bulletSpeed = 10f;
    
    [Tooltip("Merminin yerden yüksekliği (düşman boyuna göre ayarla)")]
    public float bulletHeight = 0.3f;
    
    [Tooltip("Ana kamera referansı (boş bırakılırsa otomatik bulunur)")]
    public Camera mainCamera;

    [Header("Çim Bırakma Ayarları (Tile Painting)")]
    [Tooltip("Çim Y pozisyonu (zemine yapışık)")]
    public float grassYPosition = 0.01f;
    
    [Header("Ağaç Yerleştirme Ayarları")]
    [Tooltip("Ağaç yerleştirme maliyeti (puan)")]
    public int treeCost = 50;
    
    [Tooltip("Ağaç Y pozisyonu")]
    public float treeYPosition = 0.5f;

    [Header("Animasyon")]
    [Tooltip("Karakterin Animator bileşeni")]
    public Animator animator;

    // ============== PRIVATE DEĞİŞKENLER ==============
    // Rigidbody referansı - Fizik tabanlı hareket için gerekli
    private Rigidbody rb;
    
    // Hareket yönü vektörü
    private Vector3 movementDirection;
    
    // ============== FARE İLE NİŞAN ALMA ==============
    // Farenin dünya pozisyonundaki hedef noktası
    private Vector3 mouseWorldPosition;
    
    // Karakterden fareye olan yön vektörü (ateş yönü)
    private Vector3 aimDirection;
    
    // ============== TILE PAINTING SİSTEMİ ==============
    // Son çim bırakılan grid koordinatları (tam sayı X ve Z)
    // Vector2Int kullanarak sadece X ve Z eksenlerini takip ediyoruz
    private Vector2Int lastTileCoordinate;
    
    // İlk çim bırakılıp bırakılmadığını takip et
    private bool hasInitialTile = false;
    
    // ============== ÇAKIŞMA KONTROLÜ (OVERLAP PREVENTION) ==============
    // Daha önce çim bırakılan koordinatları tutan HashSet
    // HashSet kullanıyoruz çünkü O(1) arama performansı sağlar
    // Aynı koordinata asla iki kez çim bırakılmaz
    private HashSet<Vector2Int> paintedTiles = new HashSet<Vector2Int>();

    /// <summary>
    /// Başlangıç fonksiyonu - Component referanslarını al
    /// </summary>
    void Start()
    {
        // Rigidbody component'ini al - Fizik hareketleri için zorunlu
        rb = GetComponent<Rigidbody>();
        
        // Rigidbody bulunamazsa hata ver
        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody component'i bulunamadı! Lütfen karaktere Rigidbody ekleyin.");
        }
        
        // ============== BAŞLANGIÇ TILE KOORDİNATINI HESAPLA ==============
        // Karakterin başlangıç pozisyonunu en yakın tam sayıya yuvarla
        lastTileCoordinate = GetCurrentTileCoordinate();
        
        // ============== KAMERA REFERANSINI BUL ==============
        // Eğer Inspector'dan atanmamışsa, ana kamerayı otomatik bul
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: Ana kamera bulunamadı! Lütfen kamerayı atayın.");
        }
    }

    /// <summary>
    /// Her frame çağrılır - Input okuma ve ateş etme işlemleri burada
    /// </summary>
    void Update()
    {
        // ============== INPUT OKUMA (NEW INPUT SYSTEM) ==============
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
        }
        
        // Hareket yönünü Vector3 olarak oluştur (Top-Down için X ve Z eksenleri)
        movementDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // ============== FARE İLE NİŞAN ALMA ==============
        // Farenin ekran pozisyonunu dünya koordinatlarına çevir
        UpdateMouseAim();
        
        // ============== ATEŞ ETME (SOL TIK - NEW INPUT SYSTEM) ==============
        // LeftButton.wasPressedThisFrame sol tık basıldığında bir seferliğine true döner
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            FireBullet();
        }

        // ============== TILE PAINTING - ÇİM BIRAKMA SİSTEMİ ==============
        // Her frame mevcut tile koordinatını kontrol et
        CheckAndSpawnGrass();
        
        // ============== AĞAÇ YERLEŞTİRME (Q TUŞU - NEW INPUT SYSTEM) ==============
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            PlaceTree();
        }
    }

    /// <summary>
    /// Fizik güncellemesi - Sabit aralıklarla çağrılır (varsayılan 0.02 saniye)
    /// ÖNEMLI: Rigidbody hareketleri burada yapılmalı, aksi halde titreme olur!
    /// </summary>
    void FixedUpdate()
    {
        // ============== FİZİK TABANLI HAREKET ==============
        // Rigidbody varsa ve hareket yönü belirlenmişse hareket et
        if (rb != null && movementDirection.magnitude > 0.1f)
        {
            // Yeni hız vektörünü hesapla
            Vector3 newVelocity = movementDirection * moveSpeed;
            
            // Y eksenindeki hızı koru (yerçekimi için)
            newVelocity.y = rb.linearVelocity.y;
            
            // Rigidbody hızını ayarla
            rb.linearVelocity = newVelocity;

            // ============== KARAKTER DÖNÜŞÜ (QUATERNION) ==============
            // Quaternion.LookRotation kullanarak karakteri hareket yönüne döndür
            // Bu fonksiyon, verilen ileri yön vektörüne göre bir rotasyon oluşturur
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            
            // Yumuşak dönüş için Slerp kullan (Spherical Linear Interpolation)
            // Bu, karakterin aniden dönmesini önler ve daha doğal görünür
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
        else if (rb != null)
        {
            // Hareket inputu yoksa yatay hızı sıfırla (düşme hızını koru)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // ============== ANIMASYON PARAMETRELERINI GÜNCELLE ==============
        if (animator != null)
        {
            // Karakter hareket ediyorsa (vektör uzunluğu > 0.1) yürüme animasyonu çalışır
            bool isWalking = movementDirection.magnitude > 0.1f;
            animator.SetBool("isWalking", isWalking);
        }
        else
        {
            // Eğer animator kutusu boşsa her 2 saniyede bir hata logu bas ki kullanıcı fark etsin
            if (Time.frameCount % 120 == 0) 
            {
                Debug.LogWarning("<color=yellow>PLAYER INFO:</color> Animator kutusu boş! Karakterin canlanması için görseli (Beard_man) Inspector'daki Animator kutusuna sürükle.");
            }
        }
    }

    /// <summary>
    /// Fare pozisyonunu dünya koordinatlarına çevir ve nişan yönünü hesapla
    /// Bu fonksiyon Top-Down kamera için Raycast kullanır
    /// </summary>
    void UpdateMouseAim()
    {
        // Kamera yoksa çık
        if (mainCamera == null) return;
        
        // ============== FARE POZİSYONUNDAN RAYCAST AT (NEW INPUT SYSTEM) ==============
        if (Mouse.current == null) return;
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        
        // Raycast sonucu için RaycastHit yapısı
        RaycastHit hit;
        
        // ============== ZEMİN İLE KESİŞİM NOKTASINI BUL ==============
        // Zemini temsil eden görünmez bir düzlem oluştur (Y = karakterin Y pozisyonu)
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        
        // Ray'in düzlemle kesişim noktasını hesapla
        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Farenin dünya pozisyonunu al
            mouseWorldPosition = ray.GetPoint(rayDistance);
            
            // ============== NİŞAN YÖNÜNÜ HESAPLA ==============
            // Karakterden fareye olan yön vektörü
            aimDirection = mouseWorldPosition - transform.position;
            aimDirection.y = 0f; // Yatay düzlemde kal
            aimDirection.Normalize(); // Birim vektöre çevir
        }
    }

    /// <summary>
    /// Mermi ateşleme fonksiyonu
    /// Fare yönüne doğru BulletPrefab'ı Instantiate eder
    /// </summary>
    void FireBullet()
    {
        // Bullet prefab'ı atanmış mı kontrol et
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerController: BulletPrefab atanmamış!");
            return;
        }
        
        // Nişan yönü geçerli mi kontrol et
        if (aimDirection.magnitude < 0.1f)
        {
            Debug.LogWarning("PlayerController: Geçerli bir nişan yönü yok!");
            return;
        }

        // ============== MERMİ POZİSYONU HESAPLAMA ==============
        // Merminin spawn pozisyonu = Karakterin pozisyonu + (Nişan yönü * offset mesafesi)
        Vector3 spawnPosition = transform.position + aimDirection * bulletSpawnOffset;
        
        // Mermi yüksekliğini ayarla (Inspector'dan değiştirilebilir)
        // Düşman boyuna göre bu değeri ayarla (varsayılan: 0.3f)
        spawnPosition.y = bulletHeight;
        
        // ============== MERMİ ROTASYONU (QUATERNION) ==============
        // Merminin dönüşünü nişan yönüne göre ayarla
        // Quaternion.LookRotation: Verilen yöne bakan bir rotasyon oluşturur
        Quaternion bulletRotation = Quaternion.LookRotation(aimDirection, Vector3.up);

        // ============== INSTANTIATE (PREFAB OLUŞTURMA) ==============
        // Mermi prefab'ını belirtilen pozisyon ve fare yönüne göre rotasyonla oluştur
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, bulletRotation);
        
        // ============== MERMİYE HIZ VER ==============
        // Rigidbody üzerinden nişan yönünde hız ver
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            // Mermi, fare yönüne doğru gider (karakterin baktığı yöne değil!)
            bulletRb.linearVelocity = aimDirection * bulletSpeed;
        }
        
        Debug.Log($"Mermi ateşlendi! Yön: {aimDirection}");
    }

    /// <summary>
    /// TILE PAINTING: Mevcut tile koordinatını hesapla
    /// Mathf.Round kullanarak X ve Z pozisyonlarını en yakın tam sayıya yuvarlar
    /// </summary>
    /// <returns>Mevcut grid koordinatları (tam sayı X ve Z)</returns>
    Vector2Int GetCurrentTileCoordinate()
    {
        // ============== MATHF.ROUND İLE GRID SNAPPING ==============
        // Karakterin X ve Z pozisyonlarını en yakın tam sayıya yuvarla
        // Örnek: 2.3 -> 2, 2.7 -> 3, -1.4 -> -1
        int tileX = Mathf.RoundToInt(transform.position.x);
        int tileZ = Mathf.RoundToInt(transform.position.z);
        
        return new Vector2Int(tileX, tileZ);
    }

    /// <summary>
    /// TILE PAINTING: Yeni bir kareye geçilip geçilmediğini kontrol et ve çim bırak
    /// Optimizasyon: Sadece farklı bir kareye geçildiğinde Instantiate çalışır
    /// </summary>
    void CheckAndSpawnGrass()
    {
        // Grass prefab'ı atanmış mı kontrol et
        if (grassPrefab == null) return;
        
        // ============== MEVCUT TILE KOORDİNATINI AL ==============
        Vector2Int currentTile = GetCurrentTileCoordinate();
        
        // ============== YENİ KAREYE GEÇİŞ KONTROLÜ (OPTİMİZASYON) ==============
        // Eğer karakter hala aynı karenin üzerindeyse, çim oluşturma
        // Bu, gereksiz Instantiate çağrılarını önler
        if (hasInitialTile && currentTile == lastTileCoordinate)
        {
            // Aynı karedeyiz, çim oluşturmaya gerek yok
            return;
        }
        
        // ============== YENİ KAREYE GEÇİLDİ - ÇİM OLUŞTUR ==============
        SpawnGrassAtTile(currentTile);
        
        // Son tile koordinatını güncelle
        lastTileCoordinate = currentTile;
        hasInitialTile = true;
    }

    /// <summary>
    /// Belirtilen tile koordinatında çim oluştur
    /// ÇAKIŞMA KONTROLÜ: Aynı koordinatta zaten çim varsa yenisini oluşturmaz
    /// </summary>
    /// <param name="tileCoord">Grid koordinatları (tam sayı X ve Z)</param>
    void SpawnGrassAtTile(Vector2Int tileCoord)
    {
        // ============== ÇAKIŞMA KONTROLÜ (OVERLAP PREVENTION) ==============
        // Bu koordinatta daha önce çim bırakıldı mı kontrol et
        // HashSet.Contains() O(1) karmaşıklığında çalışır - çok hızlı!
        if (paintedTiles.Contains(tileCoord))
        {
            // Bu koordinatta zaten çim var, tekrar oluşturma
            Debug.Log($"Tile ({tileCoord.x}, {tileCoord.y}) zaten boyalı, atlandı.");
            return;
        }
        
        // ============== GRID-SNAPPED POZİSYON HESAPLAMA ==============
        // Çimin pozisyonu tam sayı koordinatlarına göre belirlenir
        // Mathf.Round ile hesaplanan koordinatlar kullanılıyor
        Vector3 spawnPosition = new Vector3(
            tileCoord.x,           // X: Tam sayı koordinat (grid-snapped)
            grassYPosition,         // Y: Sabit yükseklik (0.01f - zemine yapışık)
            tileCoord.y            // Z: Vector2Int.y aslında Z eksenini temsil ediyor
        );
        
        // ============== INSTANTIATE (TILE PAINTING) ==============
        // Çim prefab'ını oluştur
        // KESİN ROTASYON: Quaternion.identity = Sıfır rotasyon (dümdüz duracak!)
        // ASLA transform.rotation kullanılmaz - çimler karakterle birlikte dönmez!
        GameObject grass = Instantiate(grassPrefab, spawnPosition, Quaternion.identity);
        
        // ============== KOORDİNATI KAYDET ==============
        // Bu koordinatı boyanan listesine ekle (tekrar boyanmasını önlemek için)
        paintedTiles.Add(tileCoord);
        
        Debug.Log($"Çim bırakıldı! Tile: ({tileCoord.x}, {tileCoord.y}) - Toplam: {paintedTiles.Count}");
    }

    /// <summary>
    /// Ağaç yerleştirme fonksiyonu
    /// Belirli bir puan karşılığında oyuncunun pozisyonuna ağaç yerleştirir
    /// </summary>
    void PlaceTree()
    {
        // ============== GAMEMANAGER KONTROLÜ ==============
        // GameManager singleton instance var mı?
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("PlayerController: GameManager bulunamadı! Ağaç yerleştirilemez.");
            return;
        }
        
        // ============== TREE PREFAB KONTROLÜ ==============
        if (treePrefab == null)
        {
            Debug.LogWarning("PlayerController: TreePrefab atanmamış! Inspector'dan atayın.");
            return;
        }
        
        // ============== SKOR KONTROLÜ ==============
        // Yeterli puan var mı?
        if (GameManager.Instance.currentScore < treeCost)
        {
            Debug.LogWarning($"PlayerController: Yetersiz puan! Gerekli: {treeCost}, Mevcut: {GameManager.Instance.currentScore}");
            return;
        }
        
        // ============== GRID-SNAPPED POZİSYON HESAPLAMA ==============
        // Ağacı mevcut pozisyonun en yakın tam sayı koordinatına yerleştir
        Vector2Int treeCoord = GetCurrentTileCoordinate();
        
        Vector3 treePlacePosition = new Vector3(
            treeCoord.x,        // X: Grid-snapped (tam sayı)
            treeYPosition,      // Y: Ağaç yüksekliği
            treeCoord.y         // Z: Grid-snapped (tam sayı)
        );
        
        // ============== AĞAÇ INSTANTIATE ==============
        // Ağaç prefab'ını oluştur
        // Quaternion.identity = Düz rotasyon
        GameObject tree = Instantiate(treePrefab, treePlacePosition, Quaternion.identity);
        
        // İsim ver
        tree.name = $"Tree_{treeCoord.x}_{treeCoord.y}";
        
        // ============== SKORU HARCA ==============
        // GameManager'dan puan harcama
        GameManager.Instance.SpendScore(treeCost);
        
        Debug.Log($"PlayerController: Ağaç yerleştirildi! Pozisyon: ({treeCoord.x}, {treeCoord.y}), Harcanan: {treeCost} puan");
    }
}
