using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : ScriptableObject {

    public int Seed = 0;
    [Range(1, 10)]
    public int Octaves = 1;
    [Range(0.1f, 5f)]
    public float Persistance = 0.5f;
    [Range(1f, 5f)]
    public float Lacunarity = 1.8f;
    public float Scale = 1;

    public NoiseData() {
        //Ensure variables are not 0
        Scale = (Scale <= 0) ? Scale = 0.001f : Scale;
        Octaves = (Octaves <= 0) ? Octaves = 1 : Octaves;
        Persistance = (Persistance <= 0) ? Persistance = 0.001f : Persistance;
        Lacunarity = (Lacunarity <= 0) ? Lacunarity = 0.001f : Lacunarity;
    }
}
