using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GrassData : ScriptableObject {

    public Texture2D GrassTexture;
    public Color GrassColour;
    public Color GrassColourDry;
    public float GrassMaxHeight = 2f;
    public float GrassMinHeight = 1f;
    [Range(0, 1)]
    public float MaxGrassSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinGrassSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float GrassSpawnDensity = 1f;
    [Range(0, 1)]
    public float GrassSpawnThreshold = 0.5f;

    public NoiseData GrassNoiseData { get; private set; }
    public NoiseData NoiseData;
    
    void OnEnable() {
        GrassNoiseData = NoiseData.CreateClone(NoiseData);
    }
}
