using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FillBasins
{
    #region Singleton
    static FillBasins myInstance = null;

    FillBasins()
    {
    }

    public static FillBasins instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new FillBasins();
            return myInstance;
        }
    }
    #endregion

    public int mapWidth;
    public int mapHeight;
    public int stabilizationCount = 0;
    public float waterLevel;
    public float[] heightMap;
    public float[] basinsMap;
    public int[] drainageMap;
    int maxStabilizationCount = 400;

    public void Run()
    {
        stabilizationCount = 0;
        while (true)
        {
            bool adjusted = false;
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    int index = x + y * mapWidth;

                    int leftX = x - 1;
                    int rightX = x + 1;
                    int topY = y - 1;
                    int bottomY = y + 1;

                    if (leftX < 0) leftX += mapWidth;
                    if (rightX >= mapWidth) rightX -= mapWidth;
                    if (bottomY >= mapHeight) bottomY = mapHeight - 1;
                    if (topY < 0) topY = 0;

                    int indexL = leftX + mapWidth * y;
                    int indexR = rightX + mapWidth * y;
                    int indexU = x + mapWidth * topY;
                    int indexD = x + mapWidth * bottomY;

                    float height = heightMap[index];
                    float heightD = heightMap[indexD];
                    float heightR = heightMap[indexR];
                    float heightU = heightMap[indexU];
                    float heightL = heightMap[indexL];

                    int minHeightIndex = -1;
                    float minHeight = MinNotEqual(height, heightD, heightR, heightU, heightL, indexL, indexR, indexU, indexD, out minHeightIndex);

                    int nextBasinIndex = -1;
                    int nextDrainageIndex = -1;
                    float nextBasinHeight = -1;
                    NextBasinAndDrainageIndex(indexL, indexR, indexU, indexD, out nextBasinIndex, out nextDrainageIndex, out nextBasinHeight);

                    if (minHeight > height && height > waterLevel / 2)
                    {
                        // My lowest neighbor is above me.
                        if (nextBasinIndex != -1 && nextDrainageIndex != -1)
                        {
                            // I have a neighbor with drainage
                            float nextDrainageHeight = heightMap[nextDrainageIndex];
                            if (nextDrainageHeight <= height)
                            {
                                // My neighbor's drainage is beneath or equal to me.
                                drainageMap[index] = drainageMap[nextDrainageIndex];
                                heightMap[index] = basinsMap[nextBasinIndex];
                                basinsMap[index] = basinsMap[nextBasinIndex];
                                adjusted = true;
                            }
                        }

                        if (drainageMap[index] == 0)
                        {
                            // I still have no drainage.
                            heightMap[index] = minHeight;
                            basinsMap[index] = minHeight;
                            adjusted = true;
                        }
                    }
                    else if (minHeight != 0 && minHeight < height && basinsMap[index] != 0)
                    {
                        // My lowest neighbor is beneath me and I am at a basin.
                        drainageMap[index] = minHeightIndex;
                    }
                    else if (minHeight == 0 && nextDrainageIndex != -1)
                    {
                        // Everyone around me is equal and I have a neighbor with drainage.
                        drainageMap[index] = drainageMap[nextDrainageIndex];
                    }
                }
            }

            if (adjusted)
                stabilizationCount++;
            else
                break;

            if (stabilizationCount >= maxStabilizationCount)
                return;
        }
    }

    float MinNotEqual(float height, float f1, float f2, float f3, float f4, int index1, int index2, int index3, int index4, out int minHeightIndex)
    {
        minHeightIndex = -1;
        if (f1 != height && f1 < f2 && f1 < f3 && f1 < f4)
        {
            minHeightIndex = index1;
            return f1;
        }
        if (f2 != height && f2 < f3 && f2 < f4)
        {
            minHeightIndex = index2;
            return f2;
        }
        if (f3 != height && f3 < f4)
        {
            minHeightIndex = index3;
            return f3;
        }
        if (f4 != height)
        {
            minHeightIndex = index4;
            return f4;
        }
        return 0;
    }

    void NextBasinAndDrainageIndex(int indexL, int indexR, int indexU, int indexD, out int nextBasinIndex, out int nextDrainageIndex, out float nextBasinHeight)
    {
        nextBasinIndex = -1;
        nextDrainageIndex = -1;
        nextBasinHeight = -1;

        if (basinsMap[indexL] != 0)
        {
            nextBasinIndex = indexL;
            nextBasinHeight = basinsMap[indexL];
            if (drainageMap[indexL] != 0)
                nextDrainageIndex = drainageMap[indexL];
        }

        if (basinsMap[indexR] != 0)
        {
            float basinHeight = basinsMap[indexR];
            if (nextBasinHeight == -1 || basinHeight < nextBasinHeight)
            {
                nextBasinIndex = indexR;
                nextBasinHeight = basinHeight;

                int drainageIndex = drainageMap[indexR];
                if (drainageIndex != 0 && nextDrainageIndex == -1)
                {
                    nextDrainageIndex = drainageIndex;
                }
            }
        }

        if (basinsMap[indexU] != 0)
        {
            float basinHeight = basinsMap[indexU];
            if (nextBasinHeight == -1 || basinHeight < nextBasinHeight)
            {
                nextBasinIndex = indexU;
                nextBasinHeight = basinHeight;

                int drainageIndex = drainageMap[indexU];
                if (drainageIndex != 0 && nextDrainageIndex == -1)
                {
                    nextDrainageIndex = drainageIndex;
                }
            }
        }

        if (basinsMap[indexD] != 0)
        {
            float basinHeight = basinsMap[indexD];
            if (nextBasinHeight == -1 || basinHeight < nextBasinHeight)
            {
                nextBasinIndex = indexD;
                nextBasinHeight = basinHeight;

                int drainageIndex = drainageMap[indexD];
                if (drainageIndex != 0 && nextDrainageIndex == -1)
                {
                    nextDrainageIndex = drainageIndex;
                }
            }
        }
    }
}
