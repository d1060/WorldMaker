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
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + mapWidth * y;
                float height = heightMap[index];
                float waterHeight = waterHeightMap[index];

                // Update water Height.
                float d1 = waterScale * (humidityMap[index] + riverSourcesMap[index]) + waterFixedAmount + waterHeightMap[index];
                //float d1 = humidityMap[index] * humidityScale + riverSourcesMap[index] * riverSourceScale + waterHeightMap[index];
                //float d1 = humidityScale + waterHeightMap[index];

                // Calculate outflow.
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

                float outflowLeft = gravity * (height + waterHeight - heightMap[indexLeft] - waterHeightMap[indexLeft]);
                float outflowRight = gravity * (height + waterHeight - heightMap[indexRight] - waterHeightMap[indexRight]);
                float outflowTop = gravity * (height + waterHeight - heightMap[indexTop] - waterHeightMap[indexTop]);
                float outflowBottom = gravity * (height + waterHeight - heightMap[indexBottom] - waterHeightMap[indexBottom]);

                outflowLeft += outflowMap[index].x;
                outflowRight += outflowMap[index].y;
                outflowTop += outflowMap[index].z;
                outflowBottom += outflowMap[index].w;

                if (outflowLeft < 0) outflowLeft = 0;
                if (outflowRight < 0) outflowRight = 0;
                if (outflowTop < 0) outflowTop = 0;
                if (outflowBottom < 0) outflowBottom = 0;

                float scalingFactor = d1 / (outflowLeft + outflowRight + outflowTop + outflowBottom);
                if (scalingFactor > 1) scalingFactor = 1;

                outflowLeft *= scalingFactor;
                outflowRight *= scalingFactor;
                outflowTop *= scalingFactor;
                outflowBottom *= scalingFactor;

                if (outflowLeft > 1) outflowLeft = 1;
                if (outflowRight > 1) outflowRight = 1;
                if (outflowTop > 1) outflowTop = 1;
                if (outflowBottom > 1) outflowBottom = 1;

                float4 totalOutflow = new float4(outflowLeft, outflowRight, outflowTop, outflowBottom);
                outflowMap[index] = totalOutflow;

                // Water Surface
                float waterDelta = (outflowMap[indexLeft].y + outflowMap[indexRight].x + outflowMap[indexTop].w + outflowMap[indexBottom].z) - (outflowLeft + outflowRight + outflowTop + outflowBottom);
                float d2 = d1 + waterDelta;
                //if (d2 < 0)
                //    d2 = 0;
                waterHeightMap[index] = d2;

                // Velocity Field
                float horizontalVelocity = (outflowMap[indexLeft].y - outflowLeft + outflowRight - outflowMap[indexRight].x) / 2; // Horizontal speed is from left to right
                float u = horizontalVelocity;
                float verticalVelocity = (outflowMap[indexBottom].z - outflowBottom + outflowTop - outflowMap[indexTop].w) / 2; // Vertical speed is from bottom to top
                float v = verticalVelocity;

                velocityMap[index] = new float2(u, v);
                float velocity = Mathf.Sqrt(u * u + v * v);

                // Erosion and Deposition
                float neighborHeight = interpolate(ref heightMap, new float2(x + u, y + v));
                float heightDelta = height - neighborHeight; // If HeightDelta > 0, we are going DOWNHILL. If HeightDelta < 0, we are going UPHILL.

                if (heightDelta < minTiltAngle && heightDelta > -minTiltAngle)
                {
                    if (heightDelta >= 0)
                        heightDelta = minTiltAngle;
                    else
                        heightDelta = -minTiltAngle;
                }

                float sedimentTransportCapacity = sedimentCapacity * heightDelta * velocity;
                float currentSediment = sedimentMap[index];

                float newHeight = height;
                float sediment = currentSediment;
                float sedimentChange = 0;

                if (heightDelta < 0)
                {
                    // Going uphill
                    sedimentChange = -heightDelta;
                    if (sedimentChange > -sedimentTransportCapacity)
                        sedimentChange = -sedimentTransportCapacity;
                    if (sedimentChange > currentSediment)
                        sedimentChange = currentSediment;

                    newHeight = height + sedimentChange;
                    sediment = currentSediment - sedimentChange;
                }
                else if (sedimentTransportCapacity > currentSediment)
                {
                    sedimentChange = sedimentDissolvingConstant * (sedimentTransportCapacity - currentSediment);
                    if (sedimentChange > heightDelta)
                        sedimentChange = heightDelta;
                    if (height - sedimentChange < 0)
                        sedimentChange = height;

                    newHeight = height - sedimentChange;
                    sediment = currentSediment + sedimentChange;
                }
                else if (sedimentTransportCapacity < currentSediment)
                {
                    sedimentChange = sedimentDepositionConstant * (currentSediment - sedimentTransportCapacity);
                    if (sedimentChange > heightDelta)
                        sedimentChange = heightDelta;

                    if (height + sedimentChange > neighborHeight)
                        sedimentChange = neighborHeight - height;
                    if (sedimentChange < 0)
                        sedimentChange = 0;
                    if (sedimentChange > currentSediment)
                        sedimentChange = currentSediment;

                    newHeight = height + sedimentChange;
                    sediment = currentSediment - sedimentChange;
                }

                if (newHeight < 0) newHeight = 0;
                if (newHeight > 1) newHeight = 1;
                if (sediment < 0) sediment = 0;

                if (heightMap[indexLeft] > heightMap[index] &&
                    heightMap[indexRight] > heightMap[index] &&
                    heightMap[indexTop] > heightMap[index] &&
                    heightMap[indexBottom] > heightMap[index] &&
                    newHeight < heightMap[index])
                {
                    int a = 0;
                }

                heightMap[index] = newHeight;
                sedimentMap[index] = sediment;

                // Sediment Transportation
                sedimentMap[index] = interpolate(ref sedimentMap, new float2(x - u, y - v));

                // Evaporation
                //float evaporationConstant = humidityMap[index] * waterEvaporationRetention + waterEvaporationRetention;
                float evaporationConstant = waterEvaporationRetention;
                float newWaterHeight = d2 * evaporationConstant;

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

        if (rightX >= mapWidth) rightX %= mapWidth;
        if (rightX < 0) rightX += mapWidth;
        if (leftX >= mapWidth) leftX %= mapWidth;
        if (leftX < 0) leftX += mapWidth;
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

        float deltaX = coordinates.x - leftX;
        float deltaY = coordinates.y - bottomY;

        float valueXdelta0 = (valueBR - valueBL) * deltaX + valueBL;
        float valueXdelta1 = (valueTR - valueTL) * deltaX + valueTL;

        float value = (valueXdelta1 - valueXdelta0) * deltaY + valueXdelta0;
        return value;
    }
}
