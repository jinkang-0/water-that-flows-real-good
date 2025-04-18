using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;

public class Initializer : MonoBehaviour
{
    public struct SpawnData
    {
        public int[] cellTypes;
        public Vector2[] velocities;
        public float[] zeros;
        public float[] Pressures;
        public Vector3[] colors;

        public SpawnData(int numCells)
        {
            cellTypes = new int[numCells];
            velocities = new Vector2[numCells];
            zeros = new float[numCells];
            Pressures = new float[numCells];
            colors = new Vector3[numCells];
        }
    }
    
    public SpawnData GetSpawnData(Vector2Int numCells)
    {
        int totalCells = numCells.x * numCells.y;
        var data = new SpawnData(totalCells);
        
        // generate floor and ceiling
        for (int i = 0; i < numCells.x; i++)
        {
            data.cellTypes[i] = 1;
            data.cellTypes[numCells.x * (numCells.y - 1) + i] = 1;
        }
        
        // generate wall bounds
        for (int i = 1; i < numCells.y-1; i++)
        {
            data.cellTypes[numCells.x * i] = 1;
            data.cellTypes[numCells.x * i + numCells.x - 1] = 1;
        }

        // generate random velocities
        var rng = new Unity.Mathematics.Random(42);
        for (int i = 0; i < totalCells; i++)
        {
            data.velocities[i] = 0.2f * new Vector2(rng.NextFloat() - 0.5f, rng.NextFloat() - 0.5f);
        }

        for (int i = 0; i < totalCells; i++)
        {
            data.zeros[i] = 0.0f;
        }

        // generate random pressures
        for (int i = 0; i < totalCells; i++)
        {
            data.Pressures[i] = 0.0f;//rng.NextFloat();
        }

        // generate random colors
        for (int i = 0; i < totalCells; i++)
        {
            data.colors[i] = new Vector3(rng.NextFloat(), rng.NextFloat(), rng.NextFloat());
        }
        
        return data;
    }
}
