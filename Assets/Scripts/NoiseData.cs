using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseData {

    public int Seed = 0;
    public int Octaves;
    public float Persistance;
    public float Lacunarity;
    public float Scale = 1;
    public int X;
    public int Y;

    public NoiseData() {
        //Ensure scale is not 0
        if (Scale <= 0) {
            Scale = 0.00001f;
        }
    }
}
