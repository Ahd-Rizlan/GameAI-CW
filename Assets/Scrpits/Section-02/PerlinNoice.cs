using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PerlinNoice : MonoBehaviour
{
    public int TerrainWidth = 256;
    public int TerrainHeight = 256;


    public float scale = 20f;

    public float offsetX = 100f;
    public float offsetY = 100f;



    void Start()
    {
        offsetX  = UnityEngine.Random.Range(0f, 9999f);
        offsetY  = UnityEngine.Random.Range(0f, 9999f);

    }
    void Update()
    {
       Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = GenerateTexture();
    }

    Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D(TerrainWidth, TerrainHeight);
        for (int x = 0; x < TerrainWidth; x++)
        {
            for (int y = 0; y < TerrainHeight; y++)
            {
                Color color = CalculateColor(x, y);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private Color CalculateColor(int x, int y)
    {

        float xCoord = (float)x / TerrainWidth * scale + offsetX;
        float yCoord = (float)y / TerrainHeight * scale + offsetY;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        return new Color(sample, sample, sample);
    }
}
