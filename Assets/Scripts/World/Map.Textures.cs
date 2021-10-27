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

        string title = "Save Generated Image Files";
        if (AppData.instance.ExportAsCubemap)
            title = "Export Image as Cubemap";

        string savedFile = StandaloneFileBrowser.SaveFilePanel(title, lastSavedImageFolder, MapData.instance.WorldName, new[] { new ExtensionFilter("Png Image", "png") });
        if (savedFile != null && savedFile != "")
        {
            cameraController.CloseContextMenu();

            if (AppData.instance.ExportAsCubemap)
                ExportCubemaps(savedFile);
            else
            {
                string fileNamePath = System.IO.Path.GetDirectoryName(savedFile);
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(savedFile);
                string fileNameExtension = System.IO.Path.GetExtension(savedFile);
                AppData.instance.LastSavedImageFolder = fileNamePath;

                int isEroded = planetSurfaceMaterial.GetInt("_IsEroded");
                planetSurfaceMaterial.SetInt("_IsEroded", 0);

                if (AppData.instance.SaveMainMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainMap" + fileNameExtension), SphereShaderDrawType.Land);
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
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Landmask" + fileNameExtension), SphereShaderDrawType.LandMask);
                }

                if (AppData.instance.SaveNormalMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Normalmap" + fileNameExtension), SphereShaderDrawType.Normal);
                }

                if (AppData.instance.SaveTemperature)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Temperature" + fileNameExtension), SphereShaderDrawType.Temperature);
                }

                if (AppData.instance.SaveRivers && flowTexture != null)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Rivers" + fileNameExtension), flowTexture);
                }

                planetSurfaceMaterial.SetInt("_IsEroded", isEroded);

                AppData.instance.Save();
            }
        }
        else
            cameraController.CloseContextMenu();
    }

    void SaveImageFile(string fileName, SphereShaderDrawType drawMode)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        planetSurfaceMaterial.SetFloat("_DrawType", (int)drawMode);
        //Texture source = flatMap.GetComponent<MeshRenderer>().material.mainTexture;
        Texture2D source;
        RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
        if (drawMode != SphereShaderDrawType.HeightMap)
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA32, false);
        }
        else
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGB48, false);
            outputFormat = RenderTextureFormat.DefaultHDR;
        }
        RenderTexture destination = RenderTexture.GetTemporary(textureSettings.textureWidth, textureSettings.textureHeight, 0, outputFormat);
        Graphics.Blit(source, destination, planetSurfaceMaterial, 2);
        destination.Save(fileName);
        RenderTexture.ReleaseTemporary(destination);
        UnityEngine.Object.Destroy(source);
        UnityEngine.Object.Destroy(destination);
        planetSurfaceMaterial.SetFloat("_DrawType", showTemperature ? (int)SphereShaderDrawType.Temperature : (int)SphereShaderDrawType.Land);
    }

    Texture2D ShaderToTexture(SphereShaderDrawType drawMode)
    {
        Texture2D source;
        RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
        if (drawMode != SphereShaderDrawType.HeightMap)
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA32, false);
        }
        else
        {
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGB48, false);
            outputFormat = RenderTextureFormat.DefaultHDR;
        }
        float prevDrawType = planetSurfaceMaterial.GetFloat("_DrawType");
        planetSurfaceMaterial.SetFloat("_DrawType", (int)drawMode);
        RenderTexture destination = RenderTexture.GetTemporary(textureSettings.textureWidth, textureSettings.textureHeight, 0, outputFormat);
        Graphics.Blit(source, destination, planetSurfaceMaterial, 2);
        planetSurfaceMaterial.SetFloat("_DrawType", prevDrawType);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = destination;
        source.ReadPixels(new Rect(0, 0, textureSettings.textureWidth, textureSettings.textureHeight), 0, 0);
        source.Apply();
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(destination);
        return source;
    }

    void SaveImageFile(string fileName, Texture2D texture)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        texture.SaveAsPNG(fileName);
    }

    public ComputeShader equirectangular2CubemapShader;
    void ExportCubemaps(string savedFile)
    {
        string savePath = Path.GetDirectoryName(savedFile);
        string saveFile = Path.GetFileNameWithoutExtension(savedFile);
        //string saveExtension = Path.GetExtension(savedFile);

        // Gets the Main Map.
        Texture2D mainMap = ShaderToTexture(SphereShaderDrawType.LandNormal);
        // Gets the Landmask.
        Texture2D landMask = ShaderToTexture(SphereShaderDrawType.LandMask);
        // Gets the Bump.
        Texture2D bumpMap = ShaderToTexture(SphereShaderDrawType.HeightMap);

        float mainMapHeightToExtract = mainMap.height / 2f;
        float mainMapWidthToExtract = mainMap.width / 4f;
        int bottomY = Mathf.RoundToInt(mainMapHeightToExtract / 2);
        int topY = Mathf.RoundToInt(bottomY + mainMapHeightToExtract);
        int cropHeight = topY - bottomY;
        int cropWidth = Mathf.RoundToInt(mainMapWidthToExtract);

        string baseFolder = Path.Combine(savePath, saveFile, "Surface");
        if (!Directory.Exists(baseFolder))
            Directory.CreateDirectory(baseFolder);

        string bumpBaseFolder = Path.Combine(savePath, saveFile, "Bump");
        if (!Directory.Exists(bumpBaseFolder))
            Directory.CreateDirectory(bumpBaseFolder);

        for (int faceCount = 0; faceCount < 6; faceCount++)
        {
            string baseSubfolder = "neg_x";
            switch (faceCount)
            {
                case 1:
                    baseSubfolder = "pos_z";
                    break;
                case 2:
                    baseSubfolder = "pos_x";
                    break;
                case 3:
                    baseSubfolder = "neg_z";
                    break;
                case 4:
                    baseSubfolder = "pos_y";
                    break;
                case 5:
                    baseSubfolder = "neg_y";
                    break;
            }

            string folder = Path.Combine(savePath, saveFile, "Surface", baseSubfolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string bumpFolder = Path.Combine(savePath, saveFile, "Bump", baseSubfolder);
            if (!Directory.Exists(bumpFolder))
                Directory.CreateDirectory(bumpFolder);

            ExportCubeMapFile(mainMap, folder, "_c", AppData.instance.CubemapDimension, faceCount, 0, 0, 0);
            ExportCubeMapFile(landMask, folder, "_a", AppData.instance.CubemapDimension, faceCount, 0, 0, 0);
            ExportCubeMapFile(bumpMap, bumpFolder, "", AppData.instance.CubemapDimension, faceCount, 0, 0, 0);

            for (int i = 1; i < AppData.instance.CubemapDivisions; i++)
            {
                SaveMapSubDivisions(mainMap, faceCount, i, folder, "_c");
                SaveMapSubDivisions(landMask, faceCount, i, folder, "_a");
                SaveMapSubDivisions(bumpMap, faceCount, i, bumpFolder, "");
            }
        }

        mainMap = mainMap.ResizePixels(AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);
        landMask = landMask.ResizePixels(AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);
        bumpMap = bumpMap.ResizePixels(AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);

        mainMap.SaveAsJPG(Path.Combine(baseFolder, "base.jpg"));
        landMask.SaveAsJPG(Path.Combine(baseFolder, "base_a.jpg"));
        bumpMap.SaveAsJPG(Path.Combine(bumpBaseFolder, "base.jpg"));

        UnityEngine.Object.Destroy(mainMap);
        UnityEngine.Object.Destroy(landMask);
        UnityEngine.Object.Destroy(bumpMap);

        mainMap = null;
        landMask = null;
        bumpMap = null;

        AppData.instance.LastSavedImageFolder = savePath;
        AppData.instance.Save();
    }

    void ExportCubeMapFile(Texture2D map, string folder, string filePosfix, int dimension, int face, int division, int divisionX, int divisionY)
    {
        bool useCpu = false;
        string fileName = division + "_" + divisionY + "_" + divisionX + filePosfix + ".jpg";

        if (!useCpu)
        {
            equirectangular2CubemapShader.SetInt("mapWidth", textureSettings.textureWidth);
            equirectangular2CubemapShader.SetInt("mapHeight", textureSettings.textureHeight);
            equirectangular2CubemapShader.SetInt("cubemapDimension", dimension);
            equirectangular2CubemapShader.SetInt("subdivision", division);
            equirectangular2CubemapShader.SetInt("subDivisionX", divisionX);
            equirectangular2CubemapShader.SetInt("subDivisionY", divisionY);
            equirectangular2CubemapShader.SetInt("faceId", face);

            equirectangular2CubemapShader.SetTexture(0, "base", map);

            RenderTexture result = RenderTexture.GetTemporary(dimension, dimension);
            result.enableRandomWrite = true;
            result.Create();

            equirectangular2CubemapShader.SetTexture(0, "Result", result);

            equirectangular2CubemapShader.Dispatch(0, Mathf.CeilToInt(dimension / 32f), Mathf.CeilToInt(dimension / 32f), 1);

            Texture2D resultTexture = new Texture2D(dimension, dimension);

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = result;
            resultTexture.ReadPixels(new Rect(0, 0, dimension, dimension), 0, 0);
            RenderTexture.active = prevActive;
            resultTexture.Apply();
            resultTexture.SaveAsJPG(Path.Combine(folder, fileName));

            RenderTexture.ReleaseTemporary(result);
            UnityEngine.Object.Destroy(resultTexture);
            resultTexture = null;
        }
        else
        {
            Equirectangular2Cubemap.instance.mapWidth = textureSettings.textureWidth;
            Equirectangular2Cubemap.instance.mapHeight = textureSettings.textureHeight;
            Equirectangular2Cubemap.instance.cubemapDimension = dimension;
            Equirectangular2Cubemap.instance.subdivision = division;
            Equirectangular2Cubemap.instance.subDivisionX = divisionX;
            Equirectangular2Cubemap.instance.subDivisionY = divisionY;
            Equirectangular2Cubemap.instance.faceId = face;
            Equirectangular2Cubemap.instance.baseTex = map;
            Equirectangular2Cubemap.instance.Result = new Texture2D(dimension, dimension);

            Equirectangular2Cubemap.instance.Run();

            Equirectangular2Cubemap.instance.Result.Apply();
            Equirectangular2Cubemap.instance.Result.SaveAsJPG(Path.Combine(folder, fileName));

            UnityEngine.Object.Destroy(Equirectangular2Cubemap.instance.Result);
        }
    }


    void SaveMapSubDivisions(Texture2D tex, int faceCount, int subdivision, string folder, string filePosfix)
    {
        int sizeDivisor = (int)Mathf.Pow(2, subdivision);

        for (int countX = 0; countX < sizeDivisor; countX++)
        {
            for (int countY = 0; countY < sizeDivisor; countY++)
            {
                ExportCubeMapFile(tex, folder, filePosfix, AppData.instance.CubemapDimension, faceCount, subdivision, countX, countY);
            }
        }
    }

    void UpdateSurfaceMaterialProperties(bool resetEroded = true)
    {
        if (planetSurfaceMaterial == null)
            return;

        planetSurfaceMaterial.SetInt("_Seed", textureSettings.surfaceNoiseSettings.seed);
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
        planetSurfaceMaterial.SetFloat("_HeightRange", textureSettings.surfaceNoiseSettings.heightScale);
        planetSurfaceMaterial.SetFloat("_DrawType", showTemperature ? 3 : 0);
        planetSurfaceMaterial.SetInt("_RidgedNoise", textureSettings.surfaceNoiseSettings.ridged ? 1 : 0);
        planetSurfaceMaterial.SetFloat("_HeightExponent", textureSettings.surfaceNoiseSettings.heightExponent);
        planetSurfaceMaterial.SetFloat("_LayerStrength", textureSettings.surfaceNoiseSettings.layerStrength);
        planetSurfaceMaterial.SetInt("_DomainWarping", textureSettings.surfaceNoiseSettings.domainWarping ? 1 : 0);

        planetSurfaceMaterial.SetInt("_Seed2", textureSettings.surfaceNoiseSettings2.seed);
        planetSurfaceMaterial.SetFloat("_XOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.x);
        planetSurfaceMaterial.SetFloat("_YOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.y);
        planetSurfaceMaterial.SetFloat("_ZOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.z);
        planetSurfaceMaterial.SetFloat("_Multiplier2", textureSettings.surfaceNoiseSettings2.multiplier);
        planetSurfaceMaterial.SetInt("_Octaves2", textureSettings.surfaceNoiseSettings2.octaves);
        planetSurfaceMaterial.SetFloat("_Lacunarity2", textureSettings.surfaceNoiseSettings2.lacunarity);
        planetSurfaceMaterial.SetFloat("_Persistence2", textureSettings.surfaceNoiseSettings2.persistence);
        planetSurfaceMaterial.SetInt("_RidgedNoise2", textureSettings.surfaceNoiseSettings2.ridged ? 1 : 0);
        planetSurfaceMaterial.SetFloat("_HeightExponent2", textureSettings.surfaceNoiseSettings2.heightExponent);
        planetSurfaceMaterial.SetFloat("_LayerStrength2", textureSettings.surfaceNoiseSettings2.layerStrength);
        planetSurfaceMaterial.SetFloat("_HeightRange2", textureSettings.surfaceNoiseSettings2.heightScale);
        planetSurfaceMaterial.SetInt("_DomainWarping2", textureSettings.surfaceNoiseSettings2.domainWarping ? 1 : 0);

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
            //drainageIndexesMap = null;
            connectivityMap = null;
            //basinsHeightMap = null;
            planetSurfaceMaterial.SetInt("_IsEroded", 0);
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
        }
    }

    public void UpdateSurfaceMaterialHeightMap(bool isEroded = false)
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
    public ComputeShader heightmap2TextureShader;
    float[] erodedHeightMap;
    float[] originalHeightMap;
    float[] mergedHeightMap;
    float[] humidityMap;

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

            heightMapComputeShader.SetInt("seed", textureSettings.surfaceNoiseSettings.seed);
            heightMapComputeShader.SetFloat("xOffset", textureSettings.surfaceNoiseSettings.noiseOffset.x);
            heightMapComputeShader.SetFloat("yOffset", textureSettings.surfaceNoiseSettings.noiseOffset.y);
            heightMapComputeShader.SetFloat("zOffset", textureSettings.surfaceNoiseSettings.noiseOffset.z);
            heightMapComputeShader.SetInt("mapWidth", textureSettings.textureWidth);
            heightMapComputeShader.SetInt("mapHeight", textureSettings.textureHeight);
            heightMapComputeShader.SetInt("octaves", textureSettings.surfaceNoiseSettings.octaves);
            heightMapComputeShader.SetFloat("lacunarity", textureSettings.surfaceNoiseSettings.lacunarity);
            heightMapComputeShader.SetFloat("persistence", textureSettings.surfaceNoiseSettings.persistence);
            heightMapComputeShader.SetFloat("multiplier", textureSettings.surfaceNoiseSettings.multiplier);
            heightMapComputeShader.SetFloat("heightRange", textureSettings.surfaceNoiseSettings.heightScale);
            heightMapComputeShader.SetInt("ridgedNoise", textureSettings.surfaceNoiseSettings.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("heightExponent", textureSettings.surfaceNoiseSettings.heightExponent);
            heightMapComputeShader.SetFloat("layerStrength", textureSettings.surfaceNoiseSettings.layerStrength);
            heightMapComputeShader.SetFloat("domainWarping", textureSettings.surfaceNoiseSettings.domainWarping ? 1 : 0);
            heightMapComputeShader.SetFloat("xOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.x);
            heightMapComputeShader.SetFloat("yOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.y);
            heightMapComputeShader.SetFloat("zOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.z);
            heightMapComputeShader.SetInt("seed2", textureSettings.surfaceNoiseSettings2.seed);
            heightMapComputeShader.SetFloat("multiplier2", textureSettings.surfaceNoiseSettings2.multiplier);
            heightMapComputeShader.SetInt("octaves2", textureSettings.surfaceNoiseSettings2.octaves);
            heightMapComputeShader.SetFloat("lacunarity2", textureSettings.surfaceNoiseSettings2.lacunarity);
            heightMapComputeShader.SetFloat("persistence2", textureSettings.surfaceNoiseSettings2.persistence);
            heightMapComputeShader.SetInt("ridgedNoise2", textureSettings.surfaceNoiseSettings2.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("heightExponent2", textureSettings.surfaceNoiseSettings2.heightExponent);
            heightMapComputeShader.SetFloat("layerStrength2", textureSettings.surfaceNoiseSettings2.layerStrength);
            heightMapComputeShader.SetFloat("heightRange2", textureSettings.surfaceNoiseSettings2.heightScale);
            heightMapComputeShader.SetFloat("domainWarping2", textureSettings.surfaceNoiseSettings2.domainWarping ? 1 : 0);

            heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 8f), Mathf.CeilToInt(textureSettings.textureHeight / 8f), 1);

            mapBuffer.GetData(originalHeightMap);
            mapBuffer.Release();

            Array.Copy(originalHeightMap, erodedHeightMap, originalHeightMap.Length);
            Array.Copy(originalHeightMap, mergedHeightMap, originalHeightMap.Length);
        }
    }
    void GenerateHumiditytMap()
    {
        humidityMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];

        ComputeBuffer humidityBuffer = new ComputeBuffer(originalHeightMap.Length, sizeof(float));
        humidityBuffer.SetData(originalHeightMap);
        heightMapComputeShader.SetBuffer(0, "heightMap", humidityBuffer);

        heightMapComputeShader.SetInt("seed", textureSettings.humidityNoiseSettings.seed);
        heightMapComputeShader.SetFloat("xOffset", textureSettings.humidityNoiseSettings.noiseOffset.x);
        heightMapComputeShader.SetFloat("yOffset", textureSettings.humidityNoiseSettings.noiseOffset.y);
        heightMapComputeShader.SetFloat("zOffset", textureSettings.humidityNoiseSettings.noiseOffset.z);
        heightMapComputeShader.SetInt("mapWidth", textureSettings.textureWidth);
        heightMapComputeShader.SetInt("mapHeight", textureSettings.textureHeight);
        heightMapComputeShader.SetInt("octaves", textureSettings.humidityNoiseSettings.octaves);
        heightMapComputeShader.SetFloat("lacunarity", textureSettings.humidityNoiseSettings.lacunarity);
        heightMapComputeShader.SetFloat("persistence", textureSettings.humidityNoiseSettings.persistence);
        heightMapComputeShader.SetFloat("multiplier", textureSettings.humidityNoiseSettings.multiplier);
        heightMapComputeShader.SetFloat("heightRange", textureSettings.humidityNoiseSettings.heightScale);
        heightMapComputeShader.SetInt("ridgedNoise", textureSettings.humidityNoiseSettings.ridged ? 1 : 0);
        heightMapComputeShader.SetFloat("heightExponent", textureSettings.humidityNoiseSettings.heightExponent);
        heightMapComputeShader.SetFloat("layerStrength", 1);
        heightMapComputeShader.SetFloat("domainWarping", 0);
        heightMapComputeShader.SetFloat("xOffset2", textureSettings.humidityNoiseSettings.noiseOffset.x);
        heightMapComputeShader.SetFloat("yOffset2", textureSettings.humidityNoiseSettings.noiseOffset.y);
        heightMapComputeShader.SetFloat("zOffset2", textureSettings.humidityNoiseSettings.noiseOffset.z);
        heightMapComputeShader.SetInt("seed2", textureSettings.humidityNoiseSettings.seed);
        heightMapComputeShader.SetFloat("multiplier2", textureSettings.humidityNoiseSettings.multiplier);
        heightMapComputeShader.SetInt("octaves2", textureSettings.humidityNoiseSettings.octaves);
        heightMapComputeShader.SetFloat("lacunarity2", textureSettings.humidityNoiseSettings.lacunarity);
        heightMapComputeShader.SetFloat("persistence2", textureSettings.humidityNoiseSettings.persistence);
        heightMapComputeShader.SetInt("ridgedNoise2", textureSettings.humidityNoiseSettings.ridged ? 1 : 0);
        heightMapComputeShader.SetFloat("heightExponent2", textureSettings.humidityNoiseSettings.heightExponent);
        heightMapComputeShader.SetFloat("layerStrength2", 0);
        heightMapComputeShader.SetFloat("heightRange2", textureSettings.humidityNoiseSettings.heightScale);
        heightMapComputeShader.SetFloat("domainWarping2", 0);

        heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 8f), Mathf.CeilToInt(textureSettings.textureHeight / 8f), 1);

        humidityBuffer.GetData(humidityMap);
        humidityBuffer.Release();
    }

    RenderTexture heightmapRT;
    public void HeightMap2Texture()
    {
        if (erodedHeightMap == null || originalHeightMap == null || mergedHeightMap == null)
            return;

        if (inciseFlowMap == null || inciseFlowMap.Length < originalHeightMap.Length)
            inciseFlowMap = new float[originalHeightMap.Length];

        bool useCpu = false;

        if (heightmap2TextureShader != null && !useCpu)
        {
            ComputeBuffer mapBuffer = new ComputeBuffer(originalHeightMap.Length, sizeof(float));
            mapBuffer.SetData(originalHeightMap);
            heightmap2TextureShader.SetBuffer(0, "originalHeightMap", mapBuffer);

            ComputeBuffer mapBufferEroded = new ComputeBuffer(erodedHeightMap.Length, sizeof(float));
            mapBufferEroded.SetData(erodedHeightMap);
            heightmap2TextureShader.SetBuffer(0, "erodedHeightMap", mapBufferEroded);

            ComputeBuffer inciseFlowMapBuffer = new ComputeBuffer(inciseFlowMap.Length, sizeof(float));
            inciseFlowMapBuffer.SetData(inciseFlowMap);
            heightmap2TextureShader.SetBuffer(0, "inciseFlowMap", inciseFlowMapBuffer);

            ComputeBuffer mapBufferMerged = new ComputeBuffer(mergedHeightMap.Length, sizeof(float));
            mapBufferMerged.SetData(mergedHeightMap);
            heightmap2TextureShader.SetBuffer(0, "mergedHeightMap", mapBufferMerged);

            if (heightmapRT != null && (heightmapRT.width != textureSettings.textureWidth || heightmapRT.height != textureSettings.textureHeight))
            {
                Destroy(heightmapRT);
                heightmapRT = null;
            }

            if (heightmapRT == null)
            {
                heightmapRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGBHalf);
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.Create();
            }

            heightmap2TextureShader.SetTexture(0, "result", heightmapRT);
            heightmap2TextureShader.SetInt("mapWidth", textureSettings.textureWidth);
            heightmap2TextureShader.SetInt("mapHeight", textureSettings.textureHeight);
            heightmap2TextureShader.SetFloat("waterLevel", textureSettings.waterLevel);
            heightmap2TextureShader.SetFloat("erosionNoiseMerge", textureSettings.erosionNoiseMerge);

            heightmap2TextureShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            if (heightmap == null || heightmap.width != textureSettings.textureWidth || heightmap.height != textureSettings.textureHeight)
                heightmap = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = heightmapRT;
            heightmap.ReadPixels(new Rect(0, 0, heightmapRT.width, heightmapRT.height), 0, 0);
            RenderTexture.active = prevActive;
            heightmap.Apply();
            //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "heightmap.png"));

            mapBufferMerged.GetData(mergedHeightMap);
            mapBuffer.Release();
            mapBufferEroded.Release();
            mapBufferMerged.Release();
            inciseFlowMapBuffer.Release();
        }
        else
        {
            Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
            for (int y = 0; y < textureSettings.textureHeight; y++)
            {
                for (int x = 0; x < textureSettings.textureWidth; x++)
                {
                    int index = x + y * textureSettings.textureWidth;
                    float erodedHeight = erodedHeightMap[index] - inciseFlowMap[index];
                    if (erodedHeight < 0) erodedHeight = 0;

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
            if (heightmap == null || heightmap.width != textureSettings.textureWidth || heightmap.height != textureSettings.textureHeight)
                heightmap = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);
            heightmap.SetPixels(colors);
            heightmap.Apply();
        }
    }

    public void RunErosionCycleFromButton()
    {
        StartCoroutine(PerformErosionCycle());
    }

    IEnumerator PerformErosionCycle()
    {
        ShowErodingTerrainPanel();
        yield return null;
        GenerateHeightMap();
        //string filename = Path.Combine(Application.persistentDataPath, "Textures", "heightmap.png");
        //Textures.instance.SaveTextureFloatArray(heightMap, textureSettings.textureWidth, textureSettings.textureHeight, filename);
        HydraulicErosion.instance.mapWidth = textureSettings.textureWidth;
        HydraulicErosion.instance.mapHeight = textureSettings.textureHeight;
        HydraulicErosion.instance.erosion = erosionShader;
        HydraulicErosion.instance.erosionSettings = erosionSettings;
        HydraulicErosion.instance.Erode(ref erodedHeightMap);
        HeightMap2Texture();
        //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "heightmap-2.png"));
        UpdateSurfaceMaterialHeightMap(true);
        HideErodingTerrainPanel();
        yield return null;
    }

    public void ReplotRivers()
    {
        System.Random random = new System.Random();
        inciseFlowSettings.riverPlotSeed = random.Next();

        PerformPlotRiversRandomly();
    }

    public void UndoErosion()
    {
        erodedHeightMap = null;
        originalHeightMap = null;
        mergedHeightMap = null;
        flowTexture = null;
        flowTextureRandom = null;
        inciseFlowMap = null;
        flowMap = null;
        isInciseFlowApplied = false;

        planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
    }

    Texture2D flowTexture;
    Texture2D flowTextureRandom;
    public void RunPlotRiversFromButton()
    {
        PerformPlotRiversRandomly();
    }

    public ComputeShader randomPlotRivers;
    RenderTexture randomRiversRT;
    void PerformPlotRiversRandomly()
    {
        bool useCpu = false;

        if (!inciseFlowSettings.plotRiversRandomly)
            return;

        if (useCpu)
        {
            SetupPlottingRiversPanel(plotRiversSettings.numIterations);
            ShowPlottingRiversPanel();
        }
        GenerateHeightMap();
        if (connectivityMap == null)
            EstablishHeightmapConnectivity();

        if (flowTextureRandom == null || flowTextureRandom.width != textureSettings.textureWidth || flowTextureRandom.height != textureSettings.textureHeight)
        {
            flowTextureRandom = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight);
            Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
            flowTextureRandom.SetPixels(colors);
            flowTextureRandom.Apply();
        }

        if (useCpu)
        {
            Rivers.instance.numIterations = plotRiversSettings.numIterations;
            Rivers.instance.textureWidth = textureSettings.textureWidth;
            Rivers.instance.textureHeight = textureSettings.textureHeight;
            Rivers.instance.waterLevel = textureSettings.waterLevel;
            Rivers.instance.flowHeightDelta = plotRiversSettings.flowHeightDelta;
            Rivers.instance.startingAlpha = plotRiversSettings.startingAlpha;
            Rivers.instance.riverColor = plotRiversSettings.riverColor;
            Rivers.instance.heightWeight = plotRiversSettings.heightWeight;
            Rivers.instance.brushSize = plotRiversSettings.brushSize;
            Rivers.instance.brushExponent = plotRiversSettings.brushExponent;
            Rivers.instance.heightMap = erodedHeightMap;

            Rivers.instance.Init(ref flowTexture);
            Rivers.instance.StartThreads();

            int updateStep = plotRiversSettings.numIterations / 100;
            int startedIterations = 0;
            while (startedIterations < plotRiversSettings.numIterations)
            {
                startedIterations = Rivers.instance.RunStep();
                if (startedIterations % updateStep == 0 || startedIterations >= plotRiversSettings.numIterations)
                {
                    UpdatePlottingRiversPanel(startedIterations + 1);
                }
            }

            Rivers.instance.WaitForThreads();
            Rivers.instance.Finalize(ref flowTexture);
        }
        else
        {
            GenerateHeightMap();
            if (connectivityMap == null) 
                EstablishHeightmapConnectivity();
            CalculateLandMask();

            int actualNumberOfRiverSources = inciseFlowSettings.numberOfRivers;
            if (actualNumberOfRiverSources > landMaskCount)
                actualNumberOfRiverSources = landMaskCount;

            int[] riverFlowMask = new int[textureSettings.textureWidth * textureSettings.textureHeight];

            System.Random random = new System.Random(inciseFlowSettings.riverPlotSeed);
            for (int i = 0; i < actualNumberOfRiverSources; i++)
            {
                Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                Vector2 pointInMap = pointInSpace.CartesianToPolarRatio(1);
                int mapX = (int)(pointInMap.x * textureSettings.textureWidth);
                int mapY = (int)(pointInMap.y * textureSettings.textureHeight);

                int dropPointIndex = mapY * textureSettings.textureWidth + mapX;
                if (erodedHeightMap[dropPointIndex] <= textureSettings.waterLevel)
                {
                    i--;
                    continue;
                }
                riverFlowMask[dropPointIndex] = 1;
            }

            ComputeBuffer riverFlowMaskBuffer = new ComputeBuffer(riverFlowMask.Length, sizeof(int));
            riverFlowMaskBuffer.SetData(riverFlowMask);

            ComputeBuffer connectivityMapBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
            connectivityMapBuffer.SetData(connectivityMap);

            if (randomRiversRT != null && (randomRiversRT.width != textureSettings.textureWidth || randomRiversRT.height != textureSettings.textureHeight))
            {
                Destroy(randomRiversRT);
                randomRiversRT = null;
            }

            if (randomRiversRT == null)
            {
                randomRiversRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGBHalf);
                randomRiversRT.name = "Heightmap Render Texture";
                randomRiversRT.enableRandomWrite = true;
                randomRiversRT.Create();
            }
            randomRiversRT.Release();

            if (flowTexture == null || flowTexture.width != textureSettings.textureWidth || flowTexture.height != textureSettings.textureHeight)
            {
                flowTexture = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight);
                Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
                flowTexture.SetPixels(colors);
                flowTexture.Apply();
            }

            randomPlotRivers.SetInt("mapWidth", textureSettings.textureWidth);
            randomPlotRivers.SetInt("mapHeight", textureSettings.textureHeight);
            randomPlotRivers.SetFloat("waterLevel", textureSettings.waterLevel);
            randomPlotRivers.SetFloat("startingAlpha", inciseFlowSettings.startingAlpha);
            randomPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });
            randomPlotRivers.SetBuffer(0, "riverFlowMask", riverFlowMaskBuffer);
            randomPlotRivers.SetBuffer(0, "connectvityMap", connectivityMapBuffer);
            randomPlotRivers.SetTexture(0, "original", flowTexture);
            randomPlotRivers.SetTexture(0, "result", randomRiversRT);

            int numPasses = (int)(textureSettings.textureWidth * (1 - textureSettings.waterLevel) / 3);
            for (int i = 0; i < numPasses; i++)
                randomPlotRivers.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            if (flowTextureRandom == null || flowTextureRandom.width != textureSettings.textureWidth || flowTextureRandom.height != textureSettings.textureHeight)
                flowTextureRandom = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = randomRiversRT;
            flowTextureRandom.ReadPixels(new Rect(0, 0, randomRiversRT.width, randomRiversRT.height), 0, 0);
            RenderTexture.active = prevActive;
            flowTextureRandom.Apply();

            riverFlowMaskBuffer.Release();
            connectivityMapBuffer.Release();
            //RenderTexture.ReleaseTemporary(randomRiversRT);
        }

        //PlotRivers.instance.Run(ref erodedHeightMap, ref flowTexture);
        //flowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowMap.png"));
        //HeightMap2Texture();
        //planetSurfaceMaterial.SetTexture("_MainTex", heightmap);
        planetSurfaceMaterial.SetTexture("_FlowTex", flowTextureRandom);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
        //planetSurfaceMaterial.SetInt("_IsEroded", 1);
        if (useCpu)
            HidePlottingRiversPanel();
    }

    public ComputeShader landMaskShader;
    int[] landMask;
    int landMaskCount = 0;
    void CalculateLandMask()
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer(erodedHeightMap.Length, sizeof(float));
        heightMapBuffer.SetData(erodedHeightMap);

        landMask = new int[erodedHeightMap.Length];
        ComputeBuffer landMaskBuffer = new ComputeBuffer(landMask.Length, sizeof(int));
        landMaskBuffer.SetData(landMask);

        landMaskShader.SetInt("mapWidth", textureSettings.textureWidth);
        landMaskShader.SetInt("mapHeight", textureSettings.textureHeight);
        landMaskShader.SetFloat("waterLevel", textureSettings.waterLevel);
        landMaskShader.SetBuffer(0, "heightMap", heightMapBuffer);
        landMaskShader.SetBuffer(0, "landMask", landMaskBuffer);

        int numThreadsX = Mathf.CeilToInt(textureSettings.textureWidth / 32f);
        int numThreadsY = Mathf.CeilToInt(textureSettings.textureHeight / 32f);

        landMaskShader.Dispatch(0, numThreadsX, numThreadsY, 1);

        landMaskBuffer.GetData(landMask);
        landMaskCount = landMask.Sum();

        heightMapBuffer.Release();
        landMaskBuffer.Release();
    }

    struct float4
    {
        float flowLeft;
        float flowRight;
        float flowTop;
        float flowBottom;
    }

    struct float2
    {
        float velocityHorizontal;
        float velocityVertical;
    }

    public ComputeShader inciseFlowFlowMap;
    public ComputeShader inciseFlowErosion;

    float[] inciseFlowMap;
    float[] flowMap;
    float flowMaxValue;
    void PerformInciseFlow(bool establishConnectivity, bool replotRivers, bool replotRandomRivers)
    {
        bool useCPU = false;

        GenerateHeightMap();
        if (establishConnectivity)
            EstablishHeightmapConnectivity();

        inciseFlowMap = new float[erodedHeightMap.Length];
        flowMap = new float[erodedHeightMap.Length];

        if (useCPU)
        {
            InciseFlow.instance.mapWidth = textureSettings.textureWidth;
            InciseFlow.instance.mapHeight = textureSettings.textureHeight;
            InciseFlow.instance.logBase = inciseFlowSettings.exponent;
            InciseFlow.instance.amount = inciseFlowSettings.amount;
            InciseFlow.instance.minAmount = inciseFlowSettings.minAmount;
            InciseFlow.instance.strength = inciseFlowSettings.strength;
            InciseFlow.instance.heightFactor = 0.05f;
            InciseFlow.instance.maxFlowStrength = inciseFlowSettings.maxFlowStrength;
            InciseFlow.instance.curveFactor = inciseFlowSettings.chiselStrength;
            InciseFlow.instance.heightInfluence = inciseFlowSettings.heightInfluence;
            InciseFlow.instance.waterLevel = textureSettings.waterLevel;
            InciseFlow.instance.blur = inciseFlowSettings.preBlur;
            InciseFlow.instance.flowMap = flowMap;

            InciseFlow.instance.heightMap = erodedHeightMap;
            InciseFlow.instance.drainageIndexesMap = connectivityMap;
            InciseFlow.instance.inciseFlowMap = inciseFlowMap;
            InciseFlow.instance.Run();
        }
        else
        {
            int numPasses = (int)(textureSettings.textureWidth * (1 - textureSettings.waterLevel) / 3);

            ComputeBuffer flowMapBuffer = new ComputeBuffer(flowMap.Length, sizeof(float));
            flowMapBuffer.SetData(flowMap);

            ComputeBuffer heightMapBuffer = new ComputeBuffer(erodedHeightMap.Length, sizeof(float));
            heightMapBuffer.SetData(erodedHeightMap);

            ComputeBuffer drainageIndexesBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
            drainageIndexesBuffer.SetData(connectivityMap);

            ComputeBuffer inciseFlowMapBuffer = new ComputeBuffer(inciseFlowMap.Length, sizeof(float));
            inciseFlowMapBuffer.SetData(inciseFlowMap);

            inciseFlowFlowMap.SetInt("mapWidth", textureSettings.textureWidth);
            inciseFlowFlowMap.SetInt("mapHeight", textureSettings.textureHeight);
            inciseFlowFlowMap.SetFloat("amount", inciseFlowSettings.amount);
            inciseFlowFlowMap.SetBuffer(0, "flowMap", flowMapBuffer);
            inciseFlowFlowMap.SetBuffer(0, "drainageIndexesMap", drainageIndexesBuffer);

            int numThreadsX = Mathf.CeilToInt(textureSettings.textureWidth / 32f);
            int numThreadsY = Mathf.CeilToInt(textureSettings.textureHeight / 32f);

            for (int i = 0; i < numPasses; i++)
                inciseFlowFlowMap.Dispatch(0, numThreadsX, numThreadsY, 1);

            inciseFlowErosion.SetInt("mapWidth", textureSettings.textureWidth);
            inciseFlowErosion.SetInt("mapHeight", textureSettings.textureHeight);
            inciseFlowErosion.SetFloat("logBase", inciseFlowSettings.exponent);
            inciseFlowErosion.SetFloat("heightFactor", 0.05f);
            inciseFlowErosion.SetFloat("strength", inciseFlowSettings.strength);
            inciseFlowErosion.SetFloat("minAmount", inciseFlowSettings.minAmount);
            inciseFlowErosion.SetFloat("maxFlowStrength", inciseFlowSettings.maxFlowStrength);
            inciseFlowErosion.SetFloat("curveFactor", inciseFlowSettings.chiselStrength);
            inciseFlowErosion.SetFloat("heightInfluence", inciseFlowSettings.heightInfluence);
            inciseFlowErosion.SetFloat("waterLevel", textureSettings.waterLevel);
            inciseFlowErosion.SetFloat("blur", inciseFlowSettings.preBlur);
            inciseFlowErosion.SetBuffer(0, "heightMap", heightMapBuffer);
            inciseFlowErosion.SetBuffer(0, "flowMap", flowMapBuffer);
            inciseFlowErosion.SetBuffer(0, "inciseFlowMap", inciseFlowMapBuffer);

            inciseFlowErosion.Dispatch(0, numThreadsX, numThreadsY, 1);

            inciseFlowMapBuffer.GetData(inciseFlowMap);

            if (inciseFlowSettings.postBlur > 0)
            {
                float actualBlur = inciseFlowSettings.postBlur / 3;
                AverageHeightMap(actualBlur, ref inciseFlowMap);
            }

            flowMapBuffer.GetData(flowMap);

            flowMaxValue = flowMap.Max();

            heightMapBuffer.Release();
            flowMapBuffer.Release();
            inciseFlowMapBuffer.Release();
            drainageIndexesBuffer.Release();
        }

        if ((inciseFlowSettings.plotRivers && replotRivers) || (inciseFlowSettings.plotRiversRandomly && replotRandomRivers))
            InciseFlowPlotRivers();

        if (!inciseFlowSettings.plotRivers && !inciseFlowSettings.plotRiversRandomly)
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

        HeightMap2Texture();
        UpdateSurfaceMaterialHeightMap(true);
    }

    public ComputeShader inciseFlowPlotRivers;
    RenderTexture plotRiversRT;
    void InciseFlowPlotRivers()
    {
        if (!inciseFlowSettings.plotRivers && !inciseFlowSettings.plotRiversRandomly)
            return;

        if (flowTexture == null || flowTexture.width != textureSettings.textureWidth || flowTexture.height != textureSettings.textureHeight)
        {
            flowTexture = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight);
            Color[] colors = new Color[textureSettings.textureWidth * textureSettings.textureHeight];
            flowTexture.SetPixels(colors);
            flowTexture.Apply();
        }

        if (plotRiversRT != null && (plotRiversRT.width != textureSettings.textureWidth || plotRiversRT.height != textureSettings.textureHeight))
        {
            Destroy(plotRiversRT);
            plotRiversRT = null;
        }

        if (plotRiversRT == null)
        {
            plotRiversRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGBHalf);
            plotRiversRT.name = "Heightmap Render Texture";
            plotRiversRT.enableRandomWrite = true;
            plotRiversRT.Create();
        }
        plotRiversRT.Release();

        if (inciseFlowSettings.plotRivers)
        {
            if (flowMap == null)
                flowMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];

            ComputeBuffer flowMapBuffer = new ComputeBuffer(erodedHeightMap.Length, sizeof(float));
            flowMapBuffer.SetData(flowMap);

            //RenderTexture prevActive = RenderTexture.active;
            //RenderTexture.active = rTexture;
            inciseFlowPlotRivers.SetTexture(0, "result", plotRiversRT);
            inciseFlowPlotRivers.SetInt("mapWidth", textureSettings.textureWidth);
            inciseFlowPlotRivers.SetInt("mapHeight", textureSettings.textureHeight);
            inciseFlowPlotRivers.SetBuffer(0, "inciseFlowMap", flowMapBuffer);
            inciseFlowPlotRivers.SetFloat("riverAmount1", inciseFlowSettings.riverAmount1);
            inciseFlowPlotRivers.SetFloat("riverAmount2", inciseFlowSettings.riverAmount2);
            inciseFlowPlotRivers.SetFloat("maxValue", flowMaxValue);
            inciseFlowPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });

            inciseFlowPlotRivers.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            //RenderTexture.active = prevActive;
            //flowTexture.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "flowTexture1.png"));
            flowMapBuffer.Release();
        }
        if (inciseFlowSettings.plotRiversRandomly)
        {
            //GenerateHeightMap();
            if (connectivityMap == null)
                EstablishHeightmapConnectivity();
            CalculateLandMask();

            int actualNumberOfRiverSources = inciseFlowSettings.numberOfRivers;
            if (actualNumberOfRiverSources > landMaskCount)
                actualNumberOfRiverSources = landMaskCount;

            int[] riverFlowMask = new int[textureSettings.textureWidth * textureSettings.textureHeight];

            System.Random random = new System.Random(inciseFlowSettings.riverPlotSeed);
            for (int i = 0; i < actualNumberOfRiverSources; i++)
            {
                Vector3 pointInSpace = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                Vector2 pointInMap = pointInSpace.CartesianToPolarRatio(1);
                int mapX = (int)(pointInMap.x * textureSettings.textureWidth);
                int mapY = (int)(pointInMap.y * textureSettings.textureHeight);

                int dropPointIndex = mapY * textureSettings.textureWidth + mapX;
                if (erodedHeightMap[dropPointIndex] <= textureSettings.waterLevel)
                {
                    i--;
                    continue;
                }
                riverFlowMask[dropPointIndex] = 1;
            }

            ComputeBuffer riverFlowMaskBuffer = new ComputeBuffer(riverFlowMask.Length, sizeof(int));
            riverFlowMaskBuffer.SetData(riverFlowMask);

            ComputeBuffer connectivityMapBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
            connectivityMapBuffer.SetData(connectivityMap);

            randomPlotRivers.SetInt("mapWidth", textureSettings.textureWidth);
            randomPlotRivers.SetInt("mapHeight", textureSettings.textureHeight);
            randomPlotRivers.SetFloat("waterLevel", textureSettings.waterLevel);
            randomPlotRivers.SetFloat("startingAlpha", inciseFlowSettings.startingAlpha);
            randomPlotRivers.SetFloats("riverColor", new float[] { inciseFlowSettings.riverColor.r, inciseFlowSettings.riverColor.g, inciseFlowSettings.riverColor.b });
            randomPlotRivers.SetBuffer(0, "riverFlowMask", riverFlowMaskBuffer);
            randomPlotRivers.SetBuffer(0, "connectvityMap", connectivityMapBuffer);
            randomPlotRivers.SetTexture(0, "original", flowTexture);
            randomPlotRivers.SetTexture(0, "result", plotRiversRT);

            int numPasses = (int)(textureSettings.textureWidth * (1 - textureSettings.waterLevel) / 3);
            for (int i = 0; i < numPasses; i++)
                randomPlotRivers.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            riverFlowMaskBuffer.Release();
            connectivityMapBuffer.Release();
        }

        if (flowTexture == null || flowTexture.width != textureSettings.textureWidth || flowTexture.height != textureSettings.textureHeight)
            flowTexture = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = plotRiversRT;
        flowTexture.ReadPixels(new Rect(0, 0, plotRiversRT.width, plotRiversRT.height), 0, 0);
        RenderTexture.active = prevActive;
        flowTexture.Apply();

        //RenderTexture.ReleaseTemporary(plotRiversRT);

        planetSurfaceMaterial.SetTexture("_FlowTex", flowTexture);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
    }

    public ComputeShader heightmapConnectivityShader;
    public ComputeShader heightmapAverageShader;

    int[] connectivityMap;
    float[] distanceToWaterMap;
    void EstablishHeightmapConnectivity()
    {
        bool useCPU = false;
        //if (connectivityMap == null)
        {
            connectivityMap = new int[erodedHeightMap.Length];
            distanceToWaterMap = new float[erodedHeightMap.Length];

            if (useCPU)
            {
                // Stabilizes until done. No predefined number of passes.
                FillBasins.instance.mapWidth = textureSettings.textureWidth;
                FillBasins.instance.mapHeight = textureSettings.textureHeight;
                FillBasins.instance.waterLevel = textureSettings.waterLevel;
                FillBasins.instance.upwardWeight = inciseFlowSettings.upwardWeight;
                FillBasins.instance.downwardWeight = inciseFlowSettings.downwardWeight;
                FillBasins.instance.distanceWeight = inciseFlowSettings.distanceWeight;
                FillBasins.instance.heightMap = erodedHeightMap;
                FillBasins.instance.distanceMap = distanceToWaterMap;
                FillBasins.instance.connectivityMap = connectivityMap;
                FillBasins.instance.Run();
            }
            else
            {
                int numPasses = (int)(textureSettings.textureWidth * (1 - textureSettings.waterLevel) / 3);

                int numThreadsX = Mathf.CeilToInt(textureSettings.textureWidth / 32f);
                int numThreadsY = Mathf.CeilToInt(textureSettings.textureHeight / 32f);

                float[] connectivityHeightMap = new float[erodedHeightMap.Length];
                Array.Copy(erodedHeightMap, connectivityHeightMap, erodedHeightMap.Length);

                ComputeBuffer heightBuffer = new ComputeBuffer(connectivityHeightMap.Length, sizeof(float));
                heightBuffer.SetData(connectivityHeightMap);

                ComputeBuffer connectivityIndexesBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
                connectivityIndexesBuffer.SetData(connectivityMap);

                ComputeBuffer distanceToWaterBuffer = new ComputeBuffer(distanceToWaterMap.Length, sizeof(float));
                distanceToWaterBuffer.SetData(connectivityMap);

                heightmapConnectivityShader.SetInt("mapWidth", textureSettings.textureWidth);
                heightmapConnectivityShader.SetInt("mapHeight", textureSettings.textureHeight);
                heightmapConnectivityShader.SetFloat("waterLevel", textureSettings.waterLevel);
                heightmapConnectivityShader.SetFloat("upwardWeight", inciseFlowSettings.upwardWeight);
                heightmapConnectivityShader.SetFloat("downwardWeight", inciseFlowSettings.downwardWeight);
                heightmapConnectivityShader.SetFloat("distanceWeight", inciseFlowSettings.distanceWeight);

                heightmapConnectivityShader.SetBuffer(0, "heightMap", heightBuffer);
                heightmapConnectivityShader.SetBuffer(0, "distanceMap", distanceToWaterBuffer);
                heightmapConnectivityShader.SetBuffer(0, "connectivityMap", connectivityIndexesBuffer);

                for (int i = 0; i < numPasses; i++)
                    heightmapConnectivityShader.Dispatch(0, numThreadsX, numThreadsY, 1);

                connectivityIndexesBuffer.GetData(connectivityMap);

                heightBuffer.Release();
                connectivityIndexesBuffer.Release();
                distanceToWaterBuffer.Release();
            }
            //connectivityMap.SaveConnectivityMap(textureSettings.textureWidth, Path.Combine(Application.persistentDataPath, "Textures", "connectivityColors.png"));
        }
    }

    void AverageHeightMap(float blur, ref float[] heightMap)
    {
        int numThreadsX = Mathf.CeilToInt(textureSettings.textureWidth / 32f);
        int numThreadsY = Mathf.CeilToInt(textureSettings.textureHeight / 32f);

        ComputeBuffer heightBuffer1 = new ComputeBuffer(heightMap.Length, sizeof(float));
        heightBuffer1.SetData(heightMap);

        ComputeBuffer heightBuffer2 = new ComputeBuffer(heightMap.Length, sizeof(float));
        heightBuffer2.SetData(heightMap);

        heightmapAverageShader.SetInt("mapWidth", textureSettings.textureWidth);
        heightmapAverageShader.SetInt("mapHeight", textureSettings.textureHeight);
        heightmapAverageShader.SetFloat("blur", blur);

        int i = 0;
        for (i = 0; i < Mathf.CeilToInt(blur); i++)
        {
            heightmapAverageShader.SetInt("blurStep", i + 1);
            if (i % 2 == 0)
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap", heightBuffer1);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap", heightBuffer2);
            }
            else
            {
                heightmapAverageShader.SetBuffer(0, "sourceHeightMap", heightBuffer2);
                heightmapAverageShader.SetBuffer(0, "targetHeightMap", heightBuffer1);
            }
            heightmapAverageShader.Dispatch(0, numThreadsX, numThreadsY, 1);
        }

        if (i % 2 == 0)
            heightBuffer1.GetData(heightMap);
        else
            heightBuffer2.GetData(heightMap);

        heightBuffer1.Release();
        heightBuffer2.Release();
    }

    bool isInciseFlowApplied = false;
    public void ApplyInciseFlow()
    {
        isInciseFlowApplied = true;
        for (int y = 0; y < textureSettings.textureHeight; y++)
        {
            for (int x = 0; x < textureSettings.textureWidth; x++)
            {
                int index = x + y * textureSettings.textureWidth;
                erodedHeightMap[index] -= inciseFlowMap[index];
                if (erodedHeightMap[index] < 0)
                    erodedHeightMap[index] = 0;
                inciseFlowMap[index] = 0;
            }
        }
        inciseFlowMap = null;
        HeightMap2Texture();
        UpdateSurfaceMaterialHeightMap(true);
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

    public ComputeShader pluvialErosionOutflow;
    public ComputeShader pluvialErosionVelocitymap;
    public ComputeShader pluvialErosionErosion;
    public ComputeShader pluvialErosionSediment;

    IEnumerator PerformPluvialErosion()
    {
        bool useCPU = false;
        int numPasses = 100;
        int numRiverSources = 100;

        SetupPluvialErodingPanel(numPasses);
        ShowPluvialErodingPanel();
        yield return null;

        GenerateHeightMap();
        inciseFlowMap = new float[erodedHeightMap.Length];
        GenerateHumiditytMap();

        if (useCPU)
        {
            PluvialErosion.instance.numPasses = numPasses;
            PluvialErosion.instance.numRiverSources = numRiverSources;
            PluvialErosion.instance.waterScale = 0.1f;
            PluvialErosion.instance.waterFixedAmount = 0.1f;
            PluvialErosion.instance.gravity = 10;
            PluvialErosion.instance.sedimentCapacity = 10000f;
            PluvialErosion.instance.minTiltAngle = 0.001f;
            PluvialErosion.instance.sedimentDissolvingConstant = 1f;
            PluvialErosion.instance.sedimentDepositionConstant = 1f;
            PluvialErosion.instance.waterEvaporationRetention = 0.95f;
            PluvialErosion.instance.maxErosionDepth = 0.1f;
            PluvialErosion.instance.mapWidth = textureSettings.textureWidth;
            PluvialErosion.instance.mapHeight = textureSettings.textureHeight;
            PluvialErosion.instance.waterLevel = textureSettings.waterLevel;
            PluvialErosion.instance.map = this;
            PluvialErosion.instance.Init(ref erodedHeightMap, ref humidityMap);
            for (int i = 0; i < numPasses; i++)
            {
                PluvialErosion.instance.ErodeStep(ref erodedHeightMap, ref humidityMap);

                HeightMap2Texture();
                UpdateSurfaceMaterialHeightMap(true);
                UpdatePluvialErodingPanel(i + 1);
                yield return null;
            }
        }
        else
        {
            if (pluvialErosionOutflow != null && pluvialErosionVelocitymap != null && pluvialErosionErosion != null)
            {
                float[] riverSourcesMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
                float[] waterHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
                float[] sedimentMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
                float4[] outflowMap = new float4[textureSettings.textureWidth * textureSettings.textureHeight];
                float2[] velocityMap = new float2[textureSettings.textureWidth * textureSettings.textureHeight];
                System.Random random = new System.Random();

                for (int i = 0; i < numRiverSources; i++)
                {
                    int randomX = random.Next(textureSettings.textureWidth);
                    int randomY = random.Next(textureSettings.textureHeight);
                    int index = randomY * textureSettings.textureWidth + randomX;
                    float height = erodedHeightMap[index];
                    if (height <= textureSettings.waterLevel)
                    {
                        i--;
                        continue;
                    }
                    float riverSourceStrength = (float)(random.NextDouble());
                    riverSourcesMap[index] = riverSourceStrength;
                }

                ComputeBuffer riverSourcesBuffer = new ComputeBuffer(riverSourcesMap.Length, sizeof(float));
                ComputeBuffer heightMapBuffer = new ComputeBuffer(erodedHeightMap.Length, sizeof(float));
                ComputeBuffer waterHeightBuffer = new ComputeBuffer(waterHeightMap.Length, sizeof(float));
                ComputeBuffer sedimentBuffer = new ComputeBuffer(sedimentMap.Length, sizeof(float));
                ComputeBuffer humidityBuffer = new ComputeBuffer(humidityMap.Length, sizeof(float));
                ComputeBuffer outflowBuffer = new ComputeBuffer(outflowMap.Length, sizeof(float) * 4);
                ComputeBuffer velocityBuffer = new ComputeBuffer(velocityMap.Length, sizeof(float) * 2);

                riverSourcesBuffer.SetData(riverSourcesMap);
                heightMapBuffer.SetData(erodedHeightMap);
                waterHeightBuffer.SetData(waterHeightMap);
                sedimentBuffer.SetData(sedimentMap);
                humidityBuffer.SetData(humidityMap);
                outflowBuffer.SetData(outflowMap);
                velocityBuffer.SetData(velocityMap);

                pluvialErosionOutflow.SetInt("mapWidth", textureSettings.textureWidth);
                pluvialErosionOutflow.SetInt("mapHeight", textureSettings.textureHeight);
                pluvialErosionOutflow.SetFloat("waterScale", 0.5f);
                pluvialErosionOutflow.SetFloat("waterFixedAmount", 0.1f);
                pluvialErosionOutflow.SetFloat("gravity", 200f);
                pluvialErosionOutflow.SetBuffer(0, "riverSourcesMap", riverSourcesBuffer);
                pluvialErosionOutflow.SetBuffer(0, "humidityMap", humidityBuffer);
                pluvialErosionOutflow.SetBuffer(0, "heightMap", heightMapBuffer);
                pluvialErosionOutflow.SetBuffer(0, "waterHeightMap", waterHeightBuffer);
                pluvialErosionOutflow.SetBuffer(0, "outflowMap", outflowBuffer);

                pluvialErosionVelocitymap.SetInt("mapWidth", textureSettings.textureWidth);
                pluvialErosionVelocitymap.SetInt("mapHeight", textureSettings.textureHeight);
                pluvialErosionVelocitymap.SetBuffer(0, "outflowMap", outflowBuffer);
                pluvialErosionVelocitymap.SetBuffer(0, "waterHeightMap", waterHeightBuffer);
                pluvialErosionVelocitymap.SetBuffer(0, "velocityMap", velocityBuffer);

                pluvialErosionErosion.SetInt("mapWidth", textureSettings.textureWidth);
                pluvialErosionErosion.SetInt("mapHeight", textureSettings.textureHeight);
                pluvialErosionErosion.SetFloat("minTiltAngle", 0.001f);
                pluvialErosionErosion.SetFloat("sedimentCapacity", 10f);
                pluvialErosionErosion.SetFloat("maxErosionDepth", 1f);
                pluvialErosionErosion.SetFloat("sedimentDissolvingConstant", 0.5f);
                pluvialErosionErosion.SetFloat("sedimentDepositionConstant", 1f);
                pluvialErosionErosion.SetBuffer(0, "sedimentMap", sedimentBuffer);
                pluvialErosionErosion.SetBuffer(0, "heightMap", heightMapBuffer);
                pluvialErosionErosion.SetBuffer(0, "velocityMap", velocityBuffer);
                pluvialErosionErosion.SetBuffer(0, "waterHeightMap", waterHeightBuffer);

                pluvialErosionSediment.SetInt("mapWidth", textureSettings.textureWidth);
                pluvialErosionSediment.SetInt("mapHeight", textureSettings.textureHeight);
                pluvialErosionSediment.SetFloat("waterEvaporationRetention", 0.85f);
                pluvialErosionSediment.SetBuffer(0, "sedimentMap", sedimentBuffer);
                pluvialErosionSediment.SetBuffer(0, "velocityMap", velocityBuffer);
                pluvialErosionSediment.SetBuffer(0, "waterHeightMap", waterHeightBuffer);

                int numThreadsX = Mathf.CeilToInt(textureSettings.textureWidth / 8f);
                int numThreadsY = Mathf.CeilToInt(textureSettings.textureHeight / 8f);

                for (int i = 0; i < numPasses; i++)
                {
                    pluvialErosionOutflow.Dispatch(0, numThreadsX, numThreadsY, 1);
                    pluvialErosionVelocitymap.Dispatch(0, numThreadsX, numThreadsY, 1);
                    pluvialErosionErosion.Dispatch(0, numThreadsX, numThreadsY, 1);
                    pluvialErosionSediment.Dispatch(0, numThreadsX, numThreadsY, 1);

                    heightMapBuffer.GetData(erodedHeightMap);

                    HeightMap2Texture();
                    UpdateSurfaceMaterialHeightMap(true);
                    UpdatePluvialErodingPanel(i + 1);
                    yield return null;
                }

                heightMapBuffer.GetData(erodedHeightMap);
                waterHeightBuffer.GetData(waterHeightMap);
                sedimentBuffer.GetData(sedimentMap);
                humidityBuffer.GetData(humidityMap);
                outflowBuffer.GetData(outflowMap);
                velocityBuffer.GetData(velocityMap);

                // Release buffers
                riverSourcesBuffer.Release();
                heightMapBuffer.Release();
                waterHeightBuffer.Release();
                sedimentBuffer.Release();
                humidityBuffer.Release();
                outflowBuffer.Release();
                velocityBuffer.Release();
            }
        }
        HeightMap2Texture();
        UpdateSurfaceMaterialHeightMap(true);

        HidePluvialErodingPanel();
        yield return null;
    }
}
