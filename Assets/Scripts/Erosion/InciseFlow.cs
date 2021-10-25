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

    float2 normalize(float2 f)
    {
        float magnitude = Mathf.Sqrt(f.x * f.x + f.y * f.y);
        return new float2(f.x / magnitude, f.y / magnitude);
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
    public float maxFlowStrength;
    public float curveFactor;
    public float heightInfluence;
    public float waterLevel;
    float[] flowMap;
    public float[] heightMap;
    public float[] basinsMap;
    public float[] inciseFlowMap;
    public int[] drainageIndexesMap;

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
                while (count < 2048 && next.x != -1 && next.y != -1)
                {
                    float2 newGradient = new float2(0, 0);
                    next = FindNextCell(currentX, currentY, gradient, out newGradient);
                    gradient += newGradient;
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

                float height = heightMap[index];
                if (height < waterLevel)
                    continue;

                float flowValue = flowMap[index];// + 1;
                                                 //if (flowValue > maxFlowStrength) flowValue = maxFlowStrength;

                if (flowValue < 1) flowValue = 1;
                if (logBase <= 1) logBase = 1.1f;

                float erodeValue = (Mathf.Log(flowValue) / Mathf.Log(logBase)) * heightFactor;
                if (erodeValue < minAmount)
                    erodeValue = 0;
                else
                    erodeValue -= minAmount;

                if (erodeValue > height - waterLevel)
                    erodeValue = height - waterLevel;
                else if (erodeValue > heightFactor)
                    erodeValue = heightFactor;

                erodeValue = Mathf.Pow(Mathf.Abs(erodeValue), curveFactor);
                erodeValue = erodeValue * ((1 - heightInfluence) + heightInfluence * (height - waterLevel) * 4);
                erodeValue *= strength;
                if (erodeValue > (height - (waterLevel + 0.001f)))
                    erodeValue = (height - (waterLevel + 0.001f));

                inciseFlowMap[index] = erodeValue;
            }
        }
    }

    int2 FindNextCell(int x, int y, float2 prevGradient, out float2 newGradient)
    {
        int index = x + mapWidth * y;
        int nextIndex = drainageIndexesMap[index];
        if (index == nextIndex || nextIndex <= 0)
        {
            newGradient = new float2(0, 0);
            return new int2(-1, -1);
        }
        float height = heightMap[index];
        float lowestNeighborHeight = heightMap[nextIndex];

        int nextIndexX = nextIndex % mapWidth;
        int nextIndexY = nextIndex / mapWidth;

        int gradientX = nextIndexX - x;
        int gradientY = nextIndexY - y;
        int halfMapWidth = mapWidth / 2;

        if (gradientX >= halfMapWidth)
            gradientX -= mapWidth;
        else if (gradientX <= -halfMapWidth)
            gradientX += mapWidth;

        float2 gradient = new float2(gradientX, gradientY);

        gradient = normalize(gradient);

        if (prevGradient.x != 0 || prevGradient.y != 0)
            prevGradient = normalize(prevGradient);
        newGradient = prevGradient * inertia + gradient * (1 - inertia);

        // Rounds up the new gradient to the nearest integer direction.
        int2 finalCoordinates = new int2(-1, -1);
        if (newGradient.x != 0 || newGradient.y != 0)
        {
            float2 finalNewGradient = new float2(newGradient.x, newGradient.y);
            float magnitude = Mathf.Sqrt(newGradient.x * newGradient.x + newGradient.y * newGradient.y);
            if (magnitude != 0)
            {
                finalNewGradient /= magnitude;
                finalNewGradient *= 1.125;

                int finalX = x + (int)Mathf.Round(finalNewGradient.x);
                int finalY = y + (int)Mathf.Round(finalNewGradient.y);

                if (finalX < 0) finalX += mapWidth;
                if (finalX >= mapWidth) finalX -= mapWidth;
                if (finalY >= mapHeight) finalY = mapHeight - 1;
                if (finalY < 0) finalY = 0;

                int nextFlowIndex = finalX + mapWidth * finalY;
                flowMap[nextFlowIndex] += amount;
                finalCoordinates = new int2(finalX, finalY);
            }
        }

        return finalCoordinates;
    }

    float diagonalHeight(float diagonalHeight, float thisHeight)
    {
        float actualHeight = (diagonalHeight - thisHeight) / 1.41421356f + thisHeight;
        return actualHeight;
    }

    float getHeight(int index)
    {
        return basinsMap[index] != 0 ? basinsMap[index] : heightMap[index];
    }

    float diagonalHeight(int index, float thisHeight)
    {
        float diagonalHeight = basinsMap[index] != 0 ? basinsMap[index] : heightMap[index];
        float actualHeight = (diagonalHeight - thisHeight) / 1.41421356f + thisHeight;
        return actualHeight;
    }
}
