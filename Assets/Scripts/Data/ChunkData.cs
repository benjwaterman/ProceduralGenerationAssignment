using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData {

    public Vector2 position { get; private set; }
    public Terrain terrain { get; private set; }
    public GameObject terrainGameObject { get; private set; }
    public TerrainData terrainData { get; private set; }
    public Terrain LeftNeighbour, RightNeighbour, TopNeighbour, BottomNeighbour;
    public float[,] terrainHeightMap { get; private set; }
    //General area around each object
    public bool[,] terrainPlacableMap = new bool[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];
    //The specific size of each object (eg. trunk of a tree, so details can be placed underneath)
    public bool[,] terrainDetailPlacableMap = new bool[ProceduralTerrain.TerrainResolution, ProceduralTerrain.TerrainResolution];

    public List<GameObject> VillageCenterList = new List<GameObject>();
    public List<VillageData> VillageList = new List<VillageData>();
    public List<VillageHouseData> VillageHouseList = new List<VillageHouseData>();

    //For grass
    public DetailPrototype[] detailPrototype;

    public ChunkData(Vector2 pos) {

        position = pos;

        //Initialise arrays
        for (int i = 0; i < ProceduralTerrain.TerrainResolution; i++) {
            for (int j = 0; j < ProceduralTerrain.TerrainResolution; j++) {
                terrainPlacableMap[i, j] = true;
                terrainDetailPlacableMap[i, j] = true;
            }
        }

        GenerateChunk();
    }

    void GenerateChunk() {
        terrainHeightMap = ProceduralTerrain.CalculateHeightMap(this);
        ProceduralTerrain.Current.GenerateTerrain(this);
        ProceduralTerrain.Current.GenerateHouses(this);
        ProceduralTerrain.Current.GenerateTrees(this);
        ProceduralTerrain.Current.GenerateVillageConnections(this); //Must be called after trees to avoid clipping
        ProceduralTerrain.Current.GenerateDetails(this);
        SplatMapGenerator.GenerateSplatMap(this);
        ProceduralTerrain.Current.GenerateGrass(this);
    }

    public void AssignTerrainData(TerrainData terrainData) {
        this.terrainData = terrainData;
    }

    public void AssignTerrain(Terrain terrain) {
        this.terrain = terrain;
        this.terrain.name = "Terrain chunk (X = " + position.x + ", Y = " + position.y + ")";
    }

    public void AssignTerrainGameObject(GameObject terrainGameObject) {
        this.terrainGameObject = terrainGameObject;
    }
}
