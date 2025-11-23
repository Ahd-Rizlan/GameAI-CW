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
    public Transform playerTransform; // Drag your Player here

    [Header("Artefact Settings")]
    public GameObject[] artefactPrefabs; // Drag your 6 prefabs here
    public int amountPerType = 5; // Requirement: At least 3 of each [cite: 116]

    [Header("Visualization")]
    public bool showPathLines = true; // Requirement: Toggle visualization [cite: 121]
    public LineRenderer lineRendererPrefab; // Optional: To draw lines

    public void SpawnObjects()
    {
        // 1. Bake the NavMesh on the newly generated terrain
        navMeshSurface.BuildNavMesh();

        // 2. Loop through each artefact type
        foreach (GameObject prefab in artefactPrefabs)
        {
            int spawnedCount = 0;
            int attempts = 0;

            while (spawnedCount < amountPerType && attempts < 100)
            {
                attempts++;

                // A. Pick a random spot
                float randX = UnityEngine.Random.Range(0, terrainGenerator.Width);
                float randZ = UnityEngine.Random.Range(0, terrainGenerator.Depth);

                // B. Check Height (Don't spawn underwater)
                float height = terrainGenerator.GetTerrainHeight((int)randX, (int)randZ);

                // Assuming water is below a certain height (e.g., 30% of max height)
                // You can tweak this logic based on your specific curve
                if (height < 2f) continue; // Too low/underwater

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
        if (playerTransform == null) return true; // Skip check if no player

        NavMeshPath path = new NavMeshPath();
        // Find closest point on NavMesh to our candidate position
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            // Calculate path from Player to Object
            NavMesh.CalculatePath(playerTransform.position, hit.position, NavMesh.AllAreas, path);

            // If path is Complete, it's valid
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

        // Simple debug draw (visible in Scene view)
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            UnityEngine.Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.cyan, 10f);
        }
    }
}
