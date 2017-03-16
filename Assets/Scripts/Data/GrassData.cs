using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GrassData  {

    public Texture2D GrassTexture;
    public Color GrassColour;
    public Color GrassColourDry;
    public float GrassMaxHeight = 2f;
    public float GrassMinHeight = 1f;
    [Range(1, 3)]
    public int GrassFlatSurfaceSearchRange = 1;
    [Range(.00001f, 0.05f)]
    public float GrassFlatSurfaceSensitivity = 0.0052f;
    [Range(0, 1)]
    public float MaxGrassSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinGrassSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float GrassSpawnDensity = 0.1f;
    [Range(0, 1)]
    public float GrassSpawnThreshold = 0.5f;

    public NoiseData GrassNoiseData;
}
