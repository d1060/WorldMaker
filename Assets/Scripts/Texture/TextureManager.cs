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

    float[] heightMap;
    public float[] HeightMap { get { return heightMap; } set { heightMap = value; } }
    public void InstantiateHeightMap()
    {
        if (heightMap == null || heightMap.Length != settings.textureWidth * 4 * settings.textureWidth * 2)
            heightMap = new float[settings.textureWidth * 4 * settings.textureWidth * 2];
    }
    public void HeightMapMinMaxHeights(ref float minHeight, ref float maxHeight)
    {
        minHeight = float.PositiveInfinity;
        maxHeight = float.NegativeInfinity;

        heightMap.MinMax(ref minHeight, ref maxHeight);
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
        Vector2 textureUV = new Vector2(uv.x * settings.textureWidth * 4, uv.y * settings.textureWidth * 2);

        return interpolate(ref heightMap, textureUV);
    }

    public float HeightMapValueAtCoordinates(int x, int y)
    {
        int index = x + y * Settings.textureWidth * 4;

        if (index < 0 || index >= heightMap.Length)
        {
            return 0;
        }

        return heightMap[index];
    }

    public void SetHeightAtCoordinates(int x, int y, float newHeight)
    {
        int index = x + y * Settings.textureWidth;

        if (index < 0 || index >= heightMap.Length)
        {
            return;
        }

        heightMap[index] = newHeight;
    }

    uint[] inciseFlowMap;
    public uint[] InciseFlowMap { get { return inciseFlowMap; } set { inciseFlowMap = value; } }
    public uint InciseFlowMapMaxValue
    {
        get
        {
            uint max = 0;
            for (int i = 1; i < inciseFlowMap.Length; i++)
            {
                if (max < inciseFlowMap[i])
                    max = inciseFlowMap[i];
            }
            return max;
        }
    }

    public void InstantiateInciseFlowMap()
    {
        if (inciseFlowMap == null || inciseFlowMap.Length != settings.textureWidth * 4 * settings.textureWidth * 2)
            inciseFlowMap = new uint[settings.textureWidth * 4 * settings.textureWidth * 2];
    }

    float[] flowErosionMap;
    public float[] FlowErosionMap { get { return flowErosionMap; } set { flowErosionMap = value; } }
    public float FlowErosionMapMaxValue
    {
        get
        {
            float max = flowErosionMap.Max();
            return max;
        }
    }

    public void InstantiateFlowErosionMap()
    {
        if (flowErosionMap == null || flowErosionMap.Length != settings.textureWidth * 4 * settings.textureWidth * 2)
            flowErosionMap = new float[settings.textureWidth * 4 * settings.textureWidth * 2];
    }
}
