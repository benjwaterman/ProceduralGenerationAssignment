using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChunkDistanceChecker : MonoBehaviour {

    ChunkData currentChunk;

    void Start() {
        StartCoroutine(CheckDistanceToChunks());
    }

    //Check distance every .1s 
    IEnumerator CheckDistanceToChunks() {
        while (true) {

            foreach (ChunkData chunk in ProceduralTerrain.Current.ChunkList) {
                if (Vector3.Distance(GetPlayerPosition(), new Vector3(chunk.position.x, 0, chunk.position.y)) <= 500) {
                    currentChunk = chunk;

                    ProceduralTerrain.Current.CreateNeighbourChunks(chunk);

                    break;
                }

                //Debug.Log(GetPlayerPosition());
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    //Change player position so it works with the chunks positioning
    Vector3 GetPlayerPosition() {

        Vector3 playerPosition;
        playerPosition = gameObject.transform.position;

        playerPosition.x -= ProceduralTerrain.Current.TerrainMapData.TerrainSize / 2;
        playerPosition.z -= ProceduralTerrain.Current.TerrainMapData.TerrainSize / 2;

        return playerPosition;
    }
}
