using UnityEngine;

/// <summary>
/// Eco-Rush - Düşman Yapay Zeka Scripti
/// Bu script, düşmanın oyuncuyu takip etmesini, ağaçlara çarpmasını (Raycast), 
/// animasyon kontrolünü ve mermi ile ölmesini yönetir.
/// Gereksinimler: Vector3.MoveTowards, Raycast, Animator, OnCollisionEnter
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [Tooltip("Düşmanın hareket hızı")]
    public float moveSpeed = 3f;
    
    [Tooltip("Düşmanın oyuncuya yaklaşabileceği minimum mesafe")]
    public float stoppingDistance = 1.5f;

    [Header("Raycast Ayarları")]
    [Tooltip("Raycast'in algılama mesafesi")]
    public float raycastDistance = 2f;
    
    [Tooltip("Raycast için Layer Mask (opsiyonel)")]
    public LayerMask obstacleLayer;

    [Header("Saldırı Ayarları")]
    [Tooltip("Ağaca saldırı hızı")]
    public float attackRate = 1f;

    // ============== PRIVATE DEĞİŞKENLER ==============
    // Oyuncu referansı
    private Transform player;
    
    // Animator component referansı - Animasyon kontrolü için
    private Animator animator;
    
    // Düşman durumları
    private bool isAttacking = false;
    private bool isTreeBlocking = false;
    
    // Saldırı zamanlayıcısı
    private float attackTimer = 0f;
    
    // Algılanan ağaç referansı
    private GameObject blockedTree;

    /// <summary>
    /// Başlangıç fonksiyonu - Referansları bul ve ata
    /// </summary>
    void Start()
    {
        // ============== OYUNCU REFERANSINI BUL ==============
        // "Player" tag'ine sahip objeyi bul
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("EnemyAI: 'Player' tag'ine sahip obje bulunamadı!");
        }

        // ============== ANIMATOR REFERANSINI AL ==============
        // Animator component'ini alt objelerden al (Survivalist objesi üzerinde)
        animator = GetComponentInChildren<Animator>();
        
        if (animator == null)
        {
            Debug.Log("EnemyAI: Animator component bulunamadı. Animasyonlar çalışmayacak.");
        }
        else
        {
            // Başlangıçta koşma animasyonunu başlat
            SetAnimatorState(false);
        }
    }

    /// <summary>
    /// Her frame çağrılır - Düşman davranış mantığı burada
    /// </summary>
    void Update()
    {
        // Oyuncu yoksa hiçbir şey yapma
        if (player == null) return;

        // ============== RAYCAST İLE ENGEL TESPİTİ ==============
        // Düşmanın önüne Raycast at ve engel var mı kontrol et
        CheckForObstacles();

        // ============== DURUM MAKİNESİ ==============
        if (isTreeBlocking)
        {
            // Ağaç engel oluyorsa saldır
            HandleAttackState();
        }
        else
        {
            // Engel yoksa oyuncuya doğru hareket et
            HandleChaseState();
        }
    }

    /// <summary>
    /// Raycast ile önümüzde engel (ağaç) var mı kontrol et
    /// KRİTİK SINAV GEREKSİNİMİ: Raycast kullanımı
    /// </summary>
    void CheckForObstacles()
    {
        // ============== RAYCAST OLUŞTURMA ==============
        // Raycast başlangıç noktası (düşmanın pozisyonu, biraz yukarıda)
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        
        // Raycast yönü (düşmanın baktığı yön - ileri)
        Vector3 rayDirection = transform.forward;
        
        // Raycast sonucu için RaycastHit yapısı
        RaycastHit hit;
        
        // ============== RAYCAST ATMA ==============
        // Physics.Raycast: Işın izleme ile çarpışma tespiti
        // Parametreler: başlangıç noktası, yön, çıkış bilgisi, maksimum mesafe
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance))
        {
            // Raycast bir şeye çarptı, tag'ini kontrol et
            if (hit.collider.CompareTag("Tree"))
            {
                // ============== AĞAÇ TESPİT EDİLDİ ==============
                // Ağaca çarptık, saldırı moduna geç
                if (!isTreeBlocking)
                {
                    Debug.Log("EnemyAI: Ağaç tespit edildi, saldırı moduna geçiliyor!");
                    isTreeBlocking = true;
                    isAttacking = true;
                    blockedTree = hit.collider.gameObject;
                    
                    // Animasyonu saldırı moduna geçir
                    SetAnimatorState(true);
                }
            }
        }
        else
        {
            // Raycast hiçbir şeye çarpmadı veya ağaç yok
            if (isTreeBlocking)
            {
                Debug.Log("EnemyAI: Engel kalktı, takip moduna geçiliyor!");
                isTreeBlocking = false;
                isAttacking = false;
                blockedTree = null;
                
                // Animasyonu koşma moduna geçir
                SetAnimatorState(false);
            }
        }
        
        // ============== DEBUG İÇİN RAYCAST ÇİZİMİ ==============
        // Scene görünümünde Raycast'i görselleştir
        Debug.DrawRay(rayOrigin, rayDirection * raycastDistance, isTreeBlocking ? Color.red : Color.green);
    }

    /// <summary>
    /// Takip durumu - Oyuncuya doğru hareket et
    /// </summary>
    void HandleChaseState()
    {
        // Oyuncuya olan mesafeyi hesapla
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Oyuncu yeterince yakında değilse hareket et
        if (distanceToPlayer > stoppingDistance)
        {
            // ============== VECTOR3.MOVETOWARDS KULLANIMI ==============
            // MoveTowards: Bir noktadan diğerine sabit hızla hareket
            // Parametreler: mevcut pozisyon, hedef pozisyon, maksimum adım
            Vector3 targetPosition = Vector3.MoveTowards(
                transform.position,           // Mevcut pozisyon
                player.position,              // Hedef pozisyon (oyuncu)
                moveSpeed * Time.deltaTime    // Bu frame'de alınacak maksimum mesafe
            );
            
            // Yeni pozisyonu uygula
            transform.position = targetPosition;
            
            // ============== OYUNCUYA DOĞRU DÖN ==============
            // Oyuncuya bak (Y ekseni hariç)
            Vector3 lookDirection = player.position - transform.position;
            lookDirection.y = 0f; // Yatay düzlemde kal
            
            if (lookDirection.magnitude > 0.1f)
            {
                // Quaternion.LookRotation ile hedef rotasyonu hesapla
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                
                // Yumuşak dönüş için Slerp kullan
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    /// <summary>
    /// Saldırı durumu - Ağaca saldır
    /// </summary>
    void HandleAttackState()
    {
        // Saldırı zamanlayıcısını artır (DeltaTime kullanarak)
        attackTimer += Time.deltaTime;
        
        // Saldırı aralığı geçtiyse saldır
        if (attackTimer >= 1f / attackRate)
        {
            // Saldırı gerçekleştir
            PerformAttack();
            attackTimer = 0f; // Timer'ı sıfırla
        }
    }

    /// <summary>
    /// Saldırı gerçekleştir - Ağaca hasar ver
    /// </summary>
    void PerformAttack()
    {
        if (blockedTree != null)
        {
            // ============== AĞACA HASAR VERME ==============
            // Ağaçtaki TreeHealth component'ini al
            TreeHealth treeHealth = blockedTree.GetComponent<TreeHealth>();
            
            if (treeHealth != null)
            {
                // Ağaca hasar ver (10 HP)
                treeHealth.TakeDamage(10f);
                
                Debug.Log("EnemyAI: Ağaca 10 hasar verildi!");
                
                // ============== AĞAÇ YOK EDİLDİYSE ==============
                // Eğer ağaç yok edildiyse, takip moduna geri dön
                if (!treeHealth.IsAlive())
                {
                    Debug.Log("EnemyAI: Ağaç yok edildi, oyuncuyu takip ediyorum!");
                    isTreeBlocking = false;
                    isAttacking = false;
                    blockedTree = null;
                    SetAnimatorState(false);
                }
            }
            else
            {
                Debug.LogWarning("EnemyAI: Ağaçta TreeHealth component'i yok!");
            }
        }
    }

    /// <summary>
    /// Animator durumunu değiştir
    /// KRİTİK SINAV GEREKSİNİMİ: Animator SetBool kullanımı
    /// </summary>
    /// <param name="attacking">Saldırı modunda mı?</param>
    void SetAnimatorState(bool attacking)
    {
        // Animator varsa animasyon durumunu değiştir
        if (animator != null)
        {
            // ============== ANIMATOR SETBOOL KULLANIMI ==============
            // SetBool: Animator'daki bool parametresini ayarla
            // Bu parametre, Animator Controller'da geçişleri tetikler
            animator.SetBool("isAttacking", attacking);
            animator.SetBool("isRunning", !attacking);
            
            Debug.Log($"EnemyAI: Animator durumu güncellendi - Saldırı: {attacking}");
        }
    }

    /// <summary>
    /// Çarpışma algılama (Fiziksel çarpışma)
    /// KRİTİK SINAV GEREKSİNİMİ: OnCollisionEnter kullanımı
    /// </summary>
    /// <param name="collision">Çarpışma bilgisi</param>
    void OnCollisionEnter(Collision collision)
    {
        // ============== MERMİ ÇARPIŞMASI KONTROLÜ ==============
        // Çarpan objenin tag'ini kontrol et
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("EnemyAI: Mermi çarptı! Düşman öldü.");
            
            // Mermiyi de yok et (opsiyonel, BulletController zaten yapıyor)
            // Destroy(collision.gameObject);
            
            // ============== GAMEMANAGER'A BİLDİR ==============
            // GameManager'a düşman öldüğünü bildir (skor için)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyDeath();
            }
            
            // ============== DÜŞMANI YOK ET ==============
            // Destroy: GameObject'i yok et
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Trigger çarpışma algılama (Fiziksel olmayan çarpışma)
    /// Alternatif olarak Trigger da kullanılabilir
    /// </summary>
    /// <param name="other">Trigger'a giren collider</param>
    void OnTriggerEnter(Collider other)
    {
        // Trigger ile de mermi algılama
        if (other.CompareTag("Bullet"))
        {
            Debug.Log("EnemyAI: Mermi (Trigger) çarptı! Düşman öldü.");
            
            // ============== GAMEMANAGER'A BİLDİR ==============
            // GameManager'a düşman öldüğünü bildir (skor için)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyDeath();
            }
            
            Destroy(gameObject);
        }
    }
}
