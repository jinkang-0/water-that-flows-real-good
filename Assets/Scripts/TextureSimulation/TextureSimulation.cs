using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


public class CPU_Eulerian : MonoBehaviour
{
    public float simulationCellSize = 0.1f;

    [ReadOnly]
    public int simulationWidth;
    [ReadOnly]
    public int simulationHeight;
    private Color32[] pixels;
    private Texture2D drawTexture;

    void Init(Transform t)
    {
        simulationWidth = (int)(t.localScale.x / simulationCellSize + 0.5);
        simulationHeight = (int)(t.localScale.z / simulationCellSize + 0.5);
        drawTexture = new Texture2D(simulationWidth, simulationHeight);
        drawTexture.filterMode = FilterMode.Point;
        
        pixels = drawTexture.GetPixels32();

        Material mat = GetComponent<MeshRenderer>().material;
        mat.SetTexture("_MainTex", drawTexture);
    }

    // Start is called before the first frame update
    void Start()
    {
        Transform t = gameObject.transform;
        Init(t);
        t.hasChanged = false;
    }

    // Update is called once per frame
    void Update()
    {
        Transform t = gameObject.transform;
        if (t.hasChanged) Init(t);
        t.hasChanged = false;

        for (int y = 0; y < drawTexture.height; ++y) {
            for (int x = 0; x < drawTexture.width; ++x)
            {
                if (x * x + y * y < simulationWidth * simulationHeight / 2)
                    pixels[drawTexture.width * y + x] = Color.cyan;
                else
                    pixels[drawTexture.width * y + x] = Color.blue;
            }
        }
        drawTexture.SetPixels32(pixels);
        drawTexture.Apply();
    }
}
