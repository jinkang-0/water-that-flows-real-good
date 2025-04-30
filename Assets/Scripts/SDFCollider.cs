using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SDFCollider : MonoBehaviour
{
    public MeshRenderer test_renderer;

    public Texture2D terrain_source;

    public Texture2D terrain_source_outside;
    public Texture2D terrain_source_inside;

    public int width = 128;
    public int height = 128;

    public float circle_r_max = 20;
    public int circle_count = 10;

    public Texture2D distance_field;

    int2[,] JFPass(int2[,] positions, int o)
    {
        int2[,] positions_new = new int2[positions.GetLength(0), positions.GetLength(1)];
        // iterate through every pixel
        for (int y = 0; y < terrain_source.height; ++y)
        {
            for (int x = 0; x < terrain_source.width; ++x)
            {
                positions_new[x, y] = positions[x, y];
                // iterate through 9 samples with offset `o`
                for (int dy = -o; dy <= o; dy += o)
                {
                    for (int dx = -o; dx <= o; dx += o)
                    {
                        // sample indices
                        int ox = x + dx;
                        int oy = y + dy;
                        // only use samples within image bounds
                        if (ox >= 0 && ox < terrain_source.width && oy >= 0 && oy < terrain_source.height)
                        {
                            int2 existing = positions[x, y];
                            int2 sample = positions[ox, oy];
                            // only use samples that have a value
                            if (sample.x != -1)
                            {
                                if (existing.x == -1)
                                { // this position has no value => set it to the sample
                                    positions_new[x, y] = sample;
                                }
                                else
                                { // this position has a value => set it to the sample if the distance is smaller
                                    // get (square of) distance to this position's source
                                    int dist_this_x = x - existing.x;
                                    int dist_this_y = y - existing.y;
                                    int dist_this = dist_this_x * dist_this_x + dist_this_y * dist_this_y;
                                    // get (square of) distance to the sample position's source
                                    int dist_other_x = x - sample.x;
                                    int dist_other_y = y - sample.y;
                                    int dist_other = dist_other_x * dist_other_x + dist_other_y * dist_other_y;
                                    // set to the new position if it's closer
                                    if (dist_other < dist_this)
                                    {
                                        positions_new[x, y] = sample;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return positions_new;
    }

    // Start is called before the first frame update
    void Start()
    {
        int2[,] outside = new int2[terrain_source.width, terrain_source.height];
        int2[,] inside = new int2[terrain_source.width, terrain_source.height];
        distance_field = new Texture2D(width, height, GraphicsFormat.R32_SFloat, 0, TextureCreationFlags.None);
        distance_field.wrapMode = TextureWrapMode.Mirror;

        Material mat = test_renderer.material;
        mat.SetTexture("_MainTex", distance_field);

        // algorithm from:
        // https://www.youtube.com/watch?v=QjrAJwaUy64
        // https://en.wikipedia.org/wiki/Jump_flooding_algorithm

        for (int y = 0; y < terrain_source.height; ++y)
        {
            for (int x = 0; x < terrain_source.width; ++x)
            {
                outside[x, y] = new int2(-1, -1);
                inside[x, y] = new int2(-1, -1);
                float alpha = terrain_source.GetPixel(x, y).a;
                if (alpha < 0.5f)
                    inside[x, y] = new int2(x, y);
                else
                    outside[x, y] = new int2(x, y);
            }
        }

        //outside = JFPass(outside, 32);
        //outside = JFPass(outside, 16);
        //outside = JFPass(outside, 8);
        //outside = JFPass(outside, 4);
        //outside = JFPass(outside, 2);
        //outside = JFPass(outside, 1);
        //outside = JFPass(outside, 32);
        //outside = JFPass(outside, 16);
        //outside = JFPass(outside, 8);
        //outside = JFPass(outside, 4);
        //outside = JFPass(outside, 2);
        //outside = JFPass(outside, 1);
        //
        //inside = JFPass(inside, 32);
        //inside = JFPass(inside, 16);
        //inside = JFPass(inside, 8);
        //inside = JFPass(inside, 4);
        //inside = JFPass(inside, 2);
        //inside = JFPass(inside, 1);
        //inside = JFPass(inside, 32);
        //inside = JFPass(inside, 16);
        //inside = JFPass(inside, 8);
        //inside = JFPass(inside, 4);
        //inside = JFPass(inside, 2);
        //inside = JFPass(inside, 1);

        for (int y = 0; y < width; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                //int x_other = x * terrain_source.width / width;
                //int y_other = y * terrain_source.height / height;
                //
                //int2 pos_outside = outside[x_other, y_other];
                //int dxo = x_other - pos_outside.x;
                //int dyo = y_other - pos_outside.y;
                //float dist_outside = (float)Math.Sqrt(dxo * dxo + dyo * dyo);
                //
                //int2 pos_inside = inside[x_other, y_other];
                //int dxi = x_other - pos_inside.x;
                //int dyi = y_other - pos_inside.y;
                //float dist_inside = (float)Math.Sqrt(dxi * dxi + dyi * dyi);

                float dist_outside = terrain_source_outside.GetPixel(x, y).r;
                float dist_inside = terrain_source_inside.GetPixel(x, y).r;


                if (dist_inside <= dist_outside)
                {
                    distance_field.SetPixel(x, y, new Color(dist_outside, 0f, 0f));
                }
                else
                {
                    distance_field.SetPixel(x, y, new Color(-dist_inside, 0f, 0f));
                }
                //if (dist_inside < dist_outside)
                //{
                //    distance_field.SetPixel(x, y, new Color(0.5f + dist_outside / 80f, 0f, 0f));
                //}
                //else
                //{
                //    distance_field.SetPixel(x, y, new Color(0.5f - dist_inside / 80f, 0f, 0f));
                //}
                //distance_field.SetPixel(x, y, new Color((float)position_stored.x / (float)terrain_source.width, (float)position_stored.y / (float)terrain_source.height, 0f));
            }
        }

        /*
        // approximate distance field generation
        // (assumes that initial terrain map is not too high frequency / detailed)
        for (int y = 0; y < width; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                float2 pos = new float2(((float)x + 0.5f) / (float)width, ((float)y + 0.5f) / (float)height);
                int2 center = (int2) (pos * new float2(terrain_source.width, terrain_source.height));

                // terrain alpha at this position
                float this_alpha = terrain_source.GetPixel(center.x, center.y).a;
                // whether starting inside or outside the area
                bool inside = this_alpha > 0.5;
                // default distance
                distance_field.SetPixel(x, y, new Color(inside ? 0f : 1f, 0f, 0f));

                // once a distance is found, further checks not needed
                bool found_distance = false;

                // iterate circles outward
                for (int c = 0; c < circle_count; ++c)
                {
                    // test more circles near the center
                    // https://www.desmos.com/calculator/8nfkpizrpz
                    float r = ( (float)(c*c) / (float)(circle_count * circle_count) ) * circle_r_max;
                    // test some multiple of `r` angles
                    for (int i = 0; i <= 7f * r; ++i)
                    {
                        // get angle
                        float theta = 2 * Mathf.PI * (float)i / r;
                        // get direction at that angle
                        float2 dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                        // get other position `r` away in `dir` direction
                        int2 other_pos = (int2)((float2)center + dir * r);

                        if (other_pos.x >= 0 && other_pos.x < terrain_source.width && other_pos.y >= 0 && other_pos.y < terrain_source.height)
                        {
                            float alpha = terrain_source.GetPixel(other_pos.x, other_pos.y).a;
                            if (inside && alpha <= 0.5)
                            {
                                float dist_normalized = r / circle_r_max;
                                float final_dist = 0.5f - 0.5f * dist_normalized;
                                distance_field.SetPixel(x, y, new Color(final_dist, 0f, 0f));
                                found_distance = true;
                            }
                            else if (!inside && alpha >= 0.5)
                            {
                                float dist_normalized = r / circle_r_max;
                                float final_dist = 0.5f + 0.5f * dist_normalized;
                                distance_field.SetPixel(x, y, new Color(final_dist, 0f, 0f));
                                found_distance = true;
                            }
                        }
                        if (found_distance) break;
                    }
                    if (found_distance) break;
                }
            }
        }
        */


        distance_field.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
