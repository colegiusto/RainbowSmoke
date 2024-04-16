using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TexRenderer : MonoBehaviour
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

    // Start is called before the first frame update
    void Start()
    {
        tex = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;

        boundaryPixels = new List<Vector2Int>();
        bPHash = new HashSet<Vector2Int>();
        used = new HashSet<Vector2Int>();
        boundaryPixels.Add(resolution.TLtoBR().RandomElement());
        bPHash.Add(boundaryPixels[0]);

        space = new CSpace(csize, cMins, cMaxs);
        space.SortOpens(bc);

        Color[,] cs = resolution.ToBoxArray((x, y) => Color.black);
        tex.SetPixels(cs.Flatten());
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
        Vector2Int pt = boundaryPixels.ToArray().RandomElement();
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

public class CSpace
{

    public Vector3Int size;
    List<Vector3Int> openVals;
    public HashSet<Vector3Int> unused;

    public Vector3Int[] offsets;

    Vector3 cMins;
    Vector3 cMaxs;
    public CSpace(int sideLength, Vector3 cMins_, Vector3 cMaxs_)
    {
        cMins = cMins_;
        cMaxs = cMaxs_;
        size = new Vector3Int(sideLength, sideLength, sideLength);
        openVals = new List<Vector3Int>();
        unused = new HashSet<Vector3Int>();
        for (int i = 0; i < sideLength * sideLength * sideLength; i++)
        {
            int x = i % sideLength;
            int y = i / sideLength % sideLength;
            int z = i / sideLength / sideLength;
            openVals.Add(new Vector3Int(x, y, z));
            unused.Add(new Vector3Int(x, y, z));
        }
        List<Vector3Int> offsetsList = new List<Vector3Int>(8*sideLength * sideLength * sideLength);
        int oSide = sideLength * 2;
        for (int i = 0; i < oSide * oSide * oSide; i++)
        {
            offsetsList.Add((new Vector3Int(i % oSide, i / oSide % oSide, i / oSide / oSide))-size);
        }
        offsetsList.Remove(Vector3Int.zero);
        offsetsList.Shuffle();
        offsetsList.Sort((x, y) => x.sqrMagnitude-y.sqrMagnitude);
        offsets = offsetsList.ToArray();
    }
    public Color GetOpenNeighbor(Color c)
    {
        
        Vector3Int index = GetIndex(c);
        Vector3Int ccolor = Vector3Int.zero;


        foreach (Vector3Int offset in offsets)
        {
            if (unused.Contains(index + offset))
            {
                ccolor = index + offset;
                unused.Remove(index + offset);

                break;
            }
        }


        return GetColor(ccolor);
    }
    public Vector3Int GetIndex(Color c)
    {
        Vector3 cv = new Vector3(c.r, c.g, c.b);
        cv -= cMins;
        Vector3 diffs = cMaxs - cMins;
        cv.x /= diffs.x;
        cv.y /= diffs.y;
        cv.z /= diffs.z;

        Vector3Int ints = Vector3Int.RoundToInt(cv);

        return Vector3Int.RoundToInt(new Vector3(cv.x*size.x, cv.y * size.y, cv.z * size.z));
    }
    public Color GetColor(Vector3Int v)
    {
        Vector3 c = new Vector3((float)v.x/size.x, (float)v.y/size.y, (float)v.z/size.z);
        Vector3 diffs = cMaxs - cMins;
        return new Color(cMins.x + diffs.x*c.x, cMins.y+diffs.y*c.y, cMins.z+diffs.z*c.z);
    }

    

    public void SortOpens(Color c)
    {
        Vector3Int v = GetIndex(c);
        openVals.Sort((x, y) => (int)(((Vector3)(x-v)).magnitude-((Vector3)(y-v)).magnitude));
    }
    
    

}

public static class Extensions
{
    
    public static Vector2Int[] TLtoBR(this Vector2Int BR)
    {
        Vector2Int[] vals = new Vector2Int[(BR.x) * (BR.y)];
        for(int i = 0; i < vals.Length; i++)
        {
            vals[i] = new Vector2Int(i%BR.x, i/BR.x);
        }
        
        return vals;
    }
    public static T[,] ToBoxArray<T>(this Vector2Int box, Func<int, int, T> initializer = null)
    {
        T[,] arr = new T[box.x, box.y];
        if(initializer != null)
        {
            foreach(Vector2Int pt in box.TLtoBR())
            {
                arr[pt.x, pt.y] = initializer(pt.x, pt.y);
            }
        }
        
        return arr;
    }
    public static List<Vector2Int> BoundedNeighbors(this Vector2Int v, Vector2Int size, Vector2Int? mins = null)
    {

        Vector2Int[] offsets = new Vector2Int[]
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
        };

        Vector2Int mins_ = Vector2Int.zero;
        if (mins.HasValue)
        {
            mins_ = mins.Value;
        }
        List<Vector2Int> arr = new List<Vector2Int>();
        RectInt bounds = new RectInt(mins_, size);


        for(int i = 0; i<4; i++)
        {
            Vector2Int nv = v + offsets[i];

            if (bounds.Contains(nv))
            {
                arr.Add(nv);
            }
        }
        return arr;

    }
    public static List<Vector3Int> BoundedNeighbors(this Vector3Int v, Vector3Int size, Vector3Int? mins = null)
    {
        Vector3Int[] offsets = new Vector3Int[]
        {
            new Vector3Int(1,0),
            new Vector3Int(-1,0),
            new Vector3Int(0,1),
            new Vector3Int(0,-1),
            new Vector3Int(0,0,1),
            new Vector3Int(0,0,-1),

        };

        Vector3Int mins_ = Vector3Int.zero;
        if (mins.HasValue)
        {
            mins_ = mins.Value;
        }
        List<Vector3Int> arr = new List<Vector3Int>();
        RectInt bounds = new RectInt(mins_.x, mins_.y, size.x, size.y);


        for (int i = 0; i < 6; i++)
        {
            Vector3Int nv = v + offsets[i];

            if (bounds.Contains((Vector2Int)nv))
            {
                arr.Add(nv);
            }
        }
        for(int i = -1; i < 2; i += 2)
        {
            Vector3Int nv = v + Vector3Int.forward * i;
            if(nv.z >= mins_.z && nv.z < size.z)
            {
                arr.Add(nv);
            }
        }
        return arr;

    }

    public static T RandomElement<T>(this T[] arr)
    {
        return arr[(int)(UnityEngine.Random.value*arr.Length)];
    }
    public static T RandomElement<T>(this T[,] arr)
    {
        int i = (int)(UnityEngine.Random.value*arr.Length);
        int s = arr.GetLength(0);
        return arr[i%s, i/s];
        
    }
    public static T[] Flatten<T>(this T[,] input)
    {
        // Step 1: get total size of 2D array, and allocate 1D array.
        int size = input.Length;
        T[] result = new T[size];

        // Step 2: copy 2D array elements into a 1D array.
        int write = 0;
        for (int i = 0; i <= input.GetUpperBound(0); i++)
        {
            for (int z = 0; z <= input.GetUpperBound(1); z++)
            {
                result[write++] = input[i, z];
            }
        }
        // Step 3: return the new array.
        return result;
    }

    public static Color ToColor(this Vector3Int v, Vector3Int size)
    {
        return new Color((float)v.x / size.x, (float)v.y / size.y, (float)v.z / size.z);
    }

    private static System.Random rng = new System.Random();
   
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

}