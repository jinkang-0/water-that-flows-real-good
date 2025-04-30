using System;
using UnityEngine;
using Unity.Mathematics;

public class CPUCompute
{
    // constants
    private const int AIR_CELL = 0;
    private const int TERRAIN_CELL = 1;
    private const int STONE_CELL = 2;
    private const int WATER_CELL = 3;
    
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

    private InterpolationData ParticleCellInterpolation(float2 pos, float2 dPos, int2 min, int2 max)
    {
        var size = simulation.numCells;
        
        // clamp position
        var x = Mathf.Clamp(pos.x, 1, size.x - 1);
        var y = Mathf.Clamp(pos.y, 1, size.y - 1);
        
        // compute coords
        var x0 = (uint)Mathf.Clamp(Mathf.FloorToInt(x - dPos.x), min.x, max.x);
        var x1 = (uint)Mathf.Clamp(x0 + 1,  min.x, max.x);
        var y0 = (uint)Mathf.Clamp(Mathf.FloorToInt(y - dPos.y), min.y, max.y);
        var y1 = (uint)Mathf.Clamp(y0 + 1, min.y, max.y);
        
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
    public void SimulateParticles(float2[] particlePositions, float2[] particleVelocities, float gravity, float deltaTime)
    {
        var numParticles = particlePositions.Length;

        // integrate position & velocity
        for (int i = 0; i < numParticles; i++)
        {
            particleVelocities[i].y += gravity * deltaTime;
            particlePositions[i] += particleVelocities[i] * deltaTime;
        }
    }

    public void HandleObstacleCollisions(float2[] particlePositions, float2[] particleVelocities)
    {
        var numParticles = particlePositions.Length;
        
        // handle bounding collision
        var r = simulation.particleRadius;
        var size = simulation.numCells;
        
        var minX = r + 1;
        var maxX = size.x - 1 - r;
        var minY = r + 1;
        var maxY = size.y - 1 - r;

        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            if (pos.x < minX)
            {
                pos.x = minX;
                particleVelocities[i].x = 0;
            }

            if (pos.x > maxX)
            {
                pos.x = maxX;
                particleVelocities[i].x = 0;
            }

            if (pos.y < minY)
            {
                pos.y = minY;
                particleVelocities[i].y = 0;
            }

            if (pos.y > maxY)
            {
                pos.y = maxY;
                particleVelocities[i].y = 0;
            }

            particlePositions[i] = pos;
        }
    }
    
    public void VelocityTransferParticle(int[] cellTypes, float2[] cellVelocities, float2[] cellWeights, float2[] particlePositions, float2[] particleVelocities)
    {
        var numParticles = particlePositions.Length;
        var numCells = cellWeights.Length;
        var size = simulation.numCells;
        var sizeSub2 = new int2(size.x - 2, size.y - 2);
        var prevCellVelocities = new float2[numCells];
        
        // empty water
        for (int i = 0; i < numCells; i++)
        {
            if (cellTypes[i] == WATER_CELL)
                cellTypes[i] = AIR_CELL;

            cellWeights[i] = 0;
            prevCellVelocities[i] = cellVelocities[i];
            cellVelocities[i] = 0;
        }
        
        // fill water
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var col = Mathf.Clamp(Mathf.FloorToInt(pos.x), 0, size.x - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt(pos.y), 0, size.y - 1);
            var idx = row * size.x + col;
            if (cellTypes[idx] == AIR_CELL)
                cellTypes[idx] = WATER_CELL;
        }
        
        // transfer particle velocity to grid
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            // get interpolation data
            var pos = particlePositions[i];
            var uInterpolation = ParticleCellInterpolation(pos, new float2(0f, 0.5f), 0, sizeSub2);
            var vInterpolation = ParticleCellInterpolation(pos, new float2(0.5f, 0f), 0, sizeSub2);

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
        
