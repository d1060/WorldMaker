using System;
using SFB;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public partial class Map : MonoBehaviour
{
    public void ReplotRivers()
    {
        System.Random random = new System.Random();
        inciseFlowSettings.riverPlotSeed = random.Next();

        PerformPlotRiversRandomly();
    }

    public void RunPlotRiversFromButton()
    {
        PerformPlotRiversRandomly();
    }

    public ComputeShader randomPlotRivers;
    RenderTexture randomRiversRT;

    void PerformPlotRiversRandomly()
    {
        //bool useCpu = false;

        if (!inciseFlowSettings.plotRiversRandomly)
            return;

        //if (useCpu)
        //{
        //    SetupPlottingRiversPanel(plotRiversSettings.numIterations);
        //    ShowPlottingRiversPanel();
        //}

        GenerateHeightMap();
        if (connectivityMap == null)
            EstablishHeightmapConnectivity();

        RandomPlotRivers();

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = plotRiversRT;
        TextureManager.instance.FlowTexture.ReadPixels(new Rect(0, 0, plotRiversRT.width, plotRiversRT.height), 0, 0);
        RenderTexture.active = prevActive;
        TextureManager.instance.FlowTexture.Apply();

        //RenderTexture.ReleaseTemporary(randomRiversRT);

        //Destroy(randomRiversRT);
        //randomRiversRT = null;

        //PlotRivers.instance.Run(ref TextureManager.instance.ErodedHeightMap, ref flowTexture);
        //flowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowMap.png"));
        //HeightMap2Texture();
        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            TextureManager.instance.FlowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowMap.png"));
        }
        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);

        //connectivityMap = null;
        //distanceToWaterMap = null;
    }

    public ComputeShader inciseFlowErosion;

    void PerformInciseFlow(bool establishConnectivity, bool replotRivers, bool replotRandomRivers)
    {
        GenerateHeightMap();

        if (establishConnectivity)
            EstablishHeightmapConnectivity();

        InciseFlowErosion(heightMapBuffer);

        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            ImageTools.SaveTextureFloatArray(TextureManager.instance.FlowErosionMap, TextureManager.instance.Settings.textureWidth * 4, Path.Combine(Application.persistentDataPath, "Textures", "flowErosionMap.png"));
        }

        if (inciseFlowSettings.postBlur > 0)
        {
            float actualBlur = inciseFlowSettings.postBlur / 3;
            AverageHeightMap(actualBlur);
        }

        //ImageTools.SaveTextureRawCubemapFloatArray(TextureManager.instance.FlowErosionMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap-Blurred.png"), TextureManager.instance.FlowErosionMap.Max());

        if ((inciseFlowSettings.plotRivers && replotRivers) || (inciseFlowSettings.plotRiversRandomly && replotRandomRivers))
            InciseFlowPlotRivers();

        if (!inciseFlowSettings.plotRivers && !inciseFlowSettings.plotRiversRandomly)
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

        //ImageTools.SaveTextureRawCubemapFloatArray(TextureManager.instance.FlowErosionMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap.png"), TextureManager.instance.FlowErosionMap.Max());
        //int maxErosionIndex = -1;
        //float maxErosion = TextureManager.instance.FlowErosionMap.MaxAndIndex(ref maxErosionIndex);

        HeightMap2Texture();
        isEroded = true;
        UpdateSurfaceMaterialHeightMap();
        UpdateZoomCamMaterialProperties();
    }

    void InciseFlowErosion(ComputeBuffer heightMapBuffer)
    {
        ComputeBuffer flowMapBuffer = new ComputeBuffer(TextureManager.instance.InciseFlowMap.Length, sizeof(uint));
        flowMapBuffer.SetData(TextureManager.instance.InciseFlowMap, 0, 0, TextureManager.instance.InciseFlowMap.Length);

        if (TextureManager.instance.FlowErosionMap == null || TextureManager.instance.FlowErosionMap.Length != TextureManager.instance.HeightMap.Length)
            TextureManager.instance.InstantiateFlowErosionMap();

        ComputeBuffer inciseFlowMapBuffer = new ComputeBuffer(TextureManager.instance.FlowErosionMap.Length, sizeof(uint));

        heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

        inciseFlowErosion.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        inciseFlowErosion.SetFloat("exponent", inciseFlowSettings.exponent);
        inciseFlowErosion.SetFloat("heightFactor", 0.05f);
        inciseFlowErosion.SetFloat("strength", inciseFlowSettings.strength);
        inciseFlowErosion.SetFloat("minAmount", inciseFlowSettings.minAmount);
        inciseFlowErosion.SetFloat("curveFactor", inciseFlowSettings.chiselStrength);
        inciseFlowErosion.SetFloat("heightInfluence", inciseFlowSettings.heightInfluence);
        inciseFlowErosion.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        inciseFlowErosion.SetFloat("minimumHeight", MapData.instance.LowestHeight);
        inciseFlowErosion.SetFloat("maximumHeight", MapData.instance.HighestHeight);
        inciseFlowErosion.SetFloat("blur", inciseFlowSettings.preBlur);
        inciseFlowErosion.SetFloat("flowMapMaxValue", flowMapMaxValue);

        inciseFlowErosion.SetBuffer(0, "heightMap", heightMapBuffer);
        inciseFlowErosion.SetBuffer(0, "flowMap", flowMapBuffer);
        inciseFlowErosion.SetBuffer(0, "inciseFlowErosionMap", inciseFlowMapBuffer);

        inciseFlowErosion.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 16f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 16f), 6);

        inciseFlowMapBuffer.GetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);

        flowMapBuffer.Release();
        inciseFlowMapBuffer.Release();
    }

    public ComputeShader inciseFlowPlotRivers;
    RenderTexture plotRiversRT;
    void InciseFlowPlotRivers()
    {
        if (!inciseFlowSettings.plotRivers && !inciseFlowSettings.plotRiversRandomly)
            return;

        if (TextureManager.instance.FlowTexture == null || TextureManager.instance.FlowTexture.width != TextureManager.instance.Settings.textureWidth * 4 || TextureManager.instance.FlowTexture.height != TextureManager.instance.Settings.textureWidth * 2)
        {
            TextureManager.instance.InstantiateFlowTexture();
            //Color[] colors = new Color[6 * TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth];
            //TextureManager.instance.FlowTexture.SetPixels(colors);
            //TextureManager.instance.FlowTexture.Apply();
        }

        if (plotRiversRT != null && (plotRiversRT.width != 2 * TextureManager.instance.Settings.textureWidth || plotRiversRT.height != TextureManager.instance.Settings.textureWidth))
        {
            Destroy(plotRiversRT);
            plotRiversRT = null;
        }

        if (plotRiversRT == null)
        {
            plotRiversRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 32, RenderTextureFormat.ARGBHalf);
            plotRiversRT.wrapMode = TextureWrapMode.Repeat;
            plotRiversRT.name = "Heightmap Render Texture";
            plotRiversRT.enableRandomWrite = true;
            plotRiversRT.Create();
        }
        plotRiversRT.Release();

        if (inciseFlowSettings.plotRivers && TextureManager.instance.InciseFlowMap != null)
        {
            //float flowMaxValue = TextureManager.instance.InciseFlowMapMaxValue;

            ComputeBuffer flowMapBuffer = new ComputeBuffer(TextureManager.instance.InciseFlowMap.Length, sizeof(uint));
            flowMapBuffer.SetData(TextureManager.instance.InciseFlowMap, 0, 0, TextureManager.instance.InciseFlowMap.Length);

            //RenderTexture prevActive = RenderTexture.active;
            //RenderTexture.active = rTexture;
            inciseFlowPlotRivers.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
            inciseFlowPlotRivers.SetFloat("lowerLimit", inciseFlowSettings.LowerRiverAmount);
            inciseFlowPlotRivers.SetFloat("higherLimit", inciseFlowSettings.UpperRiverAmount);
            inciseFlowPlotRivers.SetFloat("maxValue", flowMapMaxValue);
            inciseFlowPlotRivers.SetFloat("riverExponent", inciseFlowSettings.riverExponent);
            inciseFlowPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });

            inciseFlowPlotRivers.SetTexture(0, "result", plotRiversRT);
            inciseFlowPlotRivers.SetBuffer(0, "inciseFlowMap", flowMapBuffer);

            inciseFlowPlotRivers.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 32f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 32f), 1);

            //RenderTexture.active = prevActive;
            //plotRiversRT.SaveToFile(Path.Combine(Application.persistentDataPath, "flowTexture1.png"));
            flowMapBuffer.Release();
        }

        if (inciseFlowSettings.plotRiversRandomly)
        {
            if (connectivityMap == null)
                EstablishHeightmapConnectivity();

            RandomPlotRivers();
        }

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = plotRiversRT;
        TextureManager.instance.FlowTexture.ReadPixels(new Rect(0, 0, plotRiversRT.width, plotRiversRT.height), 0, 0);
        RenderTexture.active = prevActive;
        TextureManager.instance.FlowTexture.Apply();

        //RenderTexture.ReleaseTemporary(plotRiversRT);
        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            TextureManager.instance.FlowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowMap.png"));
        }
        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);

        //Destroy(plotRiversRT);
        //plotRiversRT = null;
    }

    void RandomPlotRivers()
    {
        int actualNumberOfRiverSources = inciseFlowSettings.numberOfRivers;

        int[] riverFlowMask = new int[2 * TextureManager.instance.Settings.textureWidth * 4 * TextureManager.instance.Settings.textureWidth];

        System.Random random = new System.Random(inciseFlowSettings.riverPlotSeed);
        for (int i = 0; i < actualNumberOfRiverSources; i++)
        {
            Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
            Vector2 uv = pointInSpace.CartesianToPolarRatio(1);

            int mapX = (int)(uv.x * TextureManager.instance.Settings.textureWidth * 4);
            int mapY = (int)(uv.y * TextureManager.instance.Settings.textureWidth * 2);

            if (TextureManager.instance.HeightMapValueAtCoordinates(mapX, mapY) <= TextureManager.instance.Settings.waterLevel)
            {
                i--;
                continue;
            }

            int dropPointIndex = mapY * TextureManager.instance.Settings.textureWidth * 4 + mapX;

            riverFlowMask[dropPointIndex] = 1;
        }

        if (plotRiversRT != null && (plotRiversRT.width != 2 * TextureManager.instance.Settings.textureWidth || plotRiversRT.height != TextureManager.instance.Settings.textureWidth))
        {
            Destroy(plotRiversRT);
            plotRiversRT = null;
        }

        if (plotRiversRT == null)
        {
            plotRiversRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 32, RenderTextureFormat.ARGBHalf);
            plotRiversRT.wrapMode = TextureWrapMode.Repeat;
            plotRiversRT.name = "Heightmap Render Texture";
            plotRiversRT.enableRandomWrite = true;
            plotRiversRT.Create();
        }
        plotRiversRT.Release();

        ComputeBuffer riverFlowMaskBuffer = new ComputeBuffer(riverFlowMask.Length, sizeof(int));
        riverFlowMaskBuffer.SetData(riverFlowMask);

        ComputeBuffer connectivityMapBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
        connectivityMapBuffer.SetData(connectivityMap, 0, 0, connectivityMap.Length);

        ComputeBuffer flowMapBuffer = new ComputeBuffer(TextureManager.instance.InciseFlowMap.Length, sizeof(uint));
        flowMapBuffer.SetData(TextureManager.instance.InciseFlowMap, 0, 0, TextureManager.instance.InciseFlowMap.Length);

        ComputeBuffer alphasBuffer = new ComputeBuffer(TextureManager.instance.InciseFlowMap.Length, sizeof(float));

        randomPlotRivers.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        randomPlotRivers.SetInt("mapHeight", TextureManager.instance.Settings.textureWidth);
        randomPlotRivers.SetFloat("flowMapMaxValue", flowMapMaxValue);
        randomPlotRivers.SetFloat("riverExponent", inciseFlowSettings.riverExponent);
        randomPlotRivers.SetFloat("lowerLimit", inciseFlowSettings.LowerRiverAmount);
        randomPlotRivers.SetFloat("higherLimit", inciseFlowSettings.UpperRiverAmount);
        randomPlotRivers.SetInt("maxRiverLength", 1000);
        randomPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });
        randomPlotRivers.SetBuffer(0, "riverFlowMask", riverFlowMaskBuffer);
        randomPlotRivers.SetBuffer(0, "connectivityMap", connectivityMapBuffer);
        randomPlotRivers.SetBuffer(0, "flowMap", flowMapBuffer);
        randomPlotRivers.SetBuffer(0, "riverAlphas", alphasBuffer);

        randomPlotRivers.Dispatch(0, Mathf.CeilToInt(plotRiversRT.width / 32f), Mathf.CeilToInt(plotRiversRT.height / 32f), 1);

        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            float[] alphas = new float[TextureManager.instance.InciseFlowMap.Length];
            alphasBuffer.GetData(alphas);
            ImageTools.SaveTextureFloatArray(alphas, TextureManager.instance.Settings.textureWidth * 4, Path.Combine(Application.persistentDataPath, "Textures", "riverAlphas.png"));
        }

        randomPlotRivers.SetBuffer(1, "riverAlphas", alphasBuffer);
        randomPlotRivers.SetTexture(1, "result", plotRiversRT);

        randomPlotRivers.Dispatch(1, Mathf.CeilToInt(plotRiversRT.width / 32f), Mathf.CeilToInt(plotRiversRT.height / 32f), 1);

        riverFlowMaskBuffer.Release();
        connectivityMapBuffer.Release();
        flowMapBuffer.Release();
        alphasBuffer.Release();
    }

    public ComputeShader heightmapConnectivityShader;
    public ComputeShader heightmapFlowMapShader;
    public ComputeShader heightmapAverageShader;

    int[] connectivityMap;

    float flowMapMaxValue = 1000000;

    void EstablishHeightmapConnectivity()
    {
        if (connectivityMap == null || connectivityMap.Length != TextureManager.instance.HeightMap.Length)
        {
            connectivityMap = new int[TextureManager.instance.HeightMap.Length];
        }

        int numPasses = (int)(TextureManager.instance.Settings.textureWidth / 4);

        //float[] outputMap = new float[1];
        //outputMap[0] = 1;
        flowMapMaxValue = 1000000;

        heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

        ComputeBuffer connectivityMapBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
        connectivityMapBuffer.SetData(connectivityMap, 0, 0, connectivityMap.Length);

        ComputeBuffer distanceToWaterBuffer = new ComputeBuffer(TextureManager.instance.HeightMap.Length, sizeof(float));

        if (TextureManager.instance.InciseFlowMap == null || TextureManager.instance.InciseFlowMap.Length != TextureManager.instance.HeightMap.Length)
            TextureManager.instance.InstantiateInciseFlowMap();

        ComputeBuffer flowMapBuffer = new ComputeBuffer(TextureManager.instance.InciseFlowMap.Length, sizeof(uint));
        flowMapBuffer.SetData(TextureManager.instance.InciseFlowMap, 0, 0, TextureManager.instance.InciseFlowMap.Length);

        heightmapConnectivityShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        heightmapConnectivityShader.SetInt("mapHeight", TextureManager.instance.Settings.textureWidth);
        heightmapConnectivityShader.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        heightmapConnectivityShader.SetFloat("flowAmount", 1);
        heightmapConnectivityShader.SetFloat("maxFlow", flowMapMaxValue);
        heightmapConnectivityShader.SetFloat("upwardWeight", inciseFlowSettings.upwardWeight);
        heightmapConnectivityShader.SetFloat("downwardWeight", inciseFlowSettings.downwardWeight);
        heightmapConnectivityShader.SetFloat("distanceWeight", inciseFlowSettings.distanceWeight);
        heightmapConnectivityShader.SetInt("maxRiverLength", 1000);

        heightmapConnectivityShader.SetBuffer(0, "heightMap", heightMapBuffer);
        heightmapConnectivityShader.SetBuffer(0, "distanceMap", distanceToWaterBuffer);
        heightmapConnectivityShader.SetBuffer(0, "connectivityMap", connectivityMapBuffer);

        for (int i = 0; i < numPasses; i++)
        {
            //outputMap[0] = 1;
            //outputBuffer.SetData(outputMap);
            heightmapConnectivityShader.Dispatch(0, Mathf.CeilToInt((TextureManager.instance.Settings.textureWidth * 4) / 8f), Mathf.CeilToInt((TextureManager.instance.Settings.textureWidth * 2) / 8f), 1);
            //connectivityIndexesBuffer.GetData(connectivityMap);
            //connectivityMap.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth * 6, Path.Combine(Application.persistentDataPath, "connectivityMap-" + i + ".png"));
            //outputBuffer.GetData(outputMap);
            //if (outputMap[0] == 1)
            //    break;
        }

        connectivityMapBuffer.GetData(connectivityMap, 0, 0, connectivityMap.Length);

        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            connectivityMap.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth * 4, Path.Combine(Application.persistentDataPath, "Textures", "connectivityMap.png"));
        }

        //Flow Map
        heightmapConnectivityShader.SetBuffer(1, "connectivityMap", connectivityMapBuffer);
        heightmapConnectivityShader.SetBuffer(1, "flowMap", flowMapBuffer);
        heightmapConnectivityShader.Dispatch(1, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 8f), 1);

        flowMapBuffer.GetData(TextureManager.instance.InciseFlowMap, 0, 0, TextureManager.instance.InciseFlowMap.Length);

        flowMapMaxValue = TextureManager.instance.InciseFlowMapMaxValue;

        if (saveTemporaryTextures)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
            ImageTools.SaveTextureUIntArray(TextureManager.instance.InciseFlowMap, TextureManager.instance.Settings.textureWidth * 4, Path.Combine(Application.persistentDataPath, "Textures", "inciseFlowMap.png"), flowMapMaxValue);
        }

        connectivityMapBuffer.Release();
        distanceToWaterBuffer.Release();
        flowMapBuffer.Release();
    }

    void AverageHeightMap(float blur)
    {
        ComputeBuffer heightBuffer = new ComputeBuffer(TextureManager.instance.FlowErosionMap.Length, sizeof(float));
        heightBuffer.SetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);

        ComputeBuffer targetBuffer = new ComputeBuffer(TextureManager.instance.FlowErosionMap.Length, sizeof(float));
        targetBuffer.SetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);

        heightmapAverageShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        heightmapAverageShader.SetFloat("blur", blur);

        int i = 0;
        for (i = 0; i < Mathf.CeilToInt(blur); i++)
        {
            heightmapAverageShader.SetInt("blurStep", i + 1);
            if (i % 2 == 0)
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap", heightBuffer);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap", targetBuffer);
            }
            else
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap", targetBuffer);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap", heightBuffer);
            }

            heightmapAverageShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 8f), 1);
        }

        if (i % 2 == 0)
        {
            heightBuffer.GetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);
        }
        else
        {
            targetBuffer.GetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);
        }

        heightBuffer.Release();
        targetBuffer.Release();
    }
}
