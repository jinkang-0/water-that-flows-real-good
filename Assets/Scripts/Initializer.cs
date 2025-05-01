using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Initializer : MonoBehaviour
{
    public Texture2D terrainTexture;
    public Texture2D waterTexture;

    // this is the threshold to check if color is black or not
    private const float threshold = 0.01f;

    public struct SpawnData
    {
        public int[] cellTypes;
        public float[] cellWeights;
        public float2[] cellVelocities;
        public float2[] positions;
        public float2[] particleVelocities;
        public bool[] disabledParticles;
        public bool[] isCellBucket;

        public SpawnData(int numCells, int numParticles)
        {
            // note: in C#, arrays are initialized to default values (not garbage) (it's 0)
            cellTypes = new int[numCells];
            cellWeights = new float[numCells];
            positions = new float2[numParticles];
            cellVelocities = new float2[numCells];
            particleVelocities = new float2[numParticles];
            disabledParticles = new bool[numParticles];
            isCellBucket = new bool[numCells];
        }
    }

    public SpawnData GetSpawnData(Vector2Int gridSize, int numParticles)
    {
        int totalCells = gridSize.x * gridSize.y;
        var data = new SpawnData(totalCells, numParticles);
        
        // generate terrain from texture
        if (terrainTexture != null && terrainTexture.isReadable)
        {
            int textureWidth = terrainTexture.width;
            int textureHeight = terrainTexture.height;

            for (int row = 0; row < gridSize.y; row++)
            {
                for (int col = 0; col < gridSize.x; col++)
                {
                    int cellIndex = row * gridSize.x + col;

                    // calculate where in the pixel space we are sampling from
                    float normalizedX = (col + 0.5f) / gridSize.x;
                    float normalizedY = (row + 0.5f) / gridSize.y;

                    int pixelX = Mathf.FloorToInt(normalizedX * textureWidth);
                    int pixelY = Mathf.FloorToInt(normalizedY * textureHeight);

                    // clamp the coordinates
                    pixelX = Mathf.Clamp(pixelX, 0, textureWidth - 1);
                    pixelY = Mathf.Clamp(pixelY, 0, textureHeight - 1);

                    // use the Get Pixel method to see the color
                    Color pixelColor = terrainTexture.GetPixel(pixelX, pixelY);

                    // check if the pixel is black or not
                    if (pixelColor.r > threshold || pixelColor.g > threshold || pixelColor.b > threshold)
                        data.cellTypes[cellIndex] = 1;
                    else
                        data.cellTypes[cellIndex] = 0;
                }
            }
        }
        else
        {
            // in case there is no level texture set
            Debug.LogError(terrainTexture == null ? "level layout is not assigned" : "Failed To Read Level Image.");
        }
        
        // generate water from texture
        if (waterTexture != null && waterTexture.isReadable)
        {
            int textureWidth = waterTexture.width;
            int textureHeight = waterTexture.height;
            var waterCells = new List<int>();

            for (int row = 0; row < gridSize.y; row++)
            {
                for (int col = 0; col < gridSize.x; col++)
                {
                    // calculate where in the pixel space we are sampling from
                    float normalizedX = (col + 0.5f) / gridSize.x;
                    float normalizedY = (row + 0.5f) / gridSize.y;

                    int pixelX = Mathf.FloorToInt(normalizedX *textureWidth);
                    int pixelY = Mathf.FloorToInt(normalizedY * textureHeight);

                    // clamp the coordinates
                    pixelX = Mathf.Clamp(pixelX, 0, textureWidth - 1);
                    pixelY = Mathf.Clamp(pixelY, 0, textureHeight - 1);

                    // use the Get Pixel method to see the color
                    Color pixelColor = waterTexture.GetPixel(pixelX, pixelY);

                    // check if the pixel is filled
                    if (pixelColor.a > threshold)
                    {
                        int cellIndex = row * gridSize.x + col;
                        data.cellTypes[cellIndex] = 3;
                        waterCells.Add(cellIndex);
                    }
                }
            }

            var rng = new Unity.Mathematics.Random(42);
            int particlesPerCell = numParticles / waterCells.Count;
            int numExtras = numParticles % waterCells.Count;
            int count = 0;
            for (int i = 0; i < waterCells.Count; i++)
            {
                int idx = waterCells[i];
                int row = idx / gridSize.x;
                int col = idx % gridSize.x;
                float2 gridPos = new float2(col, row);
                
                for (int j = 0; j < particlesPerCell; j++)
                {
                    data.positions[count] = gridPos + rng.NextFloat2();
                    count++;
                }

                if (numExtras > 0)
                {
                    data.positions[count] = gridPos + rng.NextFloat2();
                    count++;
                    numExtras--;
                }
            }
        }
        else
        {
            // in case there is no texture set
            if (waterTexture == null)
                Debug.Log("no water layout assigned, spawning water at the center");
            else
                Debug.LogError("Failed To Read Level Image.");
            
            // spawn particles in middle of screen
            var rng = new Unity.Mathematics.Random(42);
            var spread = 20f;
            float2 center = new float2(gridSize.x, gridSize.y) / 2;
            for (int i = 0; i < numParticles; i++)
            {
                data.positions[i] = center + spread * (rng.NextFloat2() - 0.5f);
            }
        }
        
        // generate bounding box
        for (int i = 0; i < gridSize.x; i++)
        {
            data.cellTypes[i] = 2;
            data.cellTypes[gridSize.x * (gridSize.y - 1) + i] = 2;
        }

        for (int i = 1; i < gridSize.y - 1; i++)
        {
            data.cellTypes[gridSize.x * i] = 2;
            data.cellTypes[gridSize.x * i + gridSize.x - 1] = 2;
        }

        // generate random velocities
        // var rng = new Unity.Mathematics.Random(42);
        // for (int i = 0; i < totalCells; i++)
        // {
        //     data.vrVelocities[i] = 0;
        //     data.hrVelocities[i] = -10;
        // }

        return data;
    }
}