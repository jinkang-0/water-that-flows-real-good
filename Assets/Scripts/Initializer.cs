using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;

public class Initializer : MonoBehaviour
{
    public struct SpawnData
    {
        public int[] cellTypes;
        public float[] vrVelocities;
        public float[] hrVelocities;
        public float[] Pressures;

        public SpawnData(int numCells)
        {
            cellTypes = new int[numCells];
            vrVelocities = new float[numCells];
            hrVelocities = new float[numCells];
            Pressures = new float[numCells];
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
            data.vrVelocities[i] = rng.NextFloat() - 0.5f;
            data.hrVelocities[i] = rng.NextFloat() - 0.5f;
        }

        // generate random pressures
        for (int i = 0; i < totalCells; i++)
        {
            data.Pressures[i] = rng.NextFloat();
        }
        
        return data;
    }
}
