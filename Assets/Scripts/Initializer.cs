using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

public class Initializer : MonoBehaviour
{
    public Texture2D terrainTexture;

    public Texture2D dynamicTerrainTextureSDFOutside;
    public Texture2D dynamicTerrainTextureSDFInside;

    public Texture2D staticTerrainTextureSDFOutside;
    public Texture2D staticTerrainTextureSDFInside;

    public ComputeShader SDFInit;

    public Texture2D waterTexture;
    public Texture2D drainTexture;

    // this is the threshold to check if color is black or not
    private const float threshold = 0.01f;
    private const int DRAIN_CELL = 4;


    private Texture2D GenerateSDF(Texture2D inside, Texture2D outside)
    {
        outside.filterMode = FilterMode.Bilinear;
        outside.wrapMode = TextureWrapMode.Mirror;
        inside.filterMode = FilterMode.Bilinear;
        inside.wrapMode = TextureWrapMode.Mirror;

        RenderTexture terrainSDF = new RenderTexture(outside.width, outside.height, 1, GraphicsFormat.R32_SFloat, 0);
        terrainSDF.enableRandomWrite = true;

        {
            int kernel = SDFInit.FindKernel("Init");
            SDFInit.GetKernelThreadGroupSizes(kernel, out uint thread_group_w, out uint thread_group_h, out uint _z);
            int w = terrainSDF.width / (int)thread_group_w + 1;
            int h = terrainSDF.width / (int)thread_group_h + 1;

            SDFInit.SetInt("width", terrainSDF.width);
            SDFInit.SetInt("height", terrainSDF.height);
            SDFInit.SetTexture(kernel, "in_DistanceOutside", outside, 0);
            SDFInit.SetTexture(kernel, "in_DistanceInside", inside, 0);
            SDFInit.SetTexture(kernel, "out_SignedDistance", terrainSDF, 0);
            SDFInit.Dispatch(kernel, w, h, 1);
        }

        Texture2D terrainSDF_2 = new Texture2D(terrainSDF.width, terrainSDF.height, GraphicsFormat.R32_SFloat, 0, TextureCreationFlags.None);
        terrainSDF_2.wrapMode = TextureWrapMode.Mirror;

        {
            RenderTexture.active = terrainSDF;
            terrainSDF_2.ReadPixels(new Rect(0, 0, terrainSDF.width, terrainSDF.height), 0, 0);
            terrainSDF_2.Apply();
        }

        return terrainSDF_2;
    }
    
    public (Texture2D staticTerrain, Texture2D dynamicTerrain) GenerateSDFs()
    {
        return (GenerateSDF(staticTerrainTextureSDFInside, staticTerrainTextureSDFOutside), GenerateSDF(dynamicTerrainTextureSDFInside, dynamicTerrainTextureSDFOutside));
    }
    
    public struct SpawnData
    {
        public int[] cellTypes;
        public float2[] cellVelocities;
        public float2[] positions;
        public float2[] particleVelocities;
        public int[] disabledParticles;
        public int[] isCellBucket;
        public Texture2D staticTerrainSDF;
        public Texture2D dynamicTerrainSDF;

        public SpawnData(int numCells, int numParticles)
        {
            // note: in C#, arrays are initialized to default values (not garbage) (it's 0)
            cellTypes = new int[numCells];
            positions = new float2[numParticles];
            cellVelocities = new float2[numCells];
            particleVelocities = new float2[numParticles];
            disabledParticles = new int[numParticles];
            isCellBucket = new int[numCells];
            staticTerrainSDF = null;
            dynamicTerrainSDF = null;
        }
    }

    public SpawnData GetSpawnData(Vector2Int gridSize, int numParticles, float cellSize)
    {
        int totalCells = gridSize.x * gridSize.y;
        var data = new SpawnData(totalCells, numParticles);
        
        // generate terrain SDFs
        data.staticTerrainSDF = GenerateSDF(staticTerrainTextureSDFInside, staticTerrainTextureSDFOutside);
        data.dynamicTerrainSDF = GenerateSDF(dynamicTerrainTextureSDFInside, dynamicTerrainTextureSDFOutside);
        
        // generate drain from texture
        if (drainTexture != null && drainTexture.isReadable)
        {
            int textureWidth = drainTexture.width;
            int textureHeight = drainTexture.height;
            // var drainCells = new List<int>();

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
                        data.cellTypes[cellIndex] = DRAIN_CELL;
                        // drainCells.Add(cellIndex);
                    }
                }
            }
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
                    data.positions[count] = (gridPos + rng.NextFloat2()) * cellSize;
                    count++;
                }

                if (numExtras > 0)
                {
                    data.positions[count] = (gridPos + rng.NextFloat2()) * cellSize;
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
            
            // spawn particles in block
            var rng = new Unity.Mathematics.Random(42);
            var spread = 60f;
            float2 center = new float2(spread, spread) / 2 + 1;
            for (int i = 0; i < numParticles; i++)
            {
                data.positions[i] = (center + spread * (rng.NextFloat2() - 0.5f)) * cellSize;
            }
        }

        // int drainSize = 5;
        // int drainStartX = gridSize.x - drainSize - 1;
        // int drainStartY = 1;

        // for (int row = drainStartY; row < drainStartY + drainSize && row < gridSize.y - 1; row++)
        // {
        //     for (int col = drainStartX; col < drainStartX + drainSize && col < gridSize.x - 1; col++)
        //     {
        //         int cellIndex = row * gridSize.x + col;
        //         if(data.cellTypes[cellIndex] == 0)
        //             data.cellTypes[cellIndex] = DRAIN_CELL;
        //     }
        // }

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