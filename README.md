# **EcoRush Proje Raporu**

**DEMIRELINT

## **Genel Oyun Mantığı ve Oynanış**

**Eco-Rush**, oyuncunun doğayı canlandırmaya çalıştığı, aynı zamanda düşman akınlarına karşı hayatta kalma mücadelesi verdiği bir Top-Down Survival (Kuşbakışı Hayatta Kalma) oyunudur.

### **Temel Mekanikler:**

1. **Doğayı Canlandırma (Tile Painting):** Oyuncu karakteri hareket ettikçe, geçtiği gri zeminlerin üzerinde otomatik olarak çimler oluşur. Bu, görsel olarak dünyanın "iyileştirildiğini" hissettirir.
2. **Ağaç Dikme ve Ekonomi:** Oyuncu topladığı puanları kullanarak (Q tuşu ile) stratejik noktalara ağaç dikebilir. Bu ağaçlar hem skora katkı sağlar hem de düşmanlar için birer hedef/engel teşkil eder.
3. **Savaş Sistemi:** Oyuncu, fare ile nişan alarak düşmanlara "arı mermisi" fırlatır. Düşmanlar, haritanın 4 köşesinden dalgalar halinde gelir.
4. **Düşman Yapay Zekası:** Düşmanlar oyuncuyu takip eder. Ancak yollarında bir ağaç ile karşılaşırlarsa durup ağaca saldırırlar. Bu, oyuncuya kaçmak veya pozisyon almak için zaman kazandırır.
5. **Zorluk Artışı:** Oyun ilerledikçe düşmanların gelme sıklığı artar (Spawn Interval azalır), bu da oyunu giderek zorlaştırır.

---

Bu rapor, EcoRush projesinde kullanılan temel Unity bileşenlerinin ve programlama yapılarının hangi noktalarda ve nasıl kullanıldığını detaylandırmaktadır.

## **1. Prefab Kullanımı**

Prefab'lar, tekrar kullanılabilir oyun nesneleri oluşturmak için projenin temel taşını oluşturur.

- **PlayerController.cs**: Mermi (`bulletPrefab`), çim (`grassPrefab`) ve ağaç (`treePrefab`) oluşturmak için prefab referansları tutar.
- **GameManager.cs**: Düşmanları dinamik olarak oluşturmak için `enemyPrefab` kullanır.
- **FenceGenerator.cs**: Oyun alanının sınırlarını belirlemek için `fencePrefab` kullanarak otomatik çit dizer.
- **BulletController.cs**: Mermi patladığında görsel efekt oluşturmak için `explosionEffectPrefab` kullanır.

## **2. Vector3 ve Quaternion**

Matematiksel işlemler, pozisyon ve rotasyon kontrolleri için yoğun olarak kullanılmıştır.

- **Vector3**:
    - **PlayerController.cs**: Karakterin hareket yönü (`movementDirection`), farenin dünya üzerindeki pozisyonu (`mouseWorldPosition`) ve mermi spawn noktası hesaplamalarında kullanılır.
    - **EnemyAI.cs**: Düşmanın oyuncuya olan mesafesini hesaplamak (`Vector3.Distance`) ve oyuncuya doğru hareket etmek (`Vector3.MoveTowards`) için kullanılır.
    - **GameManager.cs**: Haritanın 4 köşesini temsil eden spawn noktalarını saklamak için `Vector3` dizisi kullanılır.
- **Quaternion**:
    - **PlayerController.cs**: Karakterin ve merminin baktığı yönü ayarlamak için `Quaternion.LookRotation` kullanılır. Ayrıca yumuşak dönüş sağlamak için `Quaternion.Slerp` tercih edilmiştir.
    - **FenceGenerator.cs**: Çitlerin doğru açıda yerleştirilmesi için `Quaternion.Euler` kullanılır.

## **3. Partikül Efektleri (Particle Systems)**

Görsel geri bildirim sağlamak amacıyla efektler entegre edilmiştir.

- **BulletController.cs**: Mermi bir engele veya düşmana çarptığında `Instantiate(explosionEffectPrefab, ...)` kodu ile patlama efekti oluşturulur. Bu prefab genellikle bir Particle System içerir ved yok edilmeden önce kısa bir süre sahnede kalır.

## **4. Instantiate**

Nesnelerin çalışma zamanında (runtime) dinamik olarak oluşturulması işlemidir.

