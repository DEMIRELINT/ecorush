using UnityEngine;

/// <summary>
/// Eco-Rush - Çit Oluşturucu Scripti
/// Bu script, oyun alanının etrafına otomatik olarak çitleri dizer.
/// Kullanımı: Boş bir objeye atın, çit prefabını sürükleyin ve "Generate Fences" butonuna basın (veya Play modunda çalışır).
/// </summary>
public class FenceGenerator : MonoBehaviour
{
    [Header("Çit Ayarları")]
    [Tooltip("Dizilecek çit prefabı")]
    public GameObject fencePrefab;
    
    [Tooltip("Oyun alanı boyutu (GameManager'daki ile aynı olmalı)")]
    public float playAreaSize = 30f;
    
    [Tooltip("İki çit arasındaki mesafe (çitinizin genişliğine göre ayarlar)")]
    public float fenceWidth = 2f;
    
    [Tooltip("Çitlerin yerden yüksekliği")]
    public float yOffset = 0.5f;

    [Tooltip("Çitlerin kendi eksenindeki rotasyon düzeltmesi (Eğer ters durursa buradan düzeltin)")]
    public float rotationOffset = 90f;

    [Header("Otomasyon")]
    [Tooltip("Oyun başladığında otomatik oluştur")]
    public bool generateOnStart = true;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateFences();
        }
    }

    [ContextMenu("Generate Fences")]
    public void GenerateFences()
    {
        if (fencePrefab == null)
        {
            Debug.LogError("FenceGenerator: Fence Prefab atanmamış!");
            return;
        }

        // Mevcut eski çitleri temizle (eğer varsa)
        ClearFences();

        float halfSize = playAreaSize / 2f;
        int countPerSide = Mathf.CeilToInt(playAreaSize / fenceWidth);

        // 4 Kenar için döngü
        for (int i = 0; i < countPerSide; i++)
        {
            float pos = -halfSize + (i * fenceWidth) + (fenceWidth / 2f);

            // Kuzey Kenarı (Z = halfSize)
            SpawnFence(new Vector3(pos, yOffset, halfSize), Quaternion.Euler(0, 0 + rotationOffset, 0), "North");

            // Güney Kenarı (Z = -halfSize)
            SpawnFence(new Vector3(pos, yOffset, -halfSize), Quaternion.Euler(0, 180 + rotationOffset, 0), "South");

            // Doğu Kenarı (X = halfSize)
            SpawnFence(new Vector3(halfSize, yOffset, pos), Quaternion.Euler(0, 90 + rotationOffset, 0), "East");

            // Batı Kenarı (X = -halfSize)
            SpawnFence(new Vector3(-halfSize, yOffset, pos), Quaternion.Euler(0, -90 + rotationOffset, 0), "West");
        }

        Debug.Log($"FenceGenerator: Toplam {countPerSide * 4} adet çit başarıyla yerleştirildi.");
    }

    void SpawnFence(Vector3 position, Quaternion rotation, string side)
    {
        GameObject fence = Instantiate(fencePrefab, position, rotation, transform);
        fence.name = $"Fence_{side}_{transform.childCount}";
    }

    /// <summary>
    /// Oluşturulan tüm çitleri siler
    /// </summary>
    [ContextMenu("Clear Fences")]
    public void ClearFences()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            // Editör modunda çalışması için DestroyImmediate kullanıyoruz
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
