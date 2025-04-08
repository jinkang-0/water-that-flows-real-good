using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class Sim : MonoBehaviour
{
    // dimensions of the simulation
    public int sim_width = 100;
    public int sim_height = 100;

    // slow down simulation (`frames_per_step` calls to `FixedUpdate` for each actual update)
    public int frames_per_step = 3;
    private int frame_counter = 1;

    private Shader shader;

    private Material mat_A;
    private Material mat_B;
    private CustomRenderTexture render_texture_A;
    private CustomRenderTexture render_texture_B;

    // toggle which texture is being drawn
    private bool tex_toggle = false;
    // draw the most recently updated texture when it's time to draw
    private bool last_updated_texture = false;
    // Start is called before the first frame update
    void Start()
    {
        // get shader to run simulation
        shader = Shader.Find("RenderTexture/TestUV");
        // create materials for simulation (each material will have one of the textures bound)
        mat_A = new Material(shader);
        mat_B = new Material(shader);

        // set simulation dimensions in shader (used for getting texture data)
        mat_A.SetInteger("_Width", sim_width);
        mat_A.SetInteger("_Height", sim_height);
        mat_B.SetInteger("_Width", sim_width);
        mat_B.SetInteger("_Height", sim_height);

        render_texture_A = new CustomRenderTexture(sim_width, sim_height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        render_texture_B = new CustomRenderTexture(sim_width, sim_height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        render_texture_A.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        render_texture_B.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        render_texture_A.filterMode = FilterMode.Point;
        render_texture_B.filterMode = FilterMode.Point;

        // create a texture to initialize the RenderTextures
        {
            Texture2D texture;
            texture = new Texture2D(sim_width, sim_height, GraphicsFormat.R8G8B8A8_SRGB, 0, TextureCreationFlags.None);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (x * x + y * y < sim_width * sim_height)
                        texture.SetPixel(x, y, Color.white);
                    else
                        texture.SetPixel(x, y, Color.black);
                }
            }
            texture.Apply();

            Graphics.Blit(texture, render_texture_A);
            Graphics.Blit(texture, render_texture_B);
        }

        // tell custom render textures to use to use the shader
        render_texture_A.material = mat_A;
        render_texture_B.material = mat_B;

        // tell the materials to point to the other render texture
        // (each render texture will update based on the other)
        mat_A.SetTexture("_Tex", render_texture_B);
        mat_B.SetTexture("_Tex", render_texture_A);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // TODO: Need to put some sort of rendering barrier here!

        // only update every `frames_per_step` frames
        if (frame_counter == 0)
        {
            // toggle which render texture is the previous and which is the next
            if (tex_toggle)
            {
                render_texture_A.Update();
                last_updated_texture = true;
            }
            else
            {
                render_texture_B.Update();
                last_updated_texture = false;
            }
            tex_toggle = !tex_toggle;
        }
        frame_counter = (frame_counter + 1) % frames_per_step;
    }
    void OnGUI()
    {
        if (last_updated_texture)
            Graphics.DrawTexture(new Rect(0, 0, Math.Min(Screen.width, Screen.height), Math.Min(Screen.width, Screen.height)), render_texture_A);
        else
            Graphics.DrawTexture(new Rect(0, 0, Math.Min(Screen.width, Screen.height), Math.Min(Screen.width, Screen.height)), render_texture_B);
    }
}
