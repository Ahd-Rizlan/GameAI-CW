using TMPro;
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
    
    [Header("UI Elements")]
    [SerializeField] public TMP_Text HP;

    private TerrainScanner scanner;


    void Awake()
    {
        
        scanner = GetComponent<TerrainScanner>();
        currentHealth = maxHealth;
        UpdateUI();
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

        if(currentHealth <= maxHealth)
        {
            currentHealth += 5f * Time.deltaTime;
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");
        }
        if(currentHealth <= maxHealth*0.3f)
        {
            HP.color = Color.red;
        }
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

    void UpdateUI()
    {
        if (HP != null)
        {
            
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");
            HP.color = Color.green;


            // Billboard effect
            if (Camera.main != null)
            {
                HP.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
   

}
