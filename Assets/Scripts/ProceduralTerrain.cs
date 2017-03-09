using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType { Tree, House };

public class ProceduralTerrain : MonoBehaviour {

    public static ProceduralTerrain Current;

    [Header("Terrain Options")]
    public const int TerrainResolution = 513;
    public int TerrainSize = 1000;
    public int TerrainHeight = 20;
    public int TerrainSeed = 0;
    public float TerrainScale = 1;
    public int TerrainOctaves;
    public float TerrainPersistance;
    public float TerrainLacunarity;
    public float TerrainHeightMultiplier = 1;
    public AnimationCurve TerrainHeightCurve;
    [Range(1, 3)]
    public int FlatSurfaceSearchRange = 1;
    [Range(.00001f, 0.05f)]
    public float FlatSurfaceSensitivity = 0.0052f;
    public Texture2D TerrainTexture;
    public Vector2 TextureTileSize;
    public int X;
    public int Y;

    [Header("Tree Options")]
    public int TreeSeed;
    public GameObject[] TreePrefabs;
    [Range(0, 1)]
    public float MaxTreeSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinTreeSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float TreeSpawnDensity = 0.2f;
    [Range(0, 1)]
    public float TreeSpawnThreshold = 0.2f;
    public int TreeRequiredSpaceX = 1;
    public int TreeRequiredSpaceZ = 1;

    [Header("House Options")]
    public int HouseSeed;
    public GameObject[] HousePrefabs;
    [Range(0, 1)]
    public float MaxHouseSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinHouseSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float HouseSpawnDensity = 0.2f;
    [Range(0, 1)]
    public float HouseSpawnThreshold = 0.2f;
    public int HouseRequiredSpaceX = 2;
    public int HouseRequiredSpaceZ = 2;

    [Header("Grass Options")]
    public int GrassSeed;
    public Texture2D GrassTexture;
    public Color GrassColour;
    public Color GrassColourDry;
    [Range(0, 1)]
    public float GrassSpawnDensity = 0.1f;
    [Range(0, 1)]
    public float GrassSpawnThreshold = 0.5f;

    [Header("Debug Options")]
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
    [System.NonSerialized]
    public bool[,] placableArea = new bool[TerrainResolution, TerrainResolution];

    void Awake() {
        Current = this;
    }

    void Start() {

        terrainHeightMap = CalculateHeightMap();

        GenerateTerrain();
        CalculateFlatTerrain();
        GenerateHouses();
        GenerateTrees();
        GenerateGrass(GrassTexture);

        AssignTexture(terrainHeightMap, HeightMat);
        AssignTexture(placableArea, PlacableMat);
        AssignTexture(terrainTreeMap, TreeMat);
        AssignTexture(terrainHouseMap, HouseMat);
    }

    public float[,] GenerateNoiseMap(int seed, int res = TerrainResolution) {

        //Store temp heightmap data
        float[,] noiseMap = new float[res, res];

        //Get a random modifier based on seed
        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[TerrainOctaves];

        float maxheight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < TerrainOctaves; i++) {
            int randX = rand.Next(-100000, 100000) + X;
            int randY = rand.Next(-100000, 100000) - Y;
            octaveOffsets[i] = new Vector2(randX, randY);

            maxheight += amplitude;
            amplitude *= TerrainPersistance;
        }

        //Generate the noisemap
        for (int x = 0; x < res; x++) {
            for (int y = 0; y < res; y++) {

                amplitude = 1;
                frequency = 1;

                float heightValue = 0;

                for (int i = 0; i < TerrainOctaves; i++) {

                    float xCoord = (x - octaveOffsets[i].x) / TerrainScale * frequency;
                    float yCoord = (y - octaveOffsets[i].y) / TerrainScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    //Set height to perlin value
                    heightValue += perlinValue * amplitude;

                    amplitude *= TerrainPersistance;
                    frequency *= TerrainLacunarity;
                }

                //Assign value
                noiseMap[x, y] = heightValue;
            }
        }

        return noiseMap;
    }

    float[,] CalculateHeightMap() {

        //Store temp heightmap data
        float[,] heightMap = GenerateNoiseMap(TerrainSeed);

        //Ensure scale is not 0
        if (TerrainScale <= 0) {
            TerrainScale = 0.00001f;
        }

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
                heightMap[x, y] = TerrainHeightCurve.Evaluate(heightMap[x, y]) * TerrainHeightMultiplier;
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
        terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

        //Set textures
        SplatPrototype[] splatPrototype = new SplatPrototype[1];
        splatPrototype[0] = new SplatPrototype();
        splatPrototype[0].texture = TerrainTexture;
        splatPrototype[0].tileOffset = new Vector2(0, 0);
        splatPrototype[0].tileSize = TextureTileSize;

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
    void CalculateFlatTerrain() {

        int range = FlatSurfaceSearchRange;
        float sensitivity = FlatSurfaceSensitivity;

        for (int x = range; x < TerrainResolution - range; x++) {
            for (int y = range; y < TerrainResolution - range; y++) {
                float terrainHeight = terrainHeightMap[x, y];

                int ctr = 0;
                bool[] placableLocations = new bool[(2 * range + 1) * (2 * range + 1)];
                for (int i = -range; i <= range; i++) {
                    for (int j = -range; j <= range; j++) {
                        if (Mathf.Abs(terrainHeight - terrainHeightMap[x + i, y + j]) < sensitivity) {
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
    }

    void GenerateHouses() {

        ObjectGenerator.GenerateObjects(ObjectType.House, out terrainHouseMap, HousePrefabs, HouseSeed, new Vector2(HouseRequiredSpaceX, HouseRequiredSpaceZ), MinHouseSpawnHeight, MaxHouseSpawnHeight, HouseSpawnDensity, HouseSpawnThreshold);
    }

    void GenerateTrees() {

        ObjectGenerator.GenerateObjects(ObjectType.Tree, out terrainTreeMap, TreePrefabs, TreeSeed, new Vector2(TreeRequiredSpaceX, TreeRequiredSpaceZ), MinTreeSpawnHeight, MaxTreeSpawnHeight, TreeSpawnDensity, TreeSpawnThreshold);
    }

    //Generate grass using unitys terrain detail
    void GenerateGrass(Texture2D grassTexture) {

        //Set detail texture
        DetailPrototype[] detailPrototype = new DetailPrototype[1];
        detailPrototype[0] = new DetailPrototype();
        detailPrototype[0].prototypeTexture = grassTexture;
        //Set grass colour
        detailPrototype[0].healthyColor = GrassColour;
        detailPrototype[0].dryColor = GrassColourDry;
        detailPrototype[0].renderMode = DetailRenderMode.GrassBillboard;
        terrainData.detailPrototypes = detailPrototype;

        const int grassResolution = TerrainResolution * 2;
        const int patchDetail = 16;
    
        //Set detail and resolution
        terrain.terrainData.SetDetailResolution(grassResolution, patchDetail);
        //Set distance details can be seen for
        terrain.detailObjectDistance = 250;

        int[,] grassMap =  new int[grassResolution, grassResolution];
        float[,] fGrassMap = GenerateNoiseMap(GrassSeed, grassResolution);

        int incremement = (int)(1 / GrassSpawnDensity);

        for (int i = 0; i < grassResolution; i+= incremement) {
            for (int j = 0; j < grassResolution; j+= incremement) {
                //float height = terrain.terrainData.GetHeight(i, j);
                if(fGrassMap[i, j] > GrassSpawnThreshold) {
                    grassMap[i, j] = 6;
                }
                else {
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
