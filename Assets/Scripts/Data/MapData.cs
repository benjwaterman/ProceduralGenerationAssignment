using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MapData : ScriptableObject {

    public int TerrainSize = 1000;
    public int TerrainHeight = 20;
    public NoiseData TerrainNoiseData;
    public float TerrainHeightMultiplier = 1;
    public AnimationCurve TerrainHeightCurve;
    public TextureData[] TerrainTextures;
    [Header("Other Properties")]
    public float TextureBlendAmount = 0.1f;
    public float Texture1ConstantWeight = 0.5f;
    public float TextureSteepnessScaleFactor = 5;

    public int TerrainSeed() { return TerrainNoiseData.Seed; }
    public int TerrainOctaves() { return TerrainNoiseData.Octaves; }
    public float TerrainPersistance() { return TerrainNoiseData.Persistance; }
    public float TerrainLacunarity() { return TerrainNoiseData.Lacunarity; }
    public float TerrainScale() { return TerrainNoiseData.Scale; }
}

[System.Serializable]
public struct TextureData {
    public Texture2D Texture;
    public Texture2D TextureNormal;
    public Vector2 TextureTileSize;
    public Color TextureTint;
    public float TextureStartHeight;
}