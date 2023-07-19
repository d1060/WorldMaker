// Erosion Logic
// Original code by Sebastian Lague

// https://www.youtube.com/watch?v=9RHGLZLUuwc
// https://github.com/SebLague/Hydraulic-Erosion

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicErosion
{
    public int mapWidth = 512;
    public int mapHeight = 256;
    public ComputeShader erosion;
    public ComputeShader erosionUpdate;
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

    public void Erode(ComputeBuffer heightMapBuffer)
    {
        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionSettings.erosionBrushRadius; brushY <= erosionSettings.erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionSettings.erosionBrushRadius; brushX <= erosionSettings.erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst <= erosionSettings.erosionBrushRadius * erosionSettings.erosionBrushRadius)
                {
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionSettings.erosionBrushRadius;
                    if (erosionSettings.erosionBrushRadius == 0)
                        brushWeight = 1;

                    weightSum += brushWeight;

                    if (brushWeight != 0)
                    {
                        brushIndexOffsets.Add(brushX);
                        brushIndexOffsets.Add(brushY);
                        brushWeights.Add(brushWeight);
                    }
                }
            }
        }

        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int) * 2);
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Heightmap buffer
        heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

        erosion.SetBuffer(0, "map", heightMapBuffer);

        // Settings
        erosion.SetInt("mapWidth", mapWidth);
        erosion.SetInt("brushLength", brushWeights.Count);
        erosion.SetInt("maxLifetime", erosionSettings.maxLifetime);
        erosion.SetFloat("inertia", erosionSettings.inertia);
        erosion.SetFloat("acceleration", 1 - erosionSettings.inertia);
        erosion.SetFloat("drag", 1 - erosionSettings.inertia);
        erosion.SetFloat("sedimentCapacityFactor", erosionSettings.sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", erosionSettings.minSedimentCapacity);
        erosion.SetFloat("depositSpeed", erosionSettings.depositSpeed);
        erosion.SetFloat("erodeSpeed", erosionSettings.erodeSpeed);
        erosion.SetFloat("evaporateSpeed", erosionSettings.evaporateSpeed);
        erosion.SetFloat("gravity", erosionSettings.gravity);
        erosion.SetFloat("startSpeed", erosionSettings.startSpeed);
        erosion.SetFloat("startWater", erosionSettings.startWater);
        //erosion.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);

        // Run compute shader
        int numTotalInteractions = erosionSettings.numErosionIterations; // Iterations per dipatch cannot be greater then 128 * 65535
        while (numTotalInteractions > 0)
        {
            int currentInteractions = numTotalInteractions;
            if (currentInteractions >= 65535 * 64)
            {
                currentInteractions = 65535 * 64;
            }

            // Generate random indices for droplet placement
            Vector2[] randomUVs = new Vector2[currentInteractions];
            for (int i = 0; i < currentInteractions; i ++)
            {
                float randomX = Random.Range(-1f, 1f);
                float randomY = Random.Range(-1f, 1f);
                float randomZ = Random.Range(-1f, 1f);

                if (randomX * randomX + randomY * randomY + randomZ * randomZ > 1)
                {
                    i--;
                    continue;
                }

                Vector3 sphere = new Vector3(randomX, randomY, randomZ);
                Vector2 uv = sphere.CartesianToPolarRatio(1);

                randomUVs[i] = uv;
            }

            // Send random indices to compute shader
            ComputeBuffer randomUVsBuffer = new ComputeBuffer(randomUVs.Length, sizeof(float) * 2);
            randomUVsBuffer.SetData(randomUVs);
            erosion.SetBuffer(0, "randomUVs", randomUVsBuffer);

            erosion.Dispatch(0, Mathf.CeilToInt(currentInteractions / 64f), 1, 1);

            randomUVsBuffer.Release();
            numTotalInteractions -= 65535 * 64;
        }

        heightMapBuffer.GetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

        //float[] erosionMap1 = new float[TextureManager.instance.HeightMap1.Length];
        //float[] erosionMap2 = new float[TextureManager.instance.HeightMap1.Length];
        //float[] erosionMap3 = new float[TextureManager.instance.HeightMap1.Length];
        //float[] erosionMap4 = new float[TextureManager.instance.HeightMap1.Length];
        //float[] erosionMap5 = new float[TextureManager.instance.HeightMap1.Length];
        //float[] erosionMap6 = new float[TextureManager.instance.HeightMap1.Length];

        //erosionMapBuffer12.GetData(erosionMap1, 0, 0, erosionMap1.Length);
        //erosionMapBuffer12.GetData(erosionMap2, 0, erosionMap2.Length, erosionMap2.Length);
        //erosionMapBuffer34.GetData(erosionMap3, 0, 0, erosionMap3.Length);
        //erosionMapBuffer34.GetData(erosionMap4, 0, erosionMap4.Length, erosionMap4.Length);
        //erosionMapBuffer56.GetData(erosionMap5, 0, 0, erosionMap5.Length);
        //erosionMapBuffer56.GetData(erosionMap6, 0, erosionMap6.Length, erosionMap6.Length);

        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap0.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap1.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap2.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap3.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap4.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap5.png"), 0.01f);

        // Release buffers
        //heightMapBuffer12.Release();
        //heightMapBuffer34.Release();
        //heightMapBuffer56.Release();

        //erosionMapBuffer12.Release();
        //erosionMapBuffer34.Release();
        //erosionMapBuffer56.Release();

        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }

    public void ErodeCPU()
    {
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionSettings.erosionBrushRadius; brushY <= erosionSettings.erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionSettings.erosionBrushRadius; brushX <= erosionSettings.erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst <= erosionSettings.erosionBrushRadius * erosionSettings.erosionBrushRadius)
                {
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionSettings.erosionBrushRadius;
                    if (erosionSettings.erosionBrushRadius == 0)
                        brushWeight = 1;

                    weightSum += brushWeight;

                    if (brushWeight != 0)
                    {
                        brushIndexOffsets.Add(brushX);
                        brushIndexOffsets.Add(brushY);
                        brushWeights.Add(brushWeight);
                    }
                }
            }
        }

        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        float[] erosionMap = new float[TextureManager.instance.HeightMap.Length];

        // Generate random indices for droplet placement
        //for (int i = 0; i < erosionSettings.numErosionIterations; i++)
        //{
        //    float randomX = Random.Range(0f, mapWidth - 0.000001f);
        //    float randomY = Random.Range(0f, mapHeight - 0.000001f);
        //    int randomZ = Random.Range(0, 6);

        //    ErodeDroplet(randomX, randomY, randomZ, brushIndexOffsets, brushWeights, erosionMap1, erosionMap2, erosionMap3, erosionMap4, erosionMap5, erosionMap6);
        //}

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapWidth; y++)
            {
                for (int z = 0; z < 6; z++)
                {
                    ErodeDroplet(x, y, z, brushIndexOffsets, brushWeights, erosionMap);
                }
            }
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapWidth; y++)
            {
                int index = x + y * mapWidth;

                float erosionValue1 = erosionMap[index];

                TextureManager.instance.HeightMap[index] -= erosionValue1;
            }
        }

        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap0.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap1.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap2.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap3.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap4.png"), 0.01f);
        //ImageTools.SaveTextureCubemapFaceFloatArray(erosionMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "erosionMap5.png"), 0.01f);
    }

    void ErodeDroplet(float x, float y, int z, List<int> brushIndices, List<float> brushWeights, float[] erosionMap)
    {
        if (x >= mapWidth || y >= mapHeight || z >= 6)
            return;

        Vector3 coordinates = new Vector3(x, y, z);

        float dirX = 0;
        float dirY = 0;
        float speed = erosionSettings.startSpeed;
        float water = erosionSettings.startWater;
        float sediment = 0;

        for (int lifetime = 0; lifetime < erosionSettings.maxLifetime; lifetime++)
        {
            int nodeX = (int)(coordinates.x + 0.5f);
            int nodeY = (int)(coordinates.y + 0.5f);

            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float cellOffsetX = coordinates.x - nodeX;
            float cellOffsetY = coordinates.y - nodeY;

            // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
            float height = 0;
            Vector2 gradient = new Vector2();
            CalculateHeightAndGradient(coordinates, ref gradient, ref height);

            // Update the droplet's direction and position (move position 1 unit regardless of speed)
            dirX = (dirX * erosionSettings.inertia - gradient.x * (1 - erosionSettings.inertia));
            dirY = (dirY * erosionSettings.inertia - gradient.y * (1 - erosionSettings.inertia));

            // Normalize direction
            float len = Mathf.Max(0.0001f, Mathf.Sqrt(dirX * dirX + dirY * dirY));
            dirX /= len;
            dirY /= len;

            Vector3 newCoordinates = Cubemap.getNewCoordinates(coordinates, dirX, dirY, mapWidth);
            shiftGradient(coordinates, newCoordinates, ref dirX, ref dirY);
            coordinates = newCoordinates;

            // Find the droplet's new height and calculate the deltaHeight
            float newHeight = 0;
            Vector2 newGradient = new Vector2();
            CalculateHeightAndGradient(coordinates, ref newGradient, ref newHeight);
            float deltaHeight = newHeight - height;

            // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * erosionSettings.sedimentCapacityFactor, erosionSettings.minSedimentCapacity);

            // If carrying more sediment than capacity, or if flowing uphill:
            if (sediment > sedimentCapacity || deltaHeight > 0)
            {
                // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * erosionSettings.depositSpeed;
                sediment -= amountToDeposit;

                // Add the sediment to the four nodes of the current cell using bilinear interpolation
                // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                Int3 eastCoordinates = Cubemap.getRightIntCoordinates(new Int3(coordinates), mapWidth);
                Int3 northCoordinates = Cubemap.getTopIntCoordinates(new Int3(coordinates), mapWidth);
                Int3 northEastCoordinates = Cubemap.getTopRightIntCoordinates(new Int3(coordinates), mapWidth);

                updateErosionMapAtCoordinates(new Int3(coordinates), amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY), erosionMap);
                updateErosionMapAtCoordinates(eastCoordinates, amountToDeposit * (1 - cellOffsetX) * cellOffsetY, erosionMap);
                updateErosionMapAtCoordinates(northCoordinates, amountToDeposit * cellOffsetX * (1 - cellOffsetY), erosionMap);
                updateErosionMapAtCoordinates(northEastCoordinates, amountToDeposit * cellOffsetX * cellOffsetY, erosionMap);
            }
            else
            {
                // Erode a fraction of the droplet's current carry capacity.
                // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erosionSettings.erodeSpeed, -deltaHeight);

                for (int i = 0; i < brushWeights.Count; i++)
                {
                    int brushX = brushIndices[i * 2];
                    int brushY = brushIndices[i * 2 + 1];

                    Vector3 erodeCoordinates = Cubemap.getNewCoordinates(coordinates, brushX, brushY, mapWidth);
                    //shiftGradient(coordinates, erodeCoordinates, dirX, dirY);

                    float weightedErodeAmount = amountToErode * brushWeights[i];
                    float mapValue = getMapValueAtCoordinates(new Int3(coordinates));
                    float deltaSediment = (mapValue < weightedErodeAmount) ? mapValue : weightedErodeAmount;
                    updateErosionMapAtCoordinates(new Int3(coordinates), -deltaSediment, erosionMap);
                    sediment += deltaSediment;
                }
            }

            // Update droplet's speed and water content
            speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * erosionSettings.gravity));
            water *= (1 - erosionSettings.evaporateSpeed);
        }
    }

    void CalculateHeightAndGradient(Vector3 coordinates, ref Vector2 gradient, ref float height)
    {
        int coordX = (int)(coordinates.x + 0.5);
        int coordY = (int)(coordinates.y + 0.5);

        Vector3 newCoords = Cubemap.getNewCoordinates(new Vector3(coordX, coordY, coordinates.z), 0, 0, mapWidth);
        Int3 coord = new Int3(newCoords);
        Int3 coordR = Cubemap.getRightIntCoordinates(coord, mapWidth);
        Int3 coordT = Cubemap.getTopIntCoordinates(coord, mapWidth);
        Int3 coordL = Cubemap.getLeftIntCoordinates(coord, mapWidth);
        Int3 coordB = Cubemap.getBottomIntCoordinates(coord, mapWidth);
        Int3 coordTR = Cubemap.getTopRightIntCoordinates(coord, mapWidth);
        Int3 coordTL = Cubemap.getTopLeftIntCoordinates(coord, mapWidth);
        Int3 coordBR = Cubemap.getBottomRightIntCoordinates(coord, mapWidth);
        Int3 coordBL = Cubemap.getBottomLeftIntCoordinates(coord, mapWidth);

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = coordinates.x + 0.5f - coordX;
        float y = coordinates.y + 0.5f - coordY;

        float heightC = getMapValueAtCoordinates(coord);
        float heightR = getMapValueAtCoordinates(coordR);
        float heightL = getMapValueAtCoordinates(coordL);
        float heightT = getMapValueAtCoordinates(coordT);
        float heightB = getMapValueAtCoordinates(coordB);
        float heightTR = getMapValueAtCoordinates(coordTR);
        float heightTL = getMapValueAtCoordinates(coordTL);
        float heightBR = getMapValueAtCoordinates(coordBR);
        float heightBL = getMapValueAtCoordinates(coordBL);

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        //float gradientX = (heightR - heightC) * (1 - y) + (heightTR - heightT) * y;
        //float gradientY = (heightT - heightC) * (1 - x) + (heightTR - heightR) * x;
        float gradientX = 0;
        float gradientY = 0;
        if (heightR < heightL && heightT < heightB)
        {
            gradientX = (heightR - heightC) * (1 - y) + (heightTR - heightT) * y;
            gradientY = (heightT - heightC) * (1 - x) + (heightTR - heightR) * x;
        }
        else if (heightR >= heightL && heightT < heightB)
        {
            gradientX = (heightR - heightC) * (1 - y) + (heightTR - heightB) * y;
            gradientY = (heightB - heightC) * (1 - x) + (heightBR - heightR) * x;
        }
        else if (heightR < heightL && heightT >= heightB)
        {
            gradientX = (heightL - heightC) * (1 - y) + (heightTL - heightT) * y;
            gradientY = (heightT - heightC) * (1 - x) + (heightTL - heightR) * x;
        }
        else if (heightR >= heightL && heightT >= heightB)
        {
            gradientX = (heightL - heightC) * (1 - y) + (heightBL - heightB) * y;
            gradientY = (heightB - heightC) * (1 - x) + (heightBL - heightR) * x;
        }

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        height = heightC * (1 - x) * (1 - y) + heightR * x * (1 - y) + heightT * (1 - x) * y + heightTR * x * y;
        gradient = new Vector2(gradientX, gradientY);
    }

    void shiftGradient(Vector3 prevCoordinates, Vector3 newCoordinates, ref float dirX, ref float dirY)
    {
        if (prevCoordinates.z == 0 && newCoordinates.z == 4)
        {
            float prevX = dirX;
            dirX = dirY;
            dirY = -prevX;
        }
        else if (prevCoordinates.z == 2 && newCoordinates.z == 4)
        {
            float prevX = dirX;
            dirX = -dirY;
            dirY = prevX;
        }
        else if (prevCoordinates.z == 3 && newCoordinates.z == 4)
        {
            dirX = -dirX;
            dirY = -dirY;
        }

        else if (prevCoordinates.z == 4 && newCoordinates.z == 0)
        {
            float prevX = dirX;
            dirX = -dirY;
            dirY = prevX;
        }
        else if (prevCoordinates.z == 4 && newCoordinates.z == 2)
        {
            float prevX = dirX;
            dirX = dirY;
            dirY = -prevX;
        }
        else if (prevCoordinates.z == 4 && newCoordinates.z == 3)
        {
            dirX = -dirX;
            dirY = -dirY;
        }

        else if (prevCoordinates.z == 0 && newCoordinates.z == 5)
        {
            float prevX = dirX;
            dirX = -dirY;
            dirY = prevX;
        }
        else if (prevCoordinates.z == 2 && newCoordinates.z == 5)
        {
            float prevX = dirX;
            dirX = dirY;
            dirY = -prevX;
        }
        else if (prevCoordinates.z == 3 && newCoordinates.z == 5)
        {
            dirX = -dirX;
            dirY = -dirY;
        }

        else if (prevCoordinates.z == 5 && newCoordinates.z == 0)
        {
            float prevX = dirX;
            dirX = dirY;
            dirY = -prevX;
        }
        else if (prevCoordinates.z == 5 && newCoordinates.z == 2)
        {
            float prevX = dirX;
            dirX = -dirY;
            dirY = prevX;
        }
        else if (prevCoordinates.z == 5 && newCoordinates.z == 3)
        {
            dirX = -dirX;
            dirY = -dirY;
        }
    }

    float getMapValueAtCoordinates(Int3 coordinates)
    {
        int index = coordinates.x + coordinates.y * mapWidth;

        if (index < 0 || index >= TextureManager.instance.HeightMap.Length)
            return 0;

        return TextureManager.instance.HeightMap[index];
    }

    void updateErosionMapAtCoordinates(Int3 coords, float amount, float[] erosionMap)
    {
        int index = coords.x + coords.y * mapWidth;

        if (index < 0 || index >= TextureManager.instance.HeightMap.Length)
            return;

        erosionMap[index] += amount;
    }
}
