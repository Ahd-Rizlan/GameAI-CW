using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
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

    [Header("Connectivity Settings")]
    public int passageWidth = 2;

    [Header("Cluster Terrain Settings")]
    public int waterPatches = 5;
    public int waterPatchSize = 30;
    [Space(5)]
    public int sandPatches = 8;
    public int sandPatchSize = 20;

    [Header("References")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject sandPrefab;
    public GameObject waterPrefab;
    public NavMeshSurface navMeshSurface;
    public Grid customGrid; // <--- ASSIGN YOUR GRID OBJECT HERE IN INSPECTOR

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
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();


        if (useRandomSeed) seed = System.DateTime.Now.Ticks.ToString();

        // 1. Clean up Hierarchy
        if (levelHolder != null) DestroyImmediate(levelHolder.gameObject);

        GameObject holderObj = new GameObject("Level Holder");
        holderObj.transform.parent = this.transform;
        holderObj.transform.localPosition = Vector3.zero;
        levelHolder = holderObj.transform;

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer) DestroyImmediate(existingPlayer);

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in existingEnemies) DestroyImmediate(e);

        // 2. Map Generation
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        // 3. FORCE CONNECTIVITY
        ProcessMap();

        // 4. Apply Terrain details
        ApplyTerrainClusters();

        // 5. Build Visuals
        BuildMesh();

        // 6. Finalize
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(BuildLevelSequence());
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Generation Time: {stopwatch.ElapsedMilliseconds} ms");
    }

    IEnumerator BuildLevelSequence()
    {
        // Wait for visual meshes to spawn
        yield return new WaitForEndOfFrame();

        // A. Update the Custom Grid (For Single Gunner)
        if (customGrid != null)
        {
            customGrid.CreateGrid();
        }

        // B. Update NavMesh (For Double Gunner)
        if (navMeshSurface != null)
        {
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.BuildNavMesh();
        }

        // Wait one frame for the NavMesh/Grid to register
        yield return null;

        // C. Spawn Entities on valid ground
        SpawnEntities();
    }

    // --- CELLULAR AUTOMATA LOGIC ---

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

    // --- CONNECTIVITY LOGIC ---

    void ProcessMap()
    {
        List<List<Vector2Int>> wallRegions = GetRegions(1);
        int wallThresholdSize = 5;
        foreach (List<Vector2Int> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Vector2Int tile in wallRegion) map[tile.x, tile.y] = 0;
            }
        }

        List<List<Vector2Int>> floorRegions = GetRegions(0);
        List<Room> roomList = new List<Room>();

        foreach (List<Vector2Int> region in floorRegions)
        {
            roomList.Add(new Room(region, map));
        }

        if (roomList.Count > 0)
        {
            roomList.Sort();
            roomList[0].isMainRoom = true;
            roomList[0].isAccessibleFromMainRoom = true;
            ConnectClosestRooms(roomList);
        }
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom) roomListB.Add(room);
                else roomListA.Add(room);
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Vector2Int bestTileA = new Vector2Int();
        Vector2Int bestTileB = new Vector2Int();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0) continue;
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB)) continue;

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Vector2Int tileA = roomA.edgeTiles[tileIndexA];
                        Vector2Int tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Vector2Int tileA, Vector2Int tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        List<Vector2Int> line = GetLine(tileA, tileB);
        foreach (Vector2Int coord in line)
        {
            DrawCircle(coord, passageWidth);
        }
    }

    void DrawCircle(Vector2Int c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int drawX = c.x + x;
                    int drawY = c.y + y;
                    if (drawX >= 1 && drawX < width - 1 && drawY >= 1 && drawY < height - 1)
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    List<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> line = new List<Vector2Int>();
        int x = from.x;
        int y = from.y;
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);
        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);
        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }
        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Vector2Int(x, y));
            if (inverted) y += step;
            else x += step;
            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted) x += gradientStep;
                else y += gradientStep;
                gradientAccumulation -= longest;
            }
        }
        return line;
    }

    List<List<Vector2Int>> GetRegions(int tileType)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Vector2Int> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (Vector2Int tile in newRegion)
                    {
                        mapFlags[tile.x, tile.y] = 1;
                    }
                }
            }
        }
        return regions;
    }

    List<Vector2Int> GetRegionTiles(int startX, int startY)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        if (y == tile.y || x == tile.x)
                        {
                            if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                            {
                                mapFlags[x, y] = 1;
                                queue.Enqueue(new Vector2Int(x, y));
                            }
                        }
                    }
                }
            }
        }
        return tiles;
    }

    // --- TERRAIN & MESH ---

    void ApplyTerrainClusters()
    {
        for (int i = 0; i < waterPatches; i++) SpawnCluster(3, waterPatchSize);
        for (int i = 0; i < sandPatches; i++) SpawnCluster(2, sandPatchSize);
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
                if (map[currentX, currentY] != 1) map[currentX, currentY] = tileType;
            }
            int dir = UnityEngine.Random.Range(0, 4);
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
            int x = UnityEngine.Random.Range(1, width - 1);
            int y = UnityEngine.Random.Range(1, height - 1);
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
                    case 1: pos.y = 1; prefab = wallPrefab; break;
                    case 0: prefab = floorPrefab; break;
                    case 2: prefab = sandPrefab; break;
                    case 3: pos.y = 0f; prefab = waterPrefab; break;
                }
                if (prefab != null) Instantiate(prefab, pos, Quaternion.identity, levelHolder);
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
                if (map[x, y] != 1) validTiles.Add(new Vector2Int(x, y));
            }
        }
        if (validTiles.Count == 0) return Vector3.zero;
        Vector2Int rnd = validTiles[UnityEngine.Random.Range(0, validTiles.Count)];
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

// --- HELPER CLASS FOR CONNECTIVITY ---
public class Room : IComparable<Room>
{
    public List<Vector2Int> tiles;
    public List<Vector2Int> edgeTiles;
    public List<Room> connectedRooms;
    public int roomSize;
    public bool isAccessibleFromMainRoom;
    public bool isMainRoom;

    public Room() { }

    public Room(List<Vector2Int> roomTiles, int[,] map)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        connectedRooms = new List<Room>();
        edgeTiles = new List<Vector2Int>();

        foreach (Vector2Int tile in tiles)
        {
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (x == tile.x || y == tile.y)
                    {
                        if (map[x, y] == 1)
                        {
                            edgeTiles.Add(tile);
                        }
                    }
                }
            }
        }
    }

    public void SetAccessibleFromMainRoom()
    {
        if (!isAccessibleFromMainRoom)
        {
            isAccessibleFromMainRoom = true;
            foreach (Room connectedRoom in connectedRooms)
            {
                connectedRoom.SetAccessibleFromMainRoom();
            }
        }
    }

    public static void ConnectRooms(Room roomA, Room roomB)
    {
        if (roomA.isAccessibleFromMainRoom)
        {
            roomB.SetAccessibleFromMainRoom();
        }
        else if (roomB.isAccessibleFromMainRoom)
        {
            roomA.SetAccessibleFromMainRoom();
        }
        roomA.connectedRooms.Add(roomB);
        roomB.connectedRooms.Add(roomA);
    }

    public bool IsConnected(Room otherRoom)
    {
        return connectedRooms.Contains(otherRoom);
    }

    public int CompareTo(Room otherRoom)
    {
        return otherRoom.roomSize.CompareTo(roomSize);
    }
}