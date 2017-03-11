using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData {

    public int TerrainSize = 1000;
    public int TerrainHeight = 20;
    public NoiseData TerrainNoiseData;
    public float TerrainHeightMultiplier = 1;
    public AnimationCurve TerrainHeightCurve;
    public Texture2D TerrainTexture;
    public Texture2D TerrainTextureNormal;
    public Color TerrainTint = Color.white;
    public Vector2 TextureTileSize;

    public int TerrainSeed() { return TerrainNoiseData.Seed; }
    public int TerrainOctaves() { return TerrainNoiseData.Octaves; }
    public float TerrainPersistance() { return TerrainNoiseData.Persistance; }
    public float TerrainLacunarity() { return TerrainNoiseData.Lacunarity; }
    public float TerrainScale() { return TerrainNoiseData.Scale; }


}