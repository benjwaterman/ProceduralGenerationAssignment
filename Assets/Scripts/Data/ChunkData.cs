using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData {

    public Vector2 position;
    public Terrain terrain { get; private set; }
    public GameObject terrainGameObject { get; private set; }
    public TerrainData terrainData { get; private set; }
    public float[,] terrainHeightMap { get; private set; }

    public ChunkData(Vector2 pos) {

        position = pos;

        terrainHeightMap = terrainHeightMap = ProceduralTerrain.CalculateHeightMap(this);
        ProceduralTerrain.GenerateTerrain(this);
        ProceduralTerrain.GenerateHouses(this);
        ProceduralTerrain.GenerateTrees(this);
        ProceduralTerrain.GenerateGrass(this);
        SplatMapGenerator.GenerateSplatMap(this);
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
