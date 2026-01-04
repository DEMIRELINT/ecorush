using UnityEngine;

/// <summary>
/// Eco-Rush - Ağaç Sağlık Scripti
/// Bu script, ağacın sağlığını yönetir ve düşman saldırılarından hasar almasını sağlar.
/// Gereksinimler: Collision/Trigger sistemi
/// </summary>
public class TreeHealth : MonoBehaviour
{
    [Header("Ağaç Sağlık Ayarları")]
    [Tooltip("Ağacın maksimum sağlığı")]
    public float maxHealth = 100f;
    
    [Tooltip("Mevcut sağlık (sadece okuma için)")]
    public float currentHealth;

    [Header("Görsel Ayarlar")]
    [Tooltip("Sağlık azaldığında renk değiştir mi?")]
    public bool changeColorOnDamage = true;
    
    [Tooltip("Sağlıklı renk")]
    public Color healthyColor = Color.green;
    
    [Tooltip("Hasar görmüş renk (sağlık %50'nin altında)")]
    public Color damagedColor = Color.yellow;
    
    [Tooltip("Kritik sağlık rengi (sağlık %25'in altında)")]
    public Color criticalColor = Color.red;

    // ============== PRIVATE DEĞİŞKENLER ==============
    // Renderer referansı (renk değiştirmek için)
    private Renderer treeRenderer;
    
    // Başlangıç rengi
    private Color originalColor;

    /// <summary>
    /// Başlangıç fonksiyonu
    /// </summary>
    void Start()
    {
        // ============== SAĞLIĞI BAŞLAT ==============
        currentHealth = maxHealth;
        
        // ============== RENDERER REFERANSINI AL ==============
        treeRenderer = GetComponent<Renderer>();
        
        if (treeRenderer != null && changeColorOnDamage)
        {
            // Başlangıç rengini kaydet
            originalColor = treeRenderer.material.color;
            // Sağlıklı renge ayarla
            treeRenderer.material.color = healthyColor;
        }
        
        // ============== TAG KONTROLÜ ==============
        // Bu objenin "Tree" tag'ine sahip olduğundan emin ol
        if (!CompareTag("Tree"))
        {
            Debug.LogWarning($"TreeHealth: {gameObject.name} objesinin tag'i 'Tree' olmalı! Düşmanlar algılayamayacak.");
        }
        
        Debug.Log($"TreeHealth: Ağaç oluşturuldu. Sağlık: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Ağaca hasar ver
    /// Düşman AI tarafından çağrılır
    /// </summary>
    /// <param name="damage">Verilecek hasar miktarı</param>
    public void TakeDamage(float damage)
    {
        // ============== HASAR UYGULA ==============
        currentHealth -= damage;
        
        Debug.Log($"TreeHealth: Ağaç hasar aldı! -{damage} HP. Kalan: {currentHealth}/{maxHealth}");
        
        // ============== RENK DEĞİŞTİRME ==============
        UpdateVisuals();
        
        // ============== YOK ETME KONTROLÜ ==============
        if (currentHealth <= 0)
        {
            DestroyTree();
        }
    }

    /// <summary>
    /// Sağlığa göre görsel güncelleme
    /// </summary>
    void UpdateVisuals()
    {
        // Renk değiştirme aktif değilse çık
        if (!changeColorOnDamage || treeRenderer == null) return;
        
        // ============== SAĞLIK YÜZDESİNE GÖRE RENK ==============
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage > 0.5f)
        {
            // %50'den fazla: Sağlıklı (yeşil)
            treeRenderer.material.color = healthyColor;
        }
        else if (healthPercentage > 0.25f)
        {
            // %25-50 arası: Hasar görmüş (sarı)
            treeRenderer.material.color = damagedColor;
        }
        else
        {
            // %25'in altı: Kritik (kırmızı)
            treeRenderer.material.color = criticalColor;
        }
    }

    /// <summary>
    /// Ağacı yok et
    /// </summary>
    void DestroyTree()
    {
        Debug.Log($"TreeHealth: Ağaç yok edildi! ({gameObject.name})");
        
        // ============== PATLAMA EFEKTİ (OPSİYONEL) ==============
        // Burada ağaç yıkılma efekti eklenebilir
        // Örnek: Instantiate(destructionEffect, transform.position, Quaternion.identity);
        
        // ============== AĞACI YOK ET ==============
        Destroy(gameObject);
    }

    /// <summary>
    /// Sağlık yüzdesini döndür (0-1 arası)
    /// </summary>
    /// <returns>Mevcut sağlık yüzdesi</returns>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Ağaç hala hayatta mı?
    /// </summary>
    /// <returns>Sağlık 0'dan büyükse true</returns>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    /// <summary>
    /// Ağacı tam sağlığa döndür (test için)
    /// </summary>
    public void Heal()
    {
        currentHealth = maxHealth;
        UpdateVisuals();
        Debug.Log($"TreeHealth: Ağaç iyileştirildi! Sağlık: {currentHealth}/{maxHealth}");
    }
}
