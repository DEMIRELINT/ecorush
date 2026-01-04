using UnityEngine;

/// <summary>
/// Eco-Rush - Mermi Kontrol Scripti (Gelişmiş Hata Ayıklama Modu)
/// </summary>
public class BulletController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float speed = 20f;
    public float lifetime = 5f;

    [Header("Efekt Ayarları")]
    public GameObject explosionEffectPrefab;
    public float effectLifetime = 2f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isActive = true;
    private float aliveTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveDirection = transform.forward;
        
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = moveDirection * speed;
            // Fiziksel çarpışmaların daha hassas olması için
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        Destroy(gameObject, lifetime);
        Debug.Log($"<color=cyan>BULLET DEBUG:</color> Mermi oluşturuldu! Pozisyon: {transform.position}");
    }

    void Update()
    {
        aliveTime += Time.deltaTime;
        
        // Rigidbody yoksa manuel hareket
        if (rb == null && isActive)
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject, transform.position);
    }

    void HandleCollision(GameObject hitObject, Vector3 contactPoint)
    {
        if (!isActive) return;

        // ============== KRİTİK FİLTRELER (ASLA PATLAMA) ==============
        
        // 1. Oyuncunun Kendisi
        if (hitObject.CompareTag("Player")) return;

        // 2. Yer, Çimen veya Görsel Objeler
        // Bunlara çarpınca mermi patlamamalı, içinden geçmeli
        string hitName = hitObject.name.ToLower();
        string hitTag = hitObject.tag.ToLower();

        if (hitName.Contains("grass") || hitName.Contains("ground") || hitName.Contains("floor") || 
            hitTag.Contains("grass") || hitTag.Contains("ground"))
        {
            // Debug.Log($"<color=white>BULLET INFO:</color> {hitObject.name} içinden geçildi.");
            return;
        }

        // ============== PATLAMA ANI (DÜŞMAN, DUVAR, VB.) ==============
        isActive = false;
        Debug.Log($"<color=red>!!!! PATLAMA !!!!</color> Mermi şuna çarptı: <b>{hitObject.name}</b> (Tag: {hitObject.tag})");

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }

        Destroy(gameObject);
    }
}
