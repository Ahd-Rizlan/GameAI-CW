using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed = 10f;
    public float damage= 20f;
    public Player player;
    public DoubleGunner doubleGunner;



    private void OnCollisionEnter(Collision collision)
    {
        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }


        Destroy(gameObject);
    }
}
