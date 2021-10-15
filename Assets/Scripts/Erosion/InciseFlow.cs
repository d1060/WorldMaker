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

    struct int2
    {
        public int x;
        public int y;

        public int2 (int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public static int2 operator +(int2 i, int2 u)
        {
            return new int2(i.x + u.x, i.y + u.y);
        }

        public override string ToString()
        {
            return x.ToString() + ":" + y.ToString();
        }
    }

    struct float2
    {
        public float x;
        public float y;

        public float2 (float _x, float _y)
        {
            x = _x;
            y = _y;
        }

        public static float2 operator +(float2 i, float2 u)
        {
            return new float2(i.x + u.x, i.y + u.y);
        }

        public static float2 operator *(float2 i, float u)
        {
            return new float2(i.x * u, i.y * u);
        }

        public static float2 operator /(float2 i, float u)
        {
            return new float2(i.x / u, i.y / u);
        }

        public static float2 operator *(float2 i, double u)
        {
            return new float2((float)(i.x * u), (float)(i.y * u));
        }

        public override string ToString()
        {
            return x.ToString() + ":" + y.ToString();
        }
    }

    public int mapWidth;
    public int mapHeight;
    public float logBase = 2;
    public float amount = 1;
    public float heightFactor = 0.1f;
    public float minAmount = 0;
    public float strength = 1;
    public int maxPathDepth = 512;
    public float inertia = 1;
    float[] flowMap;
    public float[] heightMap;
    public float[] fillBasinsHeightMap;
    public float[] inciseFlowMap;

    public void Run()
    {
        flowMap = new float[heightMap.Length];

        // Assembles the FlowMap.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int2 id = new int2(x, y);

                int index = id.x + id.y * mapWidth;
                int currentX = id.x;
                int currentY = id.y;

                flowMap[index] = amount > 1 ? amount : 1;
                int nextX = 0;
                int nextY = 0;
                int2 next = new int2(nextX, nextY);

                int count = 0;
                float2 gradient = new float2(0, 0);
                while (count < 256 && count < maxPathDepth && next.x != -1 && next.y != -1)
                {
                    float2 newGradient = new float2(0, 0);
                    next = FindNextCell(currentX, currentY, gradient, out newGradient);
                    gradient = newGradient;
                    currentX = next.x;
                    currentY = next.y;
                    count++;
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

    int2 FindNextCell(int x, int y, float2 prevGradient, out float2 newGradient)
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

        float height = fillBasinsHeightMap[index];
        float heightDL = diagonalHeight(fillBasinsHeightMap[indexDL], height);
        float heightD = fillBasinsHeightMap[indexD];
        float heightDR = diagonalHeight(fillBasinsHeightMap[indexDR], height);
        float heightR = fillBasinsHeightMap[indexR];
        float heightUR = diagonalHeight(fillBasinsHeightMap[indexUR], height);
        float heightU = fillBasinsHeightMap[indexU];
        float heightUL = diagonalHeight(fillBasinsHeightMap[indexUL], height);
        float heightL = fillBasinsHeightMap[indexL];

        bool goingNowhere = false;
        float2 gradient = new float2(0, 0);
        float heightDifference = 0;

        if (heightDL < heightD && heightDL < heightDR && heightDL < heightR && heightDL < heightUR && heightDL < heightU && heightDL < heightUL && heightDL < heightL)
        {
            //DL is the lowest.
            heightDifference = height - heightDL;
            gradient = new float2(-0.707107f, 0.707107f);
        }
        else if (heightD < heightDR && heightD < heightR && heightD < heightUR && heightD < heightU && heightD < heightUL && heightD < heightL && heightD < heightDL)
        {
            //D is the lowest.
            heightDifference = height - heightD;
            gradient = new float2(0, 1);
        }
        else if (heightDR < heightR && heightDR < heightUR && heightDR < heightU && heightDR < heightUL && heightDR < heightL && heightDR < heightDL && heightDR < heightD)
        {
            //DR is the lowest.
            heightDifference = height - heightDR;
            gradient = new float2(0.707107f, 0.707107f);
        }
        else if (heightR < heightUR && heightR < heightU && heightR < heightUL && heightR < heightL && heightR < heightDL && heightR < heightD && heightR < heightDR)
        {
            //R is the lowest.
            heightDifference = height - heightR;
            gradient = new float2(1, 0);
        }
        else if (heightUR < heightU && heightUR < heightUL && heightUR < heightL && heightUR < heightDL && heightUR < heightD && heightUR < heightDR && heightUR < heightR)
        {
            //UR is the lowest.
            heightDifference = height - heightUR;
            gradient = new float2(0.707107f, -0.707107f);
        }
        else if (heightU < heightUL && heightU < heightL && heightU < heightDL && heightU < heightD && heightU < heightDR && heightU < heightR && heightU < heightUR)
        {
            //U is the lowest.
            heightDifference = height - heightU;
            gradient = new float2(0, -1);
        }
        else if (heightUL < heightL && heightUL < heightDL && heightUL < heightD && heightUL < heightDR && heightUL < heightR && heightUL < heightUR && heightUL < heightU)
        {
            //UL is the lowest.
            heightDifference = height - heightUL;
            gradient = new float2(-0.707107f, -0.707107f);
        }
        else if (heightL < heightDL && heightL < heightD && heightL < heightDR && heightL < heightR && heightL < heightUR && heightL < heightU && heightL < heightUL)
        {
            //L is the lowest.
            heightDifference = height - heightL;
            gradient = new float2(-1, 0);
        }
        else
        {
            goingNowhere = true;
        }

        if (heightDifference >= 0) // Going downhill
        {
            gradient *= heightDifference;
        }
        else // Going uphill - adds forced inertia and disconsiders current gradient.
        {
            gradient *= 0;
        }
        newGradient = prevGradient * inertia + gradient * (1 - inertia);

        // Rounds up the new gradient to the nearest integer direction.
        int2 finalCoordinates = new int2(-1, -1);
        if (!goingNowhere && (newGradient.x != 0 || newGradient.y != 0))
        {
            float2 finalNewGradient = new float2(newGradient.x, newGradient.y);
            float magnitude = Mathf.Sqrt(newGradient.x * newGradient.x + newGradient.y * newGradient.y);
            finalNewGradient /= magnitude;
            finalNewGradient *= 1.125;

            int finalX = x + (int)Mathf.Round(finalNewGradient.x);
            int finalY = y + (int)Mathf.Round(finalNewGradient.y);

            if (finalX < 0) finalX += mapWidth;
            if (finalX >= mapWidth) finalX -= mapWidth;
            if (finalY >= mapHeight) finalY = mapHeight - 1;
            if (finalY < 0) finalY = 0;

            int nextIndex = finalX + mapWidth * finalY;
            flowMap[nextIndex] += amount;
            finalCoordinates = new int2(finalX, finalY);
        }

        return finalCoordinates;
    }

    float diagonalHeight(float diagonalHeight, float thisHeight)
    {
        float actualHeight = (diagonalHeight - thisHeight) / 1.41421356f + thisHeight;
        return actualHeight;
    }

}
