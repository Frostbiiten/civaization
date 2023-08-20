using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChunk
{
    // 1023 limit
    public Matrix4x4[] baseMatrices; // set base matrices
    public MaterialPropertyBlock matProps;
    public Color[] colors;
    public int tileCount;

    public MapChunk(int size)
    {
        tileCount = size;
        colors = new Color[size];
        baseMatrices = new Matrix4x4[size];
        matProps = new MaterialPropertyBlock();
    }

    public void PushColors()
    {
        //matProps.SetVectorArray("_Color", new );
    }
}
