using UnityEngine;
using System.Collections.Generic;

public class TerrainScanner : MonoBehaviour
{
    [System.Serializable]
    public struct TerrainType
    {
        public string name;           // Just for your reference (e.g. "Mud")
        public LayerMask layer;       // The Layer (e.g. Mud Layer)
        public float speedMultiplier; // The Speed (e.g. 0.5)
    }

    [Header("Settings")]
    [SerializeField] private float rayLength = 2.0f;

    // A list where you can add as many terrains as you want
    [SerializeField] private TerrainType[] terrainTypes;

    public float GetSpeedMultiplier()
    {
        RaycastHit hit;

        // 1. Shoot ONE ray down to see what we are standing on
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, rayLength))
        {
            // 2. Check the object we hit against our list of Terrain Types
            foreach (TerrainType terrain in terrainTypes)
            {
                // Bitwise check: Does the object's layer match this TerrainType's mask?
                if ((terrain.layer.value & (1 << hit.collider.gameObject.layer)) > 0)
                {
                    return terrain.speedMultiplier;
                }
            }
        }

        return 1.0f; // Default to normal speed if no specific terrain is found
    }
}