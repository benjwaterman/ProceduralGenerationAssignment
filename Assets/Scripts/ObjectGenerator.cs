using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : ScriptableObject {

    public static int TreeCounter = -1;
    public static int HouseCounter = -1;
    public static int DetailCounter = -1;

    public void SpawnObjects(ObjectType objectType, ObjectData objectData, ChunkData chunkData) {

    }

    public IEnumerator GenerateObjects(ObjectType objectType, ObjectData objectData, ChunkData chunkData) {

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

        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(objectData.ObjectNoiseData, ProceduralTerrain.TerrainResolution, position);
        //Placable map for this object type based off of terrain flatness
        bool[,] flatPlacableMap = ProceduralTerrain.Current.CalculateFlatTerrain(chunkData.terrainHeightMap, flatSearchRange, flatSens);
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
        //Change material
        ChangeColourAtlas(objectData.ObjectMaterial, objectData.PrimaryColour, objectData.SecondaryColour, objectData.TertiaryColour, objectData.QuaternaryColour);

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

        //Spawn based on density
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

                int requiredSpaceX = prefabData.RequiredSpaceX;
                int requiredSpaceZ = prefabData.RequiredSpaceZ;

                //Check around this point to check it is flat
                for (int i = 0; i <= requiredSpaceX; i++) {
                    for (int j = 0; j <= requiredSpaceZ; j++) {
                        //If canPlace = false, no point checking other areas
                        if (canPlace) {

                            if (flatPlacableMap[x + i, y + j]) {
                                canPlace = true;
                            }
                            else {
                                canPlace = false;
                            }
                        }
                    }
                }

                //Check around this point to check there is no other objects
                for (int i = 0; i <= requiredSpaceX; i++) {
                    for (int j = 0; j <= requiredSpaceZ; j++) {
                        if (canPlace) {

                            if (validPlacableMap[x + i, y + j]) {
                                canPlace = true;
                            }
                            else {
                                canPlace = false;
                            }
                        }
                    }
                }

                //Randomly decide whether object can be placed based off of density
                if (canPlace) {
                    float number = Random.Range(0f, 1f);
                    //Reduce chance so each object within array has equal chance of being spawned 
                    number /= prefabDataArray.Length;

                    if (number > density) {
                        canPlace = false;
                    }
                }

                //Check against min and max height
                if (canPlace) {
                    //If its not within range, it cannot be placed here
                    if (!(terrainHeight >= minSpawnHeight && terrainHeight <= maxSpawnHeight)) {
                        canPlace = false;
                    }
                }

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

                        //If not detail
                        if (objectType != ObjectType.Detail) {

                            //This area is no longer placable
                            for (int i = 0; i <= requiredSpaceX; i++) {
                                for (int j = 0; j <= requiredSpaceZ; j++) {
                                    validPlacableMap[x + i, y + j] = false;
                                }
                            }

                            //For detail placable map
                            for (int i = 0; i <= prefabData.ActualSizeX; i++) {
                                for (int j = 0; j <= prefabData.ActualSizeZ; j++) {
                                    chunkData.terrainDetailPlacableMap[x + i, y + j] = false;
                                }
                            }
                        }
                        //If is detail
                        else {
                            for (int i = 0; i <= requiredSpaceX; i++) {
                                for (int j = 0; j <= requiredSpaceZ; j++) {
                                    validPlacableMap[x + i, y + j] = false;
                                    chunkData.terrainPlacableMap[x + i, y + j] = false;
                                }
                            }
                        }

                        objectsPlaced++;
                        int localVertices = 0;
                        MeshFilter[] meshFilters = prefabData.ObjectPrefab.GetComponentsInChildren<MeshFilter>();
                        foreach(MeshFilter meshFilter in meshFilters) {
                            localVertices += meshFilter.sharedMesh.vertexCount;
                        }
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
                        Quaternion randomRotation = Quaternion.Euler(0, rand.Next(0, 360), 0);
                        GameObject spawnedObject = (GameObject)Instantiate(prefabData.ObjectPrefab, new Vector3(xToPlace + chunkData.position.x, yToPlace, zToPlace - chunkData.position.y), randomRotation, subChunks[subChunkIndex].transform);
                        if(objectType == ObjectType.House) {
                            //Add house to list of houses
                            chunkData.VillageHouseList.Add(spawnedObject);
                        }

                        vertexCount += localVertices;

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

        //If combine meshes is enabled and not houses (houses are done later)
        //if (ProceduralTerrain.Current.CombineMeshes && objectType != ObjectType.House) {
        //    //Combine meshes of objects 
        //    foreach (GameObject go in subChunks) {
        //        go.AddComponent<CombineMeshes>();
        //        //Combine meshes over series of frames rather than in one go
        //        yield return null;
        //    }
        //}
    }

    void ChangeColourAtlas(Material material, Color primaryColour, Color secondaryColour, Color tertiaryColour, Color quaternaryColour) {//, Color tertiaryColour = Color.black, Color quaternaryColour = new Color()) {

        //If no colour atlas is assigned
        if(material.mainTexture == null) {
            return;
        }

        //Get texture
        Texture2D texture = (Texture2D)material.mainTexture;
        Color[] pixels = texture.GetPixels();

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
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }
}
