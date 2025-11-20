using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public Transform bulletSpawn;


    [Header("Health")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float currentHealth;

    [Header("Movement & Shooting")]
    [SerializeField] public float speed = 5f;
    [SerializeField] public float fireCooldown = 0.3f;
    [SerializeField] float nextFire = 0f;

    private TerrainScanner scanner;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;


    void Awake()
    {
        scanner = GetComponent<TerrainScanner>();
        currentHealth = maxHealth;
    }
    void Update()
    {
        Move();
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFire)
        {
            nextFire = Time.time + fireCooldown;
            Shoot();
        }
    }

    void Move()
    {
        float speedMultiplier = 1f;
        if (scanner != null) speedMultiplier = scanner.GetSpeedMultiplier();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(h, 0, v).normalized;
        transform.Translate(direction * (speedMultiplier * speed) * Time.deltaTime, Space.World);
        if (direction != Vector3.zero) transform.forward = direction; 
    }

    void Shoot()
    {

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        rb.velocity = transform.forward * bullet.GetComponent<Bullet>().Speed;

    }



    public void TakeDamage(float Damage)
    {
        currentHealth -= Damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject,0.2f);
    }
}
