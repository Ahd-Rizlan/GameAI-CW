using UnityEngine;
using System.Collections.Generic;

public class TerrainScanner : MonoBehaviour
{
    [System.Serializable]
    public struct TerrainType
    {
        public string name;           
        public LayerMask layer;       
        public float speedMultiplier; 
    }

    [Header("Settings")]
    [SerializeField] private float rayLength = 2.0f;

  
    [SerializeField] private TerrainType[] terrainTypes;

    public float GetSpeedMultiplier()
    {
        RaycastHit hit;

        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, rayLength))
        {
            
            foreach (TerrainType terrain in terrainTypes)
            {
               
                if ((terrain.layer.value & (1 << hit.collider.gameObject.layer)) > 0)
                {
                    return terrain.speedMultiplier;
                }
            }
        }

        return 1.0f;
    }
}