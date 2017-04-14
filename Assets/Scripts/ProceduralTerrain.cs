using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType { Tree, House, Detail };

public class ProceduralTerrain : MonoBehaviour {

    public static ProceduralTerrain Current;

    public const int TerrainResolution = 513;
    public const int NumberOfChunks = 1;

    [Header("Terrain")]
    public MapData TerrainMapData;

    [Header("Trees")]
    public TreeData[] TerrainTreeDataArray;

    [Header("Houses")]
    public HouseData TerrainHouseData;

    [Header("Details")]
    public DetailData TerrainDetailData;

    [Header("Grass")]
    public GrassData TerrainGrassData;

    [Header("Debug Options")]
    public bool RandomSeeds = false;
    public bool CombineMeshes = true;
    public bool UseMasterColour = false;
    public Color MasterColour = Color.green;

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

    //Queue for coroutines so they are executed in order
    public CoroutineQueue mainQueue;
    //Queue specifically for combining meshes
    public CoroutineQueue combineQueue;
    //Queue for misc
    public CoroutineQueue miscQueue;

    ObjectGenerator objectGenerator;

    void Awake() {
        Current = this;

        mainQueue = new CoroutineQueue(this);
        mainQueue.StartLoop();

        combineQueue = new CoroutineQueue(this);
        combineQueue.StartLoop();

        miscQueue = new CoroutineQueue(this);
        miscQueue.StartLoop();

        objectGenerator = ScriptableObject.CreateInstance<ObjectGenerator>();
    }

