using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed = 10f;
    

         
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
