using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : ScriptableObject {

    int TreeCounter = -1;
    int HouseCounter = -1;
    int DetailCounter = -1;
    bool canContinue = false;
    float[,] noiseMap;
    bool[,] flatPlacableMap;

    bool CheckIfPlacableFromMap(Vector2 position, Vector2 requiredSpace, bool[,] map) {

        bool canPlace = true;
        int x = (int)position.x;
        int y = (int)position.y;
        int requiredX = (int)requiredSpace.x;
        int requiredZ = (int)requiredSpace.y;

        for (int i = 0; i <= requiredX; i++) {
            for (int j = 0; j <= requiredZ; j++) {
                //If canPlace = false, no point checking other areas
                if (canPlace) {

                    if (map[x + i, y + j]) {
                        canPlace = true;
                    }
                    else {
                        canPlace = false;
                    }
                }
            }
        }

        return canPlace;
    }

    bool CheckIfPlacableFromDensity(float density, int totalObjectsInArray) {

        bool canPlace = true;
        float number = Random.Range(0f, 1f);

        //Reduce chance so each object within array has equal chance of being spawned 
        number /= totalObjectsInArray;

        if (number > density) {
            canPlace = false;
        }


        return canPlace;
    }

    bool CheckIfPlacableFromHeight(float currentHeight, float minHeight, float maxHeight) {

        bool canPlace = true;

        //If its not within range, it cannot be placed here
        if (!(currentHeight >= minHeight && currentHeight <= maxHeight)) {
            canPlace = false;
        }

        return canPlace;
    }

    void UpdatePlacableMap(Vector2 position, Vector2 area, bool[,] map) {

        int x = (int)position.x;
        int y = (int)position.y;
        int requiredSpaceX = (int)area.x;
        int requiredSpaceZ = (int)area.y;

        //This area is no longer placable within objects required area
        for (int i = 0; i <= requiredSpaceX; i++) {
            for (int j = 0; j <= requiredSpaceZ; j++) {
                map[x + i, y + j] = false;
            }
        }
    }

    int GetTotalVerticies(PrefabData prefabData) {
        int verts = 0;

        MeshFilter[] meshFilters = prefabData.ObjectPrefab.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters) {
            verts += meshFilter.sharedMesh.vertexCount;
        }

        return verts;
    }

    public IEnumerator ChangeColourAtlas(Material material, Color primaryColour, Color secondaryColour, Color tertiaryColour, Color quaternaryColour) {//, Color tertiaryColour = Color.black, Color quaternaryColour = new Color()) {

        //If no colour atlas is assigned
        if (material.mainTexture == null) {
            yield break;
        }

        //Get texture
        Texture2D texture = (Texture2D)material.mainTexture;
        Color[] pixels = texture.GetPixels();

        int pixelsPerFrame = (texture.width * texture.width) / 8;
        int pixelsThisFrame = 0;

        for (int i = 0; i < texture.width; i++) {
            for (int j = 0; j < texture.height; j++) { 
                //First half
                if (i < texture.width / 2) {
                    //Top half
                    if (j < texture.height / 2) {
                        pixels[i * texture.width + j] = tertiaryColour;
                    }

                    //Bottom half
                    if (j > texture.height / 2) {
                        pixels[i * texture.width + j] = quaternaryColour;
                    }
                }
                //Second half
                if (i >= texture.width / 2) {
                    //Top half
                    if (j < texture.height / 2) {
                        pixels[i * texture.width + j] = primaryColour;
                    }

                    //Bottom half
                    if (j > texture.height / 2) {
                        pixels[i * texture.width + j] = secondaryColour;
                    }
                }

                pixelsThisFrame++;
                if (pixelsThisFrame > pixelsPerFrame) {
                    pixelsThisFrame = 0;
                    yield return null;
                }
            }
        }

        texture.SetPixels(pixels);
        yield return null;

        texture.Apply();
    }

    void OnNoiseMapRecieved(float[,] returnedMap) {
        noiseMap = returnedMap;
        canContinue = true;
    }

    void OnFlatTerrainMapRecieved(bool[,] returnedMap) {
        flatPlacableMap = returnedMap;
        canContinue = true;
    }

    public IEnumerator GenerateObjects(ObjectType objectType, ObjectData objectData, ChunkData chunkData) {

        MapGenerator mapGenerator = new MapGenerator();
        float[,] map = new float[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];
        int seed = objectData.ObjectNoiseData.Seed;
        int flatSearchRange = objectData.FlatSurfaceSearchRange;
        float flatSens = objectData.FlatSurfaceSensitivity;
        PrefabData[] prefabDataArray = objectData.PrefabArray;
        float density = objectData.SpawnDensity;
        float minSpawnHeight = objectData.MinSpawnHeight;
        float maxSpawnHeight = objectData.MaxSpawnHeight;
        float threshold = objectData.SpawnThreshold;
        Vector2 position = chunkData.position;

        List<GameObject> subChunks = new List<GameObject>();
        int objectsPlaced = 0;
        int subChunkIndex = 0;

        //Keep track of vertices so can group as efficiently as possible for combine mesh
        int vertexCount = 0;

        //Keep track of objects spawned 
        int objectsSpawned = 0;
        //Max number of object per frame
        int maxSpawnsPerFrame = objectData.MaxSpawnsPerFrame;

        //Stop programming from running until thread has returned noisemap
        canContinue = false;

        //Start a new thread for requesting the noisemap
        mapGenerator.RequestNoiseMap(objectData.ObjectNoiseData, ProceduralTerrain.TerrainResolution, position, OnNoiseMapRecieved);
        //While there is no noise map, do not progress any further
        while (!canContinue) {
            mapGenerator.UpdateCallbacks();
            yield return null;
        }
        canContinue = false;

        //Placable map for this object type based off of terrain flatness
        mapGenerator.RequestFlatTerrainMap(chunkData.terrainHeightMap, flatSearchRange, flatSens, OnFlatTerrainMapRecieved);
        //While there is no flat terrain map, do not progress any further
        while (!canContinue) {
            mapGenerator.UpdateCallbacks();
            yield return null;
        }

        //Placable map based off of whether the location is valid (if there is another object there)
        bool[,] validPlacableMap;

        //If not detail type, normal placable map
        if (objectType != ObjectType.Detail) {
            validPlacableMap = chunkData.terrainPlacableMap;
        }
        //Else detail placable area
        else {
            validPlacableMap = chunkData.terrainDetailPlacableMap;
        }

        //Create empty parent to hold object type
        GameObject parentObject;
        switch (objectType) {
            case ObjectType.House:
                HouseCounter++;
                parentObject = new GameObject("Houses " + HouseCounter);
                break;

            case ObjectType.Tree:
                TreeCounter++;
                parentObject = new GameObject("Trees " + TreeCounter);
                break;

            case ObjectType.Detail:
                DetailCounter++;
                parentObject = new GameObject("Details " + DetailCounter);
                break;

            default:
                parentObject = new GameObject("Other");
                break;
        }

        //Create first sub chunk
        subChunks.Add(new GameObject("Sub chunk " + subChunkIndex));
        subChunks[subChunkIndex].transform.SetParent(parentObject.transform);
        subChunks[subChunkIndex].AddComponent<MeshRenderer>().sharedMaterial = objectData.ObjectMaterial;

        //Parent empty object to chunk
        parentObject.transform.SetParent(chunkData.terrain.gameObject.transform);

        int maxRequiredSpaceX = int.MinValue;
        int maxRequiredSpaceZ = int.MinValue;

        //Get largest for x and z
        foreach (PrefabData prefabData in prefabDataArray) {
            maxRequiredSpaceX = (prefabData.RequiredSpaceX > maxRequiredSpaceX) ? prefabData.RequiredSpaceX : maxRequiredSpaceX;
            maxRequiredSpaceZ = (prefabData.RequiredSpaceZ > maxRequiredSpaceZ) ? prefabData.RequiredSpaceZ : maxRequiredSpaceZ;
        }

        //Largest as to not go out of array range
        int range = Mathf.Max(maxRequiredSpaceX, maxRequiredSpaceZ);

        //Required for chosing of prefab to spawn
        System.Random rand = new System.Random();

        //If an area has already been tested, dont check it again for this object
        bool[,] hasAttempedPlacement = new bool[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];

        //Spawn objects
        for (int x = 0 + range; x < ProceduralTerrain.TerrainResolution - range; x++) {
            for (int y = 0 + range; y < ProceduralTerrain.TerrainResolution - range; y++) {

                //If an attempt to place an object here has been made
                if (hasAttempedPlacement[x, y]) {
                    //Next area
                    continue;
                }

                float terrainHeight = chunkData.terrainHeightMap[x, y];
                bool canPlace = true;

                //Randomly choose an object from the array to place
                PrefabData prefabData = prefabDataArray[(rand.Next(0, prefabDataArray.Length))];

                //Get the required space for that prefab
                int requiredSpaceX = prefabData.RequiredSpaceX;
                int requiredSpaceZ = prefabData.RequiredSpaceZ;

                //Check around this point to check it is flat
                if (canPlace) canPlace = CheckIfPlacableFromMap(new Vector2(x, y), new Vector2(requiredSpaceX, requiredSpaceZ), flatPlacableMap);
                //Check around this point to check there is no other objects
                if (canPlace) canPlace = CheckIfPlacableFromMap(new Vector2(x, y), new Vector2(requiredSpaceX, requiredSpaceZ), validPlacableMap);
                //Randomly decide whether object can be placed based off of density
                if (canPlace) canPlace = CheckIfPlacableFromDensity(density, prefabDataArray.Length);
                //Check against min and max height
                if (canPlace) canPlace = CheckIfPlacableFromHeight(terrainHeight, minSpawnHeight, maxSpawnHeight);
                
                if (canPlace) {
                    //Update map
                    map[x, y] = 1f;

                    //Subtract noise
                    map[x, y] = Mathf.Clamp01(map[x, y] - noiseMap[x, y]);

                    //If greater than threshold
                    if (map[x, y] >= threshold) {

                        //Need to invert as terrain is created inverted 
                        float xToPlace = ((float)y / (float)ProceduralTerrain.TerrainResolution) * (float)ProceduralTerrain.Current.TerrainMapData.TerrainSize;
                        float yToPlace = chunkData.terrainHeightMap[x, y] * (float)ProceduralTerrain.Current.TerrainMapData.TerrainHeight;
                        float zToPlace = ((float)x / (float)ProceduralTerrain.TerrainResolution) * (float)ProceduralTerrain.Current.TerrainMapData.TerrainSize;

                        //Move object to be placed in centre of area just checked
                        xToPlace += requiredSpaceX / 2;
                        zToPlace += requiredSpaceZ / 2;

                        //This area is no longer placable within objects required area
                        UpdatePlacableMap(new Vector2(x, y), new Vector2(requiredSpaceX, requiredSpaceZ), validPlacableMap);
                        //If not detail
                        if (objectType != ObjectType.Detail) {
                            //Update detail placable map with actual size, so details can be placed under within objects required size
                            UpdatePlacableMap(new Vector2(x, y), new Vector2(prefabData.ActualSizeX, prefabData.ActualSizeZ), chunkData.terrainDetailPlacableMap);
                        }
                        //If is detail
                        else {
                            UpdatePlacableMap(new Vector2(x, y), new Vector2(requiredSpaceX, requiredSpaceX), chunkData.terrainDetailPlacableMap);
                        }

                        objectsPlaced++;
                        //Get total verticies of this object's mesh and its children's meshes
                        int localVertices = GetTotalVerticies(prefabData);

                        //If more vertices than unitys max vertex count
                        if (vertexCount + localVertices >= 64000) {
                            //Combine mesh for this subchunk before creating next
                            if (ProceduralTerrain.Current.CombineMeshes && objectType != ObjectType.House) {
                                subChunks[subChunkIndex].AddComponent<CombineMeshes>();
                            }

                            //Reset vertexCount
                            vertexCount = 0;

                            subChunkIndex++;
                            subChunks.Add(new GameObject("Sub chunk " + subChunkIndex));
                            subChunks[subChunkIndex].transform.SetParent(parentObject.transform);
                            subChunks[subChunkIndex].AddComponent<MeshRenderer>().sharedMaterial = objectData.ObjectMaterial;
                        }

                        //Randomly rotate the object to make less uniform
                        Quaternion randomRotation = Quaternion.Euler(0, rand.Next(0, 360), 0);
                        //Instantiate object
                        GameObject spawnedObject = (GameObject)Instantiate(prefabData.ObjectPrefab, new Vector3(xToPlace + chunkData.position.x, yToPlace, zToPlace - chunkData.position.y), randomRotation, subChunks[subChunkIndex].transform);

                        //If a house, add to the chunkData's house list (used for village generation)
                        if (objectType == ObjectType.House) {
                            //Add house to list of houses
                            chunkData.VillageHouseList.Add(spawnedObject);
                        }
                        
                        //Update the verticies for this subchunk 
                        vertexCount += localVertices;

                        //Increment objects spawned
                        objectsSpawned++;
                        //If objects spawns is greater than max per frame, wait untill next frame to spawn next
                        if (objectsSpawned >= maxSpawnsPerFrame) {
                            objectsSpawned = 0;
                            yield return null;
                        }
                    }
                    //Else there is nothing here
                    else {
                        map[x, y] = 0;
                    }
                }

                //If can't place, mark this area as having been attemped for this object
                else {
                    //hasAttempedPlacement[x, y] = true;
                    for (int i = 0; i <= requiredSpaceX; i++) {
                        for (int j = 0; j <= requiredSpaceZ; j++) {
                            hasAttempedPlacement[x + i, y + j] = true;
                        }
                    }
                }

            }
        }

        //Combine meshes for last sub chunk
        if (ProceduralTerrain.Current.CombineMeshes && objectType != ObjectType.House) {
            subChunks[subChunkIndex].AddComponent<CombineMeshes>();
        }

        yield return null;
    }
}
