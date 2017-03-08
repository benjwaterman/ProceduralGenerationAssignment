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
    public int X;
    public int Y;
    public Texture2D TerrainTexture;

    [Header("Tree Options")]
    public int TreeSeed;
    public GameObject TreePrefab;
    [Range(1, 3)]
    public int FlatSurfaceSearchRange = 1;
    [Range(0000.1f, 0.01f)]
    public float FlatSurfaceSensitivity = 0.0052f;
    [Range(0, 1)]
    public float MaxTreeSpawnHeight = 0.5f;
    [Range(0, 1)]
    public float MinTreeSpawnHeight = 0.1f;
    [Range(0, 1)]
    public float TreeSpawnDensity = 0.2f;
    [Range(0, 1)]
    public float TreeSpawnThreshold= 0.2f;

    [Header("Debug Options")]
    public Material HeightMat;
    public Material PlacableMat;

    Terrain terrain;
    GameObject terrainGameObject;
    TerrainData terrainData;

    float[,] terrainHeightMap = new float[TerrainResolution, TerrainResolution];
    float[,] terrainTreeMap = new float[TerrainResolution, TerrainResolution];

    bool[,] placableArea = new bool[TerrainResolution, TerrainResolution];

    void Start() {

        terrainHeightMap = CalculateHeightMap();

        GenerateTerrain();
        CalculateFlatTerrain();
        GenerateTrees();

        AssignTexture(terrainHeightMap, HeightMat);
        AssignTexture(terrainTreeMap, PlacableMat);
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
        splatPrototype[0].tileSize = new Vector2(5, 5);

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

    void GenerateTrees() {

        float[,] noiseMap = GenerateNoiseMap(TreeSeed); // new float[TerrainResolution, TerrainResolution];

        int range = 1;

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

                //Check against min and max height
                if (canPlace) {
                    //If its not within range, it cannot be placed here
                    if (!(terrainHeight >= MinTreeSpawnHeight && terrainHeight <= MaxTreeSpawnHeight)) {
                        canPlace = false;
                    }
                }

                if (canPlace) {
                    //Update tree map saying there is a tree here
                    terrainTreeMap[x, y] = 1f;

                    //Subtract noise
                    terrainTreeMap[x, y] = Mathf.Clamp01(terrainTreeMap[x, y] - noiseMap[x, y]);

                    //If greater than density
                    if (terrainTreeMap[x, y] <= TreeSpawnThreshold) {
                        //Inverse needed 
                        float xToPlace = ((float)y / (float)TerrainResolution) * (float)TerrainSize;
                        float yToPlace = terrainHeightMap[x, y] * (float)TerrainHeight;
                        float zToPlace = ((float)x / (float)TerrainResolution) * (float)TerrainSize;

                        Instantiate(TreePrefab, new Vector3(xToPlace, yToPlace, zToPlace), Quaternion.identity);
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
}