    void Start() {
        //Generate random seeds
        if (RandomSeeds) {
            System.Random rand = new System.Random();
            TerrainMapData.TerrainNoiseData.Seed = rand.Next();
            TerrainHouseData.ObjectNoiseData.Seed = rand.Next();
            foreach (TreeData TerrainTreeData in TerrainTreeDataArray) {
                TerrainTreeData.ObjectNoiseData.Seed = rand.Next();
            }
            TerrainGrassData.GrassNoiseData.Seed = rand.Next();
        }

        //If using master colour
        if (UseMasterColour) {
            ChangeColour(TerrainHouseData, MasterColour);
            ChangeColour(TerrainDetailData, MasterColour);
            foreach (ObjectData objectData in TerrainTreeDataArray) {
                ChangeColour(objectData, MasterColour);
            }
            TerrainGrassData.GrassColour = MasterColour;
            TerrainGrassData.GrassColourDry = MasterColour;
            TerrainMapData.TerrainTextures[2].TextureTint = MasterColour;
        }

        //Change texture colours for objects
        UpdateColourAtlas(TerrainHouseData);
        UpdateColourAtlas(TerrainDetailData);
        foreach (ObjectData objectData in TerrainTreeDataArray) {
            UpdateColourAtlas(objectData);
        }


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
                Terrain top = (j - 1 >= 0) ? terrains[i, j - 1] : null;
                Terrain bottom = (j + 1 < terrains.GetLength(1)) ? terrains[i, j + 1] : null;

                terrains[i, j].SetNeighbors(left, top, right, bottom);

                /*
                if(left)
                    Debug.Log("Terrain " + i + j + " Left: " + left.name);
                if(right)
                    Debug.Log("Terrain " + i + j + " Right: " + right.name);
                if(top)
                    Debug.Log("Terrain " + i + j + " Top: " + top.name);
                if(bottom)
                    Debug.Log("Terrain " + i + j + " Bottom: " + bottom.name);
                    */
            }
        }
    }

    //Change material by adding it to coroutine queue
    void UpdateColourAtlas(ObjectData objectData) {
        for (int i = 0; i < objectData.PrefabArray.Length; i++) {
            miscQueue.EnqueueAction(objectGenerator.ChangeColourAtlas(objectData.ObjectMaterial, objectData.PrimaryColour, objectData.SecondaryColour, objectData.TertiaryColour, objectData.QuaternaryColour));
        }
    }

    void ChangeColour(ObjectData objectData, Color colour) {
        for (int i = 0; i < objectData.PrefabArray.Length; i++) {
            objectData.PrimaryColour = colour;
        }
    }

    public static float[,] CalculateHeightMap(ChunkData chunkData) {

        MapGenerator mapGenerator = new MapGenerator();

        Vector2 noiseOffset = new Vector2(chunkData.position.x / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution, chunkData.position.y / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution);

        //Store temp heightmap data
        float[,] tempHeightMap = mapGenerator.GenerateNoiseMap(ProceduralTerrain.Current.TerrainMapData.TerrainNoiseData, ProceduralTerrain.TerrainResolution + 2, noiseOffset);

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

                    //Corner
                    if (y == 1) {
                        tempHeight = tempHeightMap[x - 1, y - 1];
                    }
                    else if (y == tempHeightMap.GetLength(1) - 1) {
                        tempHeight = tempHeightMap[x - 1, y + 1];
                    }
                }

                else if (y == 1) {
                    tempHeight += tempHeightMap[x, y - 1];// + tempHeightMap[x, y]) / 2;
                    hasAssignedValue = true;
                }

                else if (x == tempHeightMap.GetLength(0) - 1) {
                    tempHeight += tempHeightMap[x + 1, y];// + tempHeightMap[x, y]) / 2;
                    hasAssignedValue = true;

                    //Corner
                    if (y == 1) {
                        tempHeight = tempHeightMap[x + 1, y - 1];
                    }
                    else if (y == tempHeightMap.GetLength(1) - 1) {
                        tempHeight = tempHeightMap[x + 1, y + 1];
                    }
                }

                else if (y == tempHeightMap.GetLength(1) - 1) {
                    tempHeight += tempHeightMap[x, y + 1];// + tempHeightMap[x, y]) / 2;
                    hasAssignedValue = true;
                }

                //If value not yet assigned
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

    public void GenerateTerrain(ChunkData chunkData) {

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

    public void GenerateHouses(ChunkData chunkData) {

        IEnumerator gen = objectGenerator.GenerateObjects(ObjectType.House, ProceduralTerrain.Current.TerrainHouseData, chunkData);
        mainQueue.EnqueueAction(gen);
        mainQueue.EnqueueAction(GenerateVillages(chunkData));
    }

    public void GenerateTrees(ChunkData chunkData) {

        foreach (TreeData TerrainTreeData in ProceduralTerrain.Current.TerrainTreeDataArray) {
            IEnumerator gen = objectGenerator.GenerateObjects(ObjectType.Tree, TerrainTreeData, chunkData);
            mainQueue.EnqueueAction(gen);
        }
    }

    public void GenerateDetails(ChunkData chunkData) {

        IEnumerator gen = objectGenerator.GenerateObjects(ObjectType.Detail, ProceduralTerrain.Current.TerrainDetailData, chunkData);
        mainQueue.EnqueueAction(gen);
    }

    IEnumerator GenerateVillages(ChunkData chunkData) {

        float distanceBetweenVillages = 500;
        float maxVillageDistance = 150;
        int minHousesPerVillage = 5;
        int maxHousesPerVillage = 20;

        //List to store individual villages
        List<VillageData> villageList = new List<VillageData>();
        //List to store objects that need deleting
        List<VillageData> villagesToDelete = new List<VillageData>();

        //Go through house, check through all other houses, if within maxVillageDistance, add them to village
        foreach (VillageHouseData house1 in chunkData.VillageHouseList) {
            //If already has a village, move to next house
            if (house1.Village != null) {
                continue;
            }

            foreach (VillageHouseData house2 in chunkData.VillageHouseList) {
                //Make sure not comparing to self
                if (house1 == house2) {
                    continue;
                }

                //House2 should not already have a village
                if (house2.Village != null) {
                    continue;
                }

                //If house1 doesnt have a village, create one
                if (house1.Village == null) {
                    VillageData thisVillage = new VillageData();
                    thisVillage.AddHouse(house1);
                    villageList.Add(thisVillage);
                }

                //If distance been house1 and house2 is less than the max village distance
                if (Vector3.Distance(house1.transform.position, house2.transform.position) < maxVillageDistance) {
                    //Assign house2 to village
                    house1.Village.AddHouse(house2);
                }
            }
        }

        //Assign village list to chunk
        chunkData.VillageList = villageList;

        //If village has less than required houses, delete it
        foreach (VillageData village in chunkData.VillageList) {
            if (village.VillageSize < minHousesPerVillage) {
                villagesToDelete.Add(village);
            }
        }

        for (int i = 0; i < villagesToDelete.Count; i++) {
            villageList.Remove(villagesToDelete[i]);
            villagesToDelete[i].DestroyVillage();
        }
        villagesToDelete.Clear();

        //Get middle point of villages
        Vector3 center = Vector3.zero;
        foreach (VillageData village in chunkData.VillageList) {
            foreach (VillageHouseData house in village.VillageHouses) {
                center += house.transform.position;
            }

            //Find average of all house positions
            center /= village.VillageSize;

            village.CenterPosition = center;
        }

        //If village has more than max number of houses, reduce amount of houses by removing the houses furthest from the center
        foreach (VillageData village in chunkData.VillageList) {
            //Keep removing houses until there is less than the max amount
            while (village.VillageSize > maxHousesPerVillage) {

                VillageHouseData furthestHouse = village.VillageHouses[0];
                float furthestDistance = float.MinValue;

                foreach (VillageHouseData house in village.VillageHouses) {
                    if (Vector3.Distance(house.transform.position, village.CenterPosition) > furthestDistance) {
                        furthestDistance = Vector3.Distance(house.transform.position, village.CenterPosition);
                        furthestHouse = house;
                    }
                }

                village.RemoveHouse(furthestHouse);
            }
        }

        //Find closest house to middle point of village and replace it
        foreach (VillageData village in chunkData.VillageList) {
            //Initialise
            VillageHouseData closestHouse = village.VillageHouses[0];
            float closestDistance = float.MaxValue;
            foreach (VillageHouseData house in village.VillageHouses) {
                if (Vector3.Distance(house.transform.position, village.CenterPosition) < closestDistance) {
                    closestDistance = Vector3.Distance(house.transform.position, village.CenterPosition);
                    closestHouse = house;
                }
            }

            village.RemoveHouse(closestHouse);
            //Spawn prefab
            GameObject newCenter = Instantiate(ProceduralTerrain.Current.TerrainHouseData.VillageCenterPrefab.ObjectPrefab, closestHouse.transform.position, Quaternion.identity, closestHouse.transform.parent);
            village.VillageCenter = newCenter;
            chunkData.VillageCenterList.Add(newCenter);

            //Parent objects
            GameObject villageParent = new GameObject();
            villageParent.transform.SetParent(village.VillageCenter.transform.parent);
            villageParent.name = "Village";
            foreach (VillageHouseData house in village.VillageHouses) {
                house.transform.SetParent(villageParent.transform);
            }
            village.VillageCenter.transform.SetParent(villageParent.transform);
        }

        //Rotate houses to face village center
        foreach (VillageData village in chunkData.VillageList) {
            foreach (VillageHouseData house in village.VillageHouses) {
                house.transform.LookAt(village.VillageCenter.transform);
            }
        }

        //Make sure trees arent placed in village area
        foreach (VillageData village in chunkData.VillageList) {
            Vector2 centerPosition = new Vector2((int)village.CenterPosition.x, (int)village.CenterPosition.z);
            for (int x = (int)(-maxVillageDistance / 2); x < (int)(maxVillageDistance / 2); x++) {
                for (int y = (int)(-maxVillageDistance / 2); y < (int)(maxVillageDistance / 2); y++) {
                    Vector2 positionToMark = new Vector2(centerPosition.y + y, centerPosition.x + x);

                    //If is in range
                    if (positionToMark.x >= 0 && positionToMark.x < chunkData.terrainPlacableMap.GetLength(0) && positionToMark.y >= 0 && positionToMark.y < chunkData.terrainPlacableMap.GetLength(1)) {
                        chunkData.terrainPlacableMap[(int)positionToMark.x, (int)positionToMark.y] = false;
                    }
                }
            }
        }

        yield return null;
    }

    //Generate grass using unitys terrain detail
    public void GenerateGrass(ChunkData chunkData) {

        MapGenerator mapGenerator = new MapGenerator();

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
        float[,] fGrassMap = mapGenerator.GenerateNoiseMap(TerrainGrassData.GrassNoiseData, grassResolution, chunkData.position);

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

