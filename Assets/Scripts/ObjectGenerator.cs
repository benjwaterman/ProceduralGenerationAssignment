﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : ScriptableObject {

    public static void GenerateObjects(ObjectType objectType, out float[,] map, GameObject[] prefabsToSpawn, int seed = 0, Vector2 requiredSpace = default(Vector2), float minSpawnHeight = 0f, float maxSpawnHeight = 1f, float density = 0.5f, float threshold = 0.5f) {

        //If no required space was given, default to 1x1
        if (requiredSpace == Vector2.zero) {
            requiredSpace = new Vector2(1, 1);
        }

        map = new float[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];

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

        float[,] noiseMap = ProceduralTerrain.Current.GenerateNoiseMap(seed);

        int requiredSpaceX = (int)requiredSpace.x;
        int requiredSpaceZ = (int)requiredSpace.y;

        //Largest as to not go out of array range
        int range = Mathf.Max(requiredSpaceX, requiredSpaceZ);


        //Spawn based on density
        for (int x = 0 + range; x < ProceduralTerrain.TerrainResolution - range; x++) {
            for (int y = 0 + range; y < ProceduralTerrain.TerrainResolution - range; y++) {
                float terrainHeight = ProceduralTerrain.Current.terrainHeightMap[x, y];

                bool canPlace = true;

                //Check around this point to check there is room
                for (int i = 0; i <= requiredSpaceX; i++) {
                    for (int j = 0; j <= requiredSpaceZ; j++) {
                        //If canPlace = false, no point checking other areas
                        if (canPlace) {

                            if (ProceduralTerrain.Current.placableArea[x + i, y + j]) {
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

                    //If greater than density
                    if (map[x, y] >= threshold) {

                        //Need to invert as terrain is created inverted 
                        float xToPlace = ((float)y / (float)ProceduralTerrain.TerrainResolution) * (float)ProceduralTerrain.Current.TerrainSize;
                        float yToPlace = ProceduralTerrain.Current.terrainHeightMap[x, y] * (float)ProceduralTerrain.Current.TerrainHeight;
                        float zToPlace = ((float)x / (float)ProceduralTerrain.TerrainResolution) * (float)ProceduralTerrain.Current.TerrainSize;

                        //Move object to be placed in centre of area just checked
                        xToPlace += requiredSpaceX / 2;
                        zToPlace += requiredSpaceZ / 2;

                        //This area is no longer placable
                        for (int i = 0; i <= requiredSpaceX; i++) {
                            for (int j = 0; j <= requiredSpaceZ; j++) {
                                ProceduralTerrain.Current.placableArea[x + i, y + j] = false;
                            }
                        }

                        Instantiate(prefabsToSpawn[0], new Vector3(xToPlace, yToPlace, zToPlace), Quaternion.identity, parentObject.transform);
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