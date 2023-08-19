using System;
using System.Collections;
using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapTilegen : MonoBehaviour
{
    // data
    private Dictionary<String, int> ids;
    private List<List<int>> tilemap;
    private int width, height;
    
    // drawing
    private Matrix4x4 transformations, prevTransformations;
    [SerializeField] private float innerRadius, scale;
    private float outerRadius;
    [SerializeField] private Mesh tileMesh;
    [SerializeField] private Material mat; // *** materials for each nation
    
    struct TileInstanceData
    {
        public Matrix4x4 objectToWorld;
        public uint renderingLayerMask;
    };

    private TileInstanceData[] renderInstanceData;
    void Awake()
    {
        Vector2 v = new Vector2();
        Debug.Log(v.GetType());
        TextAsset tileData = Resources.Load<TextAsset>("tiledata");
        Json tileJson = Json.Deserialize(tileData.text);

        width = tileJson["width"];
        height = tileJson["height"];
        tilemap = new List<List<int>>(height);
        foreach (var entry in tileJson["ids"])
        {
            //Debug.Log( (KeyValuePair<object, object>)entry);
            Debug.Log(entry.ToString());
            //ids.Add(entry.Key, entry.Value); ??? what
        }
        foreach (Json row in tileJson["grid"])
        {
            List<int> gridRow = new List<int>(width);
            foreach (Json cell in row)
            {
                gridRow.Add(cell);
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
        var baseMatrix = Matrix4x4.Scale(Vector3.one * scale);
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                renderInstanceData[c] = new TileInstanceData();
                renderInstanceData[c].objectToWorld = Matrix4x4.Translate(new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, z * 0.02f, z * outerRadius * 2f));
                if (tilemap[z][x] == 0) renderInstanceData[c].objectToWorld *= Matrix4x4.Translate(new Vector3(0, -999999, 0)); // :skull:
                ++c;
            }
        }
        Graphics.RenderMeshInstanced(renderParams, tileMesh, 0, renderInstanceData);
    }
}
