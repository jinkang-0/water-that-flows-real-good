using UnityEngine;

public class Display2D : MonoBehaviour
{
    public Mesh mesh;
    public Shader shader;
    public float scale;
    public Gradient colorMap;
    public int gradientResolution;
    public float velocityDisplayMax;

    private Material material;
    private ComputeBuffer argsBuffer;
    private Bounds bounds;
    private Texture2D gradientTexture;
    private bool needsUpdate;
    
    // shader property indices
    private static readonly int Positions = Shader.PropertyToID("positions");
    private static readonly int Velocities = Shader.PropertyToID("velocities");
    private static readonly int ColorMap = Shader.PropertyToID("colorMap");
    private static readonly int Scale = Shader.PropertyToID("scale");
    private static readonly int VelocityMax = Shader.PropertyToID("velocityMax");

    public void Init(Simulation sim)
    {
        material = new Material(shader);
        material.SetBuffer(Positions, sim.positionBuffer);
        material.SetBuffer(Velocities, sim.velocityBuffer);

        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.positionBuffer.count);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
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
        TextureFromGradient(ref gradientTexture, gradientResolution, colorMap);
        material.SetTexture(ColorMap, gradientTexture);
        material.SetFloat(Scale, scale);
        material.SetFloat(VelocityMax, velocityDisplayMax);
    }

    public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient,
        FilterMode filterMode = FilterMode.Bilinear)
    {
        // ensure texture is valid
        if (texture == null)
        {
            texture = new Texture2D(width, 1);
        } 
        else if (texture.width != width)
        {
            texture.Reinitialize(width, 1);
        }

        // ensure gradient is valid
        if (gradient == null)
        {
            gradient = new Gradient();
            gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1)},
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1)}
            );
        }

        // set texture modes
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filterMode;

        // evaluate colors from gradient
        Color[] colors = new Color[width];
        for (int i = 0; i < colors.Length; i++)
        {
            float t = i / (colors.Length - 1f);
            colors[i] = gradient.Evaluate(t);
        }
        
        // apply colors to texture
        texture.SetPixels(colors);
        texture.Apply();
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
