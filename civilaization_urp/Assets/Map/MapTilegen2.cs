using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using System;

struct SceneTile
{
    public Transform transform;
    public MeshRenderer renderer;

    public SceneTile(GameObject g)
    {
        transform = g.transform;
        renderer = g.GetComponent<MeshRenderer>();
    }
}

public class MapTilegen2 : MonoBehaviour
{
    // Load Data
    private List<List<int>> tilemap;
    private int width, height;
    private float[] depths;
    
    private Dictionary<int, String> IDToISO;
    private Dictionary<String, int> ISOtoID;
    
    private SceneTile[] sceneTiles;
    
    // Positioning
    [SerializeField] private float innerRadius; private float outerRadius;
    
    // Scene + Rendering
    [SerializeField] private GameObject tileObj;
    [SerializeField] private Material mat; // *** materials for each nation
    [SerializeField] private Texture2D depth;
    private int matColorID;

    [SerializeField] private List<Leader> leaders;

    [SerializeField] private float beginAnimationLerp = 1000f;
    [SerializeField] private float extrusion = 20f;
    
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

        int i = 0;
        foreach (Json entry in tileJson["ids"])
        {
            IDToISO.Add(i, entry);
            ISOtoID.Add(entry, i);
            ++i;
        }

        Debug.Log(ISOtoID["RU"]);
        
        totalTiles = 0;
        foreach (Json row in tileJson["grid"])
        {
            List<int> gridRow = new List<int>(width);
            foreach (Json cell in row)
            {
                int id = cell;
                
                // Collapse id
                // -1 = water
                // low index = significant
                // high index = insignificant
                if (id == 0)
                {
                    --id;
                }
                else
                {
                    String iso = IDToISO[id];
                    bool relevant = false;
                    for (int j = 0; j < leaders.Count; ++j)
                    {
                        if (leaders[j].isoID == iso)
                        {
                            id = j;
                            relevant = true;
                            break;
                        }
                    }

                    if (!relevant)
                    {
                        id = leaders.Count;
                    }
                }
                
                gridRow.Add(id);
                ++totalTiles;
            }
            tilemap.Add(gridRow);
        }
    }
    
    void Awake()
    {
        LoadJSON();

        // Rendering Setup
        sceneTiles = new SceneTile[totalTiles];
        depths = new float[totalTiles];
        
        int ind = 0;
        for (int i = 0; i < tilemap.Count; ++i)
        {
            float xFrac = i / (float)tilemap.Count;
            for (int j = 0; j < tilemap[i].Count; ++j)
            {
                float yFrac = 1 - j / (float)tilemap[i].Count;

                depths[ind] = distribute(depth.GetPixel((int)(xFrac * depth.width), (int)(yFrac * depth.height)).r);
                
                int id = tilemap[i][j];
                
                GameObject newTile = Instantiate(tileObj, transform);
                sceneTiles[ind] = new SceneTile(newTile);

                if (id >= 0 && id < leaders.Count)
                {
                    sceneTiles[ind].renderer.material = leaders[id].material;
                }
                ++ind;
            }
        }

        matColorID = Shader.PropertyToID("_Color");
        UpdatePositions();
    }

    void Update()
    {
        beginAnimationLerp = Mathf.Lerp(beginAnimationLerp, 0f, 0.1f);
        UpdatePositions();
    }

    float sigmoid(float value)
    {
        return 1f / (1f + Mathf.Pow((float)Math.E, -value));
    }

    float distribute(float x)
    {
        return x == 0
            ? 0
            : x == 1
                ? 1
                : x < 0.5f ? Mathf.Pow(2, 20 * x - 10) / 2
                    : (2 - Mathf.Pow(2, -20 * x + 10)) / 2;
    }

    private void UpdatePositions()
    {
        outerRadius = innerRadius * 0.866025404f;

        int ind = 0;
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                Vector3 pos = new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, depths[ind] * (beginAnimationLerp + extrusion),
                    z * outerRadius * 2f);
                sceneTiles[ind].transform.position = pos;
                ++ind;
            }
        }
    }
}
