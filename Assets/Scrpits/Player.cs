using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;
    public int health = 100;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float fireCooldown = 0.3f;
    float nextFire = 0f;
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
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(h, 0, v).normalized;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        if (direction != Vector3.zero) transform.forward = direction; 
    }

    void Shoot()
    {

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        rb.velocity = transform.forward * bullet.GetComponent<Bullet>().Speed;

    }



//public void TakeDamage(int damage)
//    {
//        health -= damage;
//        if (health <= 0)
//        {
//            Die();
//        }
//    }

//    void Die()
//    {
//        // Handle player death (e.g., respawn, game over)
//        Debug.Log("Player has died.");
//        Destroy(gameObject);
//    }
}
