using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Gausian
{

    readonly static float SQRT_2PI = Mathf.Sqrt(2 * Mathf.PI);

    public static Color[] Blur1D(Color[] pixels, int width, int height, float deviation, bool horizontal = true)
    {
        

        Color[] outputPixels = new Color[pixels.Length];

        float[] kernel = new float[(int)Mathf.Max(deviation, 1) * 3];
        float totalWeight = 0;

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] = SampleNormalDistribution(deviation, i - (kernel.Length / 2));
            totalWeight += kernel[i];
        }

        if (horizontal)
        {

            for (int i = 0; i < outputPixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;

                Vector3 current = Vector3.zero;

                for (int j = 0; j < kernel.Length; j++)
                {
                    int cur_x = x - kernel.Length / 2 + j;
                    if (cur_x < 0)
                    {
                        cur_x = Math.Abs(cur_x) - 1;
                    }
                    else if (cur_x > width - 1)
                    {
                        cur_x = width + width - cur_x - 1;
                    }

                    Color c = Color.black;
                    try
                    {

                        c = pixels[y * width + cur_x];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.Log(cur_x);
                    }
                    current += new Vector3(c.r, c.g, c.b) * kernel[j];

                }
                current = current / totalWeight;
                outputPixels[i] = new Color(current.x, current.y, current.z);
            }
            
        }
        else
        {
            for (int i = 0; i < outputPixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;

                Vector3 current = Vector3.zero;

                for (int j = 0; j < kernel.Length; j++)
                {
                    int cur_y = y - kernel.Length / 2 + j;
                    if (cur_y < 0)
                    {
                        cur_y = Math.Abs(cur_y) - 1;
                    }
                    else if (cur_y > height - 1)
                    {
                        cur_y = height - (cur_y - height) -1;
                    }

                    Color c = pixels[cur_y * width + x];
                    current += new Vector3(c.r, c.g, c.b) * kernel[j];

                }
                current = current / totalWeight;
                outputPixels[i] = new Color(current.x, current.y, current.z);
            }

        }



        return outputPixels;
    }


    public static Color[] Blur2D(Color[] tex, int width, int height, float deviation)
    {
        tex = Blur1D(tex, width, height, deviation, true);
        tex = Blur1D(tex, width,height, deviation, false);
        return tex;
    }

    public static void Blur2D_tex(Texture2D tex, float deviation)
    {
        Color[] c = Blur2D(tex.GetPixels(), tex.width, tex.height, deviation);

        tex.SetPixels(c);
    }


    public static void DifferenceOfGausians(Texture2D tex, float d1, float k)
    {
        Color[] t1, t2;

        t1 = tex.GetPixels();
        t2 = new List<Color>(t1).ToArray();


        t1 = Blur2D(t1, tex.width, tex.height, d1);

        t2 = Blur2D(t2, tex.width, tex.height, d1 * k);

        Color[] diff = new Color[t1.Length];


        for(int i = 0; i < t1.Length; i++)
        {
            Vector3 v1 = new Vector3(t1[i].r, t1[i].g, t1[i].b);
            Vector3 v2 = new Vector3(t2[i].r, t2[i].g, t2[i].b);

            v1 = v2 - v1;

            diff[i] = new Color(v1.magnitude, v1.magnitude, v1.magnitude);

        }
        tex.SetPixels(diff);
        tex.Apply();




    }


    public static float SampleNormalDistribution(float deviation, float x, float mean = 0)
    {
        return Mathf.Exp(-Mathf.Pow(x - mean, 2) / 2 / deviation / deviation) / deviation / SQRT_2PI;
    }


}