- **PlayerController.cs**: Sol tık ile mermi atıldığında, karakter hareket ettikçe arkasında çim bıraktığında ve Q tuşu ile ağaç dikildiğinde `Instantiate` kullanılır.
- **GameManager.cs**: Belirlenen zaman aralıklarında haritanın köşelerinden yeni düşmanlar oluşturmak için kullanılır.
- **BulletController.cs**: Patlama efektini oluşturmak için kullanılır.

## **5. RayCast**

Işın göndererek fiziksel algılama yapmak için kritik bir rol oynar.

- **EnemyAI.cs**: **CheckForObstacles** fonksiyonunda, düşmanın önünde ağaç (engel) olup olmadığını anlamak için `Physics.Raycast` kullanılır. Eğer ışın "Tree" etiketli bir objeye çarparsa, düşman durur ve saldırıya geçer.
- **PlayerController.cs (Mouse Aim)**: Top-down bakış açısında, farenin ekrandaki pozisyonunu 3D dünya koordinatlarına çevirmek için `Camera.main.ScreenPointToRay` ve `Plane.Raycast` mantığı (matematiksel raycast) kullanılmıştır.

## **6. FixedUpdate ve LateUpdate**

Fizik ve hareket güncellemelerinin doğru zamanlamada yapılması için kullanılır.

- **PlayerController.cs**: Fizik tabanlı bir hareket sistemi (`Rigidbody`) kullanıldığı için, hareket kodları **Update** yerine **FixedUpdate** içerisinde yazılmıştır. Bu, kare hızı (FPS) dalgalansa bile fizik hesaplamalarının (0.02 saniyede bir) tutarlı çalışmasını sağlar.
- **Update**: Kullanıcı girdileri (Input) ve zamanlayıcılar (**GameManager** spawn timer, **EnemyAI** attack timer) **Update** içerisinde işlenir.

## **7. DeltaTime**

Zamanla ilgili işlemleri kare hızından (FPS) bağımsız hale getirmek için kullanılır.

- **EnemyAI.cs**: Düşmanın hareket hızı (`moveSpeed * Time.deltaTime`) ve dönüş hızı (`Time.deltaTime * 5f`) hesaplanırken kullanılır. Ayrıca saldırı zamanlayıcısı (`attackTimer += Time.deltaTime`) için gereklidir.
- **GameManager.cs**: Düşman spawn sayacını (`spawnTimer`) artırmak için kullanılır.
- **PlayerController.cs**: **FixedUpdate** içerisinde yumuşak dönüş yaparken `Time.fixedDeltaTime` kullanılır.

## **8. Arrays (Diziler)**

Birden fazla veriyi toplu halde tutmak için kullanılır.

- **GameManager.cs**:
    - `Vector3[] cornerSpawnPoints`: Haritanın 4 köşe noktasının koordinatlarını tutmak için bir dizi kullanılır. `new Vector3[4]` ile başlatılır.
    - **UpdateEnemyCount** fonksiyonunda `GameObject.FindGameObjectsWithTag("Enemy")` çağrısı, sahnedeki tüm düşmanları bir dizi (`GameObject[]`) olarak döndürür.

## **9. Trigger ve Collision**

Çarpışma algılama mantığı iki şekilde kurgulanmıştır.

- **OnCollisionEnter**:
    - **EnemyAI.cs**: Düşmana mermi çarptığında fiziksel çarpışmayı algılar. `collision.gameObject.CompareTag("Bullet")` kontrolü ile düşmanın ölmesi ve **GameManager**'a bildirim gönderilmesi sağlanır.
    - **BulletController.cs**: Mermi bir yüzeye çarptığında patlaması için kullanılır.
- **OnTriggerEnter**:
    - **EnemyAI.cs`/`BulletController.cs**: Fiziksel itme kuvveti uygulamadan sadece "iç içe geçme" olayını algılamak için alternatif olarak Trigger metotları da eklenmiştir.

## **10. Animation ve Animator**

Karakterlerin görsel durumlarını kontrol etmek için State Machine yapısı kullanılır.

- **PlayerController.cs**: Karakterin hareket edip etmediğini kontrol eder. `movementDirection.magnitude > 0.1f` ise `animator.SetBool("isWalking", true)` ile yürüme animasyonu tetiklenir.
- **EnemyAI.cs**: Düşmanın durumuna göre animasyon değiştirir.
    - Oyuncuyu kovalarken: `animator.SetBool("isRunning", true)`
    - Ağaca saldırırken: `animator.SetBool("isAttacking", true)`
