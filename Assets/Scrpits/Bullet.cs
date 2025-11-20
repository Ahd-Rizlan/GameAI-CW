using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed = 10f;
    public float damage;
    public Player player;
    public DoubleGunner doubleGunner;



    private void OnCollisionEnter(Collision collision)
    {
        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }


        Destroy(gameObject);
    }
}
