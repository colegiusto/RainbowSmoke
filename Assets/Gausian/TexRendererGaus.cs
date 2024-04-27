using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public class TexRendererGaus : MonoBehaviour
{
    public Vector2Int resolution;
    Texture2D tex;

    [SerializeField]
    float delay = 0.2f;
    float last = 0f;

    [SerializeField]
    int csize;
    [SerializeField]
    Vector3 cMins;
    [SerializeField]
    Vector3 cMaxs;

    List<Vector2Int> boundaryPixels;
    HashSet<Vector2Int> bPHash;
    HashSet<Vector2Int> used;


    CSpace space;

    [SerializeField]
    Color bc;
    [SerializeField]
    float neighborTime;
    [SerializeField]
    float searchTime;

    [SerializeField]
    int numStartingPoints;

    // Start is called before the first frame update
    public void _start(float[,] probablility, Texture2D baseImage)
    {
        tex = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;

        boundaryPixels = new List<Vector2Int>();
        bPHash = new HashSet<Vector2Int>();
        used = new HashSet<Vector2Int>();


        space = new CSpace(csize, cMins, cMaxs);
        space.SortOpens(bc);

        Color[,] cs = resolution.ToBoxArray((x, y) => Color.black);
        tex.SetPixels(cs.Flatten());



        List<Vector2Int> posiblePixels = new List<Vector2Int>(resolution.TLtoBR());

        boundaryPixels.Add(resolution.TLtoBR().RandomElement());
        bPHash.Add(boundaryPixels[0]);

        for (int i = 0; i < numStartingPoints; i++)
        {
            Vector2Int pixel = posiblePixels.RandomElement();

            if (Mathf.Abs(probablility[pixel.x, pixel.y] + .01f) > UnityEngine.Random.value)
            {
                posiblePixels.Remove(pixel);

                tex.SetPixel(pixel.x, pixel.y, baseImage.GetPixel(pixel.x, pixel.y));
                used.Add(pixel);

            }
            else
            {
                i--;
            }

        }

        foreach (var item in used)
        {
            foreach (var item2 in item.BoundedNeighbors(resolution))
            {
                if (!used.Contains(item2))
                {
                    boundaryPixels.Add(item2);
                    bPHash.Add(item2);
                }
            }
        }


        tex.Apply();

        
        RenderPipelineManager.endContextRendering += OnEndContext;

    }


    void OnEndContext(ScriptableRenderContext context, List<Camera> cams)
    {
        Graphics.Blit(tex, (RenderTexture)null);
    }

    private void Update()
    {
        if(Time.time > last + delay)
        {
            last = Time.time;
            for(int i = 0; i<200; i++){
                Rend();
            }
            tex.Apply();


        }
    }
    private void Rend()
    {
        if(boundaryPixels.Count == 0)
        {
            return;
        }

        float startTime = Time.realtimeSinceStartup;
        Vector2Int pt = boundaryPixels.RandomElement();
        Vector3Int average = Vector3Int.zero;
        int j = 0;
        foreach (Vector2Int v in pt.BoundedNeighbors(resolution))
        {
            if (used.Contains(v))
            {
                average += space.GetIndex(tex.GetPixel(v.x, v.y));
                j++;
                continue;
            }
            if (!bPHash.Contains(v))
            {
                boundaryPixels.Add(v);
                bPHash.Add(v);
            }
            
        }
        if(j == 0)
        {
            average = space.GetIndex(bc);

        }
        else
        {
            float off = 0;
            average = Vector3Int.RoundToInt((Vector3)average / (j-off));

        }
        neighborTime += Time.realtimeSinceStartup - startTime;
        startTime = Time.realtimeSinceStartup;
        tex.SetPixel(pt.x, pt.y, space.GetOpenNeighbor(space.GetColor(average)));


        boundaryPixels.Remove(pt);
        bPHash.Remove(pt);
        used.Add(pt);
        searchTime += Time.realtimeSinceStartup - startTime;

    }
    private void OnApplicationQuit()
    {
        File.WriteAllBytes("Assets/Outputs/tex.png", tex.EncodeToPNG());
    }

}

