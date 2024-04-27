using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class GausianDriver : MonoBehaviour
{
    public Texture2D toBeBlured;
    Texture2D blured;

    public TexRendererGaus trg;
    // Start is called before the first frame update
    void Start()
    {
        //RenderPipelineManager.endContextRendering += OnEndContext;

        blured = new Texture2D(toBeBlured.width, toBeBlured.height);

        blured.SetPixels(toBeBlured.GetPixels());
        

        Gausian.DifferenceOfGausians(blured, 5, 2);
        //Gausian.Blur2D_tex(blured, 5);

        blured.Apply();

        float[,] probs = new float[blured.width, blured.height];
        Color[] pixels = blured.GetPixels();

        for (int i = 0; i < pixels.Count(); i++)
        {
            probs[i % blured.width, i / blured.width] = pixels[i].r;
        }
        trg.resolution = new Vector2Int(toBeBlured.width, toBeBlured.height);

        trg._start(probs, toBeBlured);

    }

    // Update is called once per frame
    void OnEndContext(ScriptableRenderContext context, List<Camera> cams)
    {
        Graphics.Blit(blured, (RenderTexture)null);
    }
}
