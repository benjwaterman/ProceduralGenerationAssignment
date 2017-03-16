using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType { Tree, House, Detail };

public class ProceduralTerrain : MonoBehaviour {

    public static ProceduralTerrain Current;

    public const int TerrainResolution = 513;
    public const int NumberOfChunks = 1;

    [Header("Terrain Options")]
    public MapData TerrainMapData;

    [Header("Tree Options")]
    public TreeData TerrainTreeData;

    [Header("House Options")]
    public HouseData TerrainHouseData;

    [Header("Detail Options")]
    public DetailData TerrainDetailData;

    [Header("Grass Options")]
    public GrassData TerrainGrassData;

    [Header("Debug Options")]
    public bool RandomSeeds = false;
    public Material HeightMat;
    public Material PlacableMat;
    public Material TreeMat;
    public Material HouseMat;

    //[System.NonSerialized]
    //public float[,] terrainHeightMap = new float[TerrainResolution, TerrainResolution];
    //[System.NonSerialized]
    //public float[,] terrainTreeMap = new float[TerrainResolution, TerrainResolution];
    //[System.NonSerialized]
    //public float[,] terrainHouseMap = new float[TerrainResolution, TerrainResolution];

    public bool[,] defaultPlacableMap;

    Terrain[,] terrains = new Terrain[NumberOfChunks, NumberOfChunks];

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
        //terrainHeightMap = CalculateHeightMap();
        //Generate terrain
        //GenerateTerrain();

        //Generate features
        //GenerateHouses();
        //GenerateTrees();
        //GenerateGrass(TerrainGrassData.GrassTexture);

        //For display purposes
        //defaultPlacableMap = CalculateFlatTerrain();

        //AssignTexture(terrainHeightMap, HeightMat);
        //AssignTexture(defaultPlacableMap, PlacableMat);
        //AssignTexture(terrainTreeMap, TreeMat);
        //AssignTexture(terrainHouseMap, HouseMat);

        //Create chunks
        for (int i = 0; i < NumberOfChunks; i++) {
            for (int j = 0; j < NumberOfChunks; j++) {
                ChunkData chunk = new ChunkData(new Vector2(TerrainMapData.TerrainSize * i, TerrainMapData.TerrainSize * j));
                //Assign terrain
                terrains[i, j] = chunk.terrain;
            }
        }

