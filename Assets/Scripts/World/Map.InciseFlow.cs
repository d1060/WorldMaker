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
        if (connectivityMap1 == null)
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
        //planetSurfaceMaterial.SetTexture("_HeightMap", heightmap);
        //planetSurfaceMaterial.SetFloat("_HeightmapWidth", heightmapRT.width);
        //planetSurfaceMaterial.SetFloat("_HeightmapHeight", heightmapRT.height);
        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
        //planetSurfaceMaterial.SetInt("_IsEroded", 1);

        //connectivityMap = null;
        //distanceToWaterMap = null;
    }

    public ComputeShader inciseFlowErosion;

    void PerformInciseFlow(bool establishConnectivity, bool replotRivers, bool replotRandomRivers)
    {
        GenerateHeightMap();

        if (establishConnectivity)
            EstablishHeightmapConnectivity();

        InciseFlowErosion();

        //float maxErosion = TextureManager.instance.FlowErosionMapMaxValue;
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap1.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap2.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap3.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap4.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap5.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.FlowErosionMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "flowErosionMap6.png"));

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
        UpdateSurfaceMaterialHeightMap(true);
    }

    void InciseFlowErosion()
    {
        ComputeBuffer flowMapBuffer12 = new ComputeBuffer(TextureManager.instance.InciseFlowMap1.Length * 2, sizeof(float));
        flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap1, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
        flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap2, 0, TextureManager.instance.InciseFlowMap1.Length, TextureManager.instance.InciseFlowMap1.Length);

        ComputeBuffer flowMapBuffer34 = new ComputeBuffer(TextureManager.instance.InciseFlowMap3.Length * 2, sizeof(float));
        flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap3, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
        flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap4, 0, TextureManager.instance.InciseFlowMap1.Length, TextureManager.instance.InciseFlowMap1.Length);

        ComputeBuffer flowMapBuffer56 = new ComputeBuffer(TextureManager.instance.InciseFlowMap5.Length * 2, sizeof(float));
        flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap5, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
        flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap6, 0, TextureManager.instance.InciseFlowMap1.Length, TextureManager.instance.InciseFlowMap1.Length);

        if (TextureManager.instance.FlowErosionMap1 == null || TextureManager.instance.FlowErosionMap1.Length != TextureManager.instance.HeightMap1.Length)
            TextureManager.instance.InstantiateFlowErosionMap();

        ComputeBuffer inciseFlowMapBuffer12 = new ComputeBuffer(TextureManager.instance.FlowErosionMap1.Length * 2, sizeof(float));
        //inciseFlowMapBuffer12.SetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        //inciseFlowMapBuffer12.SetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);

        ComputeBuffer inciseFlowMapBuffer34 = new ComputeBuffer(TextureManager.instance.FlowErosionMap3.Length * 2, sizeof(float));
        //inciseFlowMapBuffer34.SetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        //inciseFlowMapBuffer34.SetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);

        ComputeBuffer inciseFlowMapBuffer56 = new ComputeBuffer(TextureManager.instance.FlowErosionMap5.Length * 2, sizeof(float));
        //inciseFlowMapBuffer56.SetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        //inciseFlowMapBuffer56.SetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);

        ComputeBuffer heightMapBuffer12 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
        heightMapBuffer12.SetData(TextureManager.instance.HeightMap1, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer12.SetData(TextureManager.instance.HeightMap2, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        ComputeBuffer heightMapBuffer34 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length * 2, sizeof(float));
        heightMapBuffer34.SetData(TextureManager.instance.HeightMap3, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer34.SetData(TextureManager.instance.HeightMap4, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        ComputeBuffer heightMapBuffer56 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length * 2, sizeof(float));
        heightMapBuffer56.SetData(TextureManager.instance.HeightMap5, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer56.SetData(TextureManager.instance.HeightMap6, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        inciseFlowErosion.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        inciseFlowErosion.SetFloat("exponent", inciseFlowSettings.exponent);
        inciseFlowErosion.SetFloat("heightFactor", 0.05f);
        inciseFlowErosion.SetFloat("strength", inciseFlowSettings.strength);
        inciseFlowErosion.SetFloat("minAmount", inciseFlowSettings.minAmount);
        inciseFlowErosion.SetFloat("curveFactor", inciseFlowSettings.chiselStrength);
        inciseFlowErosion.SetFloat("heightInfluence", inciseFlowSettings.heightInfluence);
        inciseFlowErosion.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        inciseFlowErosion.SetFloat("blur", inciseFlowSettings.preBlur);
        inciseFlowErosion.SetFloat("flowMapMaxValue", flowMapMaxValue);
        inciseFlowErosion.SetBuffer(0, "heightMap12", heightMapBuffer12);
        inciseFlowErosion.SetBuffer(0, "heightMap34", heightMapBuffer34);
        inciseFlowErosion.SetBuffer(0, "heightMap56", heightMapBuffer56);
        inciseFlowErosion.SetBuffer(0, "flowMap12", flowMapBuffer12);
        inciseFlowErosion.SetBuffer(0, "flowMap34", flowMapBuffer34);
        inciseFlowErosion.SetBuffer(0, "flowMap56", flowMapBuffer56);
        inciseFlowErosion.SetBuffer(0, "inciseFlowErosionMap12", inciseFlowMapBuffer12);
        inciseFlowErosion.SetBuffer(0, "inciseFlowErosionMap34", inciseFlowMapBuffer34);
        inciseFlowErosion.SetBuffer(0, "inciseFlowErosionMap56", inciseFlowMapBuffer56);

        inciseFlowErosion.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 16f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 16f), 6);

        inciseFlowMapBuffer12.GetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        inciseFlowMapBuffer12.GetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap2.Length, TextureManager.instance.FlowErosionMap2.Length);
        inciseFlowMapBuffer34.GetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap3.Length);
        inciseFlowMapBuffer34.GetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap4.Length, TextureManager.instance.FlowErosionMap4.Length);
        inciseFlowMapBuffer56.GetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap5.Length);
        inciseFlowMapBuffer56.GetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap6.Length, TextureManager.instance.FlowErosionMap6.Length);

        heightMapBuffer12.Release();
        heightMapBuffer34.Release();
        heightMapBuffer56.Release();
        flowMapBuffer12.Release();
        flowMapBuffer34.Release();
        flowMapBuffer56.Release();
        inciseFlowMapBuffer12.Release();
        inciseFlowMapBuffer34.Release();
        inciseFlowMapBuffer56.Release();
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

        if (inciseFlowSettings.plotRivers && TextureManager.instance.InciseFlowMap1 != null)
        {
            float flowMaxValue = TextureManager.instance.InciseFlowMapMaxValue;

            ComputeBuffer flowMapBuffer12 = new ComputeBuffer(TextureManager.instance.InciseFlowMap1.Length * 2, sizeof(float));
            flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap1, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
            flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap2, 0, TextureManager.instance.InciseFlowMap2.Length, TextureManager.instance.InciseFlowMap2.Length);

            ComputeBuffer flowMapBuffer34 = new ComputeBuffer(TextureManager.instance.InciseFlowMap3.Length * 2, sizeof(float));
            flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap3, 0, 0, TextureManager.instance.InciseFlowMap3.Length);
            flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap4, 0, TextureManager.instance.InciseFlowMap4.Length, TextureManager.instance.InciseFlowMap4.Length);

            ComputeBuffer flowMapBuffer56 = new ComputeBuffer(TextureManager.instance.InciseFlowMap5.Length * 2, sizeof(float));
            flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap5, 0, 0, TextureManager.instance.InciseFlowMap5.Length);
            flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap6, 0, TextureManager.instance.InciseFlowMap6.Length, TextureManager.instance.InciseFlowMap6.Length);

            //RenderTexture prevActive = RenderTexture.active;
            //RenderTexture.active = rTexture;
            inciseFlowPlotRivers.SetTexture(0, "result", plotRiversRT);
            inciseFlowPlotRivers.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
            inciseFlowPlotRivers.SetBuffer(0, "inciseFlowMap12", flowMapBuffer12);
            inciseFlowPlotRivers.SetBuffer(0, "inciseFlowMap34", flowMapBuffer34);
            inciseFlowPlotRivers.SetBuffer(0, "inciseFlowMap56", flowMapBuffer56);
            inciseFlowPlotRivers.SetFloat("lowerLimit", inciseFlowSettings.LowerRiverAmount);
            inciseFlowPlotRivers.SetFloat("higherLimit", inciseFlowSettings.UpperRiverAmount);
            inciseFlowPlotRivers.SetFloat("maxValue", flowMaxValue);
            inciseFlowPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });

            inciseFlowPlotRivers.Dispatch(0, Mathf.CeilToInt(plotRiversRT.width / 32f), Mathf.CeilToInt(plotRiversRT.height / 32f), 1);

            //RenderTexture.active = prevActive;
            //plotRiversRT.SaveToFile(Path.Combine(Application.persistentDataPath, "flowTexture1.png"));
            flowMapBuffer12.Release();
            flowMapBuffer34.Release();
            flowMapBuffer56.Release();
        }

        if (inciseFlowSettings.plotRiversRandomly)
        {
            //GenerateHeightMap();
            if (connectivityMap1 == null)
                EstablishHeightmapConnectivity();
            CalculateLandMask();

            RandomPlotRivers();
        }

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = plotRiversRT;
        TextureManager.instance.FlowTexture.ReadPixels(new Rect(0, 0, plotRiversRT.width, plotRiversRT.height), 0, 0);
        RenderTexture.active = prevActive;
        TextureManager.instance.FlowTexture.Apply();

        //RenderTexture.ReleaseTemporary(plotRiversRT);

        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);

        //Destroy(plotRiversRT);
        //plotRiversRT = null;
    }

    void RandomPlotRivers()
    {
        int actualNumberOfRiverSources = inciseFlowSettings.numberOfRivers;
        if (actualNumberOfRiverSources > landMaskCount)
            actualNumberOfRiverSources = landMaskCount;

        int[] riverFlowMask12 = new int[2 * TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth];
        int[] riverFlowMask34 = new int[2 * TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth];
        int[] riverFlowMask56 = new int[2 * TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth];

        System.Random random = new System.Random(inciseFlowSettings.riverPlotSeed);
        for (int i = 0; i < actualNumberOfRiverSources; i++)
        {
            Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
            Vector3 cubemap = pointInSpace.CartesianToCubemap();

            int mapX = (int)(cubemap.x * TextureManager.instance.Settings.textureWidth);
            int mapY = (int)(cubemap.y * TextureManager.instance.Settings.textureWidth);
            int mapZ = (int)cubemap.z;

            if (TextureManager.instance.HeightMapValueAtCoordinates(mapX, mapY, mapZ) <= TextureManager.instance.Settings.waterLevel)
            {
                i--;
                continue;
            }

            int dropPointIndex = mapY * TextureManager.instance.Settings.textureWidth + mapX;
            if (mapZ == 1 || mapZ == 3 || mapZ == 5)
            {
                dropPointIndex += TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth;
            }

            if (mapZ == 0 || mapZ == 1)
            {
                riverFlowMask12[dropPointIndex] = 1;
            }
            else if (mapZ == 2 || mapZ == 3)
            {
                riverFlowMask34[dropPointIndex] = 1;
            }
            else if (mapZ == 4 || mapZ == 5)
            {
                riverFlowMask56[dropPointIndex] = 1;
            }
        }

        ComputeBuffer riverFlowMaskBuffer12 = new ComputeBuffer(riverFlowMask12.Length, sizeof(int));
        riverFlowMaskBuffer12.SetData(riverFlowMask12);

        ComputeBuffer riverFlowMaskBuffer34 = new ComputeBuffer(riverFlowMask34.Length, sizeof(int));
        riverFlowMaskBuffer34.SetData(riverFlowMask34);

        ComputeBuffer riverFlowMaskBuffer56 = new ComputeBuffer(riverFlowMask56.Length, sizeof(int));
        riverFlowMaskBuffer56.SetData(riverFlowMask56);

        ComputeBuffer connectivityMapBuffer12 = new ComputeBuffer(connectivityMap1.Length * 2, sizeof(float));
        connectivityMapBuffer12.SetData(connectivityMap1, 0, 0, connectivityMap1.Length);
        connectivityMapBuffer12.SetData(connectivityMap2, 0, connectivityMap2.Length, connectivityMap2.Length);

        ComputeBuffer connectivityMapBuffer34 = new ComputeBuffer(connectivityMap3.Length * 2, sizeof(float));
        connectivityMapBuffer34.SetData(connectivityMap3, 0, 0, connectivityMap3.Length);
        connectivityMapBuffer34.SetData(connectivityMap3, 0, connectivityMap4.Length, connectivityMap4.Length);

        ComputeBuffer connectivityMapBuffer56 = new ComputeBuffer(connectivityMap5.Length * 2, sizeof(float));
        connectivityMapBuffer56.SetData(connectivityMap5, 0, 0, connectivityMap5.Length);
        connectivityMapBuffer56.SetData(connectivityMap6, 0, connectivityMap6.Length, connectivityMap6.Length);

        randomPlotRivers.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        randomPlotRivers.SetInt("mapHeight", TextureManager.instance.Settings.textureWidth);
        randomPlotRivers.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        randomPlotRivers.SetFloat("startingAlpha", inciseFlowSettings.startingAlpha);
        randomPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });
        randomPlotRivers.SetBuffer(0, "riverFlowMask12", riverFlowMaskBuffer12);
        randomPlotRivers.SetBuffer(0, "riverFlowMask34", riverFlowMaskBuffer34);
        randomPlotRivers.SetBuffer(0, "riverFlowMask56", riverFlowMaskBuffer56);
        randomPlotRivers.SetBuffer(0, "connectivityMap12", connectivityMapBuffer12);
        randomPlotRivers.SetBuffer(0, "connectivityMap34", connectivityMapBuffer34);
        randomPlotRivers.SetBuffer(0, "connectivityMap56", connectivityMapBuffer56);
        randomPlotRivers.SetTexture(0, "original", TextureManager.instance.FlowTexture);
        randomPlotRivers.SetTexture(0, "result", plotRiversRT);

        int numPasses = (int)(TextureManager.instance.Settings.textureWidth * (1 - TextureManager.instance.Settings.waterLevel) / 3);
        for (int i = 0; i < numPasses; i++)
            randomPlotRivers.Dispatch(0, Mathf.CeilToInt(plotRiversRT.width / 8f), Mathf.CeilToInt(plotRiversRT.height / 8f), 1);

        riverFlowMaskBuffer12.Release();
        riverFlowMaskBuffer34.Release();
        riverFlowMaskBuffer56.Release();

        connectivityMapBuffer12.Release();
        connectivityMapBuffer34.Release();
        connectivityMapBuffer56.Release();
    }

    public ComputeShader heightmapConnectivityShader;
    public ComputeShader heightmapFlowMapShader;
    public ComputeShader heightmapAverageShader;

    int[] connectivityMap1;
    int[] connectivityMap2;
    int[] connectivityMap3;
    int[] connectivityMap4;
    int[] connectivityMap5;
    int[] connectivityMap6;

    float flowMapMaxValue = 1000000;

    void EstablishHeightmapConnectivity()
    {
        if (connectivityMap1 == null || connectivityMap1.Length != TextureManager.instance.HeightMap1.Length)
        {
            connectivityMap1 = new int[TextureManager.instance.HeightMap1.Length];
            connectivityMap2 = new int[TextureManager.instance.HeightMap1.Length];
            connectivityMap3 = new int[TextureManager.instance.HeightMap1.Length];
            connectivityMap4 = new int[TextureManager.instance.HeightMap1.Length];
            connectivityMap5 = new int[TextureManager.instance.HeightMap1.Length];
            connectivityMap6 = new int[TextureManager.instance.HeightMap1.Length];
        }

        int numPasses = (int)(TextureManager.instance.Settings.textureWidth / 4);

        //float[] outputMap = new float[1];
        //outputMap[0] = 1;
        flowMapMaxValue = 1000000;

        ComputeBuffer heightMapBuffer12 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
        heightMapBuffer12.SetData(TextureManager.instance.HeightMap1, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer12.SetData(TextureManager.instance.HeightMap2, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        ComputeBuffer heightMapBuffer34 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length * 2, sizeof(float));
        heightMapBuffer34.SetData(TextureManager.instance.HeightMap3, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer34.SetData(TextureManager.instance.HeightMap4, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        ComputeBuffer heightMapBuffer56 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length * 2, sizeof(float));
        heightMapBuffer56.SetData(TextureManager.instance.HeightMap5, 0, 0, TextureManager.instance.HeightMap1.Length);
        heightMapBuffer56.SetData(TextureManager.instance.HeightMap6, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

        ComputeBuffer connectivityMapBuffer12 = new ComputeBuffer(connectivityMap1.Length * 2, sizeof(float));
        connectivityMapBuffer12.SetData(connectivityMap1, 0, 0, connectivityMap1.Length);
        connectivityMapBuffer12.SetData(connectivityMap2, 0, connectivityMap2.Length, connectivityMap2.Length);

        ComputeBuffer connectivityMapBuffer34 = new ComputeBuffer(connectivityMap3.Length * 2, sizeof(float));
        connectivityMapBuffer34.SetData(connectivityMap3, 0, 0, connectivityMap3.Length);
        connectivityMapBuffer34.SetData(connectivityMap3, 0, connectivityMap4.Length, connectivityMap4.Length);

        ComputeBuffer connectivityMapBuffer56 = new ComputeBuffer(connectivityMap5.Length * 2, sizeof(float));
        connectivityMapBuffer56.SetData(connectivityMap5, 0, 0, connectivityMap5.Length);
        connectivityMapBuffer56.SetData(connectivityMap6, 0, connectivityMap6.Length, connectivityMap6.Length);

        ComputeBuffer distanceToWaterBuffer12 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
        ComputeBuffer distanceToWaterBuffer34 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
        ComputeBuffer distanceToWaterBuffer56 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
        //ComputeBuffer distanceHeightMapBuffer = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 6, sizeof(float));

        if (TextureManager.instance.InciseFlowMap1 == null || TextureManager.instance.InciseFlowMap1.Length != TextureManager.instance.HeightMap1.Length * 6)
            TextureManager.instance.InstantiateInciseFlowMap();

        ComputeBuffer flowMapBuffer12 = new ComputeBuffer(TextureManager.instance.InciseFlowMap1.Length * 2, sizeof(float));
        flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap1, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
        flowMapBuffer12.SetData(TextureManager.instance.InciseFlowMap2, 0, TextureManager.instance.InciseFlowMap2.Length, TextureManager.instance.InciseFlowMap2.Length);

        ComputeBuffer flowMapBuffer34 = new ComputeBuffer(TextureManager.instance.InciseFlowMap3.Length * 2, sizeof(float));
        flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap3, 0, 0, TextureManager.instance.InciseFlowMap3.Length);
        flowMapBuffer34.SetData(TextureManager.instance.InciseFlowMap4, 0, TextureManager.instance.InciseFlowMap4.Length, TextureManager.instance.InciseFlowMap4.Length);

        ComputeBuffer flowMapBuffer56 = new ComputeBuffer(TextureManager.instance.InciseFlowMap5.Length * 2, sizeof(float));
        flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap5, 0, 0, TextureManager.instance.InciseFlowMap5.Length);
        flowMapBuffer56.SetData(TextureManager.instance.InciseFlowMap6, 0, TextureManager.instance.InciseFlowMap6.Length, TextureManager.instance.InciseFlowMap6.Length);

        //ComputeBuffer outputBuffer = new ComputeBuffer(outputMap.Length, sizeof(float));
        //outputBuffer.SetData(outputMap);

        heightmapConnectivityShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        heightmapConnectivityShader.SetInt("mapHeight", TextureManager.instance.Settings.textureWidth);
        heightmapConnectivityShader.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        heightmapConnectivityShader.SetFloat("flowAmount", 1);
        heightmapConnectivityShader.SetFloat("maxFlow", flowMapMaxValue);
        heightmapConnectivityShader.SetFloat("upwardWeight", inciseFlowSettings.upwardWeight);
        heightmapConnectivityShader.SetFloat("downwardWeight", inciseFlowSettings.downwardWeight);
        heightmapConnectivityShader.SetFloat("distanceWeight", inciseFlowSettings.distanceWeight);

        heightmapConnectivityShader.SetBuffer(0, "heightMap12", heightMapBuffer12);
        heightmapConnectivityShader.SetBuffer(0, "heightMap34", heightMapBuffer34);
        heightmapConnectivityShader.SetBuffer(0, "heightMap56", heightMapBuffer56);

        heightmapConnectivityShader.SetBuffer(0, "flowMap12", flowMapBuffer12);
        heightmapConnectivityShader.SetBuffer(0, "flowMap34", flowMapBuffer34);
        heightmapConnectivityShader.SetBuffer(0, "flowMap56", flowMapBuffer56);

        heightmapConnectivityShader.SetBuffer(0, "distanceMap12", distanceToWaterBuffer12);
        heightmapConnectivityShader.SetBuffer(0, "distanceMap34", distanceToWaterBuffer34);
        heightmapConnectivityShader.SetBuffer(0, "distanceMap56", distanceToWaterBuffer56);

        //heightmapConnectivityShader.SetBuffer(0, "distanceHeightMap", distanceHeightMapBuffer);
        heightmapConnectivityShader.SetBuffer(0, "connectivityMap12", connectivityMapBuffer12);
        heightmapConnectivityShader.SetBuffer(0, "connectivityMap34", connectivityMapBuffer34);
        heightmapConnectivityShader.SetBuffer(0, "connectivityMap56", connectivityMapBuffer56);
        //heightmapConnectivityShader.SetBuffer(0, "output", outputBuffer);

        for (int i = 0; i < numPasses; i++)
        {
            //outputMap[0] = 1;
            //outputBuffer.SetData(outputMap);
            heightmapConnectivityShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 1);
            //connectivityIndexesBuffer.GetData(connectivityMap);
            //connectivityMap.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth * 6, Path.Combine(Application.persistentDataPath, "connectivityMap-" + i + ".png"));
            //outputBuffer.GetData(outputMap);
            //if (outputMap[0] == 1)
            //    break;
        }

        connectivityMapBuffer12.GetData(connectivityMap1, 0, 0, connectivityMap1.Length);
        connectivityMapBuffer12.GetData(connectivityMap2, 0, connectivityMap2.Length, connectivityMap2.Length);
        connectivityMapBuffer34.GetData(connectivityMap3, 0, 0, connectivityMap3.Length);
        connectivityMapBuffer34.GetData(connectivityMap4, 0, connectivityMap4.Length, connectivityMap4.Length);
        connectivityMapBuffer56.GetData(connectivityMap5, 0, 0, connectivityMap5.Length);
        connectivityMapBuffer56.GetData(connectivityMap6, 0, connectivityMap6.Length, connectivityMap6.Length);

        //connectivityMap1.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap1.png"));
        //connectivityMap2.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap2.png"));
        //connectivityMap3.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap3.png"));
        //connectivityMap4.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap4.png"));
        //connectivityMap5.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap5.png"));
        //connectivityMap6.SaveConnectivityMap(TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "connectivityMap6.png"));

        //float maxHeight = 0;
        //float minHeight = 0;
        //TextureManager.instance.HeightMapMinMaxHeights(ref minHeight, ref maxHeight);

        //float flowMaxValue = TextureManager.instance.InciseFlowMapMaxValue;
        //if (flowMaxValue == 0) flowMaxValue = 1;
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap1.png"), flowMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap2.png"), flowMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap3.png"), flowMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap4.png"), flowMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap5.png"), flowMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap6.png"), flowMaxValue);

        //Flow Map
        heightmapFlowMapShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        heightmapFlowMapShader.SetFloat("flowAmount", 1);
        heightmapFlowMapShader.SetFloat("maxFlow", flowMapMaxValue);
        heightmapFlowMapShader.SetInt("numPasses", 1000);

        heightmapFlowMapShader.SetBuffer(0, "connectivityMap12", connectivityMapBuffer12);
        heightmapFlowMapShader.SetBuffer(0, "connectivityMap34", connectivityMapBuffer34);
        heightmapFlowMapShader.SetBuffer(0, "connectivityMap56", connectivityMapBuffer56);

        heightmapFlowMapShader.SetBuffer(0, "flowMap12", flowMapBuffer12);
        heightmapFlowMapShader.SetBuffer(0, "flowMap34", flowMapBuffer34);
        heightmapFlowMapShader.SetBuffer(0, "flowMap56", flowMapBuffer56);

        heightmapFlowMapShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 6);

        flowMapBuffer12.GetData(TextureManager.instance.InciseFlowMap1, 0, 0, TextureManager.instance.InciseFlowMap1.Length);
        flowMapBuffer12.GetData(TextureManager.instance.InciseFlowMap2, 0, TextureManager.instance.InciseFlowMap2.Length, TextureManager.instance.InciseFlowMap2.Length);
        flowMapBuffer34.GetData(TextureManager.instance.InciseFlowMap3, 0, 0, TextureManager.instance.InciseFlowMap3.Length);
        flowMapBuffer34.GetData(TextureManager.instance.InciseFlowMap4, 0, TextureManager.instance.InciseFlowMap4.Length, TextureManager.instance.InciseFlowMap4.Length);
        flowMapBuffer56.GetData(TextureManager.instance.InciseFlowMap5, 0, 0, TextureManager.instance.InciseFlowMap5.Length);
        flowMapBuffer56.GetData(TextureManager.instance.InciseFlowMap6, 0, TextureManager.instance.InciseFlowMap6.Length, TextureManager.instance.InciseFlowMap6.Length);

        flowMapMaxValue = TextureManager.instance.InciseFlowMapMaxValue;
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap1.png"), flowMapMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap2.png"), flowMapMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap3.png"), flowMapMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap4.png"), flowMapMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap5.png"), flowMapMaxValue);
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.InciseFlowMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "inciseFlowMap6.png"), flowMapMaxValue);

        heightMapBuffer12.Release();
        heightMapBuffer34.Release();
        heightMapBuffer56.Release();

        connectivityMapBuffer12.Release();
        connectivityMapBuffer34.Release();
        connectivityMapBuffer56.Release();

        distanceToWaterBuffer12.Release();
        distanceToWaterBuffer34.Release();
        distanceToWaterBuffer56.Release();

        flowMapBuffer12.Release();
        flowMapBuffer34.Release();
        flowMapBuffer56.Release();
    }

    void AverageHeightMap(float blur)
    {
        ComputeBuffer heightBuffer12 = new ComputeBuffer(TextureManager.instance.FlowErosionMap1.Length * 2, sizeof(float));
        heightBuffer12.SetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        heightBuffer12.SetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap2.Length, TextureManager.instance.FlowErosionMap2.Length);

        ComputeBuffer heightBuffer34 = new ComputeBuffer(TextureManager.instance.FlowErosionMap3.Length * 2, sizeof(float));
        heightBuffer34.SetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap3.Length);
        heightBuffer34.SetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap4.Length, TextureManager.instance.FlowErosionMap4.Length);

        ComputeBuffer heightBuffer56 = new ComputeBuffer(TextureManager.instance.FlowErosionMap5.Length * 2, sizeof(float));
        heightBuffer56.SetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap5.Length);
        heightBuffer56.SetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap6.Length, TextureManager.instance.FlowErosionMap6.Length);

        ComputeBuffer targetBuffer12 = new ComputeBuffer(TextureManager.instance.FlowErosionMap1.Length * 2, sizeof(float));
        targetBuffer12.SetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
        targetBuffer12.SetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap2.Length, TextureManager.instance.FlowErosionMap2.Length);

        ComputeBuffer targetBuffer34 = new ComputeBuffer(TextureManager.instance.FlowErosionMap3.Length * 2, sizeof(float));
        targetBuffer34.SetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap3.Length);
        targetBuffer34.SetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap4.Length, TextureManager.instance.FlowErosionMap4.Length);

        ComputeBuffer targetBuffer56 = new ComputeBuffer(TextureManager.instance.FlowErosionMap5.Length * 2, sizeof(float));
        targetBuffer56.SetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap5.Length);
        targetBuffer56.SetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap6.Length, TextureManager.instance.FlowErosionMap6.Length);

        heightmapAverageShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        heightmapAverageShader.SetFloat("blur", blur);

        int i = 0;
        for (i = 0; i < Mathf.CeilToInt(blur); i++)
        {
            heightmapAverageShader.SetInt("blurStep", i + 1);
            if (i % 2 == 0)
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap12", heightBuffer12);
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap34", heightBuffer34);
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap56", heightBuffer56);

                heightmapAverageShader.SetBuffer(0, "targetHeightMap12", targetBuffer12);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap34", targetBuffer34);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap56", targetBuffer56);
            }
            else
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap12", targetBuffer12);
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap34", targetBuffer34);
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap56", targetBuffer56);

                heightmapAverageShader.SetBuffer(0, "targetHeightMap12", heightBuffer12);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap34", heightBuffer34);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap56", heightBuffer56);
            }

            heightmapAverageShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 1);
        }

        if (i % 2 == 0)
        {
            heightBuffer12.GetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
            heightBuffer12.GetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap2.Length, TextureManager.instance.FlowErosionMap2.Length);
            heightBuffer34.GetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap3.Length);
            heightBuffer34.GetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap4.Length, TextureManager.instance.FlowErosionMap4.Length);
            heightBuffer56.GetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap5.Length);
            heightBuffer56.GetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap6.Length, TextureManager.instance.FlowErosionMap6.Length);
        }
        else
        {
            targetBuffer12.GetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
            targetBuffer12.GetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap2.Length, TextureManager.instance.FlowErosionMap2.Length);
            targetBuffer34.GetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap3.Length);
            targetBuffer34.GetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap4.Length, TextureManager.instance.FlowErosionMap4.Length);
            targetBuffer56.GetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap5.Length);
            targetBuffer56.GetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap6.Length, TextureManager.instance.FlowErosionMap6.Length);
        }

        heightBuffer12.Release();
        heightBuffer34.Release();
        heightBuffer56.Release();

        targetBuffer12.Release();
        targetBuffer34.Release();
        targetBuffer56.Release();
    }
}
