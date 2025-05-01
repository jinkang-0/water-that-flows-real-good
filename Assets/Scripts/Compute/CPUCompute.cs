using UnityEngine;
using Unity.Mathematics;
using System.Threading;

public class CPUCompute
{
    // constants
    private const int AIR_CELL = 0;
    private const int TERRAIN_CELL = 1;
    private const int STONE_CELL = 2;
    private const int WATER_CELL = 3;
    private const int BUCKET_CELL = 4;

    
    //
    // helpers to work with unity compute buffer
    //

    public static float2[] LoadFloat2Buffer(ComputeBuffer buffer, int count)
    {
        float2[] array = new float2[count];
        buffer.GetData(array);
        return array;
    }

    public static int[] LoadIntBuffer(ComputeBuffer buffer, int count)
    {
        int[] array = new int[count];
        buffer.GetData(array);
        return array;
    }


    public static bool[] LoadBoolBuffer(ComputeBuffer buffer, int count)
    {
        bool[] array = new bool[count];
        buffer.GetData(array);
        return array;
    }
    
    //
    // class constructor/methods
    //
    private readonly Simulation simulation;
    
    public CPUCompute(Simulation sim)
    {
        simulation = sim;
    }
    
    //
    // helper functions
    //

    private uint GetCellIndex(uint col, uint row)
    {
        return (uint)(row * simulation.numCells.x + col);
    }

    private float2 ClampPosToGrid(float2 pos)
    {
        pos.x = Mathf.Clamp(pos.x, 1, simulation.numCells.x - 1);
        pos.y = Mathf.Clamp(pos.y, 1, simulation.numCells.y - 1);
        return pos;
    }

    private bool IsSolidCell(int type)
    {
        return type is STONE_CELL or TERRAIN_CELL;
    }

    private struct InterpolationData
    {
        public readonly float[] weights;
        public readonly uint[] indices;

        public InterpolationData(float[] w, uint[] i)
        {
            weights = w;
            indices = i;
        }
    }

    private InterpolationData VelocityTransferInterpolation(float2 pos, float2 dPos)
    {
        var size = simulation.numCells;
        
        // clamp position
        var x = Mathf.Clamp(pos.x, 1, size.x - 1);
        var y = Mathf.Clamp(pos.y, 1, size.y - 1);
        
        // compute coords
        var x0 = (uint)Mathf.Min(Mathf.FloorToInt(x - dPos.x), size.x - 2);
        var x1 = (uint)Mathf.Min(x0 + 1, size.x - 2);
        var y0 = (uint)Mathf.Min(Mathf.FloorToInt(y - dPos.y), size.y - 2);
        var y1 = (uint)Mathf.Min(y0 + 1, size.y - 2);
        
        // get interpolation constants
        var tx = x - dPos.x - x0;
        var ty = y - dPos.y - y0;
        var sx = 1f - tx;
        var sy = 1f - ty;
        
        // compute weights
        var weights = new float[4]
        {
            sx * sy,
            tx * sy,
            tx * ty,
            sx * ty
        };
        
        // compute cell indices
        var indices = new uint[4]
        {
            GetCellIndex(x0, y0),
            GetCellIndex(x1, y0),
            GetCellIndex(x1, y1),
            GetCellIndex(x0, y1)
        };

        return new InterpolationData(weights, indices);
    }
    
