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

    private Transform currentPlayerTransform;
    private Transform spawnContainer;

    [Header("Player Settings")]
    public GameObject playerPrefab;

    [Header("Artefact Settings")]
    public GameObject[] artefactPrefabs;
    public int artefactCount = 3;
    [Tooltip("How high the artefacts float above the ground")]
    public float artefactHoverHeight = 1.5f; 

    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;
    public int enemyCount = 5;

    [Header("Spawn Restrictions")]
    public float minSpawnHeight = 2.0f;
    public float maxSpawnHeight = 15.0f;
    [Range(0f, 60f)]
    public float maxSlopeAngle = 30f;

    [Header("Visualization")]
    public bool showPathLines = true;
    public LineRenderer lineRendererPrefab;

    public void SpawnObjects()
    {
        CleanupOldObjects();
        navMeshSurface.BuildNavMesh();

        SpawnPlayer();

      
        SpawnGroup(artefactPrefabs, artefactCount, "Artefact", artefactHoverHeight);

       
        SpawnGroup(enemyPrefabs, enemyCount, "Enemy", 0f);
    }

    void CleanupOldObjects()
    {
        if (spawnContainer != null) Destroy(spawnContainer.gameObject);
        else
        {
            GameObject old = GameObject.Find("--- Spawned Objects Container ---");
            if (old != null) Destroy(old);
        }

        GameObject container = new GameObject("--- Spawned Objects Container ---");
        spawnContainer = container.transform;
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        for (int i = 0; i < 100; i++)
        {
            if (GetValidNavMeshPosition(out Vector3 validPos))
            {
                GameObject p = Instantiate(playerPrefab, validPos, Quaternion.identity);
                p.transform.parent = spawnContainer;
                currentPlayerTransform = p.transform;
                Debug.Log("Player Spawned at: " + validPos);
                return;
            }
        }
        Debug.LogError("Could not find a valid spawn point for Player!");
    }

    
    void SpawnGroup(GameObject[] prefabs, int count, string groupName, float heightOffset)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int spawnedCount = 0;
        int attempts = 0;

        while (spawnedCount < count && attempts < 200)
        {
            attempts++;
            GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Length)];

            if (GetValidNavMeshPosition(out Vector3 validPos))
            {
                if (IsReachable(validPos))
                {
                    GameObject obj = Instantiate(prefabToSpawn, validPos, Quaternion.identity);

                    
                    obj.transform.position += Vector3.up * heightOffset;

                    obj.transform.parent = spawnContainer;
                    spawnedCount++;
                }
            }
        }
        Debug.Log($"Spawned {spawnedCount} / {count} {groupName}s.");
    }

    bool GetValidNavMeshPosition(out Vector3 finalPosition)
    {
        finalPosition = Vector3.zero;

        float randX = UnityEngine.Random.Range(0, terrainGenerator.Width);
        float randZ = UnityEngine.Random.Range(0, terrainGenerator.Depth);
        float rawHeight = terrainGenerator.GetTerrainHeight((int)randX, (int)randZ);

        if (rawHeight < minSpawnHeight || rawHeight > maxSpawnHeight) return false;

        Vector3 candidate = new Vector3(randX, rawHeight, randZ);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) return false;

            finalPosition = hit.position;
            return true;
        }

        return false;
    }

    bool IsReachable(Vector3 targetPos)
    {
        if (currentPlayerTransform == null) return false;

        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(currentPlayerTransform.position, targetPos, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            if (showPathLines) DrawDebugPath(path);
            return true;
        }
        return false;
    }

    void DrawDebugPath(NavMeshPath path)
    {
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.cyan, 10f);
        }

        if (lineRendererPrefab != null)
        {
            LineRenderer line = Instantiate(lineRendererPrefab);
            line.positionCount = path.corners.Length;
            Vector3[] liftedCorners = new Vector3[path.corners.Length];
            for (int i = 0; i < path.corners.Length; i++)
            {
                liftedCorners[i] = path.corners[i] + Vector3.up * 0.5f;
            }
            line.SetPositions(liftedCorners);
            Destroy(line.gameObject, 10f);
        }
    }
}