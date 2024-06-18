using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Smoker : MonoBehaviour
{
    Texture2D tex;
    [SerializeField]
    Vector2Int resolution;
    int size;
    [SerializeField]
    Vector3Int cSpace;
    OutputSapce<Color> smoke;
    void Start()
    {
        tex = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        size = resolution.x*resolution.y;



        InputSpace<Color> iSpace = new InputSpace<Color>(size, InputSpace<Color>.Generate3DOffsets(size, cSpace.x, cSpace.y, cSpace.y), InputSpace<Color>.RGBSpaceMap(cSpace.x, cSpace.y, cSpace.z));

        
        smoke = new OutputSapce<Color>(size, OutputSapce<Color>.Get2DNeighborMap(resolution.x, resolution.y), iSpace);
        RenderPipelineManager.endContextRendering += OnEndContext;
    }

    // Update is called once per frame
    void Update()
    {
        if (smoke.Finished)
        {
            print("done");
            return;
        }
        smoke.AddPoint();
        tex.SetPixels(smoke.GetValues());
        tex.Apply();
        
    }
    void OnEndContext(ScriptableRenderContext context, List<Camera> cams)
    {
        Graphics.Blit(tex, (RenderTexture)null);
    }


    public class OutputSapce<T> where T : struct
    {
        int size;
        int[] indices;
        T[] values;

        Func<int, int[]> neighbors;
        List<int> boundry;
        HashSet<int> used;

        InputSpace<T> inputSpace;
        public bool Finished => used.Count >= size;

        public OutputSapce(int _size, Func<int, int[]> _neighbors, InputSpace<T> input)
        {
            size = _size;
            indices = new int[size];
            values = new T[size];
            neighbors = _neighbors;
            boundry = new List<int>();
            used = new HashSet<int>();
            inputSpace = input;
            boundry.Add(UnityEngine.Random.Range(0, size));

        }

        public void AddPoint()
        {
            int index = boundry.RandomElement();
            boundry.Remove(index);
            used.Add(index);
            List<int> ns = new List<int>();
            foreach (int i in neighbors(index))
            {
                if (!used.Contains(i))
                {
                    boundry.Add(i);
                    continue;
                }
                ns.Add(i);
            }

            
            indices[index] = inputSpace.GetClosest(ns.ToArray());
            print(index);
            values[index] = inputSpace.GetVal(indices[index]);

        }
        public T[] GetValues()
        {
            return values;
        }
        public static Func<int, int[]> Get2DNeighborMap(int xs, int ys)
        {
            int s  = xs * ys;
            return (i) =>
            {
                int x = i % xs;
                int y = i / xs;
                return new int[] { (x + 1)%s, ((x - 1)%s+s)%s, (x + y)%s, ((x - y)%s+s)%s};
            };
        }

    }
    public class InputSpace<T> where T : struct
    {
        int size;
        T[] values;
        int[] offsets;
        HashSet<int> used;

        public InputSpace(int _size, int[] _offsets, Func<int, T> spaceMap)
        {
            size = _size;
            offsets = _offsets;
            values = new T[size];
            for(int i = 0; i < size; i++)
            {
                values[i] = spaceMap(i);
            }

            used = new HashSet<int>();

        }
        public T GetVal(int index)
        {
            return values[index];
        }
        public int GetClosest(int[] indices)
        {
            if(indices.Length == 0)
            {
                return InitialSample();
            }
            return 0;
        }
        public int InitialSample()
        {
            return 64*64*64-1;
            //return UnityEngine.Random.Range(0, size);
        }
        public static int[] Generate3DOffsets(int size, int x, int y, int z)
        {
            List<int> offsets = new List<int>();
            for(int i = 0;i < offsets.Count; i++)
            {

            }
            return offsets.ToArray();
        }
        public static Func<int, Color> RGBSpaceMap(int x, int y, int z)
        {
            return (i) =>
            {
                int r = i % x;
                int g = i / x % y;
                int b = i / x / y;
                return new Color((float)r/x, (float)g/y, (float)b/z);
            };
        }
    }
}
