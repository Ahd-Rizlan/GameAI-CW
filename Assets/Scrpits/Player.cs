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

    [SerializeField] public float bulletSpeed = 20f;
    [SerializeField] public float bullet_Min_Damage = 10f;
    [SerializeField] public float bullet_Max_Damage = 20f;


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
       
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        
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
        float speedMultiplier = 1f;
        if (scanner != null) speedMultiplier = scanner.GetSpeedMultiplier();

        Vector3 targetVelocity = moveInput * (speed * speedMultiplier);

        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
    }

    void Shoot()
    {
       

        if (bulletSpawn == null)return;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        Bullet script = bullet.GetComponent<Bullet>();

        if (script != null)
        {
            script.minDamage = bullet_Min_Damage;
            script.maxDamage = bullet_Max_Damage;
            script.Speed = bulletSpeed;
            script.owner = this.gameObject;
        }


        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        rb.velocity = bulletSpawn.forward * (script ? script.Speed : 10f);

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