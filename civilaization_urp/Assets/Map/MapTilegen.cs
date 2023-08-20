using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using System;

public struct TileInstanceData
{
    public Matrix4x4 objectToWorld;
    public uint renderingLayerMask;
};

public class NationRender
{
    public TileInstanceData[] renderInstanceData;

    public NationRender(int size)
    {
        renderInstanceData = new TileInstanceData[size];
    }
}

public class MapTilegen : MonoBehaviour
{
    // Load Data
    private Dictionary<int, String> ids;
    private Dictionary<String, int> reverse_ids;
    
    private List<List<int>> tilemap;
    private int width, height;
    
    // Drawing
    [SerializeField] private float innerRadius;
    private float outerRadius;
    [SerializeField] private Mesh tileMesh;
    [SerializeField] private Material mat; // *** materials for each nation
    
    // Nation renderers
    private Dictionary<int, NationRender> renderers = new();
    
    private TileInstanceData[] renderInstanceData;

    [SerializeField] private List<Leader> leaders;
    private Dictionary<int, Leader> _leadersDict;
    private const int nonaligned = 99;
    
    // Collapse all other IDs to one misc nation
    void TransformID(int id)
    {
        
    }
    
    void LoadJSON()
    {
        ids = new Dictionary<int, String>();
        reverse_ids = new Dictionary<String, int>();
        
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
                reverse_ids.Add(entry, ids.Count);
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
    }
    
    void Awake()
    {
        LoadJSON();
    }

    private void UpdateMesh(int id)
    {
        
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
                if (tilemap[z][x] == 0) renderInstanceData[c].objectToWorld *= Matrix4x4.Translate(new Vector3(0, -999999, 0)); // :skull:
                ++c;
            }
        }
        Graphics.RenderMeshInstanced(renderParams, tileMesh, 0, renderInstanceData);
    }
}
