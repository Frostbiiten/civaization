using System;
using System.Collections;
using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;

public class Nation
{
    private String iso;
    //private 
}

public class MapTilegen : MonoBehaviour
{
    // data
    private Dictionary<int, String> ids;
    private List<List<int>> tilemap;
    private int width, height;
    
    // drawing
    private Matrix4x4 transformations, prevTransformations;
    [SerializeField] private float innerRadius, scale;
    private float outerRadius;
    [SerializeField] private Mesh tileMesh;
    [SerializeField] private Material mat; // *** materials for each nation

    private Transform tileposRef;
    
    // nations
    private List<Nation> nations = new List<Nation>();
    
    struct TileInstanceData
    {
        public Matrix4x4 objectToWorld;
        public uint renderingLayerMask;
    };

    private TileInstanceData[] renderInstanceData;
    void Awake()
    {
        ids = new Dictionary<int, String>();
        TextAsset tileData = Resources.Load<TextAsset>("tiledata");
        Json tileJson = Json.Deserialize(tileData.text);

        width = tileJson["width"];
        height = tileJson["height"];
        tilemap = new List<List<int>>(height);
        foreach (Json entry in tileJson["ids"])
        {
            try
            {
                ids.Add(ids.Count, entry);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.Log(entry);
                throw;
            }
        }
        foreach (Json row in tileJson["grid"])
        {
            List<int> gridRow = new List<int>(width);
            foreach (Json cell in row)
            {
                int id = cell;
                gridRow.Add(id);
            }
            tilemap.Add(gridRow);
        }

        renderInstanceData = new TileInstanceData[width * height];
    }

    private void Update()
    {
        outerRadius = innerRadius * 0.866025404f;
        RenderParams renderParams = new RenderParams(mat);
        
        int c = 0;
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                renderInstanceData[c] = new TileInstanceData();
                renderInstanceData[c].objectToWorld = Matrix4x4.Translate(new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, Mathf.Pow(x, 0.3f) * -3f, z * outerRadius * 2f));
                //if (tilemap[z][x] == 0) renderInstanceData[c].objectToWorld *= Matrix4x4.Translate(new Vector3(0, -999999, 0)); // :skull:
                if (tilemap[z][x] == 0) renderInstanceData[c].objectToWorld *= Matrix4x4.Translate(new Vector3(0, -999999, 0)); // :skull:
                ++c;
            }
        }
        Graphics.RenderMeshInstanced(renderParams, tileMesh, 0, renderInstanceData);
    }
}
