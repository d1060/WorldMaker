using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PluvialErosion
{
    #region Singleton
    static PluvialErosion myInstance = null;

    PluvialErosion()
    {
    }

    public static PluvialErosion instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new PluvialErosion();
            return myInstance;
        }
    }
    #endregion

    public Map map;
    public int numPasses = 50;
    public int numRiverSources = 200;
    public float waterScale = 0.1f;
    public float waterFixedAmount = 0.1f;
    public float gravity = 4;
    public float sedimentCapacity = 1;
    public float minTiltAngle = 0.01f;
    public float sedimentDissolvingConstant = 1;
    public float sedimentDepositionConstant = 1;
    public float waterEvaporationRetention = 0.75f;
    public float maxErosionDepth = 0.1f;
    public int mapWidth;
    public int mapHeight;
    public float waterLevel;

    float[] riverSourcesMap;
    float[] waterHeightMap;
    float[] sedimentMap;
    float4[] outflowMap;
    float2[] velocityMap;
    System.Random random;

    struct float4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public float4(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }
    }

    struct float2
    {
        public float x;
        public float y;

        public float2(float _x, float _y)
        {
            x = _x;
            y = _y;
        }
    }

    public void Init(ref float[] heightMap, ref float[] humidityMap)
    {
        riverSourcesMap = new float[mapWidth * mapHeight];
        waterHeightMap = new float[mapWidth * mapHeight];
        sedimentMap = new float[mapWidth * mapHeight];
        outflowMap = new float4[mapWidth * mapHeight];
        velocityMap = new float2[mapWidth * mapHeight];
        random = new System.Random();

        for (int i = 0; i < numRiverSources; i++)
        {
            int randomX = random.Next(mapWidth);
            int randomY = random.Next(mapHeight);
            int index = randomY * mapWidth + randomX;
            float height = heightMap[index];
            if (height <= waterLevel)
            {
                i--;
                continue;
            }
            float riverSourceStrength = (float)(random.NextDouble() * humidityMap[index]);
            riverSourcesMap[index] = riverSourceStrength;
        }
    }

    public void ErodeStep(ref float[] heightMap, ref float[] humidityMap)
    {
        // Calculates outflow.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + mapWidth * y;
                float height = heightMap[index];
                float waterHeight = waterHeightMap[index];

                int leftX = x - 1;
                int rightX = x + 1;
                int topY = y + 1;
                int bottomY = y - 1;

                if (leftX < 0) leftX += mapWidth;
                if (rightX >= mapWidth) rightX %= mapWidth;
                if (topY >= mapHeight) topY = mapHeight - 1;
                if (bottomY < 0) bottomY = 0;

                int indexLeft = leftX + mapWidth * y;
                int indexRight = rightX + mapWidth * y;
                int indexTop = x + mapWidth * topY;
                int indexBottom = x + mapWidth * bottomY;

                // Update water Height.
                float d1 = waterScale * (humidityMap[index] + riverSourcesMap[index]) + waterFixedAmount + waterHeightMap[index];

                float outflowLeft = outflowMap[index].x + gravity * (height + waterHeight - heightMap[indexLeft] - waterHeightMap[indexLeft]);
                float outflowRight = outflowMap[index].y + gravity * (height + waterHeight - heightMap[indexRight] - waterHeightMap[indexRight]);
                float outflowTop = outflowMap[index].z + gravity * (height + waterHeight - heightMap[indexTop] - waterHeightMap[indexTop]);
                float outflowBottom = outflowMap[index].w + gravity * (height + waterHeight - heightMap[indexBottom] - waterHeightMap[indexBottom]);

                if (outflowLeft < 0) outflowLeft = 0;
                if (outflowRight < 0) outflowRight = 0;
                if (outflowTop < 0) outflowTop = 0;
                if (outflowBottom < 0) outflowBottom = 0;

                float totalOutflow = outflowLeft + outflowRight + outflowTop + outflowBottom;
                if (totalOutflow > d1)
                {
                    // Total outflow will never be greater than the water height.
                    float scalingFactor = d1 / totalOutflow;
                    outflowLeft *= scalingFactor;
                    outflowRight *= scalingFactor;
                    outflowTop *= scalingFactor;
                    outflowBottom *= scalingFactor;
                }

                float4 outflow = new float4(outflowLeft, outflowRight, outflowTop, outflowBottom);
                outflowMap[index] = outflow;
                waterHeightMap[index] = d1;
            }
        }

        // Calculates water change
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + mapWidth * y;

                int leftX = x - 1;
                int rightX = x + 1;
                int topY = y + 1;
                int bottomY = y - 1;

                if (leftX < 0) leftX += mapWidth;
                if (rightX >= mapWidth) rightX %= mapWidth;
                if (topY >= mapHeight) topY = mapHeight - 1;
                if (bottomY < 0) bottomY = 0;

                int indexLeft = leftX + mapWidth * y;
                int indexRight = rightX + mapWidth * y;
                int indexTop = x + mapWidth * topY;
                int indexBottom = x + mapWidth * bottomY;

                // Water Surface
                float waterDelta = (outflowMap[indexLeft].y + outflowMap[indexRight].x + outflowMap[indexTop].w + outflowMap[indexBottom].z) - (outflowMap[index].x + outflowMap[index].y + outflowMap[index].z + outflowMap[index].w);
                float d2 = waterHeightMap[index] + waterDelta;
                if (d2 < 0) d2 = 0;
                waterHeightMap[index] = d2;

                // Velocity Field
                float horizontalVelocity = (outflowMap[indexLeft].y - outflowMap[index].x + outflowMap[index].y - outflowMap[indexRight].x) / 2; // Horizontal speed is from left to right
                float u = horizontalVelocity;
                float verticalVelocity = (outflowMap[indexBottom].z - outflowMap[index].w + outflowMap[index].z - outflowMap[indexTop].w) / 2; // Vertical speed is from bottom to top
                float v = verticalVelocity;

                float velocity = Mathf.Sqrt(u * u + v * v);
                if (velocity > 1)
                {
                    u /= velocity;
                    v /= velocity;
                }

                velocityMap[index] = new float2(u, v);
            }
        }

        // Calculates Erosion and Deposition
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + mapWidth * y;

                float height = heightMap[index];
                float waterHeight = waterHeightMap[index];
                float neighborHeight = interpolate(ref heightMap, new float2(x + velocityMap[index].x, y + velocityMap[index].y));
                float heightDelta = height - neighborHeight; // If HeightDelta > 0, we are going DOWNHILL. If HeightDelta < 0, we are going UPHILL.
                float velocity = Mathf.Sqrt(velocityMap[index].x * velocityMap[index].x + velocityMap[index].y * velocityMap[index].y);

                if (heightDelta < minTiltAngle && heightDelta > -minTiltAngle)
                {
                    if (heightDelta >= 0)
                        heightDelta = minTiltAngle;
                    else
                        heightDelta = -minTiltAngle;
                }

                float sedimentTransportCapacity = sedimentCapacity * heightDelta * velocity;

                // Ramp-up function based on water depth
                float lMax = 0;
                if (waterHeight >= maxErosionDepth)
                {
                    lMax = 1;
                }
                else if (waterHeight < maxErosionDepth && waterHeight > 0)
                {
                    lMax = 1 - (maxErosionDepth - waterHeight) / maxErosionDepth;
                }
                sedimentTransportCapacity *= lMax;

                float currentSediment = sedimentMap[index];
                float newHeight = height;
                float sediment = currentSediment;
                float sedimentChange = 0;
                float newWaterHeight = waterHeight;

                //if (heightDelta < 0)
                //{
                //    // Going uphill
                //    sedimentChange = -heightDelta;
                //    if (sedimentChange > -sedimentTransportCapacity)
                //        sedimentChange = -sedimentTransportCapacity;
                //    if (sedimentChange > currentSediment)
                //        sedimentChange = currentSediment;

                //    newHeight = height + sedimentChange;
                //    sediment = currentSediment - sedimentChange;
                //}
                //else 

                // Dissolves soil in water.
                if (sedimentTransportCapacity > currentSediment)
                {
                    sedimentChange = sedimentDissolvingConstant * (sedimentTransportCapacity - currentSediment);
                    if (sedimentChange > heightDelta)
                        sedimentChange = heightDelta;
                    if (height - sedimentChange < 0)
                        sedimentChange = height;

                    newHeight -= sedimentChange;
                    sediment += sedimentChange;
                    newWaterHeight += sedimentChange;
                }
                // Deposits soil in ground.
                else if (sedimentTransportCapacity < currentSediment)
                {
                    sedimentChange = sedimentDepositionConstant * (currentSediment - sedimentTransportCapacity);
                    if (sedimentChange > heightDelta)
                        sedimentChange = heightDelta;

                    //if (height + sedimentChange > neighborHeight)
                    //    sedimentChange = neighborHeight - height;
                    if (sedimentChange < 0)
                        sedimentChange = 0;
                    if (sedimentChange > currentSediment)
                        sedimentChange = currentSediment;
                    if (sedimentChange > waterHeight)
                        sedimentChange = waterHeight;

                    newHeight += sedimentChange;
                    sediment -= sedimentChange;
                    newWaterHeight -= sedimentChange;
                }

                if (newHeight < 0) newHeight = 0;
                if (newHeight > 1) newHeight = 1;
                if (sediment < 0) sediment = 0;

                heightMap[index] = newHeight;
                sedimentMap[index] = sediment;
                waterHeightMap[index] = newWaterHeight;
            }
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + mapWidth * y;

                // Sediment Transportation
                sedimentMap[index] = interpolate(ref sedimentMap, new float2(x - velocityMap[index].x, y - velocityMap[index].y));

                // Evaporation
                //float evaporationConstant = humidityMap[index] * waterEvaporationRetention + waterEvaporationRetention;
                float evaporationConstant = waterEvaporationRetention;
                float newWaterHeight = waterHeightMap[index] * evaporationConstant;

                if (newWaterHeight < 0)
                    newWaterHeight = 0;
                waterHeightMap[index] = newWaterHeight;
            }
        }
    }

    float interpolate(ref float[] map, float2 coordinates)
    {
        int leftX = Mathf.FloorToInt(coordinates.x);
        int rightX = Mathf.CeilToInt(coordinates.x);
        int bottomY = Mathf.FloorToInt(coordinates.y);
        int topY = Mathf.CeilToInt(coordinates.y);

        float deltaX = coordinates.x - leftX;
        float deltaY = coordinates.y - bottomY;

        if (rightX >= mapWidth)
            rightX -= mapWidth;
        if (rightX < 0)
            rightX += mapWidth;
        if (leftX >= mapWidth)
            leftX -= mapWidth;
        if (leftX < 0) 
            leftX += mapWidth;
        if (topY >= mapHeight) topY = mapHeight - 1;
        if (topY < 0) topY = 0;
        if (bottomY >= mapHeight) bottomY = mapHeight - 1;
        if (bottomY < 0) bottomY = 0;

        int indexBL = leftX + mapWidth * bottomY;
        int indexBR = rightX + mapWidth * bottomY;
        int indexTR = rightX + mapWidth * topY;
        int indexTL = leftX + mapWidth * topY;

        float valueBL = map[indexBL];
        float valueBR = map[indexBR];
        float valueTL = map[indexTL];
        float valueTR = map[indexTR];

        float valueXdelta0 = (valueBR - valueBL) * deltaX + valueBL;
        float valueXdelta1 = (valueTR - valueTL) * deltaX + valueTL;

        float value = (valueXdelta1 - valueXdelta0) * deltaY + valueXdelta0;
        return value;
    }
}
