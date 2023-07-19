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
        if (AppData.instance.ExportAsCubemap && !doZoomBrush)
            title = "Export Image as Cubemap";

        string savedFile = StandaloneFileBrowser.SaveFilePanel(title, lastSavedImageFolder, MapData.instance.WorldName, new[] { new ExtensionFilter("Png Image", "png") });
        if (savedFile != null && savedFile != "")
        {
            cameraController.CloseContextMenu();

            if (AppData.instance.ExportAsCubemap && !doZoomBrush)
                ExportCubemaps(savedFile);
            else
            {
                string fileNamePath = System.IO.Path.GetDirectoryName(savedFile);
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(savedFile);
                string fileNameExtension = System.IO.Path.GetExtension(savedFile);
                AppData.instance.LastSavedImageFolder = fileNamePath;

                GenerateHeightMap();

                RenderTexture heightmapZoomRT = null;
                if (doZoomBrush)
                {
                    heightmapZoomRT = RenderTexture.GetTemporary(AppData.instance.ZoomWidth, AppData.instance.ZoomHeight, 16, RenderTextureFormat.ARGBHalf);
                    heightmapZoomRT.wrapMode = TextureWrapMode.Repeat;
                    heightmapZoomRT.name = "Heightmap Render Texture";
                    heightmapZoomRT.enableRandomWrite = true;
                    heightmapZoomRT.Create();

                    HeightMap2ZoomTexture(heightmapZoomRT);

                    heightmapZoomRT.SaveToFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Heightmap" + fileNameExtension));

                    planetSurfaceZoomMaterial.SetTexture("_ZoomHeightMap", heightmapZoomRT);
                    planetSurfaceZoomMaterial.SetInt("_IsZoomHeightMapSet", 1);
                }

                if (AppData.instance.SaveHeightMap)
                {
                    if (!doZoomBrush)
                    {
                        HeightMap2Texture();
                        heightmapRT.SaveToFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Heightmap" + fileNameExtension));
                    }
                }

                if (AppData.instance.SaveMainMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainMap" + fileNameExtension), SphereShaderDrawType.LandWithNoNormals);
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

                if (AppData.instance.SaveRivers && TextureManager.instance.FlowTexture != null && (inciseFlowSettings.plotRiversRandomly || inciseFlowSettings.plotRivers))
                {
                    TextureManager.instance.FlowTexture.SaveAsPNG(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Rivers" + fileNameExtension));
                }

                AppData.instance.Save();

                if (doZoomBrush)
                {
                    planetSurfaceZoomMaterial.SetInt("_IsZoomHeightMapSet", 0);
                    RenderTexture.ReleaseTemporary(heightmapZoomRT);
                }
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

        Material material = planetSurfaceMaterial;
        if (doZoomBrush)
            material = planetSurfaceZoomMaterial;

        float prevDrawMode = material.GetFloat("_DrawType");
        material.SetFloat("_DrawType", (int)drawMode);

        float exportWidth = TextureManager.instance.Settings.textureWidth * 4;
        float exportHeight = TextureManager.instance.Settings.textureWidth * 2;

        if (doZoomBrush)
        {
            exportWidth = AppData.instance.ZoomWidth;
            exportHeight = AppData.instance.ZoomHeight;
        }

        if (exportRT != null && (exportRT.width != exportWidth || exportRT.height != exportHeight))
        {
            Destroy(exportRT);
            exportRT = null;
        }

        if (exportRT == null)
        {
            exportRT = new RenderTexture((int)exportWidth, (int)exportHeight, 32, RenderTextureFormat.ARGBHalf);
            exportRT.wrapMode = TextureWrapMode.Repeat;
            exportRT.name = "Export Render Texture";
            exportRT.enableRandomWrite = true;
            exportRT.Create();
        }

        Texture2D source;
        if (drawMode != SphereShaderDrawType.HeightMap)
            source = new Texture2D((int)exportWidth, (int)exportHeight, TextureFormat.RGBA32, false);
        else
            source = new Texture2D((int)exportWidth, (int)exportHeight, TextureFormat.RGB48, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = exportRT;
        Graphics.Blit(source, exportRT, material, 2);
        exportRT.Save(fileName);
        RenderTexture.active = prevActive;
        material.SetFloat("_DrawType", prevDrawMode);

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

        UpdateZoomCamMaterialProperties();
    }

    public void UpdateZoomCamMaterialProperties()
    {
        ZoomBrush zoomBrushSctipt = zoomBrush.GetComponent<ZoomBrush>();
        zoomBrushSctipt.UpdateZoomMaterial(heightmapRT, noiseRT, mapSettings, isEroded);
    }

    public void ResetEroded()
    {
        TextureManager.instance.HeightMap = null;
        TextureManager.instance.InciseFlowMap = null;
        TextureManager.instance.FlowErosionMap = null;

        connectivityMap = null;

        isEroded = false;
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

        GenerateHeightMap();
        HeightMap2Texture();
        //GenerateEquirectangularNoiseTexture();
    }

    public void UpdateSurfaceMaterialHeightMap()
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
        {
            isEroded = true;
            planetSurfaceMaterial.SetInt("_IsEroded", 1);
        }
        else
        {
            isEroded = false;
            planetSurfaceMaterial.SetInt("_IsEroded", 0);
        }

        UpdateZoomCamMaterialProperties();
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
    public ComputeShader erosionShader;
    public ComputeShader erosionUpdateShader;
    public ComputeShader heightmap2TextureShader;
    public ComputeShader heightmap2ZoomTextureShader;
    public ComputeShader texture2HeightmapShader;
    public ComputeShader equirectangularNoiseComputeShader;

    ComputeBuffer heightMapBuffer;

    public void InstantiateComputeBuffers()
    {
        if (heightMapBuffer == null || heightMapBuffer.count != TextureManager.instance.HeightMap.Length)
        {
            if (heightMapBuffer != null) heightMapBuffer.Release();
            heightMapBuffer = new ComputeBuffer(TextureManager.instance.Settings.textureWidth * 4 * TextureManager.instance.Settings.textureWidth * 2, sizeof(float));
        }
    }

    public void ReleaseComputeBuffers()
    {
        if (heightMapBuffer != null) heightMapBuffer.Release();
    }

    void GenerateHeightMap(bool resetHeightLimits = false)
    {
        if (TextureManager.instance.HeightMap == null)
        {
            TextureManager.instance.InstantiateHeightMap();

            float[] minMax = new float[2] { 999999, -999999 };
            ComputeBuffer mapBufferMinMax = new ComputeBuffer(2, sizeof(float));
            mapBufferMinMax.SetData(minMax);
            heightMapComputeShader.SetBuffer(0, "minMax", mapBufferMinMax);

            InstantiateComputeBuffers();
            heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

            heightMapComputeShader.SetBuffer(0, "heightMap", heightMapBuffer);

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

            heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 8f), 1);

            mapBufferMinMax.GetData(minMax);

            heightMapBuffer.GetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

            if (saveTemporaryTextures)
            {
                if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
                float[] heightMap = new float[TextureManager.instance.settings.textureWidth * TextureManager.instance.settings.textureWidth];
                heightMapBuffer.GetData(heightMap, 0, 0, heightMap.Length);
                ImageTools.SaveTextureCubemapFaceFloatArray(heightMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "Textures", "heightMap.png"));
            }

            mapBufferMinMax.Release();
        }
    }

    RenderTexture noiseRT;
    public void GenerateEquirectangularNoiseTexture()
    {
        if (equirectangularNoiseComputeShader == null)
            return;

        equirectangularNoiseComputeShader.SetInt("_MapWidth", TextureManager.instance.Settings.textureWidth);

        equirectangularNoiseComputeShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
        equirectangularNoiseComputeShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);

        equirectangularNoiseComputeShader.SetFloat("_Seed1", TextureManager.instance.Settings.temperatureNoiseSettings.seed);
        equirectangularNoiseComputeShader.SetFloat("_Seed2", TextureManager.instance.Settings.humidityNoiseSettings.seed);
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

        equirectangularNoiseComputeShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 16f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 16f), 1);

        planetSurfaceMaterial.SetTexture("_NoiseMap", noiseRT);
        planetSurfaceMaterial.SetInt("_IsNoiseMapSet", 1);

        //noiseRT.SaveToFile(Path.Combine(Application.persistentDataPath, "NoiseTexture.png"));
    }

    void HeightMap2ZoomTexture(RenderTexture rt)
    {
        TextureManager.instance.InstantiateFlowErosionMap();
        InstantiateComputeBuffers();
        heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

        ComputeBuffer flowErosionMapBuffer;
        if (TextureManager.instance.FlowErosionMap.Length > 0)
        {
            flowErosionMapBuffer = new ComputeBuffer(TextureManager.instance.FlowErosionMap.Length, sizeof(float));
            flowErosionMapBuffer.SetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);
        }
        else
        {
            flowErosionMapBuffer = new ComputeBuffer(1, sizeof(float));
            flowErosionMapBuffer.SetData(new float[] { 0 }, 0, 0, 1);
        }

        ZoomBrush zoomBrushScript = zoomBrush.GetComponent<ZoomBrush>();

        heightmap2ZoomTextureShader.SetBuffer(0, "heightMap", heightMapBuffer);
        heightmap2ZoomTextureShader.SetBuffer(0, "flowErosionMap", flowErosionMapBuffer);
        heightmap2ZoomTextureShader.SetTexture(0, "result", rt);

        heightmap2ZoomTextureShader.SetInt("mapWidth", rt.width);
        heightmap2ZoomTextureShader.SetInt("mapHeight", rt.height);
        heightmap2ZoomTextureShader.SetInt("worldTextureWidth", TextureManager.instance.Settings.textureWidth);
        heightmap2ZoomTextureShader.SetFloat("boundaryU", zoomBrushScript.BoundaryUV.x);
        heightmap2ZoomTextureShader.SetFloat("boundaryV", zoomBrushScript.BoundaryUV.y);
        heightmap2ZoomTextureShader.SetFloat("centerU", zoomBrushScript.CenterUV.x);
        heightmap2ZoomTextureShader.SetFloat("centerV", zoomBrushScript.CenterUV.y);

        heightmap2ZoomTextureShader.SetFloat("erosionNoiseMerge", TextureManager.instance.Settings.erosionNoiseMerge);

        heightmap2ZoomTextureShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
        heightmap2ZoomTextureShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);

        heightmap2ZoomTextureShader.SetFloat("_Seed", TextureManager.instance.Settings.surfaceNoiseSettings.seed);
        heightmap2ZoomTextureShader.SetFloat("_xOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.x);
        heightmap2ZoomTextureShader.SetFloat("_yOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.y);
        heightmap2ZoomTextureShader.SetFloat("_zOffset", TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset.z);
        heightmap2ZoomTextureShader.SetInt("_Octaves", TextureManager.instance.Settings.surfaceNoiseSettings.octaves);
        heightmap2ZoomTextureShader.SetFloat("_Lacunarity", TextureManager.instance.Settings.surfaceNoiseSettings.lacunarity);
        heightmap2ZoomTextureShader.SetFloat("_Persistence", TextureManager.instance.Settings.surfaceNoiseSettings.persistence);
        heightmap2ZoomTextureShader.SetFloat("_Multiplier", TextureManager.instance.Settings.surfaceNoiseSettings.multiplier);
        heightmap2ZoomTextureShader.SetInt("_RidgedNoise", TextureManager.instance.Settings.surfaceNoiseSettings.ridged ? 1 : 0);
        heightmap2ZoomTextureShader.SetFloat("_HeightExponent", TextureManager.instance.Settings.surfaceNoiseSettings.heightExponent);
        heightmap2ZoomTextureShader.SetFloat("_LayerStrength", TextureManager.instance.Settings.surfaceNoiseSettings.layerStrength);
        heightmap2ZoomTextureShader.SetFloat("_DomainWarping", TextureManager.instance.Settings.surfaceNoiseSettings.domainWarping);

        heightmap2ZoomTextureShader.SetFloat("_Seed2", TextureManager.instance.Settings.surfaceNoiseSettings2.seed);
        heightmap2ZoomTextureShader.SetFloat("_xOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.x);
        heightmap2ZoomTextureShader.SetFloat("_yOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.y);
        heightmap2ZoomTextureShader.SetFloat("_zOffset2", TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset.z);
        heightmap2ZoomTextureShader.SetFloat("_Multiplier2", TextureManager.instance.Settings.surfaceNoiseSettings2.multiplier);
        heightmap2ZoomTextureShader.SetInt("_Octaves2", TextureManager.instance.Settings.surfaceNoiseSettings2.octaves);
        heightmap2ZoomTextureShader.SetFloat("_Lacunarity2", TextureManager.instance.Settings.surfaceNoiseSettings2.lacunarity);
        heightmap2ZoomTextureShader.SetFloat("_Persistence2", TextureManager.instance.Settings.surfaceNoiseSettings2.persistence);
        heightmap2ZoomTextureShader.SetInt("_RidgedNoise2", TextureManager.instance.Settings.surfaceNoiseSettings2.ridged ? 1 : 0);
        heightmap2ZoomTextureShader.SetFloat("_HeightExponent2", TextureManager.instance.Settings.surfaceNoiseSettings2.heightExponent);
        heightmap2ZoomTextureShader.SetFloat("_LayerStrength2", TextureManager.instance.Settings.surfaceNoiseSettings2.layerStrength);
        heightmap2ZoomTextureShader.SetFloat("_DomainWarping2", TextureManager.instance.Settings.surfaceNoiseSettings2.domainWarping);

        heightmap2ZoomTextureShader.Dispatch(0, Mathf.CeilToInt(rt.width / 16f), Mathf.CeilToInt(rt.height / 16f), 1);

        flowErosionMapBuffer.Release();
    }

    RenderTexture heightmapRT;
    public void HeightMap2Texture()
    {
        //if (TextureManager.instance.HeightMap == null || TextureManager.instance.HeightMap.Length == 0)
        //    return;

        if (heightmap2TextureShader != null)
        {
            TextureManager.instance.InstantiateFlowErosionMap();
            heightMapBuffer.SetData(TextureManager.instance.HeightMap, 0, 0, TextureManager.instance.HeightMap.Length);

            ComputeBuffer flowErosionMapBuffer = null;
            if (TextureManager.instance.FlowErosionMap.Length > 0)
            {
                flowErosionMapBuffer = new ComputeBuffer(TextureManager.instance.FlowErosionMap.Length, sizeof(float));
                flowErosionMapBuffer.SetData(TextureManager.instance.FlowErosionMap, 0, 0, TextureManager.instance.FlowErosionMap.Length);
            }
            else
            {
                flowErosionMapBuffer = new ComputeBuffer(1, sizeof(float));
                flowErosionMapBuffer.SetData(new float[] { 0 }, 0, 0, 1);
            }

            heightmap2TextureShader.SetBuffer(0, "heightMap", heightMapBuffer);
            heightmap2TextureShader.SetBuffer(0, "flowErosionMap", flowErosionMapBuffer);

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
            heightmap2TextureShader.SetInt("flowErosionSet", TextureManager.instance.FlowErosionMap.Length == 0 ? 0 : 1);
            heightmap2TextureShader.SetFloat("minHeight", MapData.instance.LowestHeight);
            heightmap2TextureShader.SetFloat("maxHeight", MapData.instance.HighestHeight);
            heightmap2TextureShader.SetFloat("erosionNoiseMerge", TextureManager.instance.Settings.erosionNoiseMerge);

            heightmap2TextureShader.SetFloat("seed1", TextureManager.instance.Settings.temperatureNoiseSettings.seed);
            heightmap2TextureShader.SetFloat("seed2", TextureManager.instance.Settings.humidityNoiseSettings.seed);
            heightmap2TextureShader.SetFloat("seed3", TextureManager.instance.Settings.surfaceNoiseSettings.seed);
            heightmap2TextureShader.SetFloat("seed4", TextureManager.instance.Settings.surfaceNoiseSettings2.seed);

            heightmap2TextureShader.SetInt("octaves", TextureManager.instance.Settings.temperatureNoiseSettings.octaves);
            heightmap2TextureShader.SetFloat("lacunarity", TextureManager.instance.Settings.temperatureNoiseSettings.lacunarity);
            heightmap2TextureShader.SetFloat("persistence", TextureManager.instance.Settings.temperatureNoiseSettings.persistence);
            heightmap2TextureShader.SetFloat("multiplier", TextureManager.instance.Settings.temperatureNoiseSettings.multiplier);
            heightmap2TextureShader.SetFloat("xOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.x);
            heightmap2TextureShader.SetFloat("yOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.y);
            heightmap2TextureShader.SetFloat("zOffset", TextureManager.instance.Settings.temperatureNoiseSettings.noiseOffset.z);
            heightmap2TextureShader.SetInt("ridgedNoise", TextureManager.instance.Settings.temperatureNoiseSettings.ridged ? 1 : 0);
            heightmap2TextureShader.SetFloat("domainWarping", TextureManager.instance.Settings.temperatureNoiseSettings.domainWarping);
            heightmap2TextureShader.SetFloat("heightExponent", TextureManager.instance.Settings.temperatureNoiseSettings.heightExponent);

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

            heightmap2TextureShader.SetTexture(0, "noise", noiseRT);

            heightmap2TextureShader.Dispatch(0, Mathf.CeilToInt(heightmapRT.width / 16f), Mathf.CeilToInt(heightmapRT.height / 16f), 1);

            flowErosionMapBuffer.Release();

            if (saveTemporaryTextures)
            {
                if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Textures"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
                heightmapRT.SaveToFile(Path.Combine(Application.persistentDataPath, "Textures", "heightmapFromRaw.png"));
                noiseRT.SaveToFile(Path.Combine(Application.persistentDataPath, "Textures", "noise.png"));
            }
            planetSurfaceMaterial.SetTexture("_HeightMap", heightmapRT);
            planetSurfaceMaterial.SetTexture("_NoiseMap", noiseRT);

            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
            planetSurfaceMaterial.SetInt("_IsNoiseMapSet", 1);
        }
    }

    void Texture2HeightMap(ref RenderTexture heightmapTexture)
    {
        if (texture2HeightmapShader == null)
            return;

        TextureManager.instance.InstantiateHeightMap();

        ComputeBuffer mapBuffer = new ComputeBuffer(TextureManager.instance.HeightMap.Length, sizeof(float));
        mapBuffer.SetData(TextureManager.instance.HeightMap);
        texture2HeightmapShader.SetBuffer(0, "heightMap", mapBuffer);

        texture2HeightmapShader.SetTexture(0, "renderTexture", heightmapTexture);
        texture2HeightmapShader.SetInt("textureWidth", TextureManager.instance.Settings.textureWidth);

        texture2HeightmapShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 8f), 6);

        mapBuffer.GetData(TextureManager.instance.HeightMap);

        mapBuffer.Release();

        //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap, TextureManager.instance.Settings.textureWidth, Path.Combine(Application.persistentDataPath, "heightMap1.png"));
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
        HydraulicErosion.instance.Erode(heightMapBuffer);
        HeightMap2Texture();
        //heightmap.SaveAsPNG(Path.Combine(Application.persistentDataPath, "Textures", "heightmap-2.png"));
        isEroded = true;
        UpdateSurfaceMaterialHeightMap();
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
        if (heightmapRT != null)
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
    }
    
    public ComputeShader landMaskShader;
    int landMaskCount = 0;
    void CalculateLandMask()
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer(TextureManager.instance.HeightMap.Length, sizeof(float));
        heightMapBuffer.SetData(TextureManager.instance.HeightMap);

        int[] landMask = new int[TextureManager.instance.HeightMap.Length * 6];

        ComputeBuffer landMaskBuffer = new ComputeBuffer(landMask.Length, sizeof(int));
        landMaskBuffer.SetData(landMask);

        //float unNormalizedWaterLevel = (TextureManager.instance.Settings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

        landMaskShader.SetInt("mapWidth", TextureManager.instance.Settings.textureWidth);
        landMaskShader.SetFloat("waterLevel", TextureManager.instance.Settings.waterLevel);
        landMaskShader.SetBuffer(0, "heightMap", heightMapBuffer);
        landMaskShader.SetBuffer(0, "landMask", landMaskBuffer);

        landMaskShader.Dispatch(0, Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 4 / 8f), Mathf.CeilToInt(TextureManager.instance.Settings.textureWidth * 2 / 8f), 6);

        landMaskBuffer.GetData(landMask);
        landMaskCount = landMask.Sum();

        heightMapBuffer.Release();
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
        HeightMap2Texture();

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

        for (float x = -2 * radiusInPixels; x <= 2 * radiusInPixels; x++)
        {
            for (float y = -radiusInPixels; y <= radiusInPixels; y++)
            {
                float pixelDistance = Mathf.Sqrt(x * x + y * y);
                if (pixelDistance > radiusInPixels)
                    continue;

                Vector2 textureUV = new Vector2(coordinates.x * TextureManager.instance.Settings.textureWidth * 4, coordinates.y * TextureManager.instance.Settings.textureWidth * 2);

                float distanceRatio = 1 - (pixelDistance / radiusInPixels);
                float heightToAlter = elevationDelta * distanceRatio;

                float height = TextureManager.instance.HeightMapValueAtCoordinates((int)textureUV.x, (int)textureUV.y);
                height += heightToAlter;
                TextureManager.instance.SetHeightAtCoordinates((int)textureUV.x, (int)textureUV.y, height);
            }
        }

        isEroded = true;
        planetSurfaceMaterial.SetInt("_IsEroded", 1);
    }
}
