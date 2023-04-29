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

                GenerateHeightMap();

                if (AppData.instance.SaveMainMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainMap" + fileNameExtension), SphereShaderDrawType.LandWithNoNormals);
                }

                if (AppData.instance.SaveHeightMap)
                {
                    HeightMap2Texture();
                    heightmapRT.SaveToFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Heightmap" + fileNameExtension));
                }

                if (AppData.instance.SaveLandMask)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Landmask" + fileNameExtension), SphereShaderDrawType.LandMask);
                }

                if (AppData.instance.SaveNormalMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Normalmap" + fileNameExtension), SphereShaderDrawType.Normal);
                }

                if (AppData.instance.SaveMainMap && AppData.instance.SaveNormalMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainNormalMap" + fileNameExtension), SphereShaderDrawType.LandNormal);
                }

                if (AppData.instance.SaveTemperature)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Temperature" + fileNameExtension), SphereShaderDrawType.Temperature);
                }

                AppData.instance.Save();
            }
        }
        else
            cameraController.CloseContextMenu();
    }

    RenderTexture exportRT = null;
    void SaveImageFile(string fileName, SphereShaderDrawType drawMode)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        float prevDrawMode = planetSurfaceMaterial.GetFloat("_DrawType");
        planetSurfaceMaterial.SetFloat("_DrawType", (int)drawMode);

        if (exportRT != null && (exportRT.width != TextureManager.instance.Settings.textureWidth * 4 || exportRT.height != TextureManager.instance.Settings.textureWidth * 2))
        {
            Destroy(exportRT);
            exportRT = null;
        }

        if (exportRT == null)
        {
            exportRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 32, RenderTextureFormat.ARGBHalf);
            exportRT.wrapMode = TextureWrapMode.Repeat;
            exportRT.name = "Export Render Texture";
            exportRT.enableRandomWrite = true;
            exportRT.Create();
        }

        Texture2D source;
        if (drawMode != SphereShaderDrawType.HeightMap)
            source = new Texture2D(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, TextureFormat.RGBA32, false);
        else
            source = new Texture2D(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, TextureFormat.RGB48, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = exportRT;
        Graphics.Blit(source, exportRT, planetSurfaceMaterial, 2);
        exportRT.Save(fileName);
        RenderTexture.active = prevActive;
        planetSurfaceMaterial.SetFloat("_DrawType", prevDrawMode);

        UnityEngine.Object.Destroy(source);
    }

    Texture2D ShaderToTexture(SphereShaderDrawType drawMode, int dimensions)
    {
        float prevDrawMode = planetSurfaceMaterial.GetFloat("_DrawType");
        planetSurfaceMaterial.SetFloat("_DrawType", (int)drawMode);

        if (exportRT != null && (exportRT.width != dimensions || exportRT.height != dimensions))
        {
            Destroy(exportRT);
            exportRT = null;
        }

        if (exportRT == null)
        {
            exportRT = new RenderTexture(dimensions, dimensions, 32, RenderTextureFormat.ARGBHalf);
            exportRT.wrapMode = TextureWrapMode.Repeat;
            exportRT.name = "Export Render Texture";
            exportRT.enableRandomWrite = true;
            exportRT.Create();
        }

        Texture2D source = new Texture2D(dimensions, dimensions, TextureFormat.RGBA32, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = exportRT;
        Graphics.Blit(source, exportRT, planetSurfaceMaterial, 2);
        source.ReadPixels(new Rect(0, 0, dimensions, dimensions), 0, 0);
        source.Apply();
        RenderTexture.active = prevActive;
        planetSurfaceMaterial.SetFloat("_DrawType", prevDrawMode);
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

        string baseFolder = Path.Combine(savePath, saveFile, "Surface");
        if (AppData.instance.SaveMainMap || AppData.instance.SaveLandMask)
            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

        string bumpBaseFolder = Path.Combine(savePath, saveFile, "Bump");
        if (AppData.instance.SaveHeightMap)
            if (!Directory.Exists(bumpBaseFolder))
                Directory.CreateDirectory(bumpBaseFolder);

        // Gets the Main Map.
        Texture2D baseMainMap = ShaderToTexture(SphereShaderDrawType.LandNormal, AppData.instance.CubemapDimension);
        // Gets the Landmask.
        Texture2D baseLandMask = ShaderToTexture(SphereShaderDrawType.InvertedLandMask, AppData.instance.CubemapDimension);
        // Gets the Bump.
        Texture2D baseBumpMap = ShaderToTexture(SphereShaderDrawType.HeightMap, AppData.instance.CubemapDimension);

        if (AppData.instance.TransparentOceans || (AppData.instance.SaveMainMap && !AppData.instance.SaveLandMask))
            baseMainMap.SaveAsPNG(Path.Combine(baseFolder, "base.png"));
        else
        {
            baseMainMap.SaveAsJPG(Path.Combine(baseFolder, "base_c.jpg"));
            baseLandMask.SaveAsJPG(Path.Combine(baseFolder, "base_a.jpg"));
        }

        if (AppData.instance.SaveHeightMap)
            baseBumpMap.SaveAsPNG(Path.Combine(bumpBaseFolder, "base.png"));

        UnityEngine.Object.DestroyImmediate(baseMainMap);
        UnityEngine.Object.DestroyImmediate(baseLandMask);
        UnityEngine.Object.DestroyImmediate(baseBumpMap);

        baseMainMap = null;
        baseLandMask = null;
        baseBumpMap = null;

        int cubemapResizeDimensions = (int)Mathf.Pow(2, AppData.instance.CubemapDivisions) * AppData.instance.CubemapDimension;

        if (AppData.instance.SaveHeightMap)
        {
            Texture2D bumpMap = ShaderToTexture(SphereShaderDrawType.HeightMap, cubemapResizeDimensions);
            ExportCubemapsForSingleTexture(ref bumpMap, bumpBaseFolder, "");
            UnityEngine.Object.DestroyImmediate(bumpMap);
            bumpMap = null;
        }

        if (AppData.instance.TransparentOceans || (AppData.instance.SaveMainMap && !AppData.instance.SaveLandMask))
        {
            Texture2D mainMap = ShaderToTexture(SphereShaderDrawType.LandNormal, cubemapResizeDimensions);
            ExportCubemapsForSingleTexture(ref mainMap, baseFolder, "");
            UnityEngine.Object.DestroyImmediate(mainMap);
        }
        else
        {
            Texture2D mainMap = ShaderToTexture(SphereShaderDrawType.LandNormal, cubemapResizeDimensions);
            Texture2D landMask = null;

            if (AppData.instance.TransparentOceans)
            {
                landMask = ShaderToTexture(SphereShaderDrawType.InvertedLandMask, cubemapResizeDimensions);
                ApplyTransparency(mainMap, landMask);
            }

            ExportCubemapsForSingleTexture(ref mainMap, baseFolder, "_c");
            UnityEngine.Object.DestroyImmediate(mainMap);
            mainMap = null;

            if (!AppData.instance.TransparentOceans)
                landMask = ShaderToTexture(SphereShaderDrawType.InvertedLandMask, cubemapResizeDimensions);

            ExportCubemapsForSingleTexture(ref landMask, baseFolder, "_a");
            UnityEngine.Object.DestroyImmediate(landMask);
            landMask = null;
        }

        AppData.instance.LastSavedImageFolder = savePath;
        AppData.instance.Save();
    }

    void ExportCubemapsForSingleTexture(ref Texture2D tex, string folder, string filePreExtension)
    {
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

            string finalFolder = Path.Combine(folder, baseSubfolder);
            if (AppData.instance.SaveMainMap || AppData.instance.SaveLandMask)
                if (!Directory.Exists(finalFolder))
                    Directory.CreateDirectory(finalFolder);

            ExportCubeMapFile(ref tex, finalFolder, "", AppData.instance.CubemapDimension, faceCount, 0, 0, 0, AppData.instance.OffsetPixels, true);

            for (int i = 1; i < AppData.instance.CubemapDivisions; i++)
            {
                SaveMapSubDivisions(ref tex, faceCount, i, finalFolder, filePreExtension, true);
            }
        }
    }

    public ComputeShader applyTransparencyShader;
    RenderTexture transparencyRT;
    void ApplyTransparency(Texture2D baseTexture, Texture2D transparencyMask)
    {
        applyTransparencyShader.SetInt("mapWidth", baseTexture.width);
        applyTransparencyShader.SetInt("mapHeight", baseTexture.height);
        applyTransparencyShader.SetInt("alphaAsTransparency", 1);
        applyTransparencyShader.SetTexture(0, "baseTexture", baseTexture);
        applyTransparencyShader.SetTexture(0, "transparencyMask", transparencyMask);

        if (transparencyRT != null && (transparencyRT.width != baseTexture.width || transparencyRT.height != baseTexture.height))
        {
            Destroy(transparencyRT);
            transparencyRT = null;
        }

        if (transparencyRT == null)
        {
            transparencyRT = new RenderTexture(baseTexture.width, baseTexture.height, 32, RenderTextureFormat.ARGBHalf);
            transparencyRT.wrapMode = TextureWrapMode.Repeat;
            transparencyRT.name = "Transparency Render Texture";
            transparencyRT.enableRandomWrite = true;
            transparencyRT.Create();
        }

        if (!transparencyRT.IsCreated())
        {
            transparencyRT.enableRandomWrite = true;
            transparencyRT.Create();
        }

        applyTransparencyShader.SetTexture(0, "Result", transparencyRT);

        applyTransparencyShader.Dispatch(0, Mathf.CeilToInt(baseTexture.width / 32f), Mathf.CeilToInt(baseTexture.height / 32f), 1);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = transparencyRT;
        baseTexture.ReadPixels(new Rect(0, 0, baseTexture.width, baseTexture.height), 0, 0);
        RenderTexture.active = prevActive;
        baseTexture.Apply();
    }

    void ExportCubeMapFile(ref Texture2D map, string folder, string filePosfix, int dimension, int face, int division, int divisionX, int divisionY, float offsetPixels, bool saveAsPng = false)
    {
        bool useCpu = false;
        string fileName = division + "_" + divisionY + "_" + divisionX + filePosfix;
        if (saveAsPng)
            fileName += ".png";
        else
            fileName += ".jpg";

        if (!useCpu)
        {
            equirectangular2CubemapShader.SetInt("mapWidth", map.width);
            equirectangular2CubemapShader.SetInt("mapHeight", map.height);
            equirectangular2CubemapShader.SetInt("cubemapDimension", dimension);
            equirectangular2CubemapShader.SetInt("subdivision", division);
            equirectangular2CubemapShader.SetInt("subDivisionX", divisionX);
            equirectangular2CubemapShader.SetInt("subDivisionY", divisionY);
            equirectangular2CubemapShader.SetInt("faceId", face);
            equirectangular2CubemapShader.SetFloat("offsetPixels", offsetPixels);

            equirectangular2CubemapShader.SetTexture(0, "base", map);

            RenderTexture result = RenderTexture.GetTemporary(dimension, dimension);
            result.enableRandomWrite = true;
            result.wrapMode = TextureWrapMode.Repeat;
            result.Create();

            equirectangular2CubemapShader.SetTexture(0, "Result", result);

            equirectangular2CubemapShader.Dispatch(0, Mathf.CeilToInt(dimension / 32f), Mathf.CeilToInt(dimension / 32f), 1);

            Texture2D resultTexture = new Texture2D(dimension, dimension);

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = result;
            resultTexture.ReadPixels(new Rect(0, 0, dimension, dimension), 0, 0);
            RenderTexture.active = prevActive;
            resultTexture.Apply();
            if (saveAsPng)
                resultTexture.SaveAsPNG(Path.Combine(folder, fileName));
            else
                resultTexture.SaveAsJPG(Path.Combine(folder, fileName));

            RenderTexture.ReleaseTemporary(result);
            UnityEngine.Object.DestroyImmediate(resultTexture);
            resultTexture = null;
        }
        else
        {
            Equirectangular2Cubemap.instance.mapWidth = map.width;
            Equirectangular2Cubemap.instance.mapHeight = map.height;
            Equirectangular2Cubemap.instance.cubemapDimension = dimension;
            Equirectangular2Cubemap.instance.subdivision = division;
            Equirectangular2Cubemap.instance.subDivisionX = divisionX;
            Equirectangular2Cubemap.instance.subDivisionY = divisionY;
            Equirectangular2Cubemap.instance.faceId = face;
            Equirectangular2Cubemap.instance.baseTex = map;
            Equirectangular2Cubemap.instance.Result = new Texture2D(dimension, dimension);

            Equirectangular2Cubemap.instance.Run();

            Equirectangular2Cubemap.instance.Result.Apply();
            if (saveAsPng)
                Equirectangular2Cubemap.instance.Result.SaveAsPNG(Path.Combine(folder, fileName));
            else
                Equirectangular2Cubemap.instance.Result.SaveAsJPG(Path.Combine(folder, fileName));

            UnityEngine.Object.DestroyImmediate(Equirectangular2Cubemap.instance.Result);
        }
    }

    void SaveMapSubDivisions(ref Texture2D tex, int faceCount, int subdivision, string folder, string filePosfix, bool saveAsPng = false)
    {
        int sizeDivisor = (int)Mathf.Pow(2, subdivision);

        for (int countX = 0; countX < sizeDivisor; countX++)
        {
            for (int countY = 0; countY < sizeDivisor; countY++)
            {
                ExportCubeMapFile(ref tex, folder, filePosfix, AppData.instance.CubemapDimension, faceCount, subdivision, countX, countY, AppData.instance.OffsetPixels, saveAsPng);
            }
        }
    }

    void UpdateSurfaceMaterialProperties(bool resetEroded = true)
    {
        if (planetSurfaceMaterial == null)
            return;

        TextureManager.instance.Settings.UpdateSurfaceMaterialProperties(planetSurfaceMaterial, showTemperature);

        if (mapSettings.UseImages)
        {
            if (mapSettings.HeightMapPath != "" && File.Exists(mapSettings.HeightMapPath))
            {
                UpdateSurfaceMaterialHeightMap();
            }

            if (mapSettings.MainTexturePath == "" || !File.Exists(mapSettings.MainTexturePath))
                planetSurfaceMaterial.SetInt("_IsMainmapSet", 0);
            else
            {
                UpdateSurfaceMaterialMainMap();
                planetSurfaceMaterial.SetInt("_IsMainmapSet", 1);
            }

            if (mapSettings.LandMaskPath == "" || !File.Exists(mapSettings.LandMaskPath))
                planetSurfaceMaterial.SetInt("_IsLandmaskSet", 0);
            else
            {
                UpdateSurfaceMaterialLandMask();
                planetSurfaceMaterial.SetInt("_IsLandmaskSet", 1);
            }
        }
        else
        {
            planetSurfaceMaterial.SetInt("_IsMainmapSet", 0);
            planetSurfaceMaterial.SetInt("_IsLandmaskSet", 0);
        }
    }

    public void ResetEroded()
    {
        TextureManager.instance.HeightMap1 = null;
        TextureManager.instance.HeightMap2 = null;
        TextureManager.instance.HeightMap3 = null;
        TextureManager.instance.HeightMap4 = null;
        TextureManager.instance.HeightMap5 = null;
        TextureManager.instance.HeightMap6 = null;
        TextureManager.instance.InciseFlowMap = null;
        TextureManager.instance.FlowErosionMap = null;

        connectivityMap1 = null;
        connectivityMap2 = null;
        connectivityMap3 = null;
        connectivityMap4 = null;
        connectivityMap5 = null;
        connectivityMap6 = null;

        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

        GenerateHeightMap();
        HeightMap2Texture();
        GenerateEquirectangularNoiseTexture();
    }

    public void UpdateSurfaceMaterialHeightMap(bool isEroded = false)
    {
        if (planetSurfaceMaterial == null)
            return;

        if (!isEroded && mapSettings.UseImages && mapSettings.HeightMapPath != "" && File.Exists(mapSettings.HeightMapPath))
        {
            if (heightmapRT == null)
            {
                Texture2D heightmap = LoadAnyImageFile(mapSettings.HeightMapPath);
                heightmapRT = new RenderTexture(heightmap.width, heightmap.height, 16, RenderTextureFormat.ARGBHalf);
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.wrapMode = TextureWrapMode.Repeat;
                heightmapRT.Create();

                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = heightmapRT;
                Graphics.Blit(heightmap, heightmapRT);
                RenderTexture.active = prevActive;

                Destroy(heightmap);
            }

            if (heightmapRT != null && (heightmapRT.width != TextureManager.instance.Settings.textureWidth * 4 || heightmapRT.height != TextureManager.instance.Settings.textureWidth * 2))
            {
                RenderTexture newRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 16, RenderTextureFormat.ARGBHalf);
                newRT.name = "Heightmap Render Texture";
                newRT.enableRandomWrite = true;
                newRT.wrapMode = TextureWrapMode.Repeat;
                newRT.Create();

                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = heightmapRT;
                Graphics.Blit(heightmapRT, newRT);
                RenderTexture.active = prevActive;

                heightmapRT.Release();
                heightmapRT = newRT;
            }

            if (heightmapRT == null)
            {
                heightmapRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 16, RenderTextureFormat.ARGBHalf);
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.wrapMode = TextureWrapMode.Repeat;
                heightmapRT.Create();
            }

            Texture2HeightMap(ref heightmapRT);

            if (TextureManager.instance.Settings.textureWidth != heightmapRT.height / 2)
            {
                TextureManager.instance.Settings.textureWidth = heightmapRT.height / 2;
                UpdateUIInputField(setupPanelTransform, "Texture Width Text Box", TextureHeight);
                UpdateUITextMeshPro(setupPanelTransform, "Texture Width Text", TextureWidth + (TextureManager.instance.Settings.textureWidth * 4 < 10000 ? " " : "") + " x");
            }
        }

        if (heightmapRT != null)
        {
            heightmapRT.wrapMode = TextureWrapMode.Repeat;
            planetSurfaceMaterial.SetTexture("_HeightMap", heightmapRT);
        }

        if (isEroded && heightmapRT != null)
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

        TextureManager.instance.Landmap = LoadAnyImageFile(mapSettings.MainTexturePath);

        planetSurfaceMaterial.SetTexture("_MainMap", TextureManager.instance.Landmap);
        if (mapSettings.UseImages)
            planetSurfaceMaterial.SetInt("_IsMainmapSet", 1);
    }

    void UpdateSurfaceMaterialLandMask()
    {
        if (planetSurfaceMaterial == null)
            return;

        if (mapSettings.LandMaskPath == "" || !File.Exists(mapSettings.LandMaskPath))
            return;

        TextureManager.instance.Landmask = LoadAnyImageFile(mapSettings.LandMaskPath);

        planetSurfaceMaterial.SetTexture("_LandMask", TextureManager.instance.Landmask);
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
    public ComputeShader maxMinComputeShader;
    public ComputeShader erosionShader;
    public ComputeShader erosionUpdateShader;
    public ComputeShader heightmap2TextureShader;
    public ComputeShader texture2HeightmapShader;
    public ComputeShader equirectangularNoiseComputeShader;

    void GenerateHeightMap(bool resetHeightLimits = false)
    {
        if (TextureManager.instance.HeightMap1 == null)
        {
            TextureManager.instance.InstantiateHeightMap();

            ComputeBuffer mapBuffer1 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap1);
            heightMapComputeShader.SetBuffer(0, "heightMap1", mapBuffer1);

            ComputeBuffer mapBuffer2 = new ComputeBuffer(TextureManager.instance.HeightMap2.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap2);
            heightMapComputeShader.SetBuffer(0, "heightMap2", mapBuffer2);

            ComputeBuffer mapBuffer3 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap3);
            heightMapComputeShader.SetBuffer(0, "heightMap3", mapBuffer3);

            ComputeBuffer mapBuffer4 = new ComputeBuffer(TextureManager.instance.HeightMap4.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap4);
            heightMapComputeShader.SetBuffer(0, "heightMap4", mapBuffer4);

            ComputeBuffer mapBuffer5 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap5);
            heightMapComputeShader.SetBuffer(0, "heightMap5", mapBuffer5);

            ComputeBuffer mapBuffer6 = new ComputeBuffer(TextureManager.instance.HeightMap6.Length, sizeof(float));
            mapBuffer1.SetData(TextureManager.instance.HeightMap6);
            heightMapComputeShader.SetBuffer(0, "heightMap6", mapBuffer6);

            heightMapComputeShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
            heightMapComputeShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);

            heightMapComputeShader.SetInt("_MapWidth", TextureManager.instance.Settings.textureWidth);

            heightMapComputeShader.SetFloat("_Seed", TextureManager.instance.Settings.surfaceNoiseSettings.seed);
            heightMapComputeShader.SetFloat("_xOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.x);
            heightMapComputeShader.SetFloat("_yOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.y);
            heightMapComputeShader.SetFloat("_zOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.z);
            heightMapComputeShader.SetInt("_Octaves", TextureManager.instance.Settings.surfaceNoiseSettings.octaves);
            heightMapComputeShader.SetFloat("_Lacunarity", TextureManager.instance.Settings.surfaceNoiseSettings.lacunarity);
            heightMapComputeShader.SetFloat("_Persistence", TextureManager.instance.Settings.surfaceNoiseSettings.persistence);
            heightMapComputeShader.SetFloat("_Multiplier", TextureManager.instance.Settings.surfaceNoiseSettings.multiplier);
            heightMapComputeShader.SetInt("_RidgedNoise", TextureManager.instance.Settings.surfaceNoiseSettings.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("_HeightExponent", TextureManager.instance.Settings.surfaceNoiseSettings.heightExponent);
            heightMapComputeShader.SetFloat("_LayerStrength", TextureManager.instance.Settings.surfaceNoiseSettings.layerStrength);
            heightMapComputeShader.SetFloat("_DomainWarping", TextureManager.instance.Settings.surfaceNoiseSettings.domainWarping);

            heightMapComputeShader.SetFloat("_Seed2", TextureManager.instance.Settings.surfaceNoiseSettings2.seed);
            heightMapComputeShader.SetFloat("_xOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.x);
            heightMapComputeShader.SetFloat("_yOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.y);
            heightMapComputeShader.SetFloat("_zOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.z);
            heightMapComputeShader.SetFloat("_Multiplier2", TextureManager.instance.Settings.surfaceNoiseSettings2.multiplier);
            heightMapComputeShader.SetInt("_Octaves2", TextureManager.instance.Settings.surfaceNoiseSettings2.octaves);
            heightMapComputeShader.SetFloat("_Lacunarity2", TextureManager.instance.Settings.surfaceNoiseSettings2.lacunarity);
            heightMapComputeShader.SetFloat("_Persistence2", TextureManager.instance.Settings.surfaceNoiseSettings2.persistence);
            heightMapComputeShader.SetInt("_RidgedNoise2", TextureManager.instance.Settings.surfaceNoiseSettings2.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("_HeightExponent2", TextureManager.instance.Settings.surfaceNoiseSettings2.heightExponent);
            heightMapComputeShader.SetFloat("_LayerStrength2", TextureManager.instance.Settings.surfaceNoiseSettings2.layerStrength);
            heightMapComputeShader.SetFloat("_DomainWarping2", TextureManager.instance.Settings.surfaceNoiseSettings2.domainWarping);

            heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 1);

            mapBuffer1.GetData(TextureManager.instance.HeightMap1);
            mapBuffer2.GetData(TextureManager.instance.HeightMap2);
            mapBuffer3.GetData(TextureManager.instance.HeightMap3);
            mapBuffer4.GetData(TextureManager.instance.HeightMap4);
            mapBuffer5.GetData(TextureManager.instance.HeightMap5);
            mapBuffer6.GetData(TextureManager.instance.HeightMap6);

            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap1.png"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap2.png"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap3.png"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap4.png"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap5.png"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap6.png"));

            mapBuffer1.Release();
            mapBuffer2.Release();
            mapBuffer3.Release();
            mapBuffer4.Release();
            mapBuffer5.Release();
            mapBuffer6.Release();
        }
    }

    RenderTexture noiseRT;
    public void GenerateEquirectangularNoiseTexture()
    {
        if (equirectangularNoiseComputeShader == null)
            return;

        equirectangularNoiseComputeShader.SetInt("_MapWidth", TextureManager.instance.Settings.textureWidth * 2);

        equirectangularNoiseComputeShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
        equirectangularNoiseComputeShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);

        equirectangularNoiseComputeShader.SetFloat("_Seed1", TextureManager.instance.Settings.TemperatureNoiseSeed);
        equirectangularNoiseComputeShader.SetFloat("_Seed2", TextureManager.instance.Settings.HumidityNoiseSeed);
        equirectangularNoiseComputeShader.SetFloat("_Seed3", TextureManager.instance.Settings.surfaceNoiseSettings.seed);
        equirectangularNoiseComputeShader.SetFloat("_Seed4", TextureManager.instance.Settings.surfaceNoiseSettings2.seed);

        equirectangularNoiseComputeShader.SetInt("_Octaves", TextureManager.instance.Settings.temperatureNoiseSettings.octaves);
        equirectangularNoiseComputeShader.SetFloat("_Lacunarity", TextureManager.instance.Settings.temperatureNoiseSettings.lacunarity);
        equirectangularNoiseComputeShader.SetFloat("_Persistence", TextureManager.instance.Settings.temperatureNoiseSettings.persistence);
        equirectangularNoiseComputeShader.SetFloat("_Multiplier", TextureManager.instance.Settings.temperatureNoiseSettings.multiplier);
        equirectangularNoiseComputeShader.SetFloat("_xOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.x);
        equirectangularNoiseComputeShader.SetFloat("_yOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.y);
        equirectangularNoiseComputeShader.SetFloat("_zOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.z);
        equirectangularNoiseComputeShader.SetInt("_RidgedNoise", TextureManager.instance.Settings.temperatureNoiseSettings.ridged ? 1 : 0);
        equirectangularNoiseComputeShader.SetFloat("_DomainWarping", TextureManager.instance.Settings.temperatureNoiseSettings.domainWarping);
        equirectangularNoiseComputeShader.SetFloat("_HeightExponent", TextureManager.instance.Settings.temperatureNoiseSettings.heightExponent);

        if (noiseRT != null && (noiseRT.width != TextureManager.instance.Settings.textureWidth * 4 || noiseRT.height != TextureManager.instance.Settings.textureWidth * 2))
        {
            Destroy(noiseRT);
            noiseRT = null;
        }

        if (noiseRT == null)
        {
            noiseRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 16, RenderTextureFormat.ARGBHalf);
            noiseRT.wrapMode = TextureWrapMode.Repeat;
            noiseRT.name = "Heightmap Render Texture";
            noiseRT.enableRandomWrite = true;
            noiseRT.Create();
        }

        equirectangularNoiseComputeShader.SetTexture(0, "Result", noiseRT);

        equirectangularNoiseComputeShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 32f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 32f), 1);

        planetSurfaceMaterial.SetTexture("_NoiseMap", noiseRT);
        planetSurfaceMaterial.SetInt("_IsNoiseMapSet", 1);

        //noiseRT.SaveToFile(Path.Combine(Application.persistentDataPath, "NoiseTexture.png"));
    }

    RenderTexture heightmapRT;
    public void HeightMap2Texture()
    {
        if (TextureManager.instance.HeightMap1 == null || TextureManager.instance.HeightMap1.Length == 0)
            return;

        if (heightmap2TextureShader != null)
        {
            ComputeBuffer heightMapBuffer12 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length * 2, sizeof(float));
            heightMapBuffer12.SetData(TextureManager.instance.HeightMap1, 0, 0, TextureManager.instance.HeightMap1.Length);
            heightMapBuffer12.SetData(TextureManager.instance.HeightMap2, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

            ComputeBuffer heightMapBuffer34 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length * 2, sizeof(float));
            heightMapBuffer34.SetData(TextureManager.instance.HeightMap3, 0, 0, TextureManager.instance.HeightMap1.Length);
            heightMapBuffer34.SetData(TextureManager.instance.HeightMap4, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

            ComputeBuffer heightMapBuffer56 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length * 2, sizeof(float));
            heightMapBuffer56.SetData(TextureManager.instance.HeightMap5, 0, 0, TextureManager.instance.HeightMap1.Length);
            heightMapBuffer56.SetData(TextureManager.instance.HeightMap6, 0, TextureManager.instance.HeightMap1.Length, TextureManager.instance.HeightMap1.Length);

            ComputeBuffer flowErosionMapBuffer12 = null;
            ComputeBuffer flowErosionMapBuffer34 = null;
            ComputeBuffer flowErosionMapBuffer56 = null;
            if (TextureManager.instance.FlowErosionMapLength > 0)
            {
                flowErosionMapBuffer12 = new ComputeBuffer(TextureManager.instance.FlowErosionMapLength * 2, sizeof(float));
                flowErosionMapBuffer12.SetData(TextureManager.instance.FlowErosionMap1, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
                flowErosionMapBuffer12.SetData(TextureManager.instance.FlowErosionMap2, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);

                flowErosionMapBuffer34 = new ComputeBuffer(TextureManager.instance.FlowErosionMapLength * 2, sizeof(float));
                flowErosionMapBuffer34.SetData(TextureManager.instance.FlowErosionMap3, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
                flowErosionMapBuffer34.SetData(TextureManager.instance.FlowErosionMap4, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);

                flowErosionMapBuffer56 = new ComputeBuffer(TextureManager.instance.FlowErosionMapLength * 2, sizeof(float));
                flowErosionMapBuffer56.SetData(TextureManager.instance.FlowErosionMap5, 0, 0, TextureManager.instance.FlowErosionMap1.Length);
                flowErosionMapBuffer56.SetData(TextureManager.instance.FlowErosionMap6, 0, TextureManager.instance.FlowErosionMap1.Length, TextureManager.instance.FlowErosionMap1.Length);
            }
            else
            {
                flowErosionMapBuffer12 = new ComputeBuffer(1, sizeof(float));
                flowErosionMapBuffer12.SetData(new float[] { 0 }, 0, 0, 1);

                flowErosionMapBuffer34 = new ComputeBuffer(1, sizeof(float));
                flowErosionMapBuffer34.SetData(new float[] { 0 }, 0, 0, 1);

                flowErosionMapBuffer56 = new ComputeBuffer(1, sizeof(float));
                flowErosionMapBuffer56.SetData(new float[] { 0 }, 0, 0, 1);
            }

            heightmap2TextureShader.SetBuffer(0, "heightMap12", heightMapBuffer12);
            heightmap2TextureShader.SetBuffer(0, "heightMap34", heightMapBuffer34);
            heightmap2TextureShader.SetBuffer(0, "heightMap56", heightMapBuffer56);
            heightmap2TextureShader.SetBuffer(0, "flowErosionMap12", flowErosionMapBuffer12);
            heightmap2TextureShader.SetBuffer(0, "flowErosionMap34", flowErosionMapBuffer34);
            heightmap2TextureShader.SetBuffer(0, "flowErosionMap56", flowErosionMapBuffer56);

            if (heightmapRT != null && (heightmapRT.width != TextureManager.instance.Settings.textureWidth * 4 || heightmapRT.height != TextureManager.instance.Settings.textureWidth * 2))
            {
                Destroy(heightmapRT);
                heightmapRT = null;
            }

            if (heightmapRT == null)
            {
                heightmapRT = new RenderTexture(TextureManager.instance.Settings.textureWidth * 4, TextureManager.instance.Settings.textureWidth * 2, 16, RenderTextureFormat.ARGBHalf);
                heightmapRT.wrapMode = TextureWrapMode.Repeat;
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.Create();
            }

            heightmap2TextureShader.SetTexture(0, "result", heightmapRT);
            heightmap2TextureShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
            heightmap2TextureShader.SetInt("flowErosionSet", TextureManager.instance.FlowErosionMapLength == 0 ? 0 : 1);
            heightmap2TextureShader.SetFloat("minHeight", TextureManager.instance.Settings.minHeight);
            heightmap2TextureShader.SetFloat("maxHeight", TextureManager.instance.Settings.maxHeight);
            //heightmap2TextureShader.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
            //heightmap2TextureShader.SetFloat("normalScale", 50);
            heightmap2TextureShader.SetFloat("erosionNoiseMerge", TextureManager.instance.Settings.erosionNoiseMerge);

            heightmap2TextureShader.Dispatch(0, Mathf.CeilToInt(heightmapRT.width / 32f), Mathf.CeilToInt(heightmapRT.height / 32f), 1);

            //if (heightmap == null || heightmap.width != TextureManager.instance.Settings.textureWidth || heightmap.height != TextureManager.instance.Settings.textureWidth)
            //Texture2D heightmap = new Texture2D(2 * TextureManager.instance.Settings.textureWidth, TextureManager.instance.Settings.textureWidth, TextureFormat.RGBA64, false, true);
            //RenderTexture prevActive = RenderTexture.active;
            //RenderTexture.active = heightmapRT;
            //heightmap.ReadPixels(new Rect(0, 0, heightmapRT.width, heightmapRT.height), 0, 0);
            //heightmap.Apply();
            //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "HeightmapTexture.png"));
            //Destroy(heightmap);
            //RenderTexture.active = prevActive;

            //ImageTools.SaveTextureCubemapFloatArray(TextureManager.instance.OriginalHeightMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "Test-Heightmap-Original.png"));
            //ImageTools.SaveTextureCubemapFloatArray(TextureManager.instance.ErodedHeightMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "Test-Heightmap-Eroded.png"));
            //ImageTools.SaveTextureCubemapFloatArray(TextureManager.instance.InciseFlowMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "Test-Heightmap-Incise.png"));
            //ImageTools.SaveTextureCubemapFloatArray(TextureManager.instance.MergedHeightMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "Test-Heightmap-Merged.png"));
            //heightmapRT.SaveToFile(Path.Combine(Application.persistentDataPath, "Heightmap2Texture.png"));

            heightMapBuffer12.Release();
            heightMapBuffer34.Release();
            heightMapBuffer56.Release();
            flowErosionMapBuffer12.Release();
            flowErosionMapBuffer34.Release();
            flowErosionMapBuffer56.Release();

            planetSurfaceMaterial.SetTexture("_HeightMap", heightmapRT);
            //planetSurfaceMaterial.SetFloat("_HeightmapWidth", heightmapRT.width);
            //planetSurfaceMaterial.SetFloat("_HeightmapHeight", heightmapRT.height);
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);

            //Destroy(heightmapRT);
            //heightmapRT = null;
        }
    }

    void Texture2HeightMap(ref RenderTexture heightmapTexture)
    {
        if (texture2HeightmapShader == null)
            return;

        TextureManager.instance.InstantiateHeightMap();

        ComputeBuffer mapBuffer1 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length, sizeof(float));
        mapBuffer1.SetData(TextureManager.instance.HeightMap1);
        texture2HeightmapShader.SetBuffer(0, "heightMap1", mapBuffer1);

        ComputeBuffer mapBuffer2 = new ComputeBuffer(TextureManager.instance.HeightMap2.Length, sizeof(float));
        mapBuffer2.SetData(TextureManager.instance.HeightMap2);
        texture2HeightmapShader.SetBuffer(0, "heightMap2", mapBuffer2);

        ComputeBuffer mapBuffer3 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length, sizeof(float));
        mapBuffer3.SetData(TextureManager.instance.HeightMap3);
        texture2HeightmapShader.SetBuffer(0, "heightMap3", mapBuffer3);

        ComputeBuffer mapBuffer4 = new ComputeBuffer(TextureManager.instance.HeightMap4.Length, sizeof(float));
        mapBuffer4.SetData(TextureManager.instance.HeightMap4);
        texture2HeightmapShader.SetBuffer(0, "heightMap4", mapBuffer4);

        ComputeBuffer mapBuffer5 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length, sizeof(float));
        mapBuffer5.SetData(TextureManager.instance.HeightMap5);
        texture2HeightmapShader.SetBuffer(0, "heightMap5", mapBuffer5);

        ComputeBuffer mapBuffer6 = new ComputeBuffer(TextureManager.instance.HeightMap6.Length, sizeof(float));
        mapBuffer6.SetData(TextureManager.instance.HeightMap6);
        texture2HeightmapShader.SetBuffer(0, "heightMap6", mapBuffer6);

        texture2HeightmapShader.SetTexture(0, "renderTexture", heightmapTexture);
        texture2HeightmapShader.SetInt("textureWidth", TextureManager.instance.Settings.textureWidth);

        texture2HeightmapShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 6);

        mapBuffer1.GetData(TextureManager.instance.HeightMap1);
        mapBuffer2.GetData(TextureManager.instance.HeightMap2);
        mapBuffer3.GetData(TextureManager.instance.HeightMap3);
        mapBuffer4.GetData(TextureManager.instance.HeightMap4);
        mapBuffer5.GetData(TextureManager.instance.HeightMap5);
        mapBuffer6.GetData(TextureManager.instance.HeightMap6);

        mapBuffer1.Release();
        mapBuffer2.Release();
        mapBuffer3.Release();
        mapBuffer4.Release();
        mapBuffer5.Release();
        mapBuffer6.Release();

        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap1.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap2.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap3.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap4.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap5.png"));
        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap6.png"));
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
        //Textures.instance.SaveTextureFloatArray(heightMap, TextureManager.instance.Settings.textureWidth, TextureManager.instance.Settings.textureWidth, filename);
        HydraulicErosion.instance.mapWidth = TextureManager.instance.Settings.textureWidth;
        HydraulicErosion.instance.mapHeight = TextureManager.instance.Settings.textureWidth;
        HydraulicErosion.instance.erosion = erosionShader;
        HydraulicErosion.instance.erosionUpdate = erosionUpdateShader;
        HydraulicErosion.instance.erosionSettings = erosionSettings;
        HydraulicErosion.instance.Erode();
        HeightMap2Texture();
        //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "heightmap-2.png"));
        UpdateSurfaceMaterialHeightMap(true);
        HideErodingTerrainPanel();
        yield return null;
    }

    public void ResetImages()
    {
        planetSurfaceMaterial.SetInt("_IsMainMapSet", 0);
        planetSurfaceMaterial.SetInt("_IsLandMaskSet", 0);
        //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
        if (heightmapRT != null)
        {
            heightmapRT.Release();
            UnityEngine.Object.Destroy(heightmapRT);
        }
        heightmapRT = null;
        TextureManager.instance.Landmap = null;
        TextureManager.instance.Landmask = null;
    }

    public void UndoErosion()
    {
        ResetEroded();
        //TextureManager.instance.FlowTexture = null;
        //TextureManager.instance.FlowTextureRandom = null;
        //TextureManager.instance.InciseFlowMap = null;
        //TextureManager.instance.FlowMap = null;
        //isInciseFlowApplied = false;

        //GenerateHeightMap();
        //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);

        //if (heightmapRT != null)
        //{
        //    Destroy(heightmapRT);
        //    heightmapRT = null;
        //}

        //if (mapSettings.UseImages)
        //{
            //UpdateSurfaceMaterialHeightMap();
            if (heightmapRT != null)
                planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
        //}
    }
    
    public ComputeShader landMaskShader;
    int landMaskCount = 0;
    void CalculateLandMask()
    {
        ComputeBuffer heightMapBuffer1 = new ComputeBuffer(TextureManager.instance.HeightMap1.Length, sizeof(float));
        heightMapBuffer1.SetData(TextureManager.instance.HeightMap1);

        ComputeBuffer heightMapBuffer2 = new ComputeBuffer(TextureManager.instance.HeightMap2.Length, sizeof(float));
        heightMapBuffer2.SetData(TextureManager.instance.HeightMap2);

        ComputeBuffer heightMapBuffer3 = new ComputeBuffer(TextureManager.instance.HeightMap3.Length, sizeof(float));
        heightMapBuffer3.SetData(TextureManager.instance.HeightMap3);

        ComputeBuffer heightMapBuffer4 = new ComputeBuffer(TextureManager.instance.HeightMap4.Length, sizeof(float));
        heightMapBuffer4.SetData(TextureManager.instance.HeightMap4);

        ComputeBuffer heightMapBuffer5 = new ComputeBuffer(TextureManager.instance.HeightMap5.Length, sizeof(float));
        heightMapBuffer5.SetData(TextureManager.instance.HeightMap5);

        ComputeBuffer heightMapBuffer6 = new ComputeBuffer(TextureManager.instance.HeightMap6.Length, sizeof(float));
        heightMapBuffer6.SetData(TextureManager.instance.HeightMap6);

        int[] landMask = new int[TextureManager.instance.HeightMap1.Length * 6];

        ComputeBuffer landMaskBuffer = new ComputeBuffer(landMask.Length, sizeof(int));
        landMaskBuffer.SetData(landMask);

        //float unNormalizedWaterLevel = (TextureManager.instance.Settings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

        landMaskShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        landMaskShader.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        landMaskShader.SetBuffer(0, "heightMap1", heightMapBuffer1);
        landMaskShader.SetBuffer(0, "heightMap2", heightMapBuffer2);
        landMaskShader.SetBuffer(0, "heightMap3", heightMapBuffer3);
        landMaskShader.SetBuffer(0, "heightMap4", heightMapBuffer4);
        landMaskShader.SetBuffer(0, "heightMap5", heightMapBuffer5);
        landMaskShader.SetBuffer(0, "heightMap6", heightMapBuffer6);
        landMaskShader.SetBuffer(0, "landMask", landMaskBuffer);

        landMaskShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth / 8f), 6);

        landMaskBuffer.GetData(landMask);
        landMaskCount = landMask.Sum();

        heightMapBuffer1.Release();
        heightMapBuffer2.Release();
        heightMapBuffer3.Release();
        heightMapBuffer4.Release();
        heightMapBuffer5.Release();
        heightMapBuffer6.Release();

        landMaskBuffer.Release();
        landMask = null;
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

    public void AlterTerrain(Vector2 coordinates, float radius, float elevationDelta)
    {
        GenerateHeightMap();

        float radiusInPixels = radius;
        if (!showGlobe)
        {
            radiusInPixels *= TextureManager.instance.Settings.textureWidth * 4 / mapWidth;
        }
        else
        {
            float radiusRatio = radius / geoSphere.Radius;
            if (radiusRatio > 1)
                radiusRatio = 1;
            float angle = Mathf.Asin(radiusRatio);
            radiusInPixels = (TextureManager.instance.Settings.textureWidth * 4 / (2 * Mathf.PI)) * angle;
        }

        coordinates.x = -coordinates.x;
        Vector3 cartesian = coordinates.PolarRatioToCartesian(1);
        Vector3 cubemap = cartesian.CartesianToCubemap();
        cubemap.x *= TextureManager.instance.Settings.textureWidth;
        cubemap.y *= TextureManager.instance.Settings.textureWidth;

        for (float x = -2 * radiusInPixels; x <= 2 * radiusInPixels; x++)
        {
            for (float y = -radiusInPixels; y <= radiusInPixels; y++)
            {
                Vector3 newCubemap = Cubemap.getNewCoordinates(cubemap, x, y, TextureManager.instance.Settings.textureWidth);
                Int3 cubemapInt = new Int3((int)(newCubemap.x), (int)(newCubemap.y), newCubemap.z);

                float pixelDistance = Mathf.Sqrt(x * x + y * y);
                if (pixelDistance > radiusInPixels)
                    continue;

                float distanceRatio = 1 - (pixelDistance / radiusInPixels);
                float heightToAlter = elevationDelta * distanceRatio;

                float height = TextureManager.instance.HeightMapValueAtCoordinates(cubemapInt.x, cubemapInt.y, cubemapInt.z);
                height += heightToAlter;
                //if (height < 0) height = 0;
                //if (height > 1) height = 1;
                TextureManager.instance.SetHeightAtCoordinates(cubemapInt.x, cubemapInt.y, cubemapInt.z, height);
            }
        }

        HeightMap2Texture();
        //planetSurfaceMaterial.SetTexture("_HeightMap", heightmap);
        //planetSurfaceMaterial.SetFloat("_HeightmapWidth", heightmapRT.width);
        //planetSurfaceMaterial.SetFloat("_HeightmapHeight", heightmapRT.height);
        planetSurfaceMaterial.SetInt("_IsEroded", 1);
    }
}
