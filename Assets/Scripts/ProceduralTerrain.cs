using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType { Tree, House };

public class ProceduralTerrain : MonoBehaviour {

    public static ProceduralTerrain Current;

    public const int TerrainResolution = 513;

    [Header("Terrain Options")]
    public MapData TerrainMapData;

    [Header("Tree Options")]
    public TreeData TerrainTreeData;

    [Header("House Options")]
    public HouseData TerrainHouseData;

    [Header("Grass Options")]
    public GrassData TerrainGrassData;

    [Header("Debug Options")]
    public bool RandomSeeds = false;
    public Material HeightMat;
    public Material PlacableMat;
    public Material TreeMat;
    public Material HouseMat;

    Terrain terrain;
    GameObject terrainGameObject;
    TerrainData terrainData;

    [System.NonSerialized]
    public float[,] terrainHeightMap = new float[TerrainResolution, TerrainResolution];
    [System.NonSerialized]
    public float[,] terrainTreeMap = new float[TerrainResolution, TerrainResolution];
    [System.NonSerialized]
    public float[,] terrainHouseMap = new float[TerrainResolution, TerrainResolution];

    public bool[,] defaultPlacableMap;

    void Awake() {
        Current = this;
    }

    void Start() {

        if (RandomSeeds) {
            System.Random rand = new System.Random();
            TerrainGrassData.GrassNoiseData.Seed = rand.Next();
            TerrainHouseData.ObjectNoiseData.Seed = rand.Next();
            TerrainMapData.TerrainNoiseData.Seed = rand.Next();
            TerrainTreeData.ObjectNoiseData.Seed = rand.Next();
        }

        //Create heightmap
        terrainHeightMap = CalculateHeightMap();
        //Generate terrain
        GenerateTerrain();

        //Generate features
        GenerateHouses();
        GenerateTrees();
        GenerateGrass(TerrainGrassData.GrassTexture);

        //For display purposes
        defaultPlacableMap = CalculateFlatTerrain();

        AssignTexture(terrainHeightMap, HeightMat);
        AssignTexture(defaultPlacableMap, PlacableMat);
        AssignTexture(terrainTreeMap, TreeMat);
        AssignTexture(terrainHouseMap, HouseMat);
    }

    float[,] CalculateHeightMap() {

        //Store temp heightmap data
        float[,] heightMap = NoiseGenerator.GenerateNoiseMap(TerrainMapData.TerrainNoiseData);

        //Store max and min values of height map to normalise
        float maxLocalHeight = float.MinValue;
        float minLocalHeight = float.MaxValue;

        //Generate the heightmap
        for (int x = 0; x < TerrainResolution; x++) {
            for (int y = 0; y < TerrainResolution; y++) {

                float heightValue = heightMap[x, y];

                if (heightValue > maxLocalHeight) {
                    maxLocalHeight = heightValue;
                }

                else if (heightValue < minLocalHeight) {
                    minLocalHeight = heightValue;
                }

                //Assign value
                heightMap[x, y] = heightValue;
            }
        }

        //Normalise heightmap and apply height curve
        for (int x = 0; x < TerrainResolution; x++) {
            for (int y = 0; y < TerrainResolution; y++) {
                heightMap[x, y] = Mathf.InverseLerp(minLocalHeight, maxLocalHeight, heightMap[x, y]);
                heightMap[x, y] = TerrainMapData.TerrainHeightCurve.Evaluate(heightMap[x, y]) * TerrainMapData.TerrainHeightMultiplier;
            }
        }

        return heightMap;
    }

