using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectData : ScriptableObject {

    [Range(1, 3)]
    public int FlatSurfaceSearchRange = 1;
    [Range(.00001f, 0.05f)]
    public float FlatSurfaceSensitivity = 0.0052f;
    [Range(0, 1)]
    public float MaxSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float SpawnDensity = 0.2f;
    [Range(0, 1)]
    public float SpawnThreshold = 0.2f;

    public NoiseData ObjectNoiseData { get; private set; }
    public NoiseData NoiseData;
    public PrefabData[] PrefabArray;

    [Header("Colours")]
    public Material ObjectMaterial;
    public Color PrimaryColour = Color.green;
    public Color SecondaryColour = new Color(139f / 255f, 69f / 255f, 19f / 255f);
    public Color TertiaryColour = Color.white;
    public Color QuaternaryColour = Color.black;

    void OnEnable() {
        ObjectNoiseData = NoiseData.CreateClone(NoiseData);
    }
}

[System.Serializable]
public class PrefabData {
    public GameObject ObjectPrefab;
    public int RequiredSpaceX;
    public int RequiredSpaceZ;
    public int ActualSizeX;
    public int ActualSizeZ;
}