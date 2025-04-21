using UnityEngine;

public class Display2D : MonoBehaviour
{
    public float scale;
    public Mesh mesh;
    public Shader shader;
    public Color terrain;
    public Color stone;

    private Material material;
    private ComputeBuffer argsBuffer;
    private Bounds bounds;
    private bool needsUpdate;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        material = new Material(shader);
        material.SetBuffer("cellTypes", sim.cellTypeBuffer);

        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.cellTypeBuffer.count);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        simulation = sim;
    }

    private void LateUpdate()
    {
        if (shader == null) return;

        UpdateSettings();
        
        material.SetBuffer("vrVelocities", simulation.vrVelocityBuffer.bufferRead);
        material.SetBuffer("hrVelocities", simulation.hrVelocityBuffer.bufferRead);
        material.SetBuffer("densities", simulation.densityBuffer.bufferRead);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        material.SetFloat("scale", scale);
        material.SetColor("terrainColor", terrain);
        material.SetColor("stoneColor", stone);
        material.SetInt("numCols", simulation.numCells.x);
        material.SetInt("numRows", simulation.numCells.y);
        material.SetVector("boundsSize", simulation.boundsSize);
        material.SetVector("cellSize", simulation.cellSize);
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void OnDestroy()
    {
        ComputeHelper.Release(argsBuffer);
    }
}
