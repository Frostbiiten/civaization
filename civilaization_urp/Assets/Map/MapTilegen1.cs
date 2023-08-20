using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using System;

public class MapTilegen1 : MonoBehaviour
{
    // Load Data
    private List<List<int>> tilemap;
    private int width, height;
    
    private Dictionary<int, String> IDToISO;
    private Dictionary<String, int> ISOtoID;
    
    // Positioning
    [SerializeField] private float innerRadius; private float outerRadius;
    
    // Rendering
    [SerializeField] private Mesh tileMesh;
    [SerializeField] private Material mat; // *** materials for each nation
    [SerializeField] private MapChunk[] chunks;
    private int matColorID;
    
    private int totalTiles;
    void LoadJSON()
    {
        IDToISO = new Dictionary<int, String>();
        ISOtoID = new Dictionary<String, int>();
        
        TextAsset tileData = Resources.Load<TextAsset>("tiledata");
        Json tileJson = Json.Deserialize(tileData.text);

        width = tileJson["width"];
        height = tileJson["height"];
        tilemap = new List<List<int>>(height);
        foreach (Json entry in tileJson["ids"])
        {
            try
            {
                int id = IDToISO.Count;
                IDToISO.Add(id, entry);
                ISOtoID.Add(entry, id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.Log(entry);
                throw;
            }
        }

        totalTiles = 0;
        foreach (Json row in tileJson["grid"])
        {
            List<int> gridRow = new List<int>(width);
            foreach (Json cell in row)
            {
                int id = cell;
                gridRow.Add(id);
                ++totalTiles;
            }
            tilemap.Add(gridRow);
        }
    }

    int ToChunkIndex(int index, int chunk)
    {
        return index - chunk * 1023;
    }
    
    void Awake()
    {
        LoadJSON();
        
        for (int i = 0; i < tilemap.Count; ++i)
        {
            for (int j = 0; j < tilemap[i].Count; ++j)
            {
                int id = tilemap[i][j];
            }
        }
        
        // Rendering Setup
        int tilesRemaining = totalTiles;
        chunks = new MapChunk[(tilesRemaining + 1022) / 1023];
        int currentChunk = 0;
        while (tilesRemaining > 0)
        {
            int delta = Math.Min(tilesRemaining, 1023);
            chunks[currentChunk] = new MapChunk(delta);
            tilesRemaining -= delta;
            ++currentChunk;
        }
        
        matColorID = Shader.PropertyToID("_Color");
        MatrixUpdate();
    }
    
    private void Update()
    {
        Draw();
        //RenderParams renderParams = new RenderParams(mat);

        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                //renderInstanceData[c] = new TileInstanceData();
                //renderInstanceData[c].objectToWorld = Matrix4x4.Translate();
                //if (tilemap[z][x] == 0) renderInstanceData[c].objectToWorld *= Matrix4x4.Translate(new Vector3(0, -999999, 0)); // :skull:
                //++total
            }
        }
        //Graphics.RenderMeshInstanced(renderParams, tileMesh, 0,);
    }

    private void MatrixUpdate()
    {
        outerRadius = innerRadius * 0.866025404f;
        
        int id = 0, chunk = 0;
        Matrix4x4[] matrices = chunks[0].baseMatrices;
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                Vector3 pos = new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, Mathf.Pow(x, 0.35f) * -1f,
                    z * outerRadius * 2f);

                matrices[id] = Matrix4x4.Translate(pos);
                
                ++id;
                if (id >= 1023) // or 1024 ???
                {
                    ++chunk;
                    id = 0;
                    matrices = chunks[chunk].baseMatrices;
                }
            }
        }
    }

    private void Draw()
    {
        for (int i = 0; i < chunks.Length; ++i)
        {
            var c = chunks[i];
            Matrix4x4[] m4x4 = c.baseMatrices;
            MaterialPropertyBlock mpb = c.matProps;
            Graphics.DrawMeshInstanced(tileMesh, 0, mat, m4x4);
        }
    }
}