    void GenerateTerrain() {

        terrainData = new TerrainData();
        //Resolution
        terrainData.heightmapResolution = TerrainResolution;
        terrainData.alphamapResolution = TerrainResolution;
        //Apply height map
        terrainData.SetHeights(0, 0, terrainHeightMap);

        //Terrain size and max height
        terrainData.size = new Vector3(TerrainMapData.TerrainSize, TerrainMapData.TerrainHeight, TerrainMapData.TerrainSize);

        //Create texture from TerrainTexture and tint colour
        Texture2D tintedTexture = new Texture2D(TerrainMapData.TerrainTexture.width, TerrainMapData.TerrainTexture.height);
        Color[] pixels = TerrainMapData.TerrainTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i].r = pixels[i].r - (1.0f - TerrainMapData.TerrainTint.r);
            pixels[i].g = pixels[i].g - (1.0f - TerrainMapData.TerrainTint.g);
            pixels[i].b = pixels[i].b - (1.0f - TerrainMapData.TerrainTint.b);
        }
        tintedTexture.SetPixels(pixels);
        tintedTexture.Apply();

        //Set textures
        SplatPrototype[] splatPrototype = new SplatPrototype[1];
        splatPrototype[0] = new SplatPrototype();
        splatPrototype[0].texture = tintedTexture;
        if (TerrainMapData.TerrainTextureNormal != null) {
            splatPrototype[0].normalMap = TerrainMapData.TerrainTextureNormal;
        }
        splatPrototype[0].tileOffset = new Vector2(0, 0);
        splatPrototype[0].tileSize = TerrainMapData.TextureTileSize;

        //Apply textures
        terrainData.splatPrototypes = splatPrototype;

        //Create object
        terrainGameObject = Terrain.CreateTerrainGameObject(terrainData);

        //Set position here
        ///////////////////

        terrain = terrainGameObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 8;
        terrain.materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;

        //Apply changes
        terrain.Flush();
    }

    //Calculate where in the world objects can be placed
    public static bool[,] CalculateFlatTerrain(int range = 1, float sensitivity = 0.0052f) {

        bool[,] placableArea = new bool[TerrainResolution, TerrainResolution];

        for (int x = range; x < TerrainResolution - range; x++) {
            for (int y = range; y < TerrainResolution - range; y++) {
                float terrainHeight = ProceduralTerrain.Current.terrainHeightMap[x, y];

                int ctr = 0;
                bool[] placableLocations = new bool[(2 * range + 1) * (2 * range + 1)];
                for (int i = -range; i <= range; i++) {
                    for (int j = -range; j <= range; j++) {
                        if (Mathf.Abs(terrainHeight - ProceduralTerrain.Current.terrainHeightMap[x + i, y + j]) < sensitivity) {
                            placableLocations[ctr++] = true;
                        }
                        else {
                            placableLocations[ctr++] = false;
                        }
                    }
                }

                //If all values in placable locations are true, then it can be placed
                bool canPlace = true;
                for (int i = 0; i < placableLocations.Length; i++) {
                    if (canPlace) {
                        canPlace = placableLocations[i];
                    }
                }

                if (canPlace) {
                    placableArea[x, y] = true;
                }
            }
        }

        return placableArea;
    }

    void GenerateHouses() {

        ObjectGenerator.GenerateObjects(ObjectType.House, out terrainHouseMap, TerrainTreeData);
    }

    void GenerateTrees() {

        ObjectGenerator.GenerateObjects(ObjectType.Tree, out terrainTreeMap, TerrainHouseData);
    }

    //Generate grass using unitys terrain detail
    void GenerateGrass(Texture2D grassTexture) {

        //Set detail texture
        DetailPrototype[] detailPrototype = new DetailPrototype[1];
        detailPrototype[0] = new DetailPrototype();
        detailPrototype[0].prototypeTexture = grassTexture;
        //Set grass colour
        detailPrototype[0].healthyColor = TerrainGrassData.GrassColour;
        detailPrototype[0].dryColor = TerrainGrassData.GrassColourDry;
        detailPrototype[0].renderMode = DetailRenderMode.GrassBillboard;
        terrainData.detailPrototypes = detailPrototype;

        const int grassResolution = TerrainResolution * 2;
        const int patchDetail = 16;

        //Set detail and resolution
        terrain.terrainData.SetDetailResolution(grassResolution, patchDetail);
        //Set distance details can be seen for
        terrain.detailObjectDistance = 250;

        int[,] grassMap = new int[grassResolution, grassResolution];
        //NOT GENERATING PROPERLY
        float[,] fGrassMap = NoiseGenerator.GenerateNoiseMap(TerrainGrassData.GrassNoiseData, grassResolution);

        int incremement = (int)(1 / TerrainGrassData.GrassSpawnDensity);

        for (int i = 0; i < grassResolution; i += incremement) {
            for (int j = 0; j < grassResolution; j += incremement) {

                float terrainHeight = terrain.terrainData.GetHeight((int)(j/2), (int)(i/2)) / (float)TerrainMapData.TerrainHeight;

                //Compare against generated noise
                if (fGrassMap[i, j] >= TerrainGrassData.GrassSpawnThreshold) {
                    grassMap[i, j] = 6;
                }
                else {
                    grassMap[i, j] = 0;
                }

                //If grass is not within min and max height
                if (!(terrainHeight >= TerrainGrassData.MinGrassSpawnHeight && terrainHeight <= TerrainGrassData.MaxGrassSpawnHeight)) {
                    grassMap[i, j] = 0;
                }
            }
        }

        terrain.terrainData.SetDetailLayer(0, 0, 0, grassMap);
        terrain.Flush();
    }

    //Display map on a texture for debugging
    void AssignTexture(float[,] noiseValues, Material material) {
        Texture2D texture = new Texture2D(TerrainResolution, TerrainResolution);
        material.mainTexture = texture;

        Color[] pixels = new Color[TerrainResolution * TerrainResolution];

        for (int i = 0; i < noiseValues.GetLength(0); i++) {
            for (int j = 0; j < noiseValues.GetLength(1); j++) {
                pixels[i * noiseValues.GetLength(0) + j] = Color.Lerp(Color.black, Color.white, noiseValues[i, j]);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();
    }

    //Converts bool array into float array for assign texture function
    void AssignTexture(bool[,] bNoiseValues, Material material) {
        float[,] fNoiseValues = new float[bNoiseValues.GetLength(0), bNoiseValues.GetLength(1)];

        for (int i = 0; i < fNoiseValues.GetLength(0); i++) {
            for (int j = 0; j < fNoiseValues.GetLength(1); j++) {
                //If true equal 1, if not equal 0
                fNoiseValues[i, j] = bNoiseValues[i, j] ? 1f : 0f;
            }
        }

        AssignTexture(fNoiseValues, material);
    }
}

[System.Serializable]
public class PrefabData {
    public GameObject ObjectPrefab;
    public int RequiredSpaceX;
    public int RequiredSpaceZ;
}