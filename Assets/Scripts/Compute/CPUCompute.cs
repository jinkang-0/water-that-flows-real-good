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
    // helpers to work with grid positions
    //

    private uint2 GetCellContainingPos(float2 pos)
    {
        var col = (uint)Mathf.Clamp(Mathf.Floor(pos.x), 1, simulation.numCells.x - 1);
        var row = (uint)Mathf.Clamp(Mathf.Floor(pos.y), 1, simulation.numCells.y - 1);
        return new uint2(col, row);
    }

    private uint2 ClampCellToGrid(uint2 pos)
    {
        pos.x = (uint)Mathf.Clamp(pos.x, 1, simulation.numCells.x - 1);
        pos.y = (uint)Mathf.Clamp(pos.y, 1, simulation.numCells.y - 1);
        return pos;
    }

    private uint GetCellIndex(uint col, uint row)
    {
        return (uint)(row * simulation.numCells.x + col);
    }
    
    //
    // fluid sim pipelines
    //
    public void VelocityTransferParticle(float2[] cellVelocities, float2[] cellWeights, float2[] particlePositions, float2[] particleVelocities)
    {
        var numParticles = particlePositions.Length;
        
        // transfer particle velocity to grid
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var cellPos = ClampCellToGrid(GetCellContainingPos(pos));
            var uDelta = new float2(0, 0.5f);
            var vDelta = new float2(0.5f, 0);
            
            // get bounding positions for the cells to interpolate, for both u and v
            var uLower = ClampCellToGrid((uint2)(cellPos - uDelta));
            var uUpper = uLower + 1;
            var vLower = ClampCellToGrid((uint2)(cellPos - vDelta));
            var vUpper = vLower + 1;
            
            // compute position deltas for interpolation
            var ut = pos - uDelta - uLower;
            var us = 1 - ut;
            var vt = pos - vDelta - vLower;
            var vs = 1 - vt;
            
            // compute bilinear interpolation constants
            var uw1 = us.x * us.y;
            var uw2 = ut.x * us.y;
            var uw3 = ut.x * ut.y;
            var uw4 = us.x * ut.y;
    
            var vw1 = vs.x * vs.y;
            var vw2 = vt.x * vs.y;
            var vw3 = vt.x * vt.y;
            var vw4 = vs.x * vt.y;
            
            // compute relevant grid position indices
            var uc1 = GetCellIndex(uLower.x, uLower.y);
            var uc2 = GetCellIndex(uUpper.x, uLower.y);
            var uc3 = GetCellIndex(uUpper.x, uUpper.y);
            var uc4 = GetCellIndex(uLower.x, uUpper.y);
            
            var vc1 = GetCellIndex(vLower.x, vLower.y);
            var vc2 = GetCellIndex(vUpper.x, vLower.y);
            var vc3 = GetCellIndex(vUpper.x, vUpper.y);
            var vc4 = GetCellIndex(vLower.x, vUpper.y);

            var pv = particleVelocities[i];
            
            // initiate transfer
            cellVelocities[uc1].x += pv.x * uw1;
            cellVelocities[uc2].x += pv.x * uw2;
            cellVelocities[uc3].x += pv.x * uw3;
            cellVelocities[uc4].x += pv.x * uw4;
            cellWeights[uc1].x += uw1;
            cellWeights[uc2].x += uw2;
            cellWeights[uc3].x += uw3;
            cellWeights[uc4].x += uw4;

            cellVelocities[vc1].y += pv.y * vw1;
            cellVelocities[vc2].y += pv.y * vw2;
            cellVelocities[vc3].y += pv.y * vw3;
            cellVelocities[vc4].y += pv.y * vw4;
            cellWeights[vc1].y += vw1;
            cellWeights[vc2].y += vw2;
            cellWeights[vc3].y += vw3;
            cellWeights[vc4].y += vw4;
        }
    }

    public void VelocityTransferGrid(int[] cellTypes, float2[] cellVelocities, float2[] particlePositions, float2[] particleVelocities)
    {
        var numParticles = particlePositions.Length;
        var size = simulation.numCells;
        
        // transfer particle velocity to grid
        // notation: U = grid horizontal velocity, V = grid vertical velocity
        for (int i = 0; i < numParticles; i++)
        {
            var pos = particlePositions[i];
            var cellPos = ClampCellToGrid(GetCellContainingPos(pos));
            var uDelta = new float2(0, 0.5f);
            var vDelta = new float2(0.5f, 0);
            
            // get bounding positions for the cells to interpolate, for both u and v
            var uLower = ClampCellToGrid((uint2)(cellPos - uDelta));
            var uUpper = uLower + 1;
            var vLower = ClampCellToGrid((uint2)(cellPos - vDelta));
            var vUpper = vLower + 1;
            
            // compute position deltas for interpolation
            var ut = pos - uDelta - uLower;
            var us = 1 - ut;
            var vt = pos - vDelta - vLower;
            var vs = 1 - vt;
            
            // compute bilinear interpolation constants
            var uw1 = us.x * us.y;
            var uw2 = ut.x * us.y;
            var uw3 = ut.x * ut.y;
            var uw4 = us.x * ut.y;
    
            var vw1 = vs.x * vs.y;
            var vw2 = vt.x * vs.y;
            var vw3 = vt.x * vt.y;
            var vw4 = vs.x * vt.y;
            
            // compute relevant grid position indices
            var uc1 = GetCellIndex(uLower.x, uLower.y);
            var uc2 = GetCellIndex(uUpper.x, uLower.y);
            var uc3 = GetCellIndex(uUpper.x, uUpper.y);
            var uc4 = GetCellIndex(uLower.x, uUpper.y);
            
            var vc1 = GetCellIndex(vLower.x, vLower.y);
            var vc2 = GetCellIndex(vUpper.x, vLower.y);
            var vc3 = GetCellIndex(vUpper.x, vUpper.y);
            var vc4 = GetCellIndex(vLower.x, vUpper.y);

            // check cell validity
            var uValid1 = cellTypes[uc1] != AIR_CELL || cellTypes[uc1 - size.x] != AIR_CELL ? 1f : 0f;
            var uValid2 = cellTypes[uc2] != AIR_CELL || cellTypes[uc2 - size.x] != AIR_CELL ? 1f : 0f;
            var uValid3 = cellTypes[uc3] != AIR_CELL || cellTypes[uc3 - size.x] != AIR_CELL ? 1f : 0f;
            var uValid4 = cellTypes[uc4] != AIR_CELL || cellTypes[uc4 - size.x] != AIR_CELL ? 1f : 0f;

            var vValid1 = cellTypes[vc1] != AIR_CELL || cellTypes[vc1 - 1] != AIR_CELL ? 1f : 0f;
            var vValid2 = cellTypes[vc2] != AIR_CELL || cellTypes[vc2 - 1] != AIR_CELL ? 1f : 0f;
            var vValid3 = cellTypes[vc3] != AIR_CELL || cellTypes[vc3 - 1] != AIR_CELL ? 1f : 0f;
            var vValid4 = cellTypes[vc4] != AIR_CELL || cellTypes[vc4 - 1] != AIR_CELL ? 1f : 0f;

            // interpolate velocities
            var uw = uValid1 * uw1 + uValid2 * uw2 + uValid3 * uw3 + uValid4 * uw4;
            var vw = vValid1 * vw1 + vValid2 * vw2 + vValid3 * vw3 + vValid4 * vw4;

            if (uw > 0f)
            {
                var uq1 = uValid1 * uw1 * cellVelocities[uc1].x;
                var uq2 = uValid2 * uw2 * cellVelocities[uc2].x;
                var uq3 = uValid3 * uw3 * cellVelocities[uc3].x;
                var uq4 = uValid4 * uw4 * cellVelocities[uc4].x;
                var picVx = (uq1 + uq2 + uq3 + uq4) / uw;
                particleVelocities[i].x = picVx;
            }

            if (vw > 0f)
            {
                var vq1 = vValid1 * vw1 * cellVelocities[vc1].y;
                var vq2 = vValid2 * vw2 * cellVelocities[vc2].y;
                var vq3 = vValid3 * vw3 * cellVelocities[vc3].y;
                var vq4 = vValid4 * vw4 * cellVelocities[vc4].y;
                var picVy = (vq1 + vq2 + vq3 + vq4) / vw;
                particleVelocities[i].y = picVy;
            }
        }
    }
}
