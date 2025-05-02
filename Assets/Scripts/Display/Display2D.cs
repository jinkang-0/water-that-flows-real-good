using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

[SuppressMessage("ReSharper", "Unity.PreferAddressByIdToGraphicsParams")]
public class Display2D : MonoBehaviour
{
    public float scale;
    [FormerlySerializedAs("blendThreshold")] public float particleSoftness = 1;
    [FormerlySerializedAs("particleSoftness")] [FormerlySerializedAs("blendSoftness")] public float particleThreshold = 1;
    
    public Mesh mesh;
    public Shader gridShader;
    public Shader particleShader;
    public Shader terrainShader;
    public Color terrainColor;
    public Color stoneColor;
    public Color waterColor;
    public Color drainColor = Color.gray;

    // inferred variables
    private Material gridMaterial;
    private Material particleMaterial;
    private Material terrainMaterial;
    private ComputeBuffer gridArgsBuffer;
    // private ComputeBuffer particleArgsBuffer;
    private Bounds bounds;
    private bool needsUpdate;
    
    // shared from simulation
    private Texture2D staticTerrainSDF;
    private Texture2D dynamicTerrainSDF;

    public Texture2D staticTerrainTexture;
    public Texture2D dynamicTerrainTexture;

    // buffers
    private ComputeBuffer cellTypeBuffer;
    private ComputeBuffer cellVelocityBuffer;
    private ComputeBuffer particlePositionBuffer;
    private ComputeBuffer particleActiveBuffer;
    private ComputeBuffer particleVelocityBuffer;
    private ComputeBuffer lookupStartIndicesBuffer;
    private ComputeBuffer particleLookupBuffer;

    public TMP_Text scoreText;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        this.staticTerrainSDF = sim.staticTerrainSDF;
        this.dynamicTerrainSDF = sim.dynamicTerrainSDF;

        gridMaterial = new Material(gridShader);
        particleMaterial = new Material(particleShader);
        terrainMaterial = new Material(terrainShader);
        terrainMaterial.SetTexture("_StaticDist", staticTerrainSDF);
        terrainMaterial.SetTexture("_DynamicDist", dynamicTerrainSDF);
        terrainMaterial.SetTexture("_StaticColor", staticTerrainTexture);
        terrainMaterial.SetTexture("_DynamicColor", dynamicTerrainTexture);
        needsUpdate = true;

        // initialize buffers
        var numCells = sim.numCells.x * sim.numCells.y;
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<int>(numCells);
        cellVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numCells);
        particleVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(sim.numParticles);
        particlePositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(sim.numParticles);
        particleLookupBuffer = ComputeHelper.CreateStructuredBuffer<int>(sim.numPartitionCells);
        lookupStartIndicesBuffer = ComputeHelper.CreateStructuredBuffer<int>(sim.numPartitionCells + 1);
        
        // bind buffers
        gridMaterial.SetBuffer("cellTypes", cellTypeBuffer);
        gridMaterial.SetBuffer("cellVelocities", cellVelocityBuffer);
        particleMaterial.SetBuffer("particleVelocities", particleVelocityBuffer);
        particleMaterial.SetBuffer("particlePositions", particlePositionBuffer);

        gridArgsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.cellTypes.Length);
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
        particleLookupBuffer.SetData(simulation.particleLookup);
        lookupStartIndicesBuffer.SetData(simulation.lookupStartIndices);
        
        // Update score text
        if (scoreText != null)
        {
            scoreText.text = $"Score: {simulation.score}";
        }
        
        Graphics.DrawMeshInstancedProcedural(mesh, 0, terrainMaterial, bounds, 1);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, gridMaterial, bounds, gridArgsBuffer); 
        Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, bounds, 1);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        gridMaterial.SetFloat("scale", scale);
        gridMaterial.SetColor("terrainColor", terrainColor);
        gridMaterial.SetColor("stoneColor", stoneColor);
        gridMaterial.SetColor("waterColor", waterColor);
        gridMaterial.SetColor("drainColor", drainColor);
        gridMaterial.SetInt("numCols", simulation.numCells.x);
        gridMaterial.SetInt("numRows", simulation.numCells.y);
        gridMaterial.SetVector("boundsSize", simulation.boundsSize);
        gridMaterial.SetFloat("cellSize", simulation.cellSize);
        
        particleMaterial.SetColor("waterColor", waterColor);
        particleMaterial.SetFloat("scale", scale);
        particleMaterial.SetVector("boundsSize", simulation.boundsSize);
        particleMaterial.SetFloat("particleRadius", simulation.particleRadius);
        particleMaterial.SetInt("numCols", simulation.numCells.x);
        particleMaterial.SetInt("numRows", simulation.numCells.y);
        particleMaterial.SetFloat("partitionSpacing", simulation.partitionSpacing);
        particleMaterial.SetInt("partitionNumX", simulation.partitionNumX);
        particleMaterial.SetInt("partitionNumY", simulation.partitionNumY);
        particleMaterial.SetFloat("particleThreshold", particleThreshold);
        particleMaterial.SetFloat("particleSoftness", particleSoftness);

        terrainMaterial.SetFloat("scale", scale);
        terrainMaterial.SetVector("boundsSize", simulation.boundsSize);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        ComputeHelper.Release(gridArgsBuffer);
        // ComputeHelper.Release(particleArgsBuffer);
        ComputeHelper.Release(cellTypeBuffer);
        ComputeHelper.Release(cellVelocityBuffer);
        ComputeHelper.Release(particlePositionBuffer);
        ComputeHelper.Release(particleVelocityBuffer);
    }
}
