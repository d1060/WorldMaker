using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class InciseFlow
{
    #region Singleton
    static InciseFlow myInstance = null;

    InciseFlow()
    {
    }

    public static InciseFlow instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new InciseFlow();
            return myInstance;
        }
    }
    #endregion

    public int mapWidth;
    public int mapHeight;
    public float logBase = 2;
    public float amount = 1;
    public float heightFactor = 0.1f;
    public float minAmount = 0;
    public float strength = 1;
    float[] flowMap;

    public void Run(ref float[] heightMap, ref float[] inciseFlowMap)
    {
        flowMap = new float[heightMap.Length];

        // Assembles the FlowMap.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + y * mapWidth;
                int currentX = x;
                int currentY = y;

                flowMap[index] = amount;
                int nextX = -1;
                int nextY = -1;
                while (FindNextCell(currentX, currentY, ref heightMap, ref flowMap, ref nextX, ref nextY))
                {
                    currentX = nextX;
                    currentY = nextY;
                }
            }
        }

        // Erodes the Terrain.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + y * mapWidth;

                float flowValue = flowMap[index];// + 1;

                float erodeValue = Mathf.Log(flowValue, logBase) * heightFactor;
                if (erodeValue < minAmount)
                    erodeValue = 0;
                else
                    erodeValue -= minAmount;

                erodeValue *= strength;

                if (erodeValue > heightMap[index])
                    inciseFlowMap[index] = heightMap[index];
                else if (erodeValue > heightFactor)
                    inciseFlowMap[index] = heightFactor;
                else
                    inciseFlowMap[index] = erodeValue;
            }
        }
    }

    bool FindNextCell(int x, int y, ref float[] heightMap, ref float[] flowMap, ref int nextX, ref int nextY)
    {
        int index = x + mapWidth * y;

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
        int indexDL = leftX + mapWidth * bottomY;
        int indexDR = rightX + mapWidth * bottomY;
        int indexUL = leftX + mapWidth * topY;
        int indexUR = rightX + mapWidth * topY;

        float height = heightMap[index];
        float heightDL = diagonalHeight(heightMap[indexDL], height);
        float heightD = heightMap[indexD];
        float heightDR = diagonalHeight(heightMap[indexDR], height);
        float heightR = heightMap[indexR];
        float heightUR = diagonalHeight(heightMap[indexUR], height);
        float heightU = heightMap[indexU];
        float heightUL = diagonalHeight(heightMap[indexUL], height);
        float heightL = heightMap[indexL];

        if (heightDL < height && heightDL < heightD && heightDL < heightDR && heightDL < heightR && heightDL < heightUR && heightDL < heightU && heightDL < heightUL && heightDL < heightL)
        {
            //DL is the lowest.
            nextX = leftX;
            nextY = bottomY;
            //flowMap[indexDL] += height - heightDL;
            flowMap[indexDL] += amount;
        }
        else if (heightD < height && heightD < heightDR && heightD < heightR && heightD < heightUR && heightD < heightU && heightD < heightUL && heightD < heightL && heightD < heightDL)
        {
            //D is the lowest.
            nextX = x;
            nextY = bottomY;
            //flowMap[indexD] += height - heightD;
            flowMap[indexD] += amount;
        }
        else if (heightDR < height && heightDR < heightR && heightDR < heightUR && heightDR < heightU && heightDR < heightUL && heightDR < heightL && heightDR < heightDL && heightDR < heightD)
        {
            //DR is the lowest.
            nextX = rightX;
            nextY = bottomY;
            //flowMap[indexDR] += height - heightDR;
            flowMap[indexDR] += amount;
        }
        else if (heightR < height && heightR < heightUR && heightR < heightU && heightR < heightUL && heightR < heightL && heightR < heightDL && heightR < heightD && heightR < heightDR)
        {
            //R is the lowest.
            nextX = rightX;
            nextY = y;
            //flowMap[indexR] += height - heightR;
            flowMap[indexR] += amount;
        }
        else if (heightUR < height && heightUR < heightU && heightUR < heightUL && heightUR < heightL && heightUR < heightDL && heightUR < heightD && heightUR < heightDR && heightUR < heightR)
        {
            //UR is the lowest.
            nextX = rightX;
            nextY = topY;
            //flowMap[indexUR] += height - heightUR;
            flowMap[indexUR] += amount;
        }
        else if (heightU < height && heightU < heightUL && heightU < heightL && heightU < heightDL && heightU < heightD && heightU < heightDR && heightU < heightR && heightU < heightUR)
        {
            //U is the lowest.
            nextX = x;
            nextY = topY;
            //flowMap[indexU] += height - heightU;
            flowMap[indexU] += amount;
        }
        else if (heightUL < height && heightUL < heightL && heightUL < heightDL && heightUL < heightD && heightUL < heightDR && heightUL < heightR && heightUL < heightUR && heightUL < heightU)
        {
            //UL is the lowest.
            nextX = leftX;
            nextY = topY;
            //flowMap[indexUL] += height - heightUL;
            flowMap[indexUL] += amount;
        }
        else if (heightL < height && heightL < heightDL && heightL < heightD && heightL < heightDR && heightL < heightR && heightL < heightUR && heightL < heightU && heightL < heightUL)
        {
            //L is the lowest.
            nextX = leftX;
            nextY = y;
            //flowMap[indexL] += height - heightL;
            flowMap[indexL] += amount;
        }
        else
        {
            nextX = -1;
            nextY = -1;
            return false;
        }
        return true;
    }

    float diagonalHeight(float diagonalHeight, float thisHeight)
    {
        float actualHeight = (diagonalHeight - thisHeight) / 1.41421356f + thisHeight;
        return actualHeight;
    }

}
