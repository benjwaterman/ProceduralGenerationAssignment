using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectData {

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

    public NoiseData ObjectNoiseData;
    public PrefabData[] PrefabArray;
}
