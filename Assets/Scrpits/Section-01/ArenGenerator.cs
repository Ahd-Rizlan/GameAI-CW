using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Settings")]
    public int width = 60;
    public int height = 60;

    [Header("Performance Settings")]
    public bool autoUpdate = false; // Check this ONLY if you want live updates

    [Header("Cellular Automata Settings")]
    public string seed;
    public bool useRandomSeed = true;
    [Range(0, 100)]
    public int randomFillPercent = 48;
    [Range(1, 10)]
    public int smoothingIterations = 5;

    [Header("Terrain Settings")]
    public float terrainScale = 0.1f;
    [Range(0, 1)] public float waterThreshold = 0.3f;
    [Range(0, 1)] public float sandThreshold = 0.5f;

    [Header("References")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject sandPrefab;
    public GameObject waterPrefab;
    public Grid pathfindingGrid;

    [Header("Entity Spawning")]
    public GameObject playerPrefab;
    public GameObject sniperBotPrefab;
    public GameObject gunnerBotPrefab;
    public int enemyCount = 3;

    private int[,] map;
    private float terrainSeedOffset;

    void Start()
    {
        GenerateArena();
    }

    // Right-click the script in Inspector -> Select "Generate Now"
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

        // ONLY generate if we explicitly asked for auto-updates
        // This prevents the editor from freezing while dragging sliders
        if (Application.isPlaying && autoUpdate)
        {
            GenerateArena();
        }
    }

    public void GenerateArena()
    {
        // 0. Setup Seeds
        if (useRandomSeed) seed = System.DateTime.Now.Ticks.ToString();
        terrainSeedOffset = Random.Range(0f, 9999f);

        // 1. Clear old stuff
        // Using a while loop is safer for immediate editor destruction
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer) DestroyImmediate(existingPlayer);

        // FindObjectsOfType is expensive, be careful using it often
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in existingEnemies) DestroyImmediate(e);

        // 2. Initialize Map
        map = new int[width, height];
        RandomFillMap();

        // 3. Run Cellular Automata Smoothing
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        // 4. Apply Terrain
        ApplyTerrain();

        // 5. Build Visuals
        BuildMesh();

        // 6. Spawn Entities
        if (Application.isPlaying)
        {
            SpawnEntities();
            // 7. Update Grid (Only needed at runtime)
            StopAllCoroutines();
            StartCoroutine(UpdateGrid());
        }
    }

    void RandomFillMap()
    {
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
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

                if (neighborWallCount > 4)
                    nextMap[x, y] = 1;
                else if (neighborWallCount < 4)
                    nextMap[x, y] = 0;
                else
                    nextMap[x, y] = map[x, y];
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
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    void ApplyTerrain()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == 0)
                {
                    float noiseVal = Mathf.PerlinNoise((x * terrainScale) + terrainSeedOffset, (y * terrainScale) + terrainSeedOffset);

                    if (noiseVal < waterThreshold)
                        map[x, y] = 3;
                    else if (noiseVal < sandThreshold)
                        map[x, y] = 2;
                }
            }
        }
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
                        pos.y = -0.2f;
                        prefab = waterPrefab;
                        break;
                }

                if (prefab != null)
                {
                    Instantiate(prefab, pos, Quaternion.identity, transform);
                }
            }
        }
    }

    void SpawnEntities()
    {
        if (playerPrefab != null)
        {
            Vector3 spawnPos = GetRandomFloorTile();
            if (spawnPos != Vector3.zero)
            {
                ForceSafeGround(spawnPos);
                spawnPos.y = 1f;
                Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = GetRandomFloorTile();
            if (spawnPos != Vector3.zero)
            {
                ForceSafeGround(spawnPos);
                spawnPos.y = 1f;
                GameObject prefabToSpawn = (i % 2 == 0) ? sniperBotPrefab : gunnerBotPrefab;
                if (prefabToSpawn != null) Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }
        }
    }

    Vector3 GetRandomFloorTile()
    {
        List<Vector2Int> validTiles = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == 0 || map[x, y] == 2)
                {
                    validTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (validTiles.Count == 0) return Vector3.zero;

        Vector2Int rnd = validTiles[Random.Range(0, validTiles.Count)];
        float worldX = rnd.x - (width / 2);
        float worldZ = rnd.y - (height / 2);
        return new Vector3(worldX, 0, worldZ);
    }

    void ForceSafeGround(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + (width / 2));
        int y = Mathf.RoundToInt(worldPos.z + (height / 2));

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (map[x, y] == 3 || map[x, y] == 1) map[x, y] = 0;
        }
    }

    IEnumerator UpdateGrid()
    {
        yield return new WaitForEndOfFrame();
        if (pathfindingGrid != null)
        {
            pathfindingGrid.SendMessage("CreateGrid", SendMessageOptions.DontRequireReceiver);
        }
    }
}