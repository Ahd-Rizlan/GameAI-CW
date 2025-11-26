using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Generation Settings")]
    [Range(50, 250)]
    public int Width = 50;
    [Range(50, 250)]
    public int Depth = 50;
    public int Seed;
    public Vector2 Offset;

    [Header("Perlin Noise Settings")]
    [Range(1, 100)]
    public float NoiseScale = 20f;
    [Range(1, 10)]
    public int Octaves = 4;
    [Range(0, 1)]
    public float Persistance = 0.5f;
    [Range(1, 10)]
    public float Lacunarity = 2f;

    [Header("Advanced: Voronoi Noise")]
    public bool UseVoronoi = false;
    public float VoronoiScale = 5f;
    [Range(0, 1)]
    public float VoronoiBlend = 0.3f;

    [Header("Height Settings")]
    public float HeightMultiplier = 5f;
    public AnimationCurve HeightCurve;
    public Gradient Gradient;

    [Header("Visualization")]
    public bool AutoUpdate = true;
    public bool VisualizeVertices = false;

    // Internal Data
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Color[] colors;
    private float minTerrainHeight;
    private float maxTerrainHeight;

    void Start()
    {
        Seed = UnityEngine.Random.Range(-10000, 10000);
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        GetComponent<MeshFilter>().mesh = mesh;

        GenerateTerrain();
    }

#if UNITY_EDITOR 
    void OnValidate()
    {
        if (Width < 1) Width = 1;
        if (Depth < 1) Depth = 1;
        if (Lacunarity < 1) Lacunarity = 1;
        if (Octaves < 0) Octaves = 0;

        //if (AutoUpdate)
        //{
        //    UnityEditor.EditorApplication.delayCall += () =>
        //    {
        //        if (this == null) return;

        //        if (mesh == null)
        //        {
        //            MeshFilter filter = GetComponent<MeshFilter>();
        //            if (filter != null)
        //            {
        //                if (filter.sharedMesh != null) mesh = filter.sharedMesh;
        //                else mesh = new Mesh();
        //                filter.mesh = mesh;
        //            }
        //        }

        //        if (mesh != null) GenerateTerrain();
        //    };
        //}
    }
#endif

    public void GenerateTerrain()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        if (mesh == null)
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        CreateMesh();
        UpdateMesh();

        if (Application.isPlaying && GetComponent<ObjectSpawner>() != null)
        {
            GetComponent<ObjectSpawner>().SpawnObjects();
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Generation Time: {stopwatch.ElapsedMilliseconds} ms");

    }

    void CreateMesh()
    {
        vertices = new Vector3[(Width + 1) * (Depth + 1)];
        uvs = new Vector2[vertices.Length];
        triangles = new int[Width * Depth * 6];
        colors = new Color[vertices.Length];

        float[] heightMap = GenerateNoiseMap();

        for (int i = 0, z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                float heightPercent = heightMap[i];
                float y = HeightCurve.Evaluate(heightPercent) * HeightMultiplier;

                vertices[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2((float)x / Width, (float)z / Depth);
                colors[i] = Gradient.Evaluate(heightPercent);

                i++;
            }
        }

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + Width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + Width + 1;
                triangles[tris + 5] = vert + Width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    float[] GenerateNoiseMap()
    {
        float[] noiseMap = new float[(Width + 1) * (Depth + 1)];
        System.Random prng = new System.Random(Seed);
        Vector2[] octaveOffsets = new Vector2[Octaves];

        for (int i = 0; i < Octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + Offset.x;
            float offsetY = prng.Next(-100000, 100000) + Offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        float halfWidth = Width / 2f;
        float halfDepth = Depth / 2f;

        for (int i = 0, z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int o = 0; o < Octaves; o++)
                {
                    float sampleX = (x - halfWidth) / NoiseScale * frequency + octaveOffsets[o].x;
                    float sampleY = (z - halfDepth) / NoiseScale * frequency + octaveOffsets[o].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= Persistance;
                    frequency *= Lacunarity;
                }

                if (UseVoronoi)
                {
                    float vX = (float)x / Width;
                    float vZ = (float)z / Depth;
                    float voronoiValue = GetVoronoi(vX, vZ, VoronoiScale);
                    float voronoiAdjusted = (voronoiValue * 2) - 1;
                    noiseHeight = Mathf.Lerp(noiseHeight, voronoiAdjusted, VoronoiBlend);
                }

                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

                noiseMap[i] = noiseHeight;
                i++;
            }
        }

        for (int i = 0; i < noiseMap.Length; i++)
        {
            noiseMap[i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[i]);
        }

        return noiseMap;
    }

    float GetVoronoi(float x, float z, float scale)
    {
        x *= scale;
        z *= scale;
        int iX = Mathf.FloorToInt(x);
        int iZ = Mathf.FloorToInt(z);
        float minDistance = 1.0f;

        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                int neighborX = iX + xOffset;
                int neighborZ = iZ + yOffset;
                Vector2 point = GetRandomPointInCell(neighborX, neighborZ);
                Vector2 diff = point + new Vector2(xOffset, yOffset) - new Vector2(x - iX, z - iZ);
                float distance = diff.magnitude;

                if (distance < minDistance) minDistance = distance;
            }
        }
        return 1.0f - minDistance;
    }

    Vector2 GetRandomPointInCell(int x, int z)
    {
        System.Random prng = new System.Random((x * 89) + (z * 314) + Seed);
        return new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
    }

    public float GetTerrainHeight(int x, int z)
    {
        x = Mathf.Clamp(x, 0, Width);
        z = Mathf.Clamp(z, 0, Depth);

        int index = (z * (Width + 1)) + x;
        if (vertices != null && index < vertices.Length)
        {
            return vertices[index].y;
        }
        return 0;
    }

    private void OnDrawGizmos()
    {
        if (VisualizeVertices && vertices != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < vertices.Length; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
            }
        }
    }
}