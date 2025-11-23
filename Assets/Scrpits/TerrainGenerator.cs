using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain generation")]
    public int Width;
    public int Depth;
    public Gradient gradient;
    public int Seed;
    [Range(1, 100)]
    public int Octaves;
    [Range(1, 100)]
    public float NoiseScale;
    [Range(0, 1)]
    public float Persistance;
    [Range(1, 100)]
    public float Lacunarity;
    [Range(1, 100)]
    public float HeightMultiplier;
    [Range(0, 1)]
    public float HeightTreshhold;
    public Vector2 Offset;

    [Header("Vertex visualization")]
    public GameObject VertexObject;
    public bool VisualizeVertices;

    [Header("Voronoi Settings")]
    public bool UseVoronoi = true;
    public float VoronoiScale = 5f;
    [Range(0, 1)]
    public float VoronoiBlend = 0.5f;

    private Vector3[] vertices;
    private int[] trianglePoints;
    Vector2[] uvs;
    Color[] colors;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private float minHeight;
    private float maxHeight;
    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        CreateMesh();
        UpdateMesh();
        if (VisualizeVertices)
        {
            DrawVertices();
        }
    }

    private void Update()
    {
        CreateMesh();
        UpdateMesh();

    }
    private void DrawVertices()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Instantiate(VertexObject, vertices[i], Quaternion.identity, transform);
        }
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = trianglePoints;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    void CreateMesh()
    {
        //Vertices
        vertices = new Vector3[(Width + 1) * (Depth + 1)];
        var noiseArray = PerlinNoise();

        int i = 0;
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                var currentHeight = noiseArray[i];
                if (currentHeight > HeightTreshhold)
                {
                    currentHeight *= HeightMultiplier;
                }
                vertices[i] = new Vector3(x, currentHeight, z);
                i++;
            }
        }

        //Triangles
        trianglePoints = new int[Width * Depth * 6];
        int currentTrianglePoint = 0;
        int currentVertexPoint = 0;

        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                trianglePoints[currentTrianglePoint + 0] = currentVertexPoint + 0;
                trianglePoints[currentTrianglePoint + 1] = currentVertexPoint + Width + 1;
                trianglePoints[currentTrianglePoint + 2] = currentVertexPoint + 1;
                trianglePoints[currentTrianglePoint + 3] = currentVertexPoint + 1;
                trianglePoints[currentTrianglePoint + 4] = currentVertexPoint + Width + 1;
                trianglePoints[currentTrianglePoint + 5] = currentVertexPoint + Width + 2;

                currentVertexPoint++;
                currentTrianglePoint += 6;
            }
            currentVertexPoint++;
        }

        //UVs
        uvs = new Vector2[vertices.Length];
        i = 0;
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                uvs[i] = new Vector2((float)x / Width, (float)z / Depth);
                i++;
            }
        }

        //Colors
        colors = new Color[vertices.Length];
        i = 0;
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                float height = Mathf.InverseLerp(minHeight * HeightMultiplier, maxHeight * HeightMultiplier, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }
    }

    float[] PerlinNoise()
    {
        float[] noiseArray = new float[(Width + 1) * (Depth + 1)];

        System.Random prng = new System.Random(Seed);
        Vector2[] octaveOffsets = new Vector2[Octaves];
        for (int i = 0; i < Octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + Offset.x;
            float offsetY = prng.Next(-100000, 100000) + Offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float halfWidth = Width / 2f;
        float halfDepth = Depth / 2f;

        //Apply lacunarity and persistence
        int n = 0;
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                //Use multiple frequencies (octaves)
                for (int i = 0; i < Octaves; i++)
                {
                    float sampleX = (x - halfWidth) / NoiseScale * frequency + octaveOffsets[i].x;
                    float sampleY = (z - halfDepth) / NoiseScale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= Persistance;
                    frequency *= Lacunarity;

                    if (UseVoronoi)
                    {
                        // Calculate Voronoi value for this specific coordinate
                        // We normalize x/width so it fits the 0-1 range before scaling
                        float vX = (float)x / Width;
                        float vZ = (float)z / Depth;

                        float voronoiValue = GetVoronoi(vX, vZ, VoronoiScale);

                        // Blend: Linear interpolation between the Perlin noise height and Voronoi
                        // Note: noiseHeight might be > 1 or < -1 due to octaves, 
                        // but Voronoi is 0-1. You might need to tune 'VoronoiBlend'.

                        noiseHeight = Mathf.Lerp(noiseHeight, noiseHeight * voronoiValue, VoronoiBlend);
                        // OR for a different look: noiseHeight += voronoiValue * VoronoiBlend;
                    }
                    // --- NEW VORONOI CODE END ---
                }

                if (noiseHeight > maxHeight)
                {
                    maxHeight = noiseHeight;
                }
                else if (noiseHeight < minHeight)
                {
                    minHeight = noiseHeight;
                }
                noiseArray[n] = noiseHeight;
                n++;
            }
        }

        //Normalize height
        int k = 0;
        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                noiseArray[k] = Mathf.InverseLerp(minHeight, maxHeight, noiseArray[k]);
                k++;
            }
        }

        return noiseArray;
    }


    float GetVoronoi(float x, float z, float scale)
    {
        // Scale the coordinates
        x *= scale;
        z *= scale;

        // Determine the integer grid cell we are in
        int iX = Mathf.FloorToInt(x);
        int iZ = Mathf.FloorToInt(z);

        float minDistance = 1.0f;

        // Check the current cell and the 8 surrounding neighbors
        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                // Neighbor cell coordinates
                int neighborX = iX + xOffset;
                int neighborZ = iZ + yOffset;

                // Generate a random point within that neighbor cell based on its coordinate (pseudo-random)
                // We use a hash function to get the same random point for the same cell every time
                Vector2 point = GetRandomPointInCell(neighborX, neighborZ);

                // Get the position of that point in world space relative to our current pixel
                Vector2 diff = point + new Vector2(xOffset, yOffset) - new Vector2(x - iX, z - iZ);

                // measure distance
                float distance = diff.magnitude;

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        // Invert so centers are high (peaks) and edges are low
        return 1.0f - minDistance;
    }

    // Helper to get a deterministic random point for a cell
    Vector2 GetRandomPointInCell(int x, int z)
    {
        // Simple pseudo-random hash based on coordinates
        System.Random prng = new System.Random((x * 89) + (z * 314) + Seed);
        float pX = (float)prng.NextDouble();
        float pZ = (float)prng.NextDouble();
        return new Vector2(pX, pZ);
    }
}
