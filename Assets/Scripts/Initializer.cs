using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class Initializer : MonoBehaviour
{
    public Texture2D terrainTexture;
    public Texture2D waterTexture;

    // this is the thresold to check if color is black or not
    private const float threshold = 0.01f;

    public struct SpawnData
    {
        public int[] cellTypes;
        public float[] densities;
        public float[] vrVelocities;
        public float[] hrVelocities;

        public SpawnData(int numCells)
        {
            // note: in C#, arrays are initialized to default values (not garbage) (it's 0)
            cellTypes = new int[numCells];
            densities = new float[numCells];
            vrVelocities = new float[numCells];
            hrVelocities = new float[numCells];
        }
    }

    public SpawnData GetSpawnData(Vector2Int numCells)
    {
        int totalCells = numCells.x * numCells.y;
        var data = new SpawnData(totalCells);
        
        // generate terrain from texture
        if (terrainTexture != null && terrainTexture.isReadable)
        {
            int textureWidth = terrainTexture.width;
            int textureHeight = terrainTexture.height;

            for (int row = 0; row < numCells.y; row++)
            {
                for (int col = 0; col < numCells.x; col++)
                {
                    int cellIndex = row * numCells.x + col;

                    // calculate where in the pixel space we are sampling from
                    float normalizedX = (col + 0.5f) / numCells.x;
                    float normalizedY = (row + 0.5f) / numCells.y;

                    int pixelX = Mathf.FloorToInt(normalizedX *textureWidth);
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

            for (int row = 0; row < numCells.y; row++)
            {
                for (int col = 0; col < numCells.x; col++)
                {
                    int cellIndex = row * numCells.x + col;

                    // calculate where in the pixel space we are sampling from
                    float normalizedX = (col + 0.5f) / numCells.x;
                    float normalizedY = (row + 0.5f) / numCells.y;

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
                        data.cellTypes[cellIndex] = 0;
                        data.densities[cellIndex] = 1;
                    }
                }
            }
        }
        else
        {
            // in case there is no texture set
            Debug.LogError(waterTexture == null ? "level layout is not assigned" : "Failed To Read Level Image.");
        }
        
        // generate bounding box
        for (int i = 0; i < numCells.x; i++)
        {
            data.cellTypes[i] = 2;
            data.cellTypes[numCells.x * (numCells.y - 1) + i] = 2;
        }

        for (int i = 1; i < numCells.y - 1; i++)
        {
            data.cellTypes[numCells.x * i] = 2;
            data.cellTypes[numCells.x * i + numCells.x - 1] = 2;
        }

        // generate random velocities
        // var rng = new Unity.Mathematics.Random(42);
        // for (int i = 0; i < totalCells; i++)
        // {
        //     data.vrVelocities[i] = rng.NextFloat() - 0.5f;
        //     data.hrVelocities[i] = rng.NextFloat() - 0.5f;
        // }

        return data;
    }
}