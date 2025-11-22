using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float Speed = 10f;

    [Header("Damage Randomization")]
    public float minDamage = 10f;
    public float maxDamage = 20f;


    [HideInInspector]
    public GameObject owner;
    private void OnCollisionEnter(Collision collision)
    {
        if (owner != null && collision.gameObject == owner) return;

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            float actualDamage = Random.Range(minDamage, maxDamage);
            target.TakeDamage(actualDamage);
        }


        Destroy(gameObject);
    }
}
