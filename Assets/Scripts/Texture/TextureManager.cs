using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//
//       CUBEMAP Outline
//
//         +--------+
//         |  Top   |
//         |   +y   |
// +-------+--------+-------+------+
// |  Left |  Front | Right | Back |
// |  -x   |   +z   |  +x   |  -z  |
// +-------+--------+-------+------+
//         | Bottom |
//         |   -y   |
//         +--------+
//
//                  ARRAY Outline
//
// +-------+-------+-------+------+------+--------+
// |  Left | Front | Right | Back | Top  | Bottom |
// |  -x   |  +z   |  +x   |  -z  |  +y  |   -y   |
// +-------+-------+-------+------+------+--------+
//

public class TextureManager
{
    #region Singleton
    static TextureManager myInstance = null;

    TextureManager()
    {
    }

    public static TextureManager instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new TextureManager();
            return myInstance;
        }
    }
    #endregion

    public TextureSettings settings;
    public TextureSettings Settings { get { return settings; } set { settings = value; } }

    Texture2D landmap = null;
    public Texture2D Landmap { get { return landmap; } set { if (value == null) { UnityEngine.Object.Destroy(landmap); } landmap = value; } }
    public void InstantiateLandmap()
    {
        landmap = new Texture2D(settings.textureWidth * 4, settings.textureWidth * 2, TextureFormat.RGBA64, false, true);
    }

    Texture2D landmask = null;
    public Texture2D Landmask { get { return landmask; } set { if (value == null) { UnityEngine.Object.Destroy(landmask); } landmask = value; } }
    public void InstantiateLandmask()
    {
        landmask = new Texture2D(settings.textureWidth * 4, settings.textureWidth * 2, TextureFormat.RGBA64, false, true);
    }

    Texture2D flowTexture = null;
    public Texture2D FlowTexture { get { return flowTexture; } set { if (value == null) { UnityEngine.Object.Destroy(flowTexture); } flowTexture = value; } }
    public void InstantiateFlowTexture()
    {
        flowTexture = new Texture2D(settings.textureWidth * 4, settings.textureWidth * 2, TextureFormat.RGBA64, false, true);
    }

    //Texture2D flowTextureRandom = null;
    //public Texture2D FlowTextureRandom { get { return flowTextureRandom; } set { if (value == null) { UnityEngine.Object.Destroy(flowTextureRandom); } flowTextureRandom = value; } }
    //public void InstantiateFlowTextureRandom()
    //{
    //    flowTextureRandom = new Texture2D(settings.textureWidth * 6, settings.textureWidth, TextureFormat.RGBA64, false, true);
    //}

    //float[] erodedHeightMap;
    //public float[] ErodedHeightMap { get { return erodedHeightMap; } set { erodedHeightMap = value; } }
    //public void InstantiateErodedHeightMap()
    //{
    //    erodedHeightMap = new float[settings.textureWidth * settings.textureWidth * 6];
    //}

    float[] heightMap1;
    float[] heightMap2;
    float[] heightMap3;
    float[] heightMap4;
    float[] heightMap5;
    float[] heightMap6;
    public float[] HeightMap1 { get { return heightMap1; } set { heightMap1 = value; } }
    public float[] HeightMap2 { get { return heightMap2; } set { heightMap2 = value; } }
    public float[] HeightMap3 { get { return heightMap3; } set { heightMap3 = value; } }
    public float[] HeightMap4 { get { return heightMap4; } set { heightMap4 = value; } }
    public float[] HeightMap5 { get { return heightMap5; } set { heightMap5 = value; } }
    public float[] HeightMap6 { get { return heightMap6; } set { heightMap6 = value; } }
    public void InstantiateHeightMap()
    {
        heightMap1 = new float[settings.textureWidth * settings.textureWidth];
        heightMap2 = new float[settings.textureWidth * settings.textureWidth];
        heightMap3 = new float[settings.textureWidth * settings.textureWidth];
        heightMap4 = new float[settings.textureWidth * settings.textureWidth];
        heightMap5 = new float[settings.textureWidth * settings.textureWidth];
        heightMap6 = new float[settings.textureWidth * settings.textureWidth];
    }
    public void HeightMapMinMaxHeights(ref float minHeight, ref float maxHeight)
    {
        float minHeight1 = float.PositiveInfinity;
        float maxHeight1 = float.NegativeInfinity;
        float minHeight2 = float.PositiveInfinity;
        float maxHeight2 = float.NegativeInfinity;
        float minHeight3 = float.PositiveInfinity;
        float maxHeight3 = float.NegativeInfinity;
        float minHeight4 = float.PositiveInfinity;
        float maxHeight4 = float.NegativeInfinity;
        float minHeight5 = float.PositiveInfinity;
        float maxHeight5 = float.NegativeInfinity;
        float minHeight6 = float.PositiveInfinity;
        float maxHeight6 = float.NegativeInfinity;

        heightMap1.MinMax(ref minHeight1, ref maxHeight1);
        heightMap2.MinMax(ref minHeight2, ref maxHeight2);
        heightMap3.MinMax(ref minHeight3, ref maxHeight3);
        heightMap4.MinMax(ref minHeight4, ref maxHeight4);
        heightMap5.MinMax(ref minHeight5, ref maxHeight5);
        heightMap6.MinMax(ref minHeight6, ref maxHeight6);

        if (maxHeight1 >= maxHeight2 && maxHeight1 >= maxHeight3 && maxHeight1 >= maxHeight4 && maxHeight1 >= maxHeight5 && maxHeight1 >= maxHeight6)
            maxHeight = maxHeight1;
        else if (maxHeight2 >= maxHeight3 && maxHeight2 >= maxHeight4 && maxHeight2 >= maxHeight5 && maxHeight2 >= maxHeight6)
            maxHeight = maxHeight2;
        else if (maxHeight3 >= maxHeight4 && maxHeight3 >= maxHeight5 && maxHeight3 >= maxHeight6)
            maxHeight = maxHeight3;
        else if (maxHeight4 >= maxHeight5 && maxHeight4 >= maxHeight6)
            maxHeight = maxHeight4;
        else if (maxHeight5 >= maxHeight6)
            maxHeight = maxHeight5;
        else 
            maxHeight = maxHeight6;

        if (minHeight1 <= minHeight2 && minHeight1 <= minHeight3 && minHeight1 <= minHeight4 && minHeight1 <= minHeight5 && minHeight1 <= minHeight6)
            minHeight = minHeight1;
        else if (minHeight2 <= minHeight3 && minHeight2 <= minHeight4 && minHeight2 <= minHeight5 && minHeight2 <= minHeight6)
            minHeight = minHeight2;
        else if (minHeight3 <= minHeight4 && minHeight3 <= minHeight5 && minHeight3 <= minHeight6)
            minHeight = minHeight3;
        else if (minHeight4 <= minHeight5 && minHeight4 <= minHeight6)
            minHeight = minHeight4;
        else if (minHeight5 <= minHeight6)
            minHeight = minHeight5;
        else
            minHeight = minHeight6;
    }

    float interpolate(ref float[] map, Vector2 uv)
    {
        int leftX = Mathf.FloorToInt(uv.x);
        int rightX = Mathf.CeilToInt(uv.x);
        int bottomY = Mathf.FloorToInt(uv.y);
        int topY = Mathf.CeilToInt(uv.y);

        float deltaX = uv.x - leftX;
        float deltaY = uv.y - bottomY;

        if (rightX >= settings.textureWidth)
            rightX -= settings.textureWidth;
        if (rightX < 0)
            rightX += settings.textureWidth;
        if (leftX >= settings.textureWidth)
            leftX -= settings.textureWidth;
        if (leftX < 0)
            leftX += settings.textureWidth;
        if (topY >= settings.textureWidth) topY = settings.textureWidth - 1;
        if (topY < 0) topY = 0;
        if (bottomY >= settings.textureWidth) bottomY = settings.textureWidth - 1;
        if (bottomY < 0) bottomY = 0;

        int indexBL = leftX + settings.textureWidth * bottomY;
        int indexBR = rightX + settings.textureWidth * bottomY;
        int indexTR = rightX + settings.textureWidth * topY;
        int indexTL = leftX + settings.textureWidth * topY;

        float valueBL = map[indexBL];
        float valueBR = map[indexBR];
        float valueTL = map[indexTL];
        float valueTR = map[indexTR];

        float valueXdelta0 = (valueBR - valueBL) * deltaX + valueBL;
        float valueXdelta1 = (valueTR - valueTL) * deltaX + valueTL;

        float value = (valueXdelta1 - valueXdelta0) * deltaY + valueXdelta0;
        return value;
    }

    public float HeightMapValueAtUV(Vector2 uv)
    {
        Vector3 cartesian = uv.PolarRatioToCartesian(1);
        Vector3 cubemap = cartesian.CartesianToCubemap();
        Vector2 cubemapUV = new Vector2(cubemap.x, cubemap.y);

        if (cubemap.z == 0)
        {
            return interpolate(ref heightMap1, cubemapUV);
        }
        else if (cubemap.z == 1)
        {
            return interpolate(ref heightMap2, cubemapUV);
        }
        else if (cubemap.z == 2)
        {
            return interpolate(ref heightMap3, cubemapUV);
        }
        else if (cubemap.z == 3)
        {
            return interpolate(ref heightMap4, cubemapUV);
        }
        else if (cubemap.z == 4)
        {
            return interpolate(ref heightMap5, cubemapUV);
        }
        else
        {
            return interpolate(ref heightMap6, cubemapUV);
        }
    }

    public float HeightMapValueAtCoordinates(int x, int y, int z)
    {
        int index = x + y * Settings.textureWidth;

        if (index < 0 || index >= heightMap1.Length)
        {
            return 0;
        }

        if (z == 0)
        {
            return heightMap1[index];
        }
        else if (z == 1)
        {
            return heightMap2[index];
        }
        else if (z == 2)
        {
            return heightMap3[index];
        }
        else if (z == 3)
        {
            return heightMap4[index];
        }
        else if (z == 4)
        {
            return heightMap5[index];
        }
        else
        {
            return heightMap6[index];
        }
    }

    public void SetHeightAtCoordinates(int x, int y, int z, float newHeight)
    {
        int index = x + y * Settings.textureWidth;

        if (index < 0 || index >= heightMap1.Length)
        {
            return;
        }

        if (z == 0)
        {
            heightMap1[index] = newHeight;
        }
        else if (z == 1)
        {
            heightMap2[index] = newHeight;
        }
        else if (z == 2)
        {
            heightMap3[index] = newHeight;
        }
        else if (z == 3)
        {
            heightMap4[index] = newHeight;
        }
        else if (z == 4)
        {
            heightMap5[index] = newHeight;
        }
        else
        {
            heightMap6[index] = newHeight;
        }
    }


    //float[] mergedHeightMap;
    //public float[] MergedHeightMap { get { return mergedHeightMap; } set { mergedHeightMap = value; } }
    //public void InstantiateMergedHeightMap()
    //{
    //    mergedHeightMap = new float[settings.textureWidth * settings.textureWidth * 6];
    //}

    //float[] humidityMap;
    //public float[] HumidityMap { get { return humidityMap; } set { humidityMap = value; } }
    //public void InstantiateHumidityMap()
    //{
    //    humidityMap = new float[settings.textureWidth * settings.textureWidth * 6];
    //}

    float[] inciseFlowMap1;
    float[] inciseFlowMap2;
    float[] inciseFlowMap3;
    float[] inciseFlowMap4;
    float[] inciseFlowMap5;
    float[] inciseFlowMap6;
    public float[] InciseFlowMap1 { get { return inciseFlowMap1; } set { inciseFlowMap1 = value; } }
    public float[] InciseFlowMap2 { get { return inciseFlowMap2; } set { inciseFlowMap2 = value; } }
    public float[] InciseFlowMap3 { get { return inciseFlowMap3; } set { inciseFlowMap3 = value; } }
    public float[] InciseFlowMap4 { get { return inciseFlowMap4; } set { inciseFlowMap4 = value; } }
    public float[] InciseFlowMap5 { get { return inciseFlowMap5; } set { inciseFlowMap5 = value; } }
    public float[] InciseFlowMap6 { get { return inciseFlowMap6; } set { inciseFlowMap6 = value; } }
    public float[] InciseFlowMap 
    {
        set 
        {
            inciseFlowMap1 = value;
            inciseFlowMap2 = value;
            inciseFlowMap3 = value;
            inciseFlowMap4 = value;
            inciseFlowMap5 = value;
            inciseFlowMap6 = value;
        }
    }

    public float InciseFlowMapMaxValue
    {
        get
        {
            float max1 = inciseFlowMap1.Max();
            float max2 = inciseFlowMap2.Max();
            float max3 = inciseFlowMap3.Max();
            float max4 = inciseFlowMap4.Max();
            float max5 = inciseFlowMap5.Max();
            float max6 = inciseFlowMap6.Max();

            if (max1 >= max2 && max1 >= max3 && max1 >= max4 && max1 >= max5 && max1 >= max6)
                return max1;
            if (max2 >= max3 && max2 >= max4 && max2 >= max5 && max2 >= max6)
                return max2;
            if (max3 >= max4 && max3 >= max5 && max3 >= max6)
                return max3;
            if (max4 >= max5 && max4 >= max6)
                return max4;
            if (max5 >= max6)
                return max5;
            return max6;
        }
    }

    public void InstantiateInciseFlowMap()
    {
        inciseFlowMap1 = new float[settings.textureWidth * settings.textureWidth];
        inciseFlowMap2 = new float[settings.textureWidth * settings.textureWidth];
        inciseFlowMap3 = new float[settings.textureWidth * settings.textureWidth];
        inciseFlowMap4 = new float[settings.textureWidth * settings.textureWidth];
        inciseFlowMap5 = new float[settings.textureWidth * settings.textureWidth];
        inciseFlowMap6 = new float[settings.textureWidth * settings.textureWidth];
    }

    float[] flowErosionMap1;
    float[] flowErosionMap2;
    float[] flowErosionMap3;
    float[] flowErosionMap4;
    float[] flowErosionMap5;
    float[] flowErosionMap6;
    public float[] FlowErosionMap1 { get { return flowErosionMap1; } set { flowErosionMap1 = value; } }
    public float[] FlowErosionMap2 { get { return flowErosionMap2; } set { flowErosionMap2 = value; } }
    public float[] FlowErosionMap3 { get { return flowErosionMap3; } set { flowErosionMap3 = value; } }
    public float[] FlowErosionMap4 { get { return flowErosionMap4; } set { flowErosionMap4 = value; } }
    public float[] FlowErosionMap5 { get { return flowErosionMap5; } set { flowErosionMap5 = value; } }
    public float[] FlowErosionMap6 { get { return flowErosionMap6; } set { flowErosionMap6 = value; } }
    public int FlowErosionMapLength { get { return flowErosionMap1 == null ? 0 : flowErosionMap1.Length; } }
    public float[] FlowErosionMap
    {
        set
        {
            flowErosionMap1 = value;
            flowErosionMap2 = value;
            flowErosionMap3 = value;
            flowErosionMap4 = value;
            flowErosionMap5 = value;
            flowErosionMap6 = value;
        }
    }
    public float FlowErosionMapMaxValue
    {
        get
        {
            float max1 = flowErosionMap1.Max();
            float max2 = flowErosionMap2.Max();
            float max3 = flowErosionMap3.Max();
            float max4 = flowErosionMap4.Max();
            float max5 = flowErosionMap5.Max();
            float max6 = flowErosionMap6.Max();

            if (max1 >= max2 && max1 >= max3 && max1 >= max4 && max1 >= max5 && max1 >= max6)
                return max1;
            if (max2 >= max3 && max2 >= max4 && max2 >= max5 && max2 >= max6)
                return max2;
            if (max3 >= max4 && max3 >= max5 && max3 >= max6)
                return max3;
            if (max4 >= max5 && max4 >= max6)
                return max4;
            if (max5 >= max6)
                return max5;
            return max6;
        }
    }

    public void InstantiateFlowErosionMap()
    {
        flowErosionMap1 = new float[settings.textureWidth * settings.textureWidth];
        flowErosionMap2 = new float[settings.textureWidth * settings.textureWidth];
        flowErosionMap3 = new float[settings.textureWidth * settings.textureWidth];
        flowErosionMap4 = new float[settings.textureWidth * settings.textureWidth];
        flowErosionMap5 = new float[settings.textureWidth * settings.textureWidth];
        flowErosionMap6 = new float[settings.textureWidth * settings.textureWidth];
    }
}
