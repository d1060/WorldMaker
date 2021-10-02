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
    Texture2D heightmap = null;
    Texture2D landmap = null;
    Texture2D landmask = null;

    public void SaveImages()
    {
        string lastSavedImageFolder = AppData.instance.LastSavedImageFolder;
        if (lastSavedImageFolder == null || lastSavedImageFolder == "" || !Directory.Exists(lastSavedImageFolder))
        {
            if (AppData.instance.RecentWorlds.Count > 0)
                lastSavedImageFolder = System.IO.Path.GetDirectoryName(AppData.instance.RecentWorlds[0]);
            else
                lastSavedImageFolder = Application.persistentDataPath;
        }

        string savedFile = StandaloneFileBrowser.SaveFilePanel("Save Generated Image Files", lastSavedImageFolder, MapData.instance.WorldName, new[] { new ExtensionFilter("Png Image", "png") });
        if (savedFile != null && savedFile != "")
        {
            cameraController.CloseContextMenu();

            string fileNamePath = System.IO.Path.GetDirectoryName(savedFile);
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(savedFile);
            string fileNameExtension = System.IO.Path.GetExtension(savedFile);
            AppData.instance.LastSavedImageFolder = fileNamePath;

            int isEroded = planetSurfaceMaterial.GetInt("_IsEroded");
            planetSurfaceMaterial.SetInt("_IsEroded", 0);

            if (AppData.instance.SaveMainMap)
            {
                SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainMap" + fileNameExtension), 0);
            }

            if (AppData.instance.SaveHeightMap)
            {
                GenerateHeightMap();
                if (mergedHeightMap != null)
                    ImageTools.SaveTextureFloatArray(mergedHeightMap, textureSettings.textureWidth, textureSettings.textureHeight, Path.Combine(fileNamePath, fileNameWithoutExtension + "-Heightmap" + fileNameExtension));
                else if (originalHeightMap != null)
                    ImageTools.SaveTextureFloatArray(originalHeightMap, textureSettings.textureWidth, textureSettings.textureHeight, Path.Combine(fileNamePath, fileNameWithoutExtension + "-Heightmap" + fileNameExtension));
            }

            if (AppData.instance.SaveLandMask)
            {
                SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Landmask" + fileNameExtension), 2);
            }

            if (AppData.instance.SaveNormalMap)
            {
                SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Normalmap" + fileNameExtension), 4);
            }

            if (AppData.instance.SaveTemperature)
            {
                SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Temperature" + fileNameExtension), 3);
            }

            if (AppData.instance.SaveRivers && flowTexture != null)
            {
                SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Rivers" + fileNameExtension), flowTexture);
            }

            planetSurfaceMaterial.SetInt("_IsEroded", isEroded);

            AppData.instance.Save();
        }
        else
            cameraController.CloseContextMenu();
    }

    void SaveImageFile(string fileName, int drawMode)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        planetSurfaceMaterial.SetFloat("_DrawType", drawMode);
        //Texture source = flatMap.GetComponent<MeshRenderer>().material.mainTexture;
        Texture2D source;
        RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
        if (drawMode != 1)
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA32, false);
        }
        else
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGB48, false);
            outputFormat = RenderTextureFormat.DefaultHDR;
        }
        RenderTexture destination = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 0, outputFormat);
        Graphics.Blit(source, destination, planetSurfaceMaterial, 2);
        destination.Save(fileName);
        planetSurfaceMaterial.SetFloat("_DrawType", showTemperature ? 3 : 0);
    }

    void SaveImageFile(string fileName, Texture2D texture)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        texture.SaveAsPNG(fileName);
    }

    void UpdateSurfaceMaterialProperties(bool resetEroded = true)
    {
        if (planetSurfaceMaterial == null)
            return;

        planetSurfaceMaterial.SetInt("_Seed", textureSettings.SurfaceNoiseSeed);
        planetSurfaceMaterial.SetFloat("_XOffset", textureSettings.surfaceNoiseSettings.noiseOffset.x);
        planetSurfaceMaterial.SetFloat("_YOffset", textureSettings.surfaceNoiseSettings.noiseOffset.y);
        planetSurfaceMaterial.SetFloat("_ZOffset", textureSettings.surfaceNoiseSettings.noiseOffset.z);
        planetSurfaceMaterial.SetInt("_TemperatureSeed", textureSettings.TemperatureNoiseSeed);
        planetSurfaceMaterial.SetInt("_HumiditySeed", textureSettings.HumidityNoiseSeed);
        planetSurfaceMaterial.SetFloat("_Multiplier", textureSettings.surfaceNoiseSettings.multiplier);
        planetSurfaceMaterial.SetInt("_Octaves", textureSettings.surfaceNoiseSettings.octaves);
        planetSurfaceMaterial.SetFloat("_Lacunarity", textureSettings.surfaceNoiseSettings.lacunarity);
        planetSurfaceMaterial.SetFloat("_Persistence", textureSettings.surfaceNoiseSettings.persistence);
        planetSurfaceMaterial.SetFloat("_WaterLevel", textureSettings.waterLevel);
        planetSurfaceMaterial.SetFloat("_HeightRange", textureSettings.heightScale);
        planetSurfaceMaterial.SetFloat("_DrawType", showTemperature ? 3 : 0);

        int landColorSteps = textureSettings.landColorStages.Length < textureSettings.land1Color.Length ? textureSettings.landColorStages.Length : textureSettings.land1Color.Length;
        if (landColorSteps > 8) landColorSteps = 8;
        planetSurfaceMaterial.SetInt("_ColorSteps", landColorSteps);

        for (int i = 1; i <= 8; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= landColorSteps)
            {
                stage = textureSettings.landColorStages[i - 1];
                color = textureSettings.land1Color[i - 1];
            }
            else
            {
                stage = textureSettings.landColorStages[landColorSteps - 1];
                color = textureSettings.land1Color[landColorSteps - 1];
            }
            planetSurfaceMaterial.SetFloat("_ColorStep" + i, stage);
            planetSurfaceMaterial.SetColor("_Color" + i, color);
        }

        int oceanColorSteps = textureSettings.oceanStages.Length < textureSettings.oceanColors.Length ? textureSettings.oceanStages.Length : textureSettings.oceanColors.Length;
        if (oceanColorSteps > 4) oceanColorSteps = 4;
        planetSurfaceMaterial.SetInt("_OceanColorSteps", oceanColorSteps);

        for (int i = 1; i <= 4; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= oceanColorSteps)
            {
                stage = textureSettings.oceanStages[i - 1];
                color = textureSettings.oceanColors[i - 1];
            }
            else
            {
                stage = textureSettings.oceanStages[oceanColorSteps - 1];
                color = textureSettings.oceanColors[oceanColorSteps - 1];
            }
            planetSurfaceMaterial.SetFloat("_OceanColorStep" + i, stage);
            planetSurfaceMaterial.SetColor("_OceanColor" + i, color);
        }

        planetSurfaceMaterial.SetFloat("_IceTemperatureThreshold1", textureSettings.iceTemperatureThreshold1);
        planetSurfaceMaterial.SetFloat("_IceTemperatureThreshold2", textureSettings.iceTemperatureThreshold2);
        planetSurfaceMaterial.SetFloat("_DesertThreshold1", textureSettings.desertThreshold1);
        planetSurfaceMaterial.SetFloat("_DesertThreshold2", textureSettings.desertThreshold2);
        //planetSurfaceMaterial.SetFloat("_HighHumidityLightnessPercentage", );
        planetSurfaceMaterial.SetColor("_IceColor", textureSettings.iceColor);
        planetSurfaceMaterial.SetColor("_DesertColor", textureSettings.desertColor);
        //planetSurfaceMaterial.SetFloat("_NormalScale", 50);

        if (mapSettings.UseImages)
        {
            if (mapSettings.HeightMapPath == "" || !File.Exists(mapSettings.HeightMapPath))
                planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
            else
                planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);

            if (mapSettings.MainTexturePath == "" || !File.Exists(mapSettings.MainTexturePath))
                planetSurfaceMaterial.SetInt("_IsMainmapSet", 0);
            else
                planetSurfaceMaterial.SetInt("_IsMainmapSet", 1);

            if (mapSettings.LandMaskPath == "" || !File.Exists(mapSettings.LandMaskPath))
                planetSurfaceMaterial.SetInt("_IsLandmaskSet", 0);
            else
                planetSurfaceMaterial.SetInt("_IsLandmaskSet", 1);
        }
        else
        {
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
            planetSurfaceMaterial.SetInt("_IsMainmapSet", 0);
            planetSurfaceMaterial.SetInt("_IsLandmaskSet", 0);
        }
        if (resetEroded)
        {
            erodedHeightMap = null;
            originalHeightMap = null;
            mergedHeightMap = null;
            planetSurfaceMaterial.SetInt("_IsEroded", 0);
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
        }

        planetSurfaceMaterial.SetInt("_RidgedNoise", textureSettings.surfaceNoiseSettings.ridged ? 1 : 0);
    }

    void UpdateSurfaceMaterialHeightMap(bool isEroded = false)
    {
        if (planetSurfaceMaterial == null)
            return;

        if (!isEroded && mapSettings.HeightMapPath != "" && File.Exists(mapSettings.HeightMapPath))
        {
            heightmap = LoadAnyImageFile(mapSettings.HeightMapPath);
        }

        if (heightmap != null)
        { 
            planetSurfaceMaterial.SetTexture("_MainTex", heightmap);
            if (mapSettings.UseImages)
                planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
            else
                planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
        }
        else
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);

        if (isEroded)
            planetSurfaceMaterial.SetInt("_IsEroded", 1);
        else
            planetSurfaceMaterial.SetInt("_IsEroded", 0);
    }

    void UpdateSurfaceMaterialMainMap()
    {
        if (planetSurfaceMaterial == null)
            return;

        if (mapSettings.MainTexturePath == "" || !File.Exists(mapSettings.MainTexturePath))
            return;

        landmap = LoadAnyImageFile(mapSettings.MainTexturePath);

        planetSurfaceMaterial.SetTexture("_MainMap", landmap);
        if (mapSettings.UseImages)
            planetSurfaceMaterial.SetInt("_IsMainmapSet", 1);
    }

    void UpdateSurfaceMaterialLandMask()
    {
        if (planetSurfaceMaterial == null)
            return;

        if (mapSettings.LandMaskPath == "" || !File.Exists(mapSettings.LandMaskPath))
            return;

        landmask = LoadAnyImageFile(mapSettings.LandMaskPath);

        planetSurfaceMaterial.SetTexture("_LandMask", landmask);
        if (mapSettings.UseImages)
            planetSurfaceMaterial.SetInt("_IsLandmaskSet", 1);
    }

    static Texture2D LoadAnyImageFile(string fileName)
    {
        if (fileName.EndsWith(".bmp"))
        {
            B83.Image.BMP.BMPLoader bmpLoader = new B83.Image.BMP.BMPLoader();
            B83.Image.BMP.BMPImage bmpImg = bmpLoader.LoadBMP(fileName);
            return bmpImg.ToTexture2D();
        }
        else
        {
            byte[] fileData = System.IO.File.ReadAllBytes(fileName);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            return tex;
        }
    }

    public ComputeShader heightMapComputeShader;
    public ComputeShader erosionShader;
    float[] erodedHeightMap;
    float[] originalHeightMap;
    float[] mergedHeightMap;

    void GenerateHeightMap()
    {
        if (originalHeightMap == null)
        {
            originalHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
            erodedHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
            mergedHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];

            ComputeBuffer mapBuffer = new ComputeBuffer(originalHeightMap.Length, sizeof(float));
            mapBuffer.SetData(originalHeightMap);
            heightMapComputeShader.SetBuffer(0, "heightMap", mapBuffer);

            heightMapComputeShader.SetInt("seed", textureSettings.SurfaceNoiseSeed);
            heightMapComputeShader.SetFloat("xOffset", textureSettings.surfaceNoiseSettings.noiseOffset.x);
            heightMapComputeShader.SetFloat("yOffset", textureSettings.surfaceNoiseSettings.noiseOffset.y);
            heightMapComputeShader.SetFloat("zOffset", textureSettings.surfaceNoiseSettings.noiseOffset.z);
            heightMapComputeShader.SetInt("mapWidth", textureSettings.textureWidth);
            heightMapComputeShader.SetInt("mapHeight", textureSettings.textureHeight);
            heightMapComputeShader.SetInt("octaves", textureSettings.surfaceNoiseSettings.octaves);
            heightMapComputeShader.SetFloat("lacunarity", textureSettings.surfaceNoiseSettings.lacunarity);
            heightMapComputeShader.SetFloat("persistence", textureSettings.surfaceNoiseSettings.persistence);
            heightMapComputeShader.SetFloat("multiplier", textureSettings.surfaceNoiseSettings.multiplier);
            heightMapComputeShader.SetFloat("heightRange", textureSettings.heightScale);
            heightMapComputeShader.SetInt("ridgedNoise", textureSettings.surfaceNoiseSettings.ridged ? 1 : 0);

            heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 8f), Mathf.CeilToInt(textureSettings.textureHeight / 8f), 1);

            mapBuffer.GetData(originalHeightMap);
            mapBuffer.Release();

            Array.Copy(originalHeightMap, erodedHeightMap, originalHeightMap.Length);
            Array.Copy(originalHeightMap, mergedHeightMap, originalHeightMap.Length);
        }
    }

    void HeightMap2Texture()
    {
        if (erodedHeightMap == null || originalHeightMap == null || mergedHeightMap == null)
            return;

        Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
        for (int y = 0; y < textureSettings.textureHeight; y++)
        {
            for (int x = 0; x < textureSettings.textureWidth; x++)
            {
                int index = x + y * textureSettings.textureWidth;
                float erodedHeight = erodedHeightMap[index];
                float originalHeight = originalHeightMap[index];
                float mergedHeight = erodedHeight;
                if (textureSettings.erosionNoiseMerge >= 1)
                    mergedHeight = originalHeight;
                else if (textureSettings.erosionNoiseMerge > 0)
                    mergedHeight = erodedHeight * (1 - textureSettings.erosionNoiseMerge) + originalHeight * textureSettings.erosionNoiseMerge;
                mergedHeightMap[index] = mergedHeight;
                Color color = new Color(mergedHeight, mergedHeight, mergedHeight);
                colors[index] = color;
            }
        }
        heightmap = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight);
        heightmap.SetPixels(colors);
        heightmap.Apply();
    }

    public void PerformErosionCycle()
    {
        GenerateHeightMap();
        //string filename = Path.Combine(Application.persistentDataPath, "Textures", "heightmap.png");
        //Textures.instance.SaveTextureFloatArray(heightMap, textureSettings.textureWidth, textureSettings.textureHeight, filename);
        TerrainErosion.instance.mapWidth = textureSettings.textureWidth;
        TerrainErosion.instance.mapHeight = textureSettings.textureHeight;
        TerrainErosion.instance.erosion = erosionShader;
        TerrainErosion.instance.erosionSettings = erosionSettings;
        TerrainErosion.instance.Erode(ref erodedHeightMap);
        HeightMap2Texture();
        //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "heightmap-2.png"));
        UpdateSurfaceMaterialHeightMap(true);
    }

    public void UndoErosion()
    {
        erodedHeightMap = null;
        originalHeightMap = null;
        mergedHeightMap = null;
        flowTexture = null;

        planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
    }

    public ComputeShader inciseFlow;
    Texture2D flowTexture;
    public void RunInciseFlowFromButton()
    {
        StartCoroutine(PerformInciseFlow());
    }

    public IEnumerator PerformInciseFlow()
    {
        ShowPlottingRiversPanel();
        yield return null;
        GenerateHeightMap();
        InciseFlow.instance.textureSettings = textureSettings;
        InciseFlow.instance.inciseFlowSettings = inciseFlowSettings;
        InciseFlow.instance.inciseFlow = inciseFlow;
        if (flowTexture == null)
        {
            flowTexture = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight);
            Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
            flowTexture.SetPixels(colors);
            flowTexture.Apply();
        }
        InciseFlow.instance.Run(ref erodedHeightMap, ref flowTexture);
        //flowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowMap.png"));
        HeightMap2Texture();
        planetSurfaceMaterial.SetTexture("_MainTex", heightmap);
        planetSurfaceMaterial.SetTexture("_FlowTex", flowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
        planetSurfaceMaterial.SetInt("_IsEroded", 1);
        HidePlottingRiversPanel();
        yield return null;
    }

    public void AlterTerrain(Vector2 coordinates, float radius, float elevationDelta)
    {
        GenerateHeightMap();

        float radiusInPixels = radius;
        if (!showGlobe)
        {
            radiusInPixels *= textureSettings.textureWidth / mapWidth;
        }
        else
        {
            float radiusRatio = radius / geoSphere.Radius;
            if (radiusRatio > 1)
                radiusRatio = 1;
            float angle = Mathf.Asin(radiusRatio);
            radiusInPixels = (textureSettings.textureWidth / (2 * Mathf.PI)) * angle;
        }

        Vector2i pixelCoordinates = new Vector2i(
                (int)(coordinates.x * textureSettings.textureWidth),
                (int)(coordinates.y * textureSettings.textureHeight));

        for (int x = -(int)radiusInPixels; x <= (int)radiusInPixels; x++)
        {
            int pixelX = pixelCoordinates.x + x;
            if (pixelX < 0) pixelX += textureSettings.textureWidth;
            if (pixelX >= textureSettings.textureWidth) pixelX -= textureSettings.textureWidth;

            for (int y = -(int)radiusInPixels; y <= (int)radiusInPixels; y++)
            {
                int actualY = y;
                int pixelY = pixelCoordinates.y + y;
                if (pixelY < 0)
                {
                    pixelY = 0;
                    actualY = -pixelCoordinates.y;
                }
                if (pixelY >= textureSettings.textureHeight)
                {
                    pixelY = textureSettings.textureHeight - 1;
                    actualY = textureSettings.textureHeight - 1 - pixelCoordinates.y;
                }

                float pixelDistance = Mathf.Sqrt(x * x + actualY * actualY);
                if (pixelDistance > radiusInPixels)
                    continue;

                int index = pixelY * textureSettings.textureWidth + pixelX;

                float distanceRatio = 1 - (pixelDistance / radiusInPixels);
                float heightToAlter = elevationDelta * distanceRatio;

                float height = erodedHeightMap[index];
                height += heightToAlter;
                if (height < 0) height = 0;
                if (height > 1) height = 1;
                erodedHeightMap[index] = height;
            }
        }

        HeightMap2Texture();
        planetSurfaceMaterial.SetTexture("_MainTex", heightmap);
        planetSurfaceMaterial.SetInt("_IsEroded", 1);
    }
}
