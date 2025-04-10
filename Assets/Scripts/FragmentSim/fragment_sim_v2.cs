using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class fragment_simv2 : MonoBehaviour
{
    public Texture initialState;
    public Shader simulationShader;

    private Material simulationMaterial;


    private RenderTexture tex1;
    private RenderTexture tex2;

    // Start is called before the first frame update
    void Start()
    {
        simulationMaterial = new Material(simulationShader);

        tex1 = new RenderTexture(initialState.width, initialState.height, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.None, 0);
        tex2 = new RenderTexture(initialState.width, initialState.height, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.None, 0);
        Graphics.Blit(initialState, tex1);
        Graphics.Blit(initialState, tex2);
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.Blit(tex1, tex2, simulationMaterial);
    }
}
