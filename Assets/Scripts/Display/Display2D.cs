using UnityEngine;

public class Display2D : MonoBehaviour
{
    public float scale;
    public Mesh mesh;
    public Shader shader;
    public Color terrain;

    private Material material;
    private ComputeBuffer argsBuffer;
    private Bounds bounds;
    private Texture2D gradientTexture;
    private bool needsUpdate;

    private Simulation simulation;

    public void Init(Simulation sim)
    {
        material = new Material(shader);
        material.SetBuffer("cellTypes", sim.cellTypeBuffer);
        material.SetBuffer("velocities", sim.velocitiesBuffer.buffer_read);
        material.SetBuffer("pressures", sim.pressuresBuffer.buffer_read);

        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.cellTypeBuffer.count);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        simulation = sim;
    }

    private void LateUpdate()
    {
        if (shader == null) return;

        UpdateSettings();
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void UpdateSettings()
    {
        if (!needsUpdate) return;

        needsUpdate = false;
        material.SetFloat("scale", scale);
        material.SetColor("terrainColor", terrain);
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
