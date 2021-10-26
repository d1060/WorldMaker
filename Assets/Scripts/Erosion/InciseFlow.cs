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

    const float MIN_WATER_HEIGHT = 0.0001f;
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
    public float heightFactor = 0.05f;
    public float minAmount = 0;
    public float strength = 1;
    public float maxFlowStrength;
    public float curveFactor;
    public float heightInfluence;
    public float waterLevel;
    public float blur = 0;
    public float[] flowMap;
    public float[] heightMap;
    public float[] inciseFlowMap;
    public int[] drainageIndexesMap;

    public void Run()
    {
        // Assembles the FlowMap.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + y * mapWidth;

                if (drainageIndexesMap[index] == 0)
                    continue;

                float inflowAmount = amount >= 1 ? amount : 1;
                inflowAmount += getFlowFrom(x + 1, y, index);
                inflowAmount += getFlowFrom(x, y + 1, index);
                inflowAmount += getFlowFrom(x - 1, y, index);
                inflowAmount += getFlowFrom(x, y - 1, index);
                inflowAmount += getFlowFrom(x + 1, y + 1, index);
                inflowAmount += getFlowFrom(x + 1, y - 1, index);
                inflowAmount += getFlowFrom(x - 1, y + 1, index);
                inflowAmount += getFlowFrom(x - 1, y - 1, index);

                flowMap[index] = inflowAmount;
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

                if (logBase <= 1) logBase = 1.1f;

                float erodeValue = getErodeValueFromFlowIndex(index);
                //float erodeCount = 1;

                if (blur > 0)
                {
                    int blurSquareRadius = (int)Mathf.Ceil(blur);

                    for (int blurRelativeX = -blurSquareRadius; blurRelativeX <= blurSquareRadius; blurRelativeX++)
                    {
                        for (int blurRelativeY = -blurSquareRadius; blurRelativeY <= blurSquareRadius; blurRelativeY++)
                        {
                            if (blurRelativeX == 0 && blurRelativeY == 0)
                                continue;

                            int blurX = x + blurRelativeX;
                            int blurY = y + blurRelativeY;
                            if (blurY >= mapHeight) continue;
                            if (blurY < 0) continue;

                            float distance = Mathf.Sqrt(blurRelativeX * blurRelativeX + blurRelativeY * blurRelativeY);
                            if (distance > blur) continue;

                            float distanceRatio = 1 - distance / blur;
                            int blurIndex = blurX + blurY * mapWidth;

                            float blurErodeValue = getErodeValueFromFlowIndex(blurIndex);
                            blurErodeValue *= distanceRatio;
                            //erodeCount += distanceRatio;

                            if (blurErodeValue > erodeValue)
                                erodeValue = blurErodeValue;
                        }
                    }
                }

                if (erodeValue > height - waterLevel - MIN_WATER_HEIGHT)
                    erodeValue = height - waterLevel - MIN_WATER_HEIGHT;
                if (erodeValue < 0)
                    erodeValue = 0;
                inciseFlowMap[index] = erodeValue;
            }
        }
    }

    float getFlowFrom(int x, int y, int thisIndex)
    {
        if (y < 0) return 0;
        if (y >= mapHeight) return 0;
        if (x >= mapWidth) x -= mapWidth;
        if (x < 0) x += mapWidth;

        int nextIndex = x + y * mapWidth;

        if (drainageIndexesMap[nextIndex] != thisIndex)
            return 0;

        return flowMap[nextIndex];
    }


    float getErodeValueFromFlowIndex(int index)
    {
        float flowValue = flowMap[index];
        float flowHeight = heightMap[index];

        if (flowValue < 1) flowValue = 1;

        float erodeValue = (Mathf.Log(flowValue) / Mathf.Log(logBase)) * heightFactor;
        if (erodeValue < minAmount)
            erodeValue = 0;
        else
            erodeValue -= minAmount;

        if (erodeValue > flowHeight - waterLevel - MIN_WATER_HEIGHT)
            erodeValue = flowHeight - waterLevel - MIN_WATER_HEIGHT;
        else if (erodeValue > heightFactor)
            erodeValue = heightFactor;
        if (erodeValue < 0)
            erodeValue = 0;

        erodeValue = Mathf.Pow(Mathf.Abs(erodeValue), curveFactor);
        erodeValue = erodeValue * ((1 - heightInfluence) + heightInfluence * ((flowHeight - waterLevel) / (1 - waterLevel)));
        erodeValue *= strength;
        return erodeValue;
    }
}
