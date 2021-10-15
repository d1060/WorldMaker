// Erosion Logic
// Original code by Sebastian Lague

// https://www.youtube.com/watch?v=9RHGLZLUuwc
// https://github.com/SebLague/Hydraulic-Erosion

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicErosion
{
    public int mapWidth = 512;
    public int mapHeight = 256;
    public ComputeShader erosion;
    public ErosionSettings erosionSettings;

    struct int2
    {
        int x;
        int y;

        public int2(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }

    #region Singleton
    static HydraulicErosion myInstance = null;

    HydraulicErosion()
    {
    }

    public static HydraulicErosion instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new HydraulicErosion();
            return myInstance;
        }
    }
    #endregion

    public void Erode(ref float[] map)
    {
        //int mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        int numThreads = erosionSettings.numErosionIterations / 1024;
        if (numThreads <= 0) numThreads = 1;

        // Create brush
        List<int2> brushIndexOffsets = new List<int2>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionSettings.erosionBrushRadius; brushY <= erosionSettings.erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionSettings.erosionBrushRadius; brushX <= erosionSettings.erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst <= erosionSettings.erosionBrushRadius * erosionSettings.erosionBrushRadius)
                {
                    brushIndexOffsets.Add(new int2(brushX, brushY));
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionSettings.erosionBrushRadius;
                    if (erosionSettings.erosionBrushRadius == 0)
                        brushWeight = 1;

                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int)*2);
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[erosionSettings.numErosionIterations];
        for (int i = 0; i < erosionSettings.numErosionIterations; i++)
        {
            int randomX = Random.Range(0, mapWidth);
            int randomY = Random.Range(0, mapHeight);
            randomIndices[i] = randomY * mapWidth + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        erosion.SetBuffer(0, "map", mapBuffer);

        // Settings
        erosion.SetInt("mapWidth", mapWidth);
        erosion.SetInt("mapHeight", mapHeight);
        erosion.SetInt("brushLength", brushWeights.Count);
        erosion.SetInt("maxLifetime", erosionSettings.maxLifetime);
        erosion.SetFloat("inertia", erosionSettings.inertia);
        erosion.SetFloat("sedimentCapacityFactor", erosionSettings.sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", erosionSettings.minSedimentCapacity);
        erosion.SetFloat("depositSpeed", erosionSettings.depositSpeed);
        erosion.SetFloat("erodeSpeed", erosionSettings.erodeSpeed);
        erosion.SetFloat("evaporateSpeed", erosionSettings.evaporateSpeed);
        erosion.SetFloat("gravity", erosionSettings.gravity);
        erosion.SetFloat("startSpeed", erosionSettings.startSpeed);
        erosion.SetFloat("startWater", erosionSettings.startWater);

        int numThreadsX = Mathf.CeilToInt(mapWidth / 8f);
        int numThreadsY = Mathf.CeilToInt(mapHeight / 8f);

        // Run compute shader
        erosion.Dispatch(0, numThreadsX, numThreadsY, 1);
        mapBuffer.GetData(map);

        // Release buffers
        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }
}
