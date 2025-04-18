using UnityEngine;
using Unity.Mathematics;

public class Initializer : MonoBehaviour
{
    public Texture2D levelLayoutTexture;
    

    // this is the thresold to check if color is black or not
    private const float blackThreshold = 0.01f;

    public struct SpawnData
    {
        public int[] cellTypes;
        public float[] vrVelocities;
        public float[] hrVelocities;

        public SpawnData(int numCells)
        {
            cellTypes = new int[numCells];
            vrVelocities = new float[numCells];
            hrVelocities = new float[numCells];
        }
    }

    public SpawnData GetSpawnData(Vector2Int numCells)
    {
        int totalCells = numCells.x * numCells.y;
        var data = new SpawnData(totalCells);

        // generate layout from texture
        if (levelLayoutTexture != null && levelLayoutTexture.isReadable)
        {
            int textureWidth = levelLayoutTexture.width;
            int textureHeight = levelLayoutTexture.height;

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
                    Color pixelColor = levelLayoutTexture.GetPixel(pixelX, pixelY);

                    // check if the pixel is black or not
                    if (pixelColor.r > blackThreshold || pixelColor.g > blackThreshold || pixelColor.b > blackThreshold)
                    {
                        data.cellTypes[cellIndex] = 1;
                    }
                    else
                    {
                        data.cellTypes[cellIndex] = 0;
                    }
                }
            }
        }
        else
        {   
            // in case there is no level texture set
            if (levelLayoutTexture == null)
            {
                Debug.LogError("level layout is not assigned");
            }
            else 
            {
                Debug.LogError("Failed To Read Level Image.");
            }
            for (int i = 0; i < totalCells; i++)
            {
                data.cellTypes[i] = 0;
            }
        }


        for (int i = 0; i < numCells.x; i++)
        { 
              data.cellTypes[i] = 1;
        }


        // generate floor and ceiling
        for (int i = 0; i < numCells.x; i++)
        {
            data.cellTypes[i] = 1;
            data.cellTypes[numCells.x * (numCells.y - 1) + i] = 1;
        }

        // generate wall bounds
        for (int i = 1; i < numCells.y - 1; i++)
        {
            data.cellTypes[numCells.x * i] = 1;
            data.cellTypes[numCells.x * i + numCells.x - 1] = 1;
        }

        // generate random velocities
        var rng = new Unity.Mathematics.Random(42);
        for (int i = 0; i < totalCells; i++)
        {
            data.vrVelocities[i] = rng.NextFloat() - 0.5f;
            data.hrVelocities[i] = rng.NextFloat() - 0.5f;
        }

        return data;
    }
}