        //Assign terrain neighbours
        for (int i = 0; i < terrains.GetLength(0); i++) {
            for (int j = 0; j < terrains.GetLength(1); j++) {
                Terrain left = (i - 1 >= 0) ? terrains[i - 1, j] : null;
                Terrain right = (i + 1 < terrains.GetLength(0)) ? terrains[i + 1, j] : null;
                Terrain top = (j + 1 < terrains.GetLength(1)) ? terrains[i, j + 1] : null;
                Terrain bottom = (j - 1 > 0) ? terrains[i, j - 1]: null;

                terrains[i, j].SetNeighbors(left, top, right, bottom);
            }
        }
    }

    public static float[,] CalculateHeightMap(ChunkData chunkData) {

        Vector2 noiseOffset = new Vector2(chunkData.position.x / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution, chunkData.position.y / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution);

        //Store temp heightmap data
        float[,] tempHeightMap = NoiseGenerator.GenerateNoiseMap(ProceduralTerrain.Current.TerrainMapData.TerrainNoiseData, ProceduralTerrain.TerrainResolution + 2, noiseOffset);

        //To avoid seams between terrain, edge vertices have to be = to edge + 1
        float[,] heightMap = new float[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];

        //Start at 1 and end at length-1 so not to go out of index
        for (int x = 1; x < tempHeightMap.GetLength(0) - 1; x++) {
            for (int y = 1; y < tempHeightMap.GetLength(1) - 1; y++) {

                bool hasAssignedValue = false;
                float tempHeight = 0;

                //When i == 1 or j == 1 we are at the edge
                if (x == 1) {
                    //Assign edge value to average between two points
                    tempHeight += tempHeightMap[x - 1, y];// + tempHeightMap[x, y]) / 2;
                    hasAssignedValue = true;
                }

                if (y == 1) {
                    tempHeight += tempHeightMap[x, y - 1];// + tempHeightMap[x, y]) / 2;

                    //If already assigned, means its a corner
                    if (hasAssignedValue) {
                        tempHeight /= 2;
                    }

                    hasAssignedValue = true;
                }

                if (x == tempHeightMap.GetLength(0) - 1) {
                    tempHeight += tempHeightMap[x + 1, y];/// + tempHeightMap[x, y]) / 2;

                    if (hasAssignedValue) {
                        tempHeight /= 2;
                    }

                    hasAssignedValue = true;
                }

                if (y == tempHeightMap.GetLength(1) - 1) {
                    tempHeight += tempHeightMap[x, y + 1];// + tempHeightMap[x, y]) / 2;

                    if (hasAssignedValue) {
                        tempHeight /= 2;
                    }

                    hasAssignedValue = true;
                }

                if (!hasAssignedValue) {
                    tempHeight = tempHeightMap[x, y];
                }

                //If within range
                if (x >= 1 && y >= 1 && x <= tempHeightMap.GetLength(0) - 1 && y <= tempHeightMap.GetLength(1) - 1) {
                    heightMap[x - 1, y - 1] = tempHeight;
                }
            }
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

        if (ProceduralTerrain.NumberOfChunks != 1) {
            maxLocalHeight = 1.3f;
            minLocalHeight = 0.4f;
        }

        //Normalise heightmap and apply height curve
        for (int x = 0; x < TerrainResolution; x++) {
            for (int y = 0; y < TerrainResolution; y++) {
                heightMap[x, y] = Mathf.InverseLerp(minLocalHeight, maxLocalHeight, heightMap[x, y]);
                heightMap[x, y] = ProceduralTerrain.Current.TerrainMapData.TerrainHeightCurve.Evaluate(heightMap[x, y]) * ProceduralTerrain.Current.TerrainMapData.TerrainHeightMultiplier;
            }
        }

        return heightMap;
    }

    public static void GenerateTerrain(ChunkData chunkData) {

        TerrainData terrainData = new TerrainData();
        Terrain terrain = new Terrain();
        float[,] terrainHeightMap = chunkData.terrainHeightMap;
        MapData terrainMapData = ProceduralTerrain.Current.TerrainMapData;
        TextureData[] terrainTextures = ProceduralTerrain.Current.TerrainMapData.TerrainTextures;

        terrainData = new TerrainData();
        //Resolution
        terrainData.heightmapResolution = TerrainResolution;
        terrainData.alphamapResolution = TerrainResolution;
        //Apply height map
        terrainData.SetHeights(0, 0, terrainHeightMap);

        //Terrain size and max height
        terrainData.size = new Vector3(terrainMapData.TerrainSize, terrainMapData.TerrainHeight, terrainMapData.TerrainSize);

        //Loop through textures 
        int textureIndex = 0;
        SplatPrototype[] splatPrototype = new SplatPrototype[terrainTextures.Length];
        foreach (TextureData textureData in terrainTextures) {
            //Create texture from TerrainTexture and tint colour
            Texture2D tintedTexture = new Texture2D(textureData.Texture.width, textureData.Texture.height);
            Color[] pixels = textureData.Texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i].r = pixels[i].r - (1.0f - textureData.TextureTint.r);
                pixels[i].g = pixels[i].g - (1.0f - textureData.TextureTint.g);
                pixels[i].b = pixels[i].b - (1.0f - textureData.TextureTint.b);
            }
            //Set and apply pixels
            tintedTexture.SetPixels(pixels);
            tintedTexture.Apply();

            //Create textures for terrain
            splatPrototype[textureIndex] = new SplatPrototype();
            splatPrototype[textureIndex].texture = tintedTexture;
            //If there is a normal map
            if (textureData.TextureNormal != null) {
                splatPrototype[textureIndex].normalMap = textureData.TextureNormal;
            }
            splatPrototype[textureIndex].tileOffset = new Vector2(0, 0);
            splatPrototype[textureIndex].tileSize = textureData.TextureTileSize;

            //Increment index 
            textureIndex++;
        }

        //Apply textures to terrain
        terrainData.splatPrototypes = splatPrototype;

        //Create object
        GameObject terrainGameObject = Terrain.CreateTerrainGameObject(terrainData);

        //Set position 
        terrainGameObject.transform.position = new Vector3(chunkData.position.x, 0, -chunkData.position.y);

        terrain = terrainGameObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 8;
        terrain.materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;

        //Apply changes
        terrain.Flush();

        //Update chunk
        chunkData.AssignTerrainData(terrainData);
        chunkData.AssignTerrain(terrain);
        chunkData.AssignTerrainGameObject(terrainGameObject);
    }

    //Calculate where in the world objects can be placed
    public static bool[,] CalculateFlatTerrain(float[,] terrainHeightMap, int range = 1, float sensitivity = 0.0052f) {

        bool[,] placableArea = new bool[TerrainResolution, TerrainResolution];

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

        return placableArea;
    }

    public static void GenerateHouses(ChunkData chunkData) {

        ObjectGenerator.GenerateObjects(ObjectType.House, ProceduralTerrain.Current.TerrainHouseData, chunkData);
    }

    public static void GenerateTrees(ChunkData chunkData) {

        ObjectGenerator.GenerateObjects(ObjectType.Tree, ProceduralTerrain.Current.TerrainTreeData, chunkData);
    }

    public static void GenerateDetails(ChunkData chunkData) {
        ObjectGenerator.GenerateObjects(ObjectType.Detail, ProceduralTerrain.Current.TerrainDetailData, chunkData);
    }

    //Generate grass using unitys terrain detail
    public static void GenerateGrass(ChunkData chunkData) {

        Texture2D grassTexture = ProceduralTerrain.Current.TerrainGrassData.GrassTexture;
        GrassData TerrainGrassData = ProceduralTerrain.Current.TerrainGrassData;
        TerrainData terrainData = chunkData.terrainData;
        Terrain terrain = chunkData.terrain;

        //Set detail texture
        DetailPrototype[] detailPrototype = new DetailPrototype[1];
        detailPrototype[0] = new DetailPrototype();
        detailPrototype[0].prototypeTexture = grassTexture;
        //Set grass colour
        detailPrototype[0].healthyColor = TerrainGrassData.GrassColour;
        detailPrototype[0].dryColor = TerrainGrassData.GrassColourDry;
        detailPrototype[0].renderMode = DetailRenderMode.GrassBillboard;
        detailPrototype[0].maxHeight = TerrainGrassData.GrassMaxHeight;
        detailPrototype[0].minHeight = TerrainGrassData.GrassMinHeight;
        terrainData.detailPrototypes = detailPrototype;

        const int resolutionMultipier = 2;
        const int grassResolution = TerrainResolution * resolutionMultipier;
        const int patchDetail = 32;

        //Set detail and resolution
        terrain.terrainData.SetDetailResolution(grassResolution, patchDetail);
        //Set distance details can be seen for
        terrain.detailObjectDistance = 250;

        int[,] grassMap = new int[grassResolution, grassResolution];
        float[,] fGrassMap = NoiseGenerator.GenerateNoiseMap(TerrainGrassData.GrassNoiseData, grassResolution, chunkData.position);

        int incremement = (int)(1 / TerrainGrassData.GrassSpawnDensity);

        for (int i = 0; i < grassResolution; i += incremement) {
            for (int j = 0; j < grassResolution; j += incremement) {

                float terrainHeight = terrain.terrainData.GetHeight((int)(j / resolutionMultipier), (int)(i / resolutionMultipier)) / (float)ProceduralTerrain.Current.TerrainMapData.TerrainHeight;

                //Get alpha map at this location (inverted)
                float[,,] localAlphaMap = terrainData.GetAlphamaps(j / resolutionMultipier, i / resolutionMultipier, 1, 1);
                float grassStrength = localAlphaMap[0, 0, 2]; //2 being the grass texture

                if (grassStrength >= TerrainGrassData.GrassSpawnThreshold) {
                    grassMap[i, j] = 6;
                }
                else {
                    grassMap[i, j] = 0;
                }

                //Compare against generated noise
                //if (fGrassMap[i, j] >= TerrainGrassData.GrassSpawnThreshold) {
                //    grassMap[i, j] = 6;
                //}
                //else {
                //    grassMap[i, j] = 0;
                //}

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

