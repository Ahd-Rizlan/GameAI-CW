using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class ObjectSpawner : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;
    public NavMeshSurface navMeshSurface;
    public Transform playerTransform;

    [Header("Artefact Settings")]
    public GameObject[] artefactPrefabs;
    public int amountPerType = 3; 

    [Header("Spawn Restrictions")]
    [Tooltip("Any terrain lower than this is considered Deep Water")]
    public float minSpawnHeight = 2.0f;

    [Tooltip("Any terrain higher than this is considered Mountain Peak")]
    public float maxSpawnHeight = 15.0f;

    [Header("Visualization")]
    public bool showPathLines = true; 
    public LineRenderer lineRendererPrefab;

    public void SpawnObjects()
    {
        // 1. Bake the NavMesh on the newly generated terrain
        navMeshSurface.BuildNavMesh();

        // 2. Loop through each artefact type
        foreach (GameObject prefab in artefactPrefabs)
        {
            int spawnedCount = 0;
            int attempts = 0;

            // Try 100 times to find a valid spot for this object
            while (spawnedCount < amountPerType && attempts < 100)
            {
                attempts++;

                // A. Pick a random spot
                float randX = UnityEngine.Random.Range(0, terrainGenerator.Width);
                float randZ = UnityEngine.Random.Range(0, terrainGenerator.Depth);

                // B. Get Height
                float height = terrainGenerator.GetTerrainHeight((int)randX, (int)randZ);

                // --- NEW LOGIC: Filter out Water and Mountains ---
                // If it is too low (Water) OR too high (Mountain Peak), skip it
                if (height < minSpawnHeight || height > maxSpawnHeight)
                {
                    continue;
                }

                Vector3 candidatePos = new Vector3(randX, height, randZ);

                if (IsReachable(candidatePos))
                {
                    Instantiate(prefab, candidatePos, Quaternion.identity);
                    spawnedCount++;
                }
            }
        }
    }

    bool IsReachable(Vector3 targetPos)
    {
        if (playerTransform == null) return true;

        NavMeshPath path = new NavMeshPath();

        // Increased search radius to 10f to handle uneven terrain better
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
        {
            NavMesh.CalculatePath(playerTransform.position, hit.position, NavMesh.AllAreas, path);

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                if (showPathLines) DrawDebugPath(path);
                return true;
            }
        }
        return false;
    }

    void DrawDebugPath(NavMeshPath path)
    {
        // 1. Scene View Debug Lines (For Developer)
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            UnityEngine.Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.cyan, 10f);
        }

        // 2. Game View Visualization (For Coursework)
        if (lineRendererPrefab != null)
        {
            // Create the line object
            LineRenderer line = Instantiate(lineRendererPrefab);

            // Set the number of points
            line.positionCount = path.corners.Length;

            // --- THE FIX: Lift the line slightly above the ground ---
            Vector3[] liftedCorners = new Vector3[path.corners.Length];
            for (int i = 0; i < path.corners.Length; i++)
            {
                // Add 0.5f to the Y axis so it floats above grass/water
                liftedCorners[i] = path.corners[i] + Vector3.up * 0.5f;
            }

            // Assign the lifted points to the line
            line.SetPositions(liftedCorners);

            // Cleanup: Destroy the line after 10 seconds
            Destroy(line.gameObject, 10f);
        }
    }
}
