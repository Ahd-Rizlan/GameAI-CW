using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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

    [Header("UI Elements")]
    [SerializeField] public TMP_Text HP;

    [Header("Defense Stats")]
    [SerializeField] private float dodgeChance = 0.2f;

    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Material")]
    [SerializeField] private Material DodgeMaterial;
    [SerializeField] private Material NormalMaterial;

    private TerrainScanner scanner;
    private Rigidbody rb;
    private Vector3 moveInput;

    void Awake()
    {
        scanner = GetComponent<TerrainScanner>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        UpdateUI();
    }

    void Update()
    {
        // 1. Gather Input in Update
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        // 2. Rotate to face movement direction
        if (moveInput != Vector3.zero)
        {
            transform.forward = moveInput;
        }

        // 3. Shooting
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFire)
        {
            nextFire = Time.time + fireCooldown;
            Shoot();
        }

        // 4. Regen
        if (currentHealth < maxHealth)
        {
            currentHealth += 5f * Time.deltaTime;
            UpdateUI();
        }
    }

    void FixedUpdate()
    {
        // 5. Apply Physics Movement in FixedUpdate to prevent "Sliding" or "Ghosting"
        float speedMultiplier = 1f;
        if (scanner != null) speedMultiplier = scanner.GetSpeedMultiplier();

        Vector3 targetVelocity = moveInput * (speed * speedMultiplier);

        // Apply velocity to Rigidbody (keeps Y velocity for gravity)
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb)
        {
            bulletRb.velocity = transform.forward * bullet.GetComponent<Bullet>().Speed;
        }
    }

    public void TakeDamage(float Damage)
    {
        float roll = Random.value;
        if (roll < dodgeChance)
        {
            if (meshRenderer && DodgeMaterial) meshRenderer.material = DodgeMaterial;
            return;
        }
        meshRenderer.material = NormalMaterial;
        currentHealth -= Damage;
        UpdateUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject, 0.2f);
    }

    void UpdateUI()
    {
        if (HP != null)
        {
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

            if (currentHealth <= maxHealth * 0.3f) HP.color = Color.red;
            else HP.color = Color.green;

            if (Camera.main != null)
            {
                HP.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}