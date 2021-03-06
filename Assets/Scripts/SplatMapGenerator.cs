﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // used for Sum of array

//Based on https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/
public class SplatMapGenerator {

    public static void GenerateSplatMap(ChunkData chunkData) {
        // Get the attached terrain component
        Terrain terrain = chunkData.terrain;

        // Get a reference to the terrain data
        TerrainData terrainData = chunkData.terrainData;

        TextureData[] textureData = ProceduralTerrain.Current.TerrainMapData.TerrainTextures;

        float constantWeight = ProceduralTerrain.Current.TerrainMapData.Texture1ConstantWeight;
        float steepnessScale = ProceduralTerrain.Current.TerrainMapData.TextureSteepnessScaleFactor;
        float textureBlendAmount = ProceduralTerrain.Current.TerrainMapData.TextureBlendAmount;

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++) {
            for (int x = 0; x < terrainData.alphamapWidth; x++) {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                // CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT

                height = height / ProceduralTerrain.Current.TerrainMapData.TerrainHeight;

                //For steeper terrains, last value in array is always for steepness
                splatWeights[textureData.Length - 1] = Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / steepnessScale));

                //If is not too steep apply other textures
                if (splatWeights[textureData.Length - 1] < 0.8f) {
                    //Go backwards through textures
                    for (int i = 0; i < textureData.Length; i++) {
                        if (height > textureData[i].TextureStartHeight) {
                            //If not last element in array and height value is less than the next textures start height
                            if (i < textureData.Length - 1 && textureData[i + 1].TextureStartHeight > height) {
                                splatWeights[i] = 1;
                            }
                        }
                    }
                }

                //if(height - 0.1f > 0.1f)
                //for (int i = 0; i < splatWeights.Length - 1; i++) {

                //    splatWeights[i] -= textureBlendAmount;
                //    splatWeights[i + 1] += textureBlendAmount;
                //}

                // Texture[0] has constant influence
                //splatWeights[0] = constantWeight;

                // Texture[1] is stronger at lower altitudes
                //splatWeights[1] = 1; //Mathf.Clamp01(1 - height / ProceduralTerrain.Current.TerrainMapData.TerrainHeight);

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                //splatWeights[2] = 1.0f - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / steepnessScale));

                // Texture[3] steeper terrains
                //splatWeights[3] = Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / steepnessScale));//Mathf.Clamp01(height / ProceduralTerrain.Current.TerrainMapData.TerrainHeight); //* Mathf.Clamp01(normal.z);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++) {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
