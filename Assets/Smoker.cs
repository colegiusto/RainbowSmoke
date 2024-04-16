using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoker : MonoBehaviour
{
    Texture2D tex;
    [SerializeField]
    Vector3Int resolution;
    void Start()
    {
        tex = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public class SmokeSpace
    {
        bool[] unused;
        int[] offsets;
        int sideLength;

        public SmokeSpace(int sideLength)
        {
            this.sideLength = sideLength;
            unused = new bool[sideLength*sideLength*sideLength];
            List<int> offsetsList = new List<int>();
            for(int i = 0; i< unused.Length*8; i++)
            {
                offsetsList.Add(i);
            }
            offsets = offsetsList.ToArray();

        }


    }
}
