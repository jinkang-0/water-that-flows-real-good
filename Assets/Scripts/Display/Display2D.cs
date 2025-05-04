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
    public float particleSoftness = 5;
    public float particleThreshold = 0.1f;
    
    public Mesh mesh;
    public Shader gridShader;
    public Shader particleShader;
    public Shader terrainShader;
    public Shader backgroundShader;
    public Color waterColor;

    // inferred variables
    private Material particleMaterial;
    private Material terrainMaterial;
    private Material backgroundMaterial;
    private Bounds bounds;
    private bool needsUpdate;
    
    // shared from simulation
    private Texture2D staticTerrainSDF;
    private Texture2D dynamicTerrainSDF;

    public Texture2D staticTerrainTexture;
    public Texture2D dynamicTerrainTexture;

    public Texture2D backgroundTexture;

    // buffers
    private ComputeBuffer particlePositionBuffer;
    private ComputeBuffer lookupStartIndicesBuffer;
    private ComputeBuffer particleLookupBuffer;

    public TMP_Text scoreText;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        this.staticTerrainSDF = sim.staticTerrainSDF;
        this.dynamicTerrainSDF = sim.dynamicTerrainSDF;

        particleMaterial = new Material(particleShader);
        particleMaterial.SetTexture("_BGTex", backgroundTexture);
        terrainMaterial = new Material(terrainShader);
        terrainMaterial.SetTexture("_StaticDist", staticTerrainSDF);
        terrainMaterial.SetTexture("_DynamicDist", dynamicTerrainSDF);
        terrainMaterial.SetTexture("_StaticColor", staticTerrainTexture);
        terrainMaterial.SetTexture("_DynamicColor", dynamicTerrainTexture);
        backgroundMaterial = new Material(backgroundShader);
        backgroundMaterial.SetTexture("_MainTex", backgroundTexture);
        needsUpdate = true;

        // initialize buffers
        particlePositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(sim.numParticles);
        particleLookupBuffer = ComputeHelper.CreateStructuredBuffer<int>(sim.numPartitionCells);
        lookupStartIndicesBuffer = ComputeHelper.CreateStructuredBuffer<int>(sim.numPartitionCells + 1);
        
        // bind buffers
        particleMaterial.SetBuffer("particlePositions", particlePositionBuffer);

        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        simulation = sim;
    }

    private void LateUpdate()
    {
        if (gridShader == null) return;
        if (particleShader == null) return;

        UpdateSettings();
        
        // update data
        particlePositionBuffer.SetData(simulation.particlePositions);
        particleLookupBuffer.SetData(simulation.particleLookup);
        lookupStartIndicesBuffer.SetData(simulation.lookupStartIndices);
        
        // Update score text
        if (scoreText != null)
        {
            scoreText.text = $"Score: {simulation.score}";
        }
        
        Graphics.DrawMeshInstancedProcedural(mesh, 0, backgroundMaterial, bounds, 1);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, terrainMaterial, bounds, 1);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, bounds, simulation.numParticles);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        
        particleMaterial.SetColor("waterColor", waterColor);
        particleMaterial.SetFloat("scale", scale);
        particleMaterial.SetVector("boundsSize", simulation.boundsSize);
        particleMaterial.SetFloat("particleRadius", simulation.particleRadius);
        particleMaterial.SetInt("numCols", simulation.numCells.x);
        particleMaterial.SetInt("numRows", simulation.numCells.y);
        particleMaterial.SetFloat("partitionSpacing", simulation.partitionSpacing);
        particleMaterial.SetInt("partitionNumX", simulation.partitionNumX);
        particleMaterial.SetInt("partitionNumY", simulation.partitionNumY);
        particleMaterial.SetInt("numParticles", simulation.numParticles);
        particleMaterial.SetFloat("threshold", particleThreshold);
        particleMaterial.SetFloat("softness", particleSoftness);

        terrainMaterial.SetFloat("scale", scale);
        terrainMaterial.SetVector("boundsSize", simulation.boundsSize);

        backgroundMaterial.SetFloat("scale", scale);
        backgroundMaterial.SetVector("boundsSize", simulation.boundsSize);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        ComputeHelper.Release(particlePositionBuffer);
        ComputeHelper.Release(particleLookupBuffer, lookupStartIndicesBuffer);
    }
}
