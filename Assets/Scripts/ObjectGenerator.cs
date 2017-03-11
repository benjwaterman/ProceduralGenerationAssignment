using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : ScriptableObject {

    public static void GenerateObjects(ObjectType objectType, ObjectData objectData, ChunkData chunkData) {

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

        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(objectData.ObjectNoiseData, ProceduralTerrain.TerrainResolution, position);
        bool[,] placableMap = ProceduralTerrain.CalculateFlatTerrain(chunkData.terrainHeightMap, flatSearchRange, flatSens);

        GameObject parentObject;
        switch (objectType) {
            case ObjectType.House:
                parentObject = new GameObject("Houses");
                break;

            case ObjectType.Tree:
                parentObject = new GameObject("Trees");
                break;

            default:
                parentObject = new GameObject("Other");
                break;
        }

        foreach (PrefabData objData in prefabDataArray) {
            int requiredSpaceX = objData.RequiredSpaceX;
            int requiredSpaceZ = objData.RequiredSpaceZ;

            //Largest as to not go out of array range
            int range = Mathf.Max(requiredSpaceX, requiredSpaceZ);

            //Required for chosing of prefab to spawn
            System.Random rand = new System.Random();

            //Spawn based on density
            for (int x = 0 + range; x < ProceduralTerrain.TerrainResolution - range; x++) {
                for (int y = 0 + range; y < ProceduralTerrain.TerrainResolution - range; y++) {
                    float terrainHeight = chunkData.terrainHeightMap[x, y];

                    bool canPlace = true;

                    //Check around this point to check there is room
                    for (int i = 0; i <= requiredSpaceX; i++) {
                        for (int j = 0; j <= requiredSpaceZ; j++) {
                            //If canPlace = false, no point checking other areas
                            if (canPlace) {

                                if (placableMap[x + i, y + j]) {
                                    canPlace = true;
                                }
                                else {
                                    canPlace = false;
                                }
                            }
                        }
                    }

                    //Randomly decide whether object can be placed
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
                        //Update house map
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

                            //This area is no longer placable
                            for (int i = 0; i <= requiredSpaceX; i++) {
                                for (int j = 0; j <= requiredSpaceZ; j++) {
                                    placableMap[x + i, y + j] = false;
                                }
                            }

                            Quaternion randomRotation = Quaternion.Euler(0, rand.Next(0, 360), 0);
                            Instantiate(objData.ObjectPrefab, new Vector3(xToPlace + chunkData.position.x, yToPlace, zToPlace + chunkData.position.y), randomRotation, parentObject.transform);
                        }
                        //Else there is no house here
                        else {
                            map[x, y] = 0;
                        }
                    }
                }
            }
        }
    }
}
