using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// Note: If you get an error here, check "Window > Package Manager > Unity Registry" and install "AI Navigation"
using Unity.AI.Navigation;

public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Settings")]
    public int width = 60;
    public int height = 60;

    [Header("Performance Settings")]
    public bool autoUpdate = false;

    [Tooltip("Click this checkbox to generate the map immediately.")]
    public bool generateNow = false;

    [Header("Cellular Automata Settings")]
    public string seed;
    public bool useRandomSeed = true;
    [Range(0, 100)]
    public int randomFillPercent = 48;
    [Range(1, 10)]
    public int smoothingIterations = 5;

    [Header("Cluster Terrain Settings")]
    [Tooltip("How many separate pools of water to spawn")]
    public int waterPatches = 5;
    [Tooltip("How many tiles big each water pool should be")]
    public int waterPatchSize = 30;

    [Space(5)]
    [Tooltip("How many separate patches of mud/sand to spawn")]
    public int sandPatches = 8;
    [Tooltip("How many tiles big each mud patch should be")]
    public int sandPatchSize = 20;

    [Header("References")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject sandPrefab;
    public GameObject waterPrefab;

    // Optional: If you are using Unity's new NavMeshSurface
    public NavMeshSurface navMeshSurface;

    [Header("Entity Spawning")]
    public GameObject playerPrefab;
    public GameObject sniperBotPrefab;
    public GameObject gunnerBotPrefab;
    public int enemyCount = 3;

    private int[,] map;
    private Transform levelHolder;

    void Start()
    {
        GenerateArena();
    }

    [ContextMenu("Generate Now")]
    public void GenerateNow()
    {
        GenerateArena();
    }

    void OnValidate()
    {
        if (width < 20) width = 20;
        if (height < 20) height = 20;
        if (smoothingIterations < 0) smoothingIterations = 0;

        if (generateNow)
        {
            generateNow = false;
            GenerateArena();
        }

        if (Application.isPlaying && autoUpdate)
        {
            GenerateArena();
        }
    }

    public void GenerateArena()
    {
        if (useRandomSeed) seed = System.DateTime.Now.Ticks.ToString();

        // 1. Clean up Hierarchy
        if (levelHolder != null)
        {
            DestroyImmediate(levelHolder.gameObject);
        }

        GameObject holderObj = new GameObject("Level Holder");
        holderObj.transform.parent = this.transform;
        holderObj.transform.localPosition = Vector3.zero;
        levelHolder = holderObj.transform;

        // Clean up entities from previous run
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer) DestroyImmediate(existingPlayer);

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in existingEnemies) DestroyImmediate(e);

        // 2. Map Logic
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        ApplyTerrainClusters();

        // 3. Build Visuals
        BuildMesh();

        // 4. Finalize (Bake THEN Spawn)
        if (Application.isPlaying)
        {
            // Stop any previous build routines
            StopAllCoroutines();
            // Start the sequence: Wait -> Bake -> Spawn
            StartCoroutine(BuildLevelSequence());
        }
    }

    IEnumerator BuildLevelSequence()
    {
        // A. Wait for visual meshes (walls/floors) to initialize
        yield return new WaitForEndOfFrame();

        // B. Bake the NavMesh
        if (navMeshSurface != null)
        {
            // Only look at the children of this generator (The Level Holder)
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.BuildNavMesh();
        }

        // C. Wait one more frame to ensure NavMesh is valid in the system
        yield return null;

        // D. NOW it is safe to spawn entities. They will wake up on valid ground.
        SpawnEntities();
    }

    void RandomFillMap()
    {
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    map[x, y] = 1;
                else
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        int[,] nextMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallCount = GetSurroundingWallCount(x, y);
                if (neighborWallCount > 4) nextMap[x, y] = 1;
                else if (neighborWallCount < 4) nextMap[x, y] = 0;
                else nextMap[x, y] = map[x, y];
            }
        }
        map = nextMap;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        if (map[neighborX, neighborY] == 1) wallCount++;
                    }
                }
                else wallCount++;
            }
        }
        return wallCount;
    }

    void ApplyTerrainClusters()
    {
        for (int i = 0; i < waterPatches; i++)
        {
            SpawnCluster(3, waterPatchSize);
        }
        for (int i = 0; i < sandPatches; i++)
        {
            SpawnCluster(2, sandPatchSize);
        }
    }

    void SpawnCluster(int tileType, int size)
    {
        Vector2Int startPos = GetRandomFloorCoord();
        if (startPos == new Vector2Int(-1, -1)) return;

        int currentX = startPos.x;
        int currentY = startPos.y;

        for (int i = 0; i < size; i++)
        {
            if (currentX > 0 && currentX < width - 1 && currentY > 0 && currentY < height - 1)
            {
                if (map[currentX, currentY] != 1)
                {
                    map[currentX, currentY] = tileType;
                }
            }

            int dir = Random.Range(0, 4);
            switch (dir)
            {
                case 0: currentX++; break;
                case 1: currentX--; break;
                case 2: currentY++; break;
                case 3: currentY--; break;
            }
        }
    }

    Vector2Int GetRandomFloorCoord()
    {
        for (int i = 0; i < 20; i++)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);
            if (map[x, y] == 0) return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }

    void BuildMesh()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x - (width / 2), 0, y - (height / 2));
                GameObject prefab = null;

                switch (map[x, y])
                {
                    case 1:
                        pos.y = 1;
                        prefab = wallPrefab;
                        break;
                    case 0:
                        prefab = floorPrefab;
                        break;
                    case 2:
                        prefab = sandPrefab;
                        break;
                    case 3:
                        pos.y = 0f;
                        prefab = waterPrefab;
                        break;
                }

                if (prefab != null)
                {
                    Instantiate(prefab, pos, Quaternion.identity, levelHolder);
                }
            }
        }
    }

    void SpawnEntities()
    {
        if (playerPrefab != null)
        {
            Vector3 spawnPos = GetRandomSpawnTile();
            if (spawnPos != Vector3.zero)
            {
                ForceSafeGround(spawnPos);
                spawnPos.y = 1f;
                Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnTile();
            if (spawnPos != Vector3.zero)
            {
                ForceSafeGround(spawnPos);
                spawnPos.y = 1f;
                GameObject prefabToSpawn = (i % 2 == 0) ? sniperBotPrefab : gunnerBotPrefab;
                if (prefabToSpawn != null) Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }
        }
    }

    Vector3 GetRandomSpawnTile()
    {
        List<Vector2Int> validTiles = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] != 1)
                    validTiles.Add(new Vector2Int(x, y));
            }
        }

        if (validTiles.Count == 0) return Vector3.zero;
        Vector2Int rnd = validTiles[Random.Range(0, validTiles.Count)];
        return new Vector3(rnd.x - (width / 2), 0, rnd.y - (height / 2));
    }

    void ForceSafeGround(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + (width / 2));
        int y = Mathf.RoundToInt(worldPos.z + (height / 2));
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (map[x, y] == 1) map[x, y] = 0;
        }
    }
}