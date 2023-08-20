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
    private Bounds[] boundingBoxes;

    [SerializeField] private float beginAnimationLerp = 1000f;
    [SerializeField] private float extrusion = 20f;

    [SerializeField] private GameManager gameMan;

    [SerializeField] private AnimationCurve captureCurve;
    [SerializeField] private float captureAnimSpeed;
    [SerializeField] private float captureAnimTime;
    
    private float lastSelectedTime = 0f;
    
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

        int saveCuba = 0;
        while (saveCuba < leaders.Count)
        {
            if (leaders[saveCuba].isoID == "CU") break;
            ++saveCuba;
        }
        
        int ind = 0;
        for (int i = 0; i < tilemap.Count; ++i)
        {
            float xFrac = i / (float)tilemap.Count;
            for (int j = 0; j < tilemap[i].Count; ++j)
            {
                float yFrac = 1 - j / (float)tilemap[i].Count;

                if (j == 0)
                {
                    depths[ind] = -0.7f;
                }
                else if (tilemap[i][j] == saveCuba)
                {
                    depths[ind] = 0.4f;
                }
                else
                {
                    depths[ind] = distribute(depth.GetPixel((int)(xFrac * depth.width), (int)(yFrac * depth.height)).r) - 0.7f;
                }
                
                int id = tilemap[i][j];

                GameObject newTile = Instantiate(tileObj, transform);
                sceneTiles[ind] = new SceneTile(newTile);
                newTile.name = ind.ToString();

                if (id >= 0 && id < leaders.Count)
                {
                    sceneTiles[ind].renderer.material = leaders[id].material;
                }
                ++ind;
            }
        }
        
        matColorID = Shader.PropertyToID("_Color");
        UpdatePositions();
        
        boundingBoxes = new Bounds[leaders.Count + 1];
        // Goofyf
    }

    void Update()
    {
        lastSelectedTime += Time.deltaTime;
        beginAnimationLerp = Mathf.Lerp(beginAnimationLerp, 0f, 0.05f);
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
    
    private Vector3 getPosNoDepth(int x, int z)
    {
        return new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, z * outerRadius * 2f);
    }

    private Vector3 getPosition(int x, int z, int ind, float vertical)
    {
        return new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, depths[ind] * (beginAnimationLerp + extrusion) + vertical, z * outerRadius * 2f);
    }
    
    private void UpdatePositions()
    {
        outerRadius = innerRadius * 0.866025404f;

        int selectedLeaderIndex = leaders.IndexOf(gameMan.leader);
        
        int ind = 0;
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                float vertical = 0;
                float lerpSpeed = 10f * Time.deltaTime;
                if (selectedLeaderIndex >= 0 && tilemap[z][x] == selectedLeaderIndex)
                {
                    vertical = 5f + 1 / (1f + lastSelectedTime * 2f);
                    lerpSpeed *= 3f;
                }
                
                sceneTiles[ind].transform.position = Vector3.Lerp(sceneTiles[ind].transform.position, getPosition(x, z, ind, vertical), lerpSpeed);
                ++ind;
            }
        }
    }

    public void DeselectLand()
    {
        Debug.Log("???");
        gameMan.LeaderSelected(null);
    }

    public void OnDrawGizmos()
    {
        if (boundingBoxes != null)
        {
            for (int i = 0; i < boundingBoxes.Length; ++i)
            {
                Bounds b = boundingBoxes[i];
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }

    public void SelectTile(int id)
    {
        int ind = 0;
        for (int i = 0; i < tilemap.Count; ++i)
        {
            for (int j = 0; j < tilemap[i].Count; ++j)
            { 
                if (ind == id)
                {
                    int tileID = tilemap[i][j];
                                
                    if (tileID >= 0 && tileID < leaders.Count)
                    {
                        //Debug.Log(leaders[tileID]);
                        gameMan.LeaderSelected(leaders[tileID]);
                        lastSelectedTime = 0;
                    }
                    else if (tileID < 0)
                    {
                        DeselectLand();
                    }
                }
                ++ind;
            }
        }
    }

    public void Capture(Leader leader)
    {
        for (int i = 0; i < tilemap.Count; ++i)
        {
            for (int j = 0; j < tilemap[i].Count; ++j)
            {

            }

        }
    }
}