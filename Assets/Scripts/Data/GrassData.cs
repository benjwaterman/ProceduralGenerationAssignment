using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GrassData : ScriptableObject {

    public Texture2D GrassTexture;
    public Color GrassColour;
    public Color GrassColourDry;
    public bool AllowColourOverride = false;
    public float GrassMaxHeight = 2f;
    public float GrassMinHeight = 1f;
    public float GrassMaxWidth = 2f;
    public float GrassMinWidth = 1f;
    [Range(0, 1)]
    public float MaxGrassSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinGrassSpawnHeight = 0.1f;
    [Range(0, 6)]
    public int GrassSpawnDensity = 1;
    [Range(0, 1)]
    public float GrassSpawnChance = 1f;
    [Range(0, 1)]
    public float GrassSpawnNoiseThreshold = 0.5f;
    [Range(0, 1)]
    public float GrassSpawnGroundTextureThreshold = 0.5f;

    public NoiseData GrassNoiseData { get; private set; }
    public NoiseData NoiseData;
    
    void OnEnable() {
        GrassNoiseData = NoiseData.CreateClone(NoiseData);
    }
}