        for (int i = 0; i < numCells; i++)
        {
            // normalize weights
            if (cellWeights[i].x > 0)
                cellVelocities[i].x /= cellWeights[i].x;
            if (cellWeights[i].y > 0)
                cellVelocities[i].y /= cellWeights[i].y;
            
            // restore solid cell velocities
            var col = i % size.x;
            var row = i / size.x;
            var isSolid = IsSolidCell(cellTypes[i]);
            var leftIsSolid = col > 0 && IsSolidCell(cellTypes[i - 1]);
            var bottomIsSolid = row > 0 && IsSolidCell(cellTypes[i - size.x]);

            if (isSolid || leftIsSolid)
                cellVelocities[i].x = prevCellVelocities[i].x;
            if (isSolid || bottomIsSolid)
                cellVelocities[i].y = prevCellVelocities[i].y;
        }
    }

    public void VelocityTransferGrid(int[] cellTypes, float2[] cellVelocities, float2[] particlePositions, float2[] particleVelocities)
    {
        var numParticles = particlePositions.Length;
        var size = simulation.numCells;
        var sizeSub2 = new int2(size.x - 2, size.y - 2);
        
        // transfer grid velocity to particles
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            // get interpolation data
            var pos = particlePositions[i];
            var uInterpolation = ParticleCellInterpolation(pos, new float2(0f, 0.5f), 0, sizeSub2);
            var vInterpolation = ParticleCellInterpolation(pos, new float2(0.5f, 0f), 0, sizeSub2);

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

    public void SolveIncompressibility(float2[] cellVelocities, int[] cellTypes, float[] densities, float restDensity, int numIter, float overRelaxation)
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

                    // account for drift
                    if (restDensity > 0f)
                    {
                        var p = Mathf.Max(0, densities[idx] - restDensity);
                        d -= p;
                    }
                    
                    // normalize divergence
                    var ds = d / s;

                    // solve incompressibility
                    cellVelocities[idx].x += ds * sLeft;
                    cellVelocities[right].x -= ds * sRight;
                    cellVelocities[idx].y += ds * sBottom;
                    cellVelocities[top].y -= ds * sTop;
                }
            }
        }
    }

    public void PushApartParticles(float2[] particlePositions)
    {
        var numParticles = particlePositions.Length;
        
        // need to uniformly partition space to perform fast neighbor particle search
        // partition cell size has to be the same as the cell's diameter
        var partitionSpacing = 2f * simulation.particleRadius;
        var partitionSizeX = Mathf.CeilToInt(simulation.numCells.x / partitionSpacing);
        var partitionSizeY = Mathf.CeilToInt(simulation.numCells.y / partitionSpacing);
        var numPartitionCells = partitionSizeX * partitionSizeY;
        var counts = new int[numPartitionCells];
        var startIndices = new int[numPartitionCells + 1];
        var indices = new int[numParticles];

        // count particles per cell
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var x = Mathf.Clamp(Mathf.FloorToInt(pos.x / partitionSpacing), 0, partitionSizeX - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(pos.y / partitionSpacing), 0, partitionSizeY - 1);
            var idx = y * partitionSizeX + x;
            counts[idx]++;
        }

        // compute prefix sums to build start index
        int first = 0;
        for (int i = 0; i < numPartitionCells; i++)
        {
            first += counts[i];
            startIndices[i] = first;
        }

        startIndices[numPartitionCells] = first;
        
        // partition particles
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var x = Mathf.Clamp(Mathf.FloorToInt(pos.x / partitionSpacing), 0, partitionSizeX - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(pos.y / partitionSpacing), 0, partitionSizeY - 1);
            var idx = y * partitionSizeX + x;
            startIndices[idx]--;
            indices[startIndices[idx]] = i;
        }
        
        // push particles apart
        var minDist = 2f * simulation.particleRadius;
        var minDist2 = minDist * minDist;

        for (int iter = 0; iter < 2; iter++)
        {
            for (int i = 0; i < numParticles; i++)
            {
                // clamp position
                var pos = particlePositions[i];
                var px = Mathf.Clamp(Mathf.FloorToInt(pos.x / partitionSpacing), 0, partitionSizeX - 1);
                var py = Mathf.Clamp(Mathf.FloorToInt(pos.y / partitionSpacing), 0, partitionSizeY - 1);
                
                // get lower and upper bounds for partitions
                var x0 = Mathf.Clamp(Mathf.FloorToInt(px - 1), 0, partitionSizeX - 1);
                var y0 = Mathf.Clamp(Mathf.FloorToInt(py - 1), 0, partitionSizeY - 1);
                var x1 = Mathf.Clamp(Mathf.FloorToInt(px + 1), 0, partitionSizeX - 1);
                var y1 = Mathf.Clamp(Mathf.FloorToInt(py + 1), 0, partitionSizeY - 1);

                // check neighboring partition cells
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        // lookup linked list of particles in partition cell
                        var cellIdx = y * partitionSizeX + x;
                        var firstIdx = startIndices[cellIdx];
                        var lastIdx = startIndices[cellIdx + 1];

                        // check overlap with neighbor particles
                        for (var j = firstIdx; j < lastIdx; j++)
                        {
                            // ensure particle is not self
                            var id = indices[j];
                            if (id == i) continue;

                            // check if there is overlap
                            var pos2 = particlePositions[id];
                            var diff = pos2 - pos;
                            var d2 = Vector2.Dot(diff, diff);
                            if (d2 > minDist2 || d2 == 0) continue;

                            // push apart particles
                            var d = Mathf.Sqrt(d2);
                            var s = (minDist - d) / 2 / d;
                            particlePositions[i] -= diff * s;
                            particlePositions[id] += diff * s;
                        }
                    }
                }
            }
        }
    }

    public float ComputeRestDensity(int[] cellTypes, float[] densities)
    {
        var numCells = densities.Length;
        var totalFluidDensity = 0f;
        var numFluidCells = 0;

        for (int i = 0; i < numCells; i++)
        {
            if (cellTypes[i] == WATER_CELL)
            {
                totalFluidDensity += densities[i];
                numFluidCells++;
            }
        }

        return numFluidCells > 0 ? totalFluidDensity / numFluidCells : 0;
    }

    public float ComputeDensities(int[] cellTypes, float2[] particlePositions, float[] densities, float restDensity)
    {
        var numParticles = particlePositions.Length;
        var numCells = densities.Length;
        var sizeSub1 = new int2(simulation.numCells.x - 1, simulation.numCells.y - 1);
        
        // clear densities
        for (int i = 0; i < numCells; i++)
        {
            densities[i] = 0f;
        }
        
        // add densities (assume particle density = 1)
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var intData = ParticleCellInterpolation(pos, 0.5f, 0, sizeSub1);
            var cellIdx = intData.indices;
            var w = intData.weights;

            densities[cellIdx[0]] += w[0];
            densities[cellIdx[1]] += w[1];
            densities[cellIdx[2]] += w[2];
            densities[cellIdx[3]] += w[3];
        }
        
        // update rest density
        if (restDensity == 0)
        {
            restDensity = ComputeRestDensity(cellTypes, densities);
        }

        return restDensity;
    }
}
