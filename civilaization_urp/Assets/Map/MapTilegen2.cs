using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using System;
using System.Collections;

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
    private List<List<int>> raw_tilemap;
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
    [SerializeField] private Texture2D depth;
    private int matColorID;

    [SerializeField] private List<Leader> leaders;
    [SerializeField] private Vector3[] centers;
    [SerializeField] private int[] tileCount;

    [SerializeField] private float beginAnimationLerp = 1000f;
    [SerializeField] private float extrusion = 20f;

    [SerializeField] private GameManager gameMan;

    [SerializeField] private AnimationCurve captureCurve;
    [SerializeField] private float captureAnimSpeed;
    [SerializeField] private float captureAnimTime;
    
    [SerializeField] private float allyAnimSpeed;
    [SerializeField] private float allyAnimTime;
    [SerializeField] private Material intermediateMat;
    
    [SerializeField] private Transform referenceOriginTransform;
    private float lastSelectedTime = 0f;
    private int totalTiles;
    private int canadaID, captureID = -1, allyID = -1;

    [SerializeField] private CameraScript cam;

    [SerializeField] private float zoomAmt, unzoomAmt;
    
    void LoadJSON()
    {
        IDToISO = new Dictionary<int, String>();
        ISOtoID = new Dictionary<String, int>();
        
        TextAsset tileData = Resources.Load<TextAsset>("tiledata");
        Json tileJson = Json.Deserialize(tileData.text);

        width = tileJson["width"];
        height = tileJson["height"];
        tilemap = new List<List<int>>(height);
        raw_tilemap = new List<List<int>>(height);

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
            List<int> gridRowRaw = new List<int>(width);
            foreach (Json cell in row)
            {
                int id = cell;
                gridRowRaw.Add(id);
                
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
            raw_tilemap.Add(gridRow);
        }
    }

    void Awake()
    {
        // sample capture and ally with us
        //Capture(leaders[10]);
        //Allegiance(leaders[10]);
        
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
        
        int saveSA = 0;
        while (saveSA < leaders.Count)
        {
            if (leaders[saveSA].isoID == "ZA") break;
            ++saveSA;
        }

        while (canadaID < leaders.Count)
        {
            if (leaders[canadaID].isoID == "CA") break;
            ++canadaID;
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
                else if (tilemap[i][j] == saveCuba || tilemap[i][j] == saveSA)
                {
                    depths[ind] = 0.3f;
                }
                else
                {
                    depths[ind] =
                        distribute(depth.GetPixel((int)(xFrac * depth.width), (int)(yFrac * depth.height)).r) - 0.7f;
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
        
        outerRadius = innerRadius * 0.866025404f;
        
        centers = new Vector3[leaders.Count];
        for (int i = 0; i < centers.Length; ++i) centers[i] = Vector3.zero;
        tileCount = new int[leaders.Count];

        int ind2 = 0;
        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width - 1; ++x)
            {
                int eye_d = tilemap[z][x];
                if (eye_d >= 0 && eye_d < leaders.Count)
                {
                    centers[eye_d] += getPosition(x, z, ind2, 0);
                    ++tileCount[eye_d];
                }
                ++ind2;
            }
        }
        
        for (int i = 0; i < centers.Length; ++i)
        {
            centers[i] /= tileCount[i];
            centers[i].y = 0;
        }

        UpdatePositions();
    }

    void Update()
    {
        lastSelectedTime += Time.deltaTime;
        captureAnimTime += Time.deltaTime;
        allyAnimTime += Time.deltaTime;
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
        return new Vector3((x + z * 0.5f - z / 2) * innerRadius * 2f, depths[ind] * (beginAnimationLerp + extrusion) + vertical + Mathf.Sin(z * 0.357f + Time.time) * 0.437f, z * outerRadius * 2f);
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

                int id = tilemap[z][x];
                if (id == captureID && captureID >= 0 && captureID < leaders.Count)
                {
                    float t = Mathf.Sqrt(Vector3.Distance(centers[captureID], getPosition(x, z, ind, 0)) * 1.5f) + captureAnimTime * captureAnimSpeed;
                    //if (captureAnimTime > 4f) captureAnimTime = 0f;
                    vertical = captureCurve.Evaluate(t);
                    if (t < 0.1f)
                    {
                        if (t > -0.1f)
                        {
                            sceneTiles[ind].renderer.material = intermediateMat;
                        }
                        else
                        {
                            sceneTiles[ind].renderer.material = leaders[canadaID].material;
                        }
                    }
                    else
                    {
                        sceneTiles[ind].renderer.material = leaders[captureID].material;
                    }
                }
                else if (id == allyID && allyID >= 0 && allyID < leaders.Count)
                {
                    float t = Mathf.Sqrt(Vector3.Distance(centers[allyID], getPosition(x, z, ind, 0)) * 1.5f) + allyAnimTime * allyAnimSpeed;
                    //if (allyAnimTime > 4f) allyAnimTime = 0f;
                    vertical = captureCurve.Evaluate(t);
                    if (t < 0.1f)
                    {
                        if (t > -0.1f)
                        {
                            sceneTiles[ind].renderer.material = intermediateMat;
                        }
                        else
                        {
                            sceneTiles[ind].renderer.material = leaders[allyID].material;
                        }
                    }
                    else
                    {
                        sceneTiles[ind].renderer.material = leaders[allyID].material;
                    }
                }
                else if (selectedLeaderIndex >= 0 && id == selectedLeaderIndex)
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
        gameMan.LeaderSelected(null);
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
                                
                    if (tileID >= 0 && tileID < leaders.Count && tileID != canadaID)
                    {
                        //Debug.Log(leaders[tileID]);
                        gameMan.LeaderSelected(leaders[tileID]);
                        cam.SetTarget(centers[tileID], zoomAmt);
                        lastSelectedTime = 0;
                    }
                    else //if (tileID < 0)
                    {
                        if (tileID < 0)
                        {
                            cam.Reset();
                        }
                        
                        DeselectLand();
                    }
                }
                ++ind;
            }
        }
    }

    public void Capture(Leader leader)
    {
        captureID = leaders.IndexOf(leader);
        StartCoroutine(CaptureAnim());
        captureAnimTime = 0;
    }

    public IEnumerator CaptureAnim()
    {
        yield return new WaitForSeconds(3f);

        int ind = 0;
        for (int i = 0; i < tilemap.Count; ++i)
        {
            for (int j = 0; j < tilemap[i].Count; ++j)
            {
                int id = tilemap[i][j];
                if (id == captureID)
                {
                    tilemap[i][j] = canadaID;
                }

                ++ind;
            }
        }

        gameMan.editeable = true;
        captureID = -1;
    }
    
    public void Allegiance(Leader leader)
    {
        allyID = leaders.IndexOf(leader);
        StartCoroutine(AllyAnim());
        allyAnimTime = 0;
    }

    public IEnumerator AllyAnim()
    {
        yield return new WaitForSeconds(3f);
        gameMan.editeable = true;
        allyID = -1;
    }
}