using System.Diagnostics;
using UnityEngine;

public class ArtefactPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           
            Destroy(gameObject);
        }
    }
}