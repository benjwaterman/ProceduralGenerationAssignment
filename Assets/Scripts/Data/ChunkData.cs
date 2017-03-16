using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData {

    public Vector2 position;
    public Terrain terrain { get; private set; }
    public GameObject terrainGameObject { get; private set; }
    public TerrainData terrainData { get; private set; }
    public float[,] terrainHeightMap { get; private set; }
    //General area around each object
    public bool[,] terrainPlacableMap = new bool[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];
    //The specific size of each object (eg. trunk of a tree, so details can be placed underneath)
    public bool[,] terrainDetailPlacableMap = new bool[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];

    public ChunkData(Vector2 pos) {

        position = pos;

        //Initialise all to true
        for (int i = 0; i < ProceduralTerrain.TerrainResolution; i++) {
            for (int j = 0; j < ProceduralTerrain.TerrainResolution; j++) {
                terrainPlacableMap[i, j] = true;
                terrainDetailPlacableMap[i, j] = true;
            }
        }

        terrainHeightMap = terrainHeightMap = ProceduralTerrain.CalculateHeightMap(this);
        ProceduralTerrain.GenerateTerrain(this);
        ProceduralTerrain.GenerateHouses(this);
        ProceduralTerrain.GenerateTrees(this);
        ProceduralTerrain.GenerateDetails(this);
        SplatMapGenerator.GenerateSplatMap(this);
        ProceduralTerrain.GenerateGrass(this);

    }

    public void AssignTerrainData(TerrainData terrainData) {
        this.terrainData = terrainData;
    }

    public void AssignTerrain(Terrain terrain) {
        this.terrain = terrain;
    }

    public void AssignTerrainGameObject(GameObject terrainGameObject) {
        this.terrainGameObject = terrainGameObject;
    }
}
