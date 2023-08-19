using System.Collections.Generic;
using RedOwl.Engine;
using UnityEngine;
using System;

public class Region
{
    private static readonly String POLY_ID = "Polygon";
    private List<List<Vector2>> points;
    private List<Mesh> meshes;

    private void CreateLandmass(Json j, Transform parent)
    {
        List<Vector2> meshOutline = new List<Vector2>();
        
        foreach (Json point in j)
        {
            meshOutline.Add(new Vector2(point[0], point[1]));
        }
        
        Triangulator meshTriangulator = new Triangulator(meshOutline);
        int[] ind = meshTriangulator.Triangulate();

        Vector3[] verts = new Vector3[meshOutline.Count];
        for (int i = 0; i < verts.Length; ++i)
        {
            verts[i] = new Vector3(meshOutline[i].x, meshOutline[i].y, 0);
        }
        
        points.Add(meshOutline);

        GameObject meshObj = new GameObject();
        meshObj.transform.parent = parent;
        Mesh m = new Mesh();
        m.vertices = verts;
        m.triangles = ind;
        m.RecalculateNormals();
        m.RecalculateBounds();
        meshObj.AddComponent<MeshRenderer>();
        MeshFilter f = meshObj.AddComponent<MeshFilter>();
        f.mesh = m;
    }
    
    public Region(Json region, GameObject root)
    {
        GameObject parent = new GameObject(region["properties"]["ADMIN"]);
        parent.transform.parent = root.transform;
        
        points = new List<List<Vector2>>(); 
        
        String geomType = region["geometry"]["type"];
        if (geomType == POLY_ID)
        {
            foreach (Json landmass in region["geometry"]["coordinates"])
            {
                CreateLandmass(landmass, parent.transform);
            }
        }
        else
        {
            foreach (Json landmass in region["geometry"]["coordinates"])
            {
                foreach (Json poly in landmass)
                {
                    CreateLandmass(poly, parent.transform);
                }
            }
        }
    }
}

public class MapManager : MonoBehaviour
{
    private Json mapGeometry;
    private List<Region> regions;
    
    void Awake()
    {
        regions = new List<Region>();
        var text = Resources.Load<TextAsset>("map");
        mapGeometry = Json.Deserialize(text.text);

        foreach (Json entry in mapGeometry["features"])
        {
            regions.Add(new Region(entry, gameObject));
        }
    }
}
