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
    public float upwardWeight = 10;
    public float downwardWeight = 1;
    public float distanceWeight = 100f;
    public float[] heightMap;
    public float[] distanceMap;
    public int[] connectivityMap;
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

                    float linearStep = 1 / (float)mapWidth;
                    linearStep *= distanceWeight;
                    float diagonalStep = linearStep * 1.41421356f;

                    float height = heightMap[index];

                    if (height < waterLevel)
                        continue;

                    int leftX = x - 1;
                    int rightX = x + 1;
                    int topY = y - 1;
                    int bottomY = y + 1;


                    bool isTop = false;
                    bool isBottom = false;

                    if (leftX < 0) leftX += mapWidth;
                    if (rightX >= mapWidth) rightX -= mapWidth;
                    if (bottomY >= mapHeight)
                    {
                        bottomY = mapHeight - 1;
                        isBottom = true;
                    }
                    if (topY < 0)
                    {
                        topY = 0;
                        isTop = true;
                    }

                    int indexL = leftX + mapWidth * y;
                    int indexR = rightX + mapWidth * y;
                    int indexU = x + mapWidth * topY;
                    int indexD = x + mapWidth * bottomY;
                    int indexDL = leftX + mapWidth * bottomY;
                    int indexDR = rightX + mapWidth * bottomY;
                    int indexUL = leftX + mapWidth * topY;
                    int indexUR = rightX + mapWidth * topY;

                    float heightD = heightMap[indexD];
                    float heightR = heightMap[indexR];
                    float heightU = heightMap[indexU];
                    float heightL = heightMap[indexL];
                    float heightDL = diagonalHeight(indexDL, height);
                    float heightDR = diagonalHeight(indexDR, height);
                    float heightUR = diagonalHeight(indexUR, height);
                    float heightUL = diagonalHeight(indexUL, height);

                    // If any neighbors have a calculated distance, or are below water:
                    if (distanceMap[indexL] != 0 || heightL < waterLevel ||
                        distanceMap[indexR] != 0 || heightR < waterLevel ||
                        distanceMap[indexU] != 0 || heightU < waterLevel ||
                        distanceMap[indexD] != 0 || heightD < waterLevel ||
                        distanceMap[indexDL] != 0 || heightDL < waterLevel ||
                        distanceMap[indexDR] != 0 || heightDR < waterLevel ||
                        distanceMap[indexUL] != 0 || heightUL < waterLevel ||
                        distanceMap[indexUR] != 0 || heightUR < waterLevel)
                    {
                        float lowestConnectedDistance = 9999999;
                        int lowestConnectedIndex = -1;

                        if (!isTop)
                        {
                            checkIsCloser(indexUL, index, heightUL, height, diagonalStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                            checkIsCloser(indexU, index, heightU, height, linearStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                            checkIsCloser(indexUR, index, heightUR, height, diagonalStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                        }
                        checkIsCloser(indexL, index, heightL, height, linearStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                        checkIsCloser(indexR, index, heightR, height, linearStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                        if (!isBottom)
                        {
                            checkIsCloser(indexDL, index, heightDL, height, diagonalStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                            checkIsCloser(indexD, index, heightD, height, linearStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                            checkIsCloser(indexDR, index, heightDR, height, diagonalStep, ref lowestConnectedDistance, ref lowestConnectedIndex);
                        }

                        if (lowestConnectedIndex == -1)
                            continue;

                        if (distanceMap[index] == 0 || distanceMap[index] > lowestConnectedDistance)
                        {
                            distanceMap[index] = lowestConnectedDistance;
                            connectivityMap[index] = lowestConnectedIndex;
                            adjusted = true;
                        }
                    }
                }
            }

            if (adjusted || stabilizationCount == 0)
                stabilizationCount++;
            else
                break;

            if (stabilizationCount >= maxStabilizationCount)
                return;
        }
    }

    float diagonalHeight(int index, float thisHeight)
    {
        float diagonalHeight = heightMap[index];
        float actualHeight = (diagonalHeight - thisHeight) / 1.41421356f + thisHeight;
        return actualHeight;
    }

    void checkIsCloser(int neighborIndex, int myIndex, float neighborHeight, float myHeight, float distance, ref float lowestConnectedDistance, ref int lowestConnectedIndex)
    {
        if (neighborHeight < waterLevel)
        {
            lowestConnectedDistance = distance + (waterLevel - myHeight) * downwardWeight;
            if (lowestConnectedDistance == 0)
                lowestConnectedDistance = 0.0000001f;
            lowestConnectedIndex = neighborIndex;
        }
        else if (distanceMap[neighborIndex] != 0 && connectivityMap[neighborIndex] != myIndex)
        {
            float slope = neighborHeight - myHeight; // Downward slope is NEGATIVE. Upward is POSITIVE.
            if (slope > 0)
                slope *= upwardWeight;
            else
                slope *= downwardWeight;

            float currentDistance = distanceMap[neighborIndex];
            float distanceThroughNeighbor = currentDistance + distance + slope;
            //float distanceThroughNeighbor = distance - slope;

            if (distanceThroughNeighbor < lowestConnectedDistance || lowestConnectedDistance == 9999999)
            {
                lowestConnectedDistance = distanceThroughNeighbor;
                lowestConnectedIndex = neighborIndex;
            }
        }
    }
}
