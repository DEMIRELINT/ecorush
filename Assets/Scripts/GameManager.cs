using UnityEngine;

/// <summary>
/// Eco-Rush - Oyun Yöneticisi Scripti
/// Bu script, düşman spawn sistemini, zorluk artışını ve skor takibini yönetir.
/// Gereksinimler: Array, Random.Range, DeltaTime, Instantiate
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Düşman Spawn Ayarları")]
    [Tooltip("Oluşturulacak düşman prefab'ı")]
    public GameObject enemyPrefab;
    
    [Tooltip("Oyun alanı boyutu (örn: 30 = 30x30 alan)")]
    public float playAreaSize = 30f;
    
    [Tooltip("Spawn offset (duvarlardan içeriye doğru mesafe)")]
    public float spawnOffset = 3f;
    
    [Tooltip("Başlangıç spawn aralığı (saniye)")]
    public float baseSpawnInterval = 3f;
    
    [Tooltip("Minimum spawn aralığı (maksimum zorluk)")]
    public float minimumSpawnInterval = 0.5f;
    
    [Tooltip("Zorluk artış oranı (0.95 = her spawn'da %5 daha hızlı)")]
    public float difficultyIncreaseRate = 0.95f;
    
    [Tooltip("Aynı anda maksimum düşman sayısı")]
    public int maxEnemies = 20;

    [Header("Skor Sistemi")]
    [Tooltip("Düşman öldürme puanı")]
    public int enemyKillScore = 10;
    
    [Tooltip("Mevcut skor (sadece okuma için)")]
    public int currentScore = 0;

    [Header("Oyun Durumu")]
    [Tooltip("Oyun aktif mi?")]
    public bool isGameActive = true;

    // ============== PRIVATE DEĞİŞKENLER ==============
    // Spawn zamanlayıcısı - DeltaTime ile artırılacak
    private float spawnTimer = 0f;
    
    // Mevcut spawn aralığı (zorluk arttıkça azalır)
    private float currentSpawnInterval;
    
    // Mevcut düşman sayısı
    private int currentEnemyCount = 0;
    
    // ============== KÖŞE SPAWN SİSTEMİ ==============
    // 4 köşe pozisyonu için dizi (otomatik oluşturulacak)
    private Vector3[] cornerSpawnPoints;

    // ============== SINGLETON PATTERN ==============
    // GameManager'a her yerden erişim için Singleton
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Awake fonksiyonu - Singleton kurulumu
    /// Diğer Start fonksiyonlarından önce çalışır
    /// </summary>
    void Awake()
    {
        // ============== SINGLETON KURULUMU ==============
        // Eğer başka bir Instance varsa bu objeyi yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Bu objeyi singleton olarak ata
        Instance = this;
        
        Debug.Log("GameManager: Singleton kuruldu.");
    }

    /// <summary>
    /// Başlangıç fonksiyonu
    /// </summary>
    void Start()
    {
        // ============== KÖŞE SPAWN NOKTALARI OLUŞTUR ==============
        // Oyun alanı boyutuna göre 4 köşe pozisyonunu otomatik hesapla
        GenerateCornerSpawnPoints();
        
        // Düşman prefab'ı kontrol et
        if (enemyPrefab == null)
        {
            Debug.LogError("GameManager: EnemyPrefab atanmamış! Inspector'dan atayın.");
        }
        
        // Başlangıç spawn aralığını ayarla
        currentSpawnInterval = baseSpawnInterval;
        
        Debug.Log("GameManager: Oyun başladı! 4 köşeden düşman spawn olacak.");
    }

    /// <summary>
    /// 4 köşe spawn noktasını otomatik oluştur
    /// Oyun alanı boyutuna göre köşe koordinatları hesaplanır
    /// Spawn offset ile duvarlardan içeriye alınır
    /// </summary>
    void GenerateCornerSpawnPoints()
    {
        // ============== ARRAY OLUŞTURMA ==============
        // 4 elemanlı Vector3 dizisi oluştur
        cornerSpawnPoints = new Vector3[4];
        
        // Yarım alan boyutu (köşe koordinatları için)
        float halfSize = playAreaSize / 2f;
        
        // Duvarlardan içeriye al (düşmanların düşmesini önlemek için)
        float spawnCoord = halfSize - spawnOffset;
        
        // ============== 4 KÖŞE POZİSYONU ==============
        // Sola üst (Northwest)
        cornerSpawnPoints[0] = new Vector3(-spawnCoord, 0.5f, spawnCoord);
        
        // Sağ üst (Northeast)
        cornerSpawnPoints[1] = new Vector3(spawnCoord, 0.5f, spawnCoord);
        
        // Sağ alt (Southeast)
        cornerSpawnPoints[2] = new Vector3(spawnCoord, 0.5f, -spawnCoord);
        
        // Sol alt (Southwest)
        cornerSpawnPoints[3] = new Vector3(-spawnCoord, 0.5f, -spawnCoord);
        
        Debug.Log($"GameManager: 4 köşe spawn noktası oluşturuldu (Alan: {playAreaSize}x{playAreaSize}, Offset: {spawnOffset})");
        for (int i = 0; i < cornerSpawnPoints.Length; i++)
        {
            Debug.Log($"  Köşe {i}: {cornerSpawnPoints[i]}");
        }
    }

    /// <summary>
    /// Her frame çağrılır - Spawn zamanlayıcısı burada
    /// </summary>
    void Update()
    {
        // Oyun aktif değilse spawn yapma
        if (!isGameActive) return;
        
        // ============== SPAWN ZAMANLAYICISI (DELTATIME) ==============
        // Timer'ı her frame DeltaTime kadar artır
        // Time.deltaTime: Son frame'den bu yana geçen süre (saniye)
        // Bu, farklı FPS'lerde tutarlı zamanlama sağlar
        spawnTimer += Time.deltaTime;
        
        // Spawn aralığı geçtiyse düşman oluştur
        if (spawnTimer >= currentSpawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f; // Timer'ı sıfırla
            
            // ============== ZORLUK ARTIŞI ==============
            // Her spawn'dan sonra aralığı azalt (zorluk artar)
            IncreaseDifficulty();
        }
        
        // ============== MEVCUT DÜŞMAN SAYISINI GÜNCELLE ==============
        // Bu yöntem basit ama biraz maliyetli, büyük oyunlarda optimize edilmeli
        UpdateEnemyCount();
    }

    /// <summary>
    /// Zorluk artırma - Spawn aralığını azalt
    /// Her spawn'dan sonra çağrılır
    /// </summary>
    void IncreaseDifficulty()
    {
        // Mevcut aralığı azalt
        currentSpawnInterval *= difficultyIncreaseRate;
        
        // Minimum değerin altına düşmesin
        if (currentSpawnInterval < minimumSpawnInterval)
        {
            currentSpawnInterval = minimumSpawnInterval;
        }
        
        Debug.Log($"GameManager: Zorluk arttı! Yeni spawn aralığı: {currentSpawnInterval:F2}s");
    }

    /// <summary>
    /// Rastgele bir köşeden düşman oluştur
    /// KRİTİK SINAV GEREKSİNİMLERİ: Array, Random.Range, Instantiate
    /// </summary>
    void SpawnEnemy()
    {
        // ============== GEREKLİ KONTROLLER ==============
        // Prefab var mı?
        if (enemyPrefab == null)
        {
            Debug.LogWarning("GameManager: EnemyPrefab null, spawn iptal edildi.");
            return;
        }
        
        // Köşe noktaları oluşturulmuş mu?
        if (cornerSpawnPoints == null || cornerSpawnPoints.Length == 0)
        {
            Debug.LogWarning("GameManager: Köşe noktaları yok, spawn iptal edildi.");
            return;
        }
        
        // Maksimum düşman sayısına ulaşıldı mı?
        if (currentEnemyCount >= maxEnemies)
        {
            Debug.Log("GameManager: Maksimum düşman sayısına ulaşıldı, yeni spawn bekleniyor.");
            return;
        }

        // ============== RASTGELE KÖŞE SEÇİMİ (RANDOM.RANGE) ==============
        // Random.Range(min, max): min dahil, max hariç rastgele int döndürür
        // 4 köşe olduğu için 0-3 arası rastgele index seçer
        int randomCornerIndex = Random.Range(0, cornerSpawnPoints.Length);
        
        // Seçilen köşenin pozisyonunu al
        Vector3 spawnPosition = cornerSpawnPoints[randomCornerIndex];
        
        Debug.Log($"GameManager: Köşe {randomCornerIndex} seçildi - Pozisyon: {spawnPosition}");

        // ============== DÜŞMAN INSTANTIATE ==============
        // Düşman prefab'ını seçilen köşe pozisyonunda oluştur
        // Parametreler: prefab, pozisyon, rotasyon
        GameObject newEnemy = Instantiate(
            enemyPrefab,                      // Oluşturulacak prefab
            spawnPosition,                    // Spawn pozisyonu (köşe)
            Quaternion.identity               // Varsayılan rotasyon
        );
        
        // Düşmana isim ver (debugging için)
        newEnemy.name = $"Enemy_{currentEnemyCount + 1}";
        
        Debug.Log($"GameManager: Yeni düşman oluşturuldu - {newEnemy.name}");
    }

    /// <summary>
    /// Sahnedeki düşman sayısını güncelle
    /// </summary>
    void UpdateEnemyCount()
    {
        // "Enemy" tag'ine sahip tüm objeleri say
        // NOT: Bu yöntem her frame çağrıldığında maliyetli olabilir
        // Daha optimize bir yöntem: Düşman oluşturulduğunda/öldüğünde sayacı güncelle
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        currentEnemyCount = enemies.Length;
    }

    /// <summary>
    /// Dış scriptlerden çağrılabilir - Düşman öldüğünde sayacı azalt ve puan ver
    /// </summary>
    public void OnEnemyDeath()
    {
        currentEnemyCount--;
        if (currentEnemyCount < 0) currentEnemyCount = 0;
        
        // ============== SKOR EKLEME ==============
        // Düşman öldürüldüğünde puan ekle
        AddScore(enemyKillScore);
        
        Debug.Log($"GameManager: Düşman öldü. Kalan düşman: {currentEnemyCount}, Skor: {currentScore}");
    }

    /// <summary>
    /// Skor ekleme fonksiyonu
    /// </summary>
    /// <param name="amount">Eklenecek puan miktarı</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"GameManager: +{amount} puan! Toplam skor: {currentScore}");
    }

    /// <summary>
    /// Skor harcama fonksiyonu (ağaç satın alma için)
    /// </summary>
    /// <param name="amount">Harcanacak puan miktarı</param>
    /// <returns>Başarılı ise true</returns>
    public bool SpendScore(int amount)
    {
        if (currentScore >= amount)
        {
            currentScore -= amount;
            Debug.Log($"GameManager: -{amount} puan harcandı. Kalan skor: {currentScore}");
            return true;
        }
        
        Debug.LogWarning($"GameManager: Yetersiz puan! Gerekli: {amount}, Mevcut: {currentScore}");
        return false;
    }

    /// <summary>
    /// Oyunu duraklat
    /// </summary>
    public void PauseGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; // Zamanı durdur
        Debug.Log("GameManager: Oyun duraklatıldı.");
    }

    /// <summary>
    /// Oyunu devam ettir
    /// </summary>
    public void ResumeGame()
    {
        isGameActive = true;
        Time.timeScale = 1f; // Zamanı normale al
        Debug.Log("GameManager: Oyun devam ediyor.");
    }

    /// <summary>
    /// Oyunu yeniden başlat
    /// </summary>
    public void RestartGame()
    {
        // Mevcut sahneyi yeniden yükle
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    /// <summary>
    /// Tüm düşmanları yok et (test için)
    /// </summary>
    public void DestroyAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        
        currentEnemyCount = 0;
        Debug.Log("GameManager: Tüm düşmanlar yok edildi.");
    }

    /// <summary>
    /// Basit UI gösterimi - Puan ve bilgi gösterilir
    /// </summary>
    void OnGUI()
    {
        // ============== PUAN GÖSTERİMİ ==============
        // Sol üst köşede büyük bir puan gösterimi
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 30;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;
        scoreStyle.alignment = TextAnchor.UpperLeft;
        
        // Puan metni
        string scoreText = $"🌱 Puan: {currentScore}";
        GUI.Label(new Rect(20, 20, 300, 50), scoreText, scoreStyle);
        
        // ============== AĞAÇ MALİYETİ BİLGİSİ ==============
        // Ağaç yerleştirme ipucu
        GUIStyle treeInfoStyle = new GUIStyle(GUI.skin.label);
        treeInfoStyle.fontSize = 20;
        treeInfoStyle.normal.textColor = Color.green;
        treeInfoStyle.alignment = TextAnchor.UpperLeft;
        
        string treeInfo = $"🌳 Ağaç: 50 puan (Q tuşu)";
        GUI.Label(new Rect(20, 70, 350, 40), treeInfo, treeInfoStyle);
        
        // ============== DÜŞMAN SAYISI ==============
        GUIStyle enemyStyle = new GUIStyle(GUI.skin.label);
        enemyStyle.fontSize = 18;
        enemyStyle.normal.textColor = Color.red;
        
        string enemyText = $"👾 Düşman: {currentEnemyCount}/{maxEnemies}";
        GUI.Label(new Rect(20, 115, 250, 40), enemyText, enemyStyle);
        
        // ============== ZORLUK SEVİYESİ ==============
        GUIStyle difficultyStyle = new GUIStyle(GUI.skin.label);
        difficultyStyle.fontSize = 16;
        difficultyStyle.normal.textColor = Color.yellow;
        
        string difficultyText = $"⚡ Spawn: {currentSpawnInterval:F1}s";
        GUI.Label(new Rect(20, 155, 250, 40), difficultyText, difficultyStyle);
    }
}
