using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using UnityEngine;

[SuppressMessage("ReSharper", "Unity.PreferAddressByIdToGraphicsParams")]
public class Display2D : MonoBehaviour
{
    public float scale;
    public Mesh mesh;
    public Shader gridShader;
    public Shader particleShader;
    public Color terrainColor;
    public Color stoneColor;
    public Color waterColor;

    private Material gridMaterial;
    private Material particleMaterial;
    // private ComputeBuffer gridArgsBuffer;
    // private ComputeBuffer particleArgsBuffer;
    private Bounds bounds;
    private bool needsUpdate;
    
    // buffers
    private ComputeBuffer cellTypeBuffer;
    private ComputeBuffer cellVelocityBuffer;
    private ComputeBuffer particlePositionBuffer;
    private ComputeBuffer particleVelocityBuffer;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        gridMaterial = new Material(gridShader);
        particleMaterial = new Material(particleShader);
        
        // initialize buffers
        var numCells = sim.numCells.x * sim.numCells.y;
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<int>(numCells);
        cellVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numCells);
        particleVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(sim.numParticles);
        particlePositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(sim.numParticles);
        
        // bind buffers
        gridMaterial.SetBuffer("cellTypes", cellTypeBuffer);
        gridMaterial.SetBuffer("cellVelocities", cellVelocityBuffer);
        particleMaterial.SetBuffer("particleVelocities", particleVelocityBuffer);
        particleMaterial.SetBuffer("particlePositions", particlePositionBuffer);

        // gridArgsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.cellTypes.Length);
        // particleArgsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.numParticles);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        simulation = sim;
    }

    private void LateUpdate()
    {
        if (gridShader == null) return;
        if (particleShader == null) return;

        UpdateSettings();
        
        // update data
        cellTypeBuffer.SetData(simulation.cellTypes);
        cellVelocityBuffer.SetData(simulation.cellVelocities);
        particleVelocityBuffer.SetData(simulation.particleVelocities);
        particlePositionBuffer.SetData(simulation.particlePositions);
        
        Graphics.DrawMeshInstancedProcedural(mesh, 0, gridMaterial, bounds, simulation.cellTypes.Length);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, bounds, simulation.numParticles);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        gridMaterial.SetFloat("scale", scale);
        gridMaterial.SetColor("terrainColor", terrainColor);
        gridMaterial.SetColor("stoneColor", stoneColor);
        gridMaterial.SetColor("waterColor", waterColor);
        gridMaterial.SetInt("numCols", simulation.numCells.x);
        gridMaterial.SetInt("numRows", simulation.numCells.y);
        gridMaterial.SetVector("boundsSize", simulation.boundsSize);
        gridMaterial.SetVector("cellSize", simulation.cellSize);
        
        particleMaterial.SetColor("waterColor", waterColor);
        particleMaterial.SetFloat("scale", scale);
        particleMaterial.SetVector("boundsSize", simulation.boundsSize);
        particleMaterial.SetVector("cellSize", simulation.cellSize);
        particleMaterial.SetFloat("particleRadius", simulation.particleRadius);
        particleMaterial.SetInt("numCols", simulation.numCells.x);
        particleMaterial.SetInt("numRows", simulation.numCells.y);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        // ComputeHelper.Release(gridArgsBuffer);
        // ComputeHelper.Release(particleArgsBuffer);
        ComputeHelper.Release(cellTypeBuffer);
        ComputeHelper.Release(cellVelocityBuffer);
        ComputeHelper.Release(particlePositionBuffer);
        ComputeHelper.Release(particleVelocityBuffer);
    }
}
