﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType { Tree, House, Detail };

public class ProceduralTerrain : MonoBehaviour {

    public static ProceduralTerrain Current;

    public const int TerrainResolution = 513;
    //public const int NumberOfChunks = 1;

    [Header("Terrain")]
    public MapData TerrainMapData;

    [Header("Trees")]
    public TreeData[] TerrainTreeDataArray;

    [Header("Houses")]
    public HouseData TerrainHouseData;

    [Header("Details")]
    public DetailData[] TerrainDetailDataArray;

    [Header("Grass")]
    public GrassData[] TerrainGrassDataArray;

    [Header("Debug Options")]
    public bool RandomSeeds = false;
    public bool CombineMeshes = true;
    public bool UseMasterColour = false;
    public Color MasterColour = Color.green;
    public bool UseRandomMasterColour = false;
    public Color[] MasterColourArray;

    [Header("UI")]
    public GameObject ChunkLoadingScreen;

    //Terrain[,] terrains = new Terrain[NumberOfChunks, NumberOfChunks];
    public List<ChunkData> ChunkList { get; private set; }

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

        ChunkList = new List<ChunkData>();
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
            foreach (GrassData TerrainGrassData in TerrainGrassDataArray) {
                TerrainGrassData.GrassNoiseData.Seed = rand.Next();
            }
        }

        //If using master colour
        if (UseMasterColour) {

            if(UseRandomMasterColour) {
                System.Random rand = new System.Random();
                int randNum = rand.Next(0, MasterColourArray.Length);
                MasterColour = MasterColourArray[randNum];
            }

            ChangeColour(TerrainHouseData, MasterColour);
            
            foreach (ObjectData TerrainDetailData in TerrainDetailDataArray) {
                ChangeColour(TerrainDetailData, MasterColour);
            }
            foreach (ObjectData objectData in TerrainTreeDataArray) {
                ChangeColour(objectData, MasterColour);
            }
            foreach (GrassData TerrainGrassData in TerrainGrassDataArray) {
                //If grass can have colour overriden
                if (TerrainGrassData.AllowColourOverride) {
                    TerrainGrassData.GrassColour = MasterColour;
                    TerrainGrassData.GrassColourDry = MasterColour;
                }
            }

            TerrainMapData.TerrainTextures[2].TextureTint = MasterColour + new Color(0.1f, 0.1f, 0.1f);
        }

        //Change texture colours for objects
        UpdateColourAtlas(TerrainHouseData);
        foreach (ObjectData TerrainDetailData in TerrainDetailDataArray) {
            UpdateColourAtlas(TerrainDetailData);
        }
        foreach (ObjectData objectData in TerrainTreeDataArray) {
            UpdateColourAtlas(objectData);
        }

        //Create initial chunk
        CreateChunk(0, 0);
    }

    public IEnumerator DisplayChunkLoadMessage() {
        ChunkLoadingScreen.SetActive(true);
        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator HideChunkLoadMessage() {
        ChunkLoadingScreen.SetActive(false);
        yield return new WaitForSeconds(0.1f);
    }

    void CreateChunk(int x, int y) {
        ChunkData chunk = new ChunkData(new Vector2(x, y));
    }

    public void OnChunkCreated(ChunkData chunk) {
        //Assign chunk to master list
        ChunkList.Add(chunk);
        //Recalculate terrain neighbours
        RecalculateTerrainNeighbours();
    }

    public void CreateNeighbourChunks(ChunkData chunk) {

        int terrainSize = ProceduralTerrain.Current.TerrainMapData.TerrainSize;

        if (chunk.BottomNeighbour == null) {
            CreateChunk((int)chunk.position.x, (int)chunk.position.y - terrainSize);
        }

        if (chunk.TopNeighbour == null) {
            CreateChunk((int)chunk.position.x, (int)chunk.position.y + terrainSize);
        }

        if (chunk.LeftNeighbour == null) {
            CreateChunk((int)chunk.position.x - terrainSize, (int)chunk.position.y);
        }

        if (chunk.RightNeighbour == null) {
            CreateChunk((int)chunk.position.x + terrainSize, (int)chunk.position.y);
        }

        //Diaganols
        bool topLeft, topRight, bottomLeft, bottomRight;
        topLeft = topRight = bottomLeft = bottomRight = false;

        //Go through every chunk to see if any match the positions of chunks we want to create
        foreach (ChunkData chunk2 in ChunkList) {
            Vector2 position = chunk2.position;

            //Upper left
            if((int)chunk.position.x - terrainSize == (int)position.x && (int)chunk.position.y - terrainSize == (int)position.y) {
                topLeft = true;
            }

            //Upper right
            if ((int)chunk.position.x + terrainSize == (int)position.x && (int)chunk.position.y - terrainSize == (int)position.y) {
                topRight = true;
            }

            //Bottom left
            if ((int)chunk.position.x - terrainSize == (int)position.x && (int)chunk.position.y + terrainSize == (int)position.y) {
                bottomLeft = true;
            }

            //Bottom right
            if ((int)chunk.position.x + terrainSize == (int)position.x && (int)chunk.position.y + terrainSize == (int)position.y) {
                bottomRight = true;
            }
        }

        //If these chunks don't exist, create them
        if(!topLeft) {
            CreateChunk((int)chunk.position.x - terrainSize, (int)chunk.position.y - terrainSize);
        }

        if (!topRight) {
            CreateChunk((int)chunk.position.x + terrainSize, (int)chunk.position.y - terrainSize);
        }

        if (!bottomLeft) {
            CreateChunk((int)chunk.position.x - terrainSize, (int)chunk.position.y + terrainSize);
        }

        if (!bottomRight) {
            CreateChunk((int)chunk.position.x + terrainSize, (int)chunk.position.y + terrainSize);
        }
    }

    void RecalculateTerrainNeighbours() {
        int terrainSize = ProceduralTerrain.Current.TerrainMapData.TerrainSize;

        foreach (ChunkData chunk1 in ChunkList) {
            Terrain right, left, above, below;
            right = left = above = below = null;

            foreach (ChunkData chunk2 in ChunkList) {
                //Make sure not comparing against self
                if (chunk1 == chunk2) {
                    continue;
                }

                //Right
                if((int)(chunk1.position.x - chunk2.position.x) == -terrainSize && chunk1.position.y == chunk2.position.y) {
                    right = chunk2.terrain;
                }

                //Left
                if ((int)(chunk1.position.x - chunk2.position.x) == terrainSize && chunk1.position.y == chunk2.position.y) {
                    left = chunk2.terrain;
                }

                //Above
                if ((int)(chunk1.position.y - chunk2.position.y) == -terrainSize && chunk1.position.x == chunk2.position.x) {
                    above = chunk2.terrain;
                }

                //Below
                if ((int)(chunk1.position.y - chunk2.position.y) == terrainSize && chunk1.position.x == chunk2.position.x) {
                    below = chunk2.terrain;
                }

            }

            chunk1.terrain.SetNeighbors(left, above, right, below);
            chunk1.LeftNeighbour = left;
            chunk1.RightNeighbour = right;
            chunk1.TopNeighbour = above;
            chunk1.BottomNeighbour = below;
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

        Vector2 noiseOffset = new Vector2(chunkData.position.x / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution, -chunkData.position.y / ProceduralTerrain.Current.TerrainMapData.TerrainSize * ProceduralTerrain.TerrainResolution);

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

        //if (ProceduralTerrain.NumberOfChunks != 1) {
            maxLocalHeight = 1.3f;
            minLocalHeight = 0.4f;
        //}

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
        //Set terrain Tag
        terrainGameObject.tag = "Terrain";

        //Set position 
        terrainGameObject.transform.position = new Vector3(chunkData.position.x, 0, chunkData.position.y);

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
        mainQueue.EnqueueAction(GenerateTreeHouseHouses(chunkData));
    }

    public void GenerateVillageConnections(ChunkData chunkData) {
        mainQueue.EnqueueAction(ConnectVillages(chunkData));
        //Unlike other combining, this must happen after bridges have been generated and so must go in the main queue
        mainQueue.EnqueueAction(CombineHouseMeshes(chunkData));
    }

    public void GenerateTrees(ChunkData chunkData) {

        foreach (TreeData TerrainTreeData in ProceduralTerrain.Current.TerrainTreeDataArray) {
            IEnumerator gen = objectGenerator.GenerateObjects(ObjectType.Tree, TerrainTreeData, chunkData);
            mainQueue.EnqueueAction(gen);
        }
    }

    public void GenerateDetails(ChunkData chunkData) {

        foreach (DetailData TerrainDetailData in ProceduralTerrain.Current.TerrainDetailDataArray) {
            IEnumerator gen = objectGenerator.GenerateObjects(ObjectType.Detail, TerrainDetailData, chunkData);
            mainQueue.EnqueueAction(gen);
        }
    }

    IEnumerator CombineHouseMeshes(ChunkData chunkData) {

        foreach(GameObject villageCenter in chunkData.VillageCenterList) {
            GameObject subChunk = villageCenter.transform.parent.parent.gameObject;
            //If subchunk hasn't had meshes combined, combine them
            if (!subChunk.GetComponent<CombineMeshes>()) {
                subChunk.AddComponent<CombineMeshes>();
            }
        }

        yield return null;
    }

    IEnumerator GenerateTreeHouseHouses(ChunkData chunkData) {

        System.Random rand = new System.Random();

        foreach (GameObject villageCenter in chunkData.VillageCenterList) {
            //For every child
            for (int i = 0; i < villageCenter.transform.childCount; i++) {
                GameObject houseSpawnPoint = villageCenter.transform.GetChild(i).gameObject;
                //If child has HouseSpawnPoint tag
                if (houseSpawnPoint.CompareTag("HouseSpawnPoint")) {
                    int randInt = rand.Next(0, TerrainHouseData.HouseSpawnPointPrefabs.Length);

                    GameObject newHouse = Instantiate(TerrainHouseData.HouseSpawnPointPrefabs[randInt], houseSpawnPoint.transform.position, houseSpawnPoint.transform.rotation);
                    newHouse.transform.SetParent(houseSpawnPoint.transform);
                }
            }
        }

        yield return null;
    }

    IEnumerator GenerateVillages(ChunkData chunkData) {

        float maxVillageDistance = ProceduralTerrain.Current.TerrainHouseData.MaxDistanceOfVillage;
        int minHousesPerVillage = ProceduralTerrain.Current.TerrainHouseData.MaxHousesPerVillage;
        int maxHousesPerVillage = ProceduralTerrain.Current.TerrainHouseData.MinHousesPerVillage;

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
            //Set center position for vilalge for this chunk
            village.LocalChunkCenterPosition = closestHouse.ChunkLocalPosition;
            //Delete closest house
            village.RemoveHouse(closestHouse);
            //Spawn village center prefab at location of closest house
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
                //Make house flat, rather than tilted
                house.transform.localEulerAngles = new Vector3(0, house.transform.localEulerAngles.y, house.transform.localEulerAngles.z);
            }
        }

        //Make sure trees arent placed in village area
        int clearAreaRadius = ProceduralTerrain.Current.TerrainHouseData.ClearAreaRadiusAroundBuildings;
        foreach (VillageData village in chunkData.VillageList) {
            bool hasClearedAroundCenter = false;
            foreach (VillageHouseData house in village.VillageHouses) {
                Vector2 centerPosition = house.ChunkLocalPosition;
                //If the area around the village center hasnt been cleared yet, clear it
                if(!hasClearedAroundCenter) {
                    centerPosition = village.LocalChunkCenterPosition;
                    PreventSpawningInArea(chunkData, centerPosition, clearAreaRadius);
                    //Set centerPosition back to this houses position
                    centerPosition = house.ChunkLocalPosition;
                }
                //Prevent spawning of trees around this house
                PreventSpawningInArea(chunkData, centerPosition, clearAreaRadius);
            }
        }

        yield return null;
    }

    //Prevents trees spawning around given position
    void PreventSpawningInArea(ChunkData chunkData, Vector2 centerPosition, int clearAreaRadius) {
        for (int x = (int)(-clearAreaRadius / 2); x < (int)(clearAreaRadius / 2); x++) {
            for (int y = (int)(-clearAreaRadius / 2); y < (int)(clearAreaRadius / 2); y++) {
                Vector2 positionToMark = new Vector2(centerPosition.x + x, centerPosition.y + y);

                //If is in range
                if (positionToMark.x >= 0 && positionToMark.x < chunkData.terrainPlacableMap.GetLength(0) && positionToMark.y >= 0 && positionToMark.y < chunkData.terrainPlacableMap.GetLength(1)) {
                    chunkData.terrainPlacableMap[(int)positionToMark.x, (int)positionToMark.y] = false;
                }
            }
        }
    }

    IEnumerator ConnectVillages(ChunkData chunkData) {
        //Cannot connect villages until all trees have been combined
        while (combineQueue.GetQueueSize() > 0) {
            yield return null;
        }

        float maxDistanceBetweenPoints = ProceduralTerrain.Current.TerrainHouseData.MaxDistanceBetweenConnectionPoints;
        int maxNumberOfConnections = ProceduralTerrain.Current.TerrainHouseData.MaxNumberOfConnectionsPerVillage;

        //Foreach village center in this chunk
        foreach (GameObject villageCenter in chunkData.VillageCenterList) {
            //Get connection point
            GameObject connectionPoint1 = villageCenter.transform.FindChild("ConnectionPoint").gameObject;
            //If connection point can't be found, exit
            if (!connectionPoint1) {
                continue;
            }

            VillageConnectionData connectionData1 = connectionPoint1.GetComponent<VillageConnectionData>();
            
            //Check there is less than max number of connections
            if (connectionData1.NumberOfConnections >= maxNumberOfConnections) {
                continue;
            }

            foreach (GameObject otherVillageCenter in chunkData.VillageCenterList) {
                //Make sure not comparing to self
                if (villageCenter == otherVillageCenter) {
                    continue;
                }

                //If connection1 has reached its max number, break out of this loops
                if (connectionData1.NumberOfConnections >= maxNumberOfConnections) {
                    break;
                }

                GameObject connectionPoint2 = otherVillageCenter.transform.FindChild("ConnectionPoint").gameObject;
                if (!connectionPoint2) {
                    continue;
                }

                VillageConnectionData connectionData2 = connectionPoint2.GetComponent<VillageConnectionData>();

                //Less than max number of connections and connection 1 has 1+ connection, prevents centers having no connections when close enough to other centers
                if (connectionData2.NumberOfConnections >= maxNumberOfConnections && connectionData1.NumberOfConnections != 0) {
                    continue;
                }

                //Check these aren't already paired
                if (connectionData2.Connections.Contains(connectionPoint1)) {
                    continue;
                }

                Vector3 pointA = connectionPoint1.transform.position;
                Vector3 pointB = connectionPoint2.transform.position;
                Vector3 halfwayPoint = (pointA + pointB) / 2;
                float distance = (pointA - pointB).magnitude;
                float treeHouseRadius = ProceduralTerrain.Current.TerrainHouseData.VillageCenterRadius; //From center point of tree house so bridge connects to outer edge not trunk

                //If the distance between the points is too large, go to next comparison
                if(distance > maxDistanceBetweenPoints) {
                    continue;
                }

                //Check for collisions in the bridge area
                bool canPlace = true;
                Collider[] colliders = Physics.OverlapCapsule(pointA, pointB, 2);
                foreach(Collider coll in colliders) {
                    //If colliding with terrain or a tree, don't build a bridge here
                    if (coll.CompareTag("Terrain") || coll.CompareTag("Tree")) {
                        canPlace = false;
                        break;
                    }

                    //If colliding with a house
                    if(coll.CompareTag("House")) {
                        //If not colliding with a connection point's house, don't build a bridge
                        if(coll.gameObject != connectionPoint1.transform.parent.gameObject && coll.gameObject != connectionPoint2.transform.parent.gameObject) {
                            canPlace = false;
                            break;
                        }
                    }
                }

                //If can't place, go to next connection point
                if(!canPlace) {
                    continue;
                }

                //Create bridge
                GameObject bridge = Instantiate(ProceduralTerrain.Current.TerrainHouseData.BridgePrefab);//GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Bridge";
                bridge.transform.localScale = new Vector3(1, 1, distance - treeHouseRadius * 2);
                bridge.transform.position = halfwayPoint;
                bridge.transform.LookAt(pointB);
                bridge.transform.SetParent(villageCenter.transform);

                //Update connections on connection points
                connectionData1.AddConnection(connectionPoint2);
                connectionData2.AddConnection(connectionPoint1);
            }
        }

        yield return null;
    }

    //Generate grass using unitys terrain detail
    public void GenerateGrass(ChunkData chunkData) {

        //Set detail and resolution
        const int resolutionMultipier = 2;
        const int grassResolution = TerrainResolution * resolutionMultipier;
        const int patchDetail = 32;
        chunkData.terrain.terrainData.SetDetailResolution(grassResolution, patchDetail);
        //Set distance details can be seen for
        chunkData.terrain.detailObjectDistance = 250;

        chunkData.detailPrototype = new DetailPrototype[TerrainGrassDataArray.Length];
        int i = 0;
        foreach (GrassData grassData in TerrainGrassDataArray) {
            AddGrassTexturesToTerrain(grassData, chunkData, i++);
        }
        chunkData.terrainData.detailPrototypes = chunkData.detailPrototype;

        i = 0;
        foreach (GrassData grassData in TerrainGrassDataArray) {
            SpawnGrass(grassData, chunkData, i++);
        }
    }

    void AddGrassTexturesToTerrain(GrassData TerrainGrassData, ChunkData chunkData, int detailLayer) {
        Texture2D grassTexture = TerrainGrassData.GrassTexture;

        //Set detail texture
        DetailPrototype[] detailPrototype = chunkData.detailPrototype;
        detailPrototype[detailLayer] = new DetailPrototype();
        detailPrototype[detailLayer].prototypeTexture = grassTexture;
        //Set grass colour
        detailPrototype[detailLayer].healthyColor = TerrainGrassData.GrassColour;
        detailPrototype[detailLayer].dryColor = TerrainGrassData.GrassColourDry;
        detailPrototype[detailLayer].renderMode = DetailRenderMode.GrassBillboard;
        detailPrototype[detailLayer].maxHeight = TerrainGrassData.GrassMaxHeight;
        detailPrototype[detailLayer].minHeight = TerrainGrassData.GrassMinHeight;
        detailPrototype[detailLayer].maxWidth = TerrainGrassData.GrassMaxWidth;
        detailPrototype[detailLayer].minWidth = TerrainGrassData.GrassMinWidth;
    }

    void SpawnGrass(GrassData TerrainGrassData, ChunkData chunkData, int detailLayer) {
        MapGenerator mapGenerator = new MapGenerator();

        TerrainData terrainData = chunkData.terrainData;
        Terrain terrain = chunkData.terrain;

        const int resolutionMultipier = 2;
        const int grassResolution = TerrainResolution * resolutionMultipier;

        int[,] grassMap = new int[grassResolution, grassResolution];
        float[,] fGrassMap = mapGenerator.GenerateNoiseMap(TerrainGrassData.GrassNoiseData, grassResolution, chunkData.position);

        int incremement = 1;// (int)(1 / TerrainGrassData.GrassSpawnDensity);
        System.Random rand = new System.Random();
        int spawnDensity = TerrainGrassData.GrassSpawnDensity;

        for (int i = 0; i < grassResolution; i += incremement) {
            for (int j = 0; j < grassResolution; j += incremement) {

                float terrainHeight = terrain.terrainData.GetHeight((int)(j / resolutionMultipier), (int)(i / resolutionMultipier)) / (float)ProceduralTerrain.Current.TerrainMapData.TerrainHeight;

                //Get alpha map at this location (inverted)
                float[,,] localAlphaMap = terrainData.GetAlphamaps(j / resolutionMultipier, i / resolutionMultipier, 1, 1);
                //Get the strength of the terrain grass texture at this point on the terrain
                float grassStrength = localAlphaMap[0, 0, 2]; //2 being the grass texture on the terrain

                if (grassStrength >= TerrainGrassData.GrassSpawnGroundTextureThreshold) {
                    grassMap[i, j] = spawnDensity;
                }
                else {
                    grassMap[i, j] = 0;
                    continue;
                }

                //Compare against generated noise
                if (fGrassMap[i, j] >= TerrainGrassData.GrassSpawnNoiseThreshold) {
                    grassMap[i, j] = spawnDensity;
                }
                else {
                    grassMap[i, j] = 0;
                    continue;
                }

                //Compare against spawn rate chance, skip if grass spawn chance is 1 as it will always spawn
                if (TerrainGrassData.GrassSpawnChance < 1) {
                    if (rand.Next(0, 100) <= TerrainGrassData.GrassSpawnChance * 100) {
                        grassMap[i, j] = spawnDensity;
                    }
                    else {
                        grassMap[i, j] = 0;
                        continue;
                    }
                }

                //If grass is not within min and max height
                if (!(terrainHeight >= TerrainGrassData.MinGrassSpawnHeight && terrainHeight <= TerrainGrassData.MaxGrassSpawnHeight)) {
                    grassMap[i, j] = 0;
                }
            }
        }

        //Remove grass already in this area in other detail maps
        for (int i = 1; i <= TerrainGrassDataArray.Length; i++) {
            if(detailLayer < i) {
                break;
            }

            //Get grass map of layer below
            int[,] tempGrassMap = terrain.terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, detailLayer - i);

            //Take grass map away from temp grass map
            for (int x = 0; x < tempGrassMap.GetLength(0); x++) {
                for (int y = 0; y < tempGrassMap.GetLength(1); y++) {
                    //If grass here, remove grass from temp grass map
                    if (grassMap[x, y] > 0) {
                        tempGrassMap[x, y] = 0;
                    }
                }
            }
            //Assign updated temp grass map back to terrain
            terrain.terrainData.SetDetailLayer(0, 0, detailLayer - i, tempGrassMap);
        }

        terrain.terrainData.SetDetailLayer(0, 0, detailLayer, grassMap);
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

