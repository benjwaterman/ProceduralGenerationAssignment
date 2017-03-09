using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour {
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
    public GameObject TreePrefab;
    [Range(0, 1)]
    public float MaxTreeSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinTreeSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float TreeSpawnDensity = 0.2f;
    [Range(0, 1)]
    public float TreeSpawnThreshold = 0.2f;

    [Header("House Options")]
    public int HouseSeed;
    public GameObject HousePrefab;
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

    [Header("Debug Options")]
    public Material HeightMat;
    public Material PlacableMat;
    public Material TreeMat;
    public Material HouseMat;

    Terrain terrain;
    GameObject terrainGameObject;
    TerrainData terrainData;

    float[,] terrainHeightMap = new float[TerrainResolution, TerrainResolution];
    float[,] terrainTreeMap = new float[TerrainResolution, TerrainResolution];
    float[,] terrainHouseMap = new float[TerrainResolution, TerrainResolution];

    bool[,] placableArea = new bool[TerrainResolution, TerrainResolution];

    void Start() {

        terrainHeightMap = CalculateHeightMap();

        GenerateTerrain();
        CalculateFlatTerrain();
        GenerateHouses();
        GenerateTrees();

        AssignTexture(terrainHeightMap, HeightMat);
        AssignTexture(placableArea, PlacableMat);
        AssignTexture(terrainTreeMap, TreeMat);
        AssignTexture(terrainHouseMap, HouseMat);
    }

    void Update() {

    }

    float[,] GenerateNoiseMap(int seed) {

        //Store temp heightmap data
        float[,] noiseMap = new float[TerrainResolution, TerrainResolution];

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
        for (int x = 0; x < TerrainResolution; x++) {
            for (int y = 0; y < TerrainResolution; y++) {

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

        GameObject houseParentObject = new GameObject("Houses");

        float[,] noiseMap = GenerateNoiseMap(HouseSeed);

        //Largest as to not go out of array range
        int range = Mathf.Max(HouseRequiredSpaceX, HouseRequiredSpaceZ); ;

        //Spawn based on density
        for (int x = 0 + range; x < TerrainResolution - range; x++) {
            for (int y = 0 + range; y < TerrainResolution - range; y++) {
                float terrainHeight = terrainHeightMap[x, y];

                bool canPlace = true;//placableArea[x, y] && placableArea[x + 1, y] && placableArea[x, y + 1] && placableArea[x + 1, y + 1];

                //Check around this point to check there is room
                for (int i = 0; i <= HouseRequiredSpaceX; i++) {
                    for (int j = 0; j <= HouseRequiredSpaceZ; j++) {
                        //If canPlace = false, no point checking other areas
                        if(canPlace) {

                            if(placableArea[x + i, y + j]) {
                                canPlace = true;
                            }
                            else {
                                canPlace = false;
                            }
                        }
                    }
                }

                //Randomly decide whether tree can be placed
                if (canPlace) {
                    float number = Random.Range(0f, 1f);
                    if (number > HouseSpawnDensity) {
                        canPlace = false;
                    }
                }

                //Check against min and max height
                if (canPlace) {
                    //If its not within range, it cannot be placed here
                    if (!(terrainHeight >= MinHouseSpawnHeight && terrainHeight <= MaxHouseSpawnHeight)) {
                        canPlace = false;
                    }
                }

                if (canPlace) {
                    //Update house map
                    terrainHouseMap[x, y] = 1f;

                    //Subtract noise
                    terrainHouseMap[x, y] = Mathf.Clamp01(terrainHouseMap[x, y] - noiseMap[x, y]);

                    //If greater than density
                    if (terrainHouseMap[x, y] >= HouseSpawnThreshold) {

                        //Inverse needed 
                        float xToPlace = ((float)y / (float)TerrainResolution) * (float)TerrainSize;
                        float yToPlace = terrainHeightMap[x, y] * (float)TerrainHeight;
                        float zToPlace = ((float)x / (float)TerrainResolution) * (float)TerrainSize;

                        //This area is no longer placable
                        for (int i = 0; i <= HouseRequiredSpaceX; i++) {
                            for (int j = 0; j <= HouseRequiredSpaceZ; j++) {
                                placableArea[x + i, y + j] = false;
                            }
                        }

                        Instantiate(HousePrefab, new Vector3(xToPlace, yToPlace, zToPlace), Quaternion.identity, houseParentObject.transform);
                    }
                    //Else there is no house here
                    else {
                        terrainHouseMap[x, y] = 0;
                    }
                }
            }
        }
    }

    void GenerateTrees() {

        GameObject treeParentObject = new GameObject("Trees");

        float[,] noiseMap = GenerateNoiseMap(TreeSeed); 

        int range = 2;

        //Spawn based on density
        for (int x = 0 + range; x < TerrainResolution - range; x++) {
            for (int y = 0 + range; y < TerrainResolution - range; y++) {
                float terrainHeight = terrainHeightMap[x, y];

                bool canPlace = placableArea[x, y];
                int numberOfTreesInRange = 0;

                //Average the number of trees currently in range
                for (int i = -range; i <= range; i++) {
                    for (int j = -range; j <= range; j++) {
                        //If tree in spot
                        if (terrainTreeMap[x + i, y + j] > 0) {
                            numberOfTreesInRange++;
                        }
                    }
                }

                //Check if avg amount of trees in range is greater than tree density
                float avgTreesInRange = numberOfTreesInRange / (range * range);
                if (avgTreesInRange > TreeSpawnDensity) {
                    canPlace = false;
                }

                //Randomly decide whether tree can be placed
                if(canPlace) {
                    float number = Random.Range(0f, 1f);
                    if(number > TreeSpawnDensity) {
                        canPlace = false;
                    }
                }

                //Check against min and max height
                if (canPlace) {
                    //If its not within range, it cannot be placed here
                    if (!(terrainHeight >= MinTreeSpawnHeight && terrainHeight <= MaxTreeSpawnHeight)) {
                        canPlace = false;
                    }
                }

                if (canPlace) {
                    //Update tree map to say there is a tree here
                    terrainTreeMap[x, y] = 1f;

                    //Subtract noise
                    terrainTreeMap[x, y] = Mathf.Clamp01(terrainTreeMap[x, y] - noiseMap[x, y]);

                    //If greater than density
                    if (terrainTreeMap[x, y] >= TreeSpawnThreshold) {

                        //Inverse needed 
                        float xToPlace = ((float)y / (float)TerrainResolution) * (float)TerrainSize;
                        float yToPlace = terrainHeightMap[x, y] * (float)TerrainHeight;
                        float zToPlace = ((float)x / (float)TerrainResolution) * (float)TerrainSize;

                        //This area is no longer placable
                        placableArea[x, y] = false;

                        Instantiate(TreePrefab, new Vector3(xToPlace, yToPlace, zToPlace), Quaternion.identity, treeParentObject.transform);
                    }
                    //Else there is no tree here
                    else {
                        terrainTreeMap[x, y] = 0f;
                    }
                }
            }
        }

        /*
        int range = 5;

        for (int x = range; x < TerrainResolution - range; x++) {
            for (int y = range; y < TerrainResolution - range; y++) {
                float terrainHeight = terrainHeightMap[x, y];

                bool canPlace = placableArea[x, y];
                int numberOfTreesInRange = 0;

                //Average the number of trees currently in range
                for (int i = -range; i <= range; i++) {
                    for (int j = -range; j <= range; j++) {
                        //If tree in spot
                        if (terrainTreeMap[x + i, y + j] > 0) {
                            numberOfTreesInRange++;
                        }
                    }
                }

                //Check if avg amount of trees in range is greater than tree density
                float avgTreesInRange = numberOfTreesInRange / (range * range);
                if(avgTreesInRange > TreeSpawnDensity) {
                    canPlace = false;
                }

                //Check against min and max height
                if (canPlace) {
                    //If its not within range, it cannot be placed here
                    if(!(terrainHeight >= MinTreeSpawnHeight && terrainHeight <= MaxTreeSpawnHeight)) {
                        canPlace = false;
                    }
                }

                if (canPlace) {
                    //Update tree map saying there is a tree here
                    terrainTreeMap[x, y] = 1f;

                    //Inverse needed 
                    float xToPlace = ((float)y / (float)TerrainResolution) * (float)TerrainSize;
                    float yToPlace = terrainHeightMap[x, y] * (float)TerrainHeight;
                    float zToPlace = ((float)x / (float)TerrainResolution) * (float)TerrainSize;

                    Instantiate(TreePrefab, new Vector3(xToPlace, yToPlace, zToPlace), Quaternion.identity);
                }
            }
        } 
        */
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
