using System.Diagnostics.CodeAnalysis;
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
    private ComputeBuffer gridArgsBuffer;
    private ComputeBuffer particleArgsBuffer;
    private Bounds bounds;
    private bool needsUpdate;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        gridMaterial = new Material(gridShader);
        gridMaterial.SetBuffer("cellTypes", sim.cellTypeBuffer);

        particleMaterial = new Material(particleShader);
        particleMaterial.SetBuffer("particleVelocities", sim.particleVelocityBuffer);
        particleMaterial.SetBuffer("positions", sim.positionBuffer);

        gridArgsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.cellTypeBuffer.count);
        particleArgsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.numParticles);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        simulation = sim;
    }

    private void LateUpdate()
    {
        if (gridShader == null) return;
        if (particleShader == null) return;

        UpdateSettings();
        
        // gridMaterial.SetBuffer("cellVelocities", simulation.cellVelocityBuffer.bufferRead);
        
        Graphics.DrawMeshInstancedIndirect(mesh, 0, gridMaterial, bounds, gridArgsBuffer);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, particleMaterial, bounds, particleArgsBuffer);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        gridMaterial.SetFloat("scale", scale);
        gridMaterial.SetColor("terrainColor", terrainColor);
        gridMaterial.SetColor("stoneColor", stoneColor);
        gridMaterial.SetInt("numCols", simulation.numCells.x);
        gridMaterial.SetInt("numRows", simulation.numCells.y);
        gridMaterial.SetVector("boundsSize", simulation.boundsSize);
        gridMaterial.SetVector("cellSize", simulation.cellSize);
        
        particleMaterial.SetColor("waterColor", waterColor);
        particleMaterial.SetFloat("scale", scale);
        particleMaterial.SetVector("boundsSize", simulation.boundsSize);
        particleMaterial.SetVector("cellSize", simulation.cellSize);
        particleMaterial.SetFloat("particleRadius", simulation.particleRadius);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        ComputeHelper.Release(gridArgsBuffer);
        ComputeHelper.Release(particleArgsBuffer);
    }
}
