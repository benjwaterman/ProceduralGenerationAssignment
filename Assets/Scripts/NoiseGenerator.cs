using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator  {

    public static float[,] GenerateNoiseMap(NoiseData noiseData, int res = ProceduralTerrain.TerrainResolution, Vector2 position = default(Vector2)) {

        int seed = noiseData.Seed;
        float scale = noiseData.Scale;
        int octaves = noiseData.Octaves;
        float persistance = noiseData.Persistance;
        float lacunarity = noiseData.Lacunarity;

        //Store temp heightmap data
        float[,] noiseMap = new float[res, res];

        //Get a random modifier based on seed
        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxheight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            float randX = rand.Next(-100000, 100000) + position.y;
            float randY = rand.Next(-100000, 100000) - position.x;
            octaveOffsets[i] = new Vector2(randX, randY);

            maxheight += amplitude;
            amplitude *= persistance;
        }

        //Generate the noisemap
        for (int x = 0; x < res; x++) {
            for (int y = 0; y < res; y++) {

                amplitude = 1;
                frequency = 1;

                float heightValue = 0;

                for (int i = 0; i < octaves; i++) {

                    float xCoord = (x - octaveOffsets[i].x) / scale * frequency;
                    float yCoord = (y - octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    //Set height to perlin value
                    heightValue += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                //Assign value
                noiseMap[x, y] = heightValue;
            }
        }

        return noiseMap;
    }
}