    //
    // fluid sim pipelines
    //
    public int DisableParticles(float2[] particlePositions, int[]disabledParticles, int[]isCellBucket, int score, Vector2 cellSize, Vector2 size) {
        for (int i = 0; i < disabledParticles.Length; i++) {
            float2 pos = ClampPosToGrid(particlePositions[i]);
            var col = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize.x), 0, (int)size.x - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt(pos.y / cellSize.y), 0, (int)size.y - 1);
            var idx = row * (int)size.x + col;
            if (isCellBucket[idx] == 1 && disabledParticles[i] == 0) {
                disabledParticles[i] = 1;
                score++;
            }
        }
        return score;
    }


    public void VelocityTransferParticle(float2[] cellVelocities, float2[] cellWeights, float2[] particlePositions, 
    float2[] particleVelocities, int[]disabledParticles)
    {
        var numParticles = particlePositions.Length;
        
        // transfer particle velocity to grid
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            // disabling particles here
            if (disabledParticles[i] == 0) {
                var pos = ClampPosToGrid(particlePositions[i]);

                // get interpolation data
                var uInterpolation = VelocityTransferInterpolation(pos, new float2(0f, 0.5f));
                var vInterpolation = VelocityTransferInterpolation(pos, new float2(0.5f, 0f));

                var ui = uInterpolation.indices;
                var uw = uInterpolation.weights;
                var vi = vInterpolation.indices;
                var vw = vInterpolation.weights;
                
                var pv = particleVelocities[i];
                
                // initiate transfer
                cellVelocities[ui[0]].x += pv.x * uw[0];
                cellVelocities[ui[1]].x += pv.x * uw[1];
                cellVelocities[ui[2]].x += pv.x * uw[2];
                cellVelocities[ui[3]].x += pv.x * uw[3];
                cellWeights[ui[0]].x += uw[0];
                cellWeights[ui[1]].x += uw[1];
                cellWeights[ui[2]].x += uw[2];
                cellWeights[ui[3]].x += uw[3];

                cellVelocities[vi[0]].y += pv.y * vw[0];
                cellVelocities[vi[1]].y += pv.y * vw[1];
                cellVelocities[vi[2]].y += pv.y * vw[2];
                cellVelocities[vi[3]].y += pv.y * vw[3];
                cellWeights[vi[0]].y += vw[0];
                cellWeights[vi[1]].y += vw[1];
                cellWeights[vi[2]].y += vw[2];
                cellWeights[vi[3]].y += vw[3];
                
            }
        }
    }

    public void VelocityTransferGrid(int[] cellTypes, float2[] cellVelocities, float2[] particlePositions, float2[] particleVelocities, int[]disabledParticles)
    {
        var numParticles = particlePositions.Length;
        var size = simulation.numCells;
        
        // transfer grid velocity to particles
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            // disabling particles here
            if (disabledParticles[i] == 0) {
                var pos = ClampPosToGrid(particlePositions[i]);

                // get interpolation data
                var uInterpolation = VelocityTransferInterpolation(pos, new float2(0f, 0.5f));
                var vInterpolation = VelocityTransferInterpolation(pos, new float2(0.5f, 0f));

                var ui = uInterpolation.indices;
                var uw = uInterpolation.weights;
                var vi = vInterpolation.indices;
                var vw = vInterpolation.weights;

                // check cell validity
                var uValid0 = cellTypes[ui[0]] != AIR_CELL || cellTypes[ui[0] - 1] != AIR_CELL ? 1f : 0f;
                var uValid1 = cellTypes[ui[1]] != AIR_CELL || cellTypes[ui[1] - 1] != AIR_CELL ? 1f : 0f;
                var uValid2 = cellTypes[ui[2]] != AIR_CELL || cellTypes[ui[2] - 1] != AIR_CELL ? 1f : 0f;
                var uValid3 = cellTypes[ui[3]] != AIR_CELL || cellTypes[ui[3] - 1] != AIR_CELL ? 1f : 0f;

                var vValid0 = cellTypes[vi[0]] != AIR_CELL || cellTypes[vi[0] - size.x] != AIR_CELL ? 1f : 0f;
                var vValid1 = cellTypes[vi[1]] != AIR_CELL || cellTypes[vi[1] - size.x] != AIR_CELL ? 1f : 0f;
                var vValid2 = cellTypes[vi[2]] != AIR_CELL || cellTypes[vi[2] - size.x] != AIR_CELL ? 1f : 0f;
                var vValid3 = cellTypes[vi[3]] != AIR_CELL || cellTypes[vi[3] - size.x] != AIR_CELL ? 1f : 0f;

                // interpolate velocities
                var uWeight = uValid0 * uw[0] + uValid1 * uw[1] + uValid2 * uw[2] + uValid3 * uw[3];
                var vWeight = vValid0 * vw[0] + vValid1 * vw[1] + vValid2 * vw[2] + vValid3 * vw[3];

                if (uWeight > 0f)
                {
                    var uq0 = uValid0 * uw[0] * cellVelocities[ui[0]].x;
                    var uq1 = uValid1 * uw[1] * cellVelocities[ui[1]].x;
                    var uq2 = uValid2 * uw[2] * cellVelocities[ui[2]].x;
                    var uq3 = uValid3 * uw[3] * cellVelocities[ui[3]].x;
                    var picVx = (uq0 + uq1 + uq2 + uq3) / uWeight;
                    particleVelocities[i].x = picVx;
                }

                if (vWeight > 0f)
                {
                    var vq0 = vValid0 * vw[0] * cellVelocities[vi[0]].y;
                    var vq1 = vValid1 * vw[1] * cellVelocities[vi[1]].y;
                    var vq2 = vValid2 * vw[2] * cellVelocities[vi[2]].y;
                    var vq3 = vValid3 * vw[3] * cellVelocities[vi[3]].y;
                    var picVy = (vq0 + vq1 + vq2 + vq3) / vWeight;
                    particleVelocities[i].y = picVy;
                }
            }
        }
    }

    public void SolveIncompressibility(float2[] cellVelocities, int[] cellTypes, int numIter, float overRelaxation)
    {
        var size = simulation.numCells;
        
        for (int i = 0; i < numIter; i++)
        {
            for (int x = 1; x < size.x - 1; x++)
            {
                for (int y = 1; y < size.y - 1; y++)
                {
                    var idx = y * size.x + x;
                    if (cellTypes[idx] != WATER_CELL) continue;
                    
                    // compute neighbor indices for ease
                    var left = idx - 1;
                    var right = idx + 1;
                    var top = idx + size.x;
                    var bottom = idx - size.x;
                    
                    // confirm solids
                    var sLeft = IsSolidCell(cellTypes[left]) ? 0f : 1f;
                    var sRight = IsSolidCell(cellTypes[right]) ? 0f : 1f;
                    var sTop = IsSolidCell(cellTypes[top]) ? 0f : 1f;
                    var sBottom = IsSolidCell(cellTypes[bottom]) ? 0f : 1f;
                    var s = sLeft + sRight + sTop + sBottom;
                    if (s == 0.0) continue;
                    
                    // compute divergence
                    var d = cellVelocities[right].x - cellVelocities[idx].x + cellVelocities[top].y -
                            cellVelocities[idx].y;
                    var p = -d * overRelaxation / s;
                    
                    // solve incompressibility
                    cellVelocities[idx].x -= p * sLeft;
                    cellVelocities[right].x += p * sRight;
                    cellVelocities[idx].y -= p * sBottom;
                    cellVelocities[top].y += p * sTop;
                }
            }
        }
    }
}
