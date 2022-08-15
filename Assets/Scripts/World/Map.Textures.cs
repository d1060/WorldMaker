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
    //Texture2D heightmap = null;
    //Texture2D normalmap = null;
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

                //int isEroded = planetSurfaceMaterial.GetInt("_IsEroded");
                //planetSurfaceMaterial.SetInt("_IsEroded", 0);
                GenerateHeightMap();

                if (AppData.instance.SaveMainMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainMap" + fileNameExtension), SphereShaderDrawType.Land);
                }

                if (AppData.instance.SaveHeightMap)
                {
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

                if (AppData.instance.SaveMainMap && AppData.instance.SaveNormalMap)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-MainNormalMap" + fileNameExtension), SphereShaderDrawType.LandNormal);
                }

                if (AppData.instance.SaveTemperature)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Temperature" + fileNameExtension), SphereShaderDrawType.Temperature);
                }

                if (AppData.instance.SaveRivers && flowTexture != null)
                {
                    SaveImageFile(Path.Combine(fileNamePath, fileNameWithoutExtension + "-Rivers" + fileNameExtension), flowTexture);
                }

                //planetSurfaceMaterial.SetInt("_IsEroded", isEroded);

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

        if (exportRT != null && (exportRT.width != textureSettings.textureWidth || exportRT.height != textureSettings.textureHeight))
        {
            Destroy(exportRT);
            exportRT = null;
        }

        if (exportRT == null)
        {
            exportRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGBHalf);
            exportRT.wrapMode = TextureWrapMode.Repeat;
            exportRT.name = "Export Render Texture";
            exportRT.enableRandomWrite = true;
            exportRT.Create();
        }

        Texture2D source;
        if (drawMode != SphereShaderDrawType.HeightMap)
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA32, false);
        else
            source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGB48, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = exportRT;
        Graphics.Blit(source, exportRT, planetSurfaceMaterial, 2);
        exportRT.Save(fileName);
        RenderTexture.active = prevActive;
        Destroy(exportRT);
        exportRT = null;
        planetSurfaceMaterial.SetFloat("_DrawType", prevDrawMode);
        UnityEngine.Object.Destroy(source);
    }

    Texture2D ShaderToTexture(SphereShaderDrawType drawMode)
    {
        float prevDrawMode = planetSurfaceMaterial.GetFloat("_DrawType");
        planetSurfaceMaterial.SetFloat("_DrawType", (int)drawMode);

        if (exportRT != null && (exportRT.width != textureSettings.textureWidth || exportRT.height != textureSettings.textureHeight))
        {
            Destroy(exportRT);
            exportRT = null;
        }

        if (exportRT == null)
        {
            exportRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 32, RenderTextureFormat.ARGBHalf);
            exportRT.wrapMode = TextureWrapMode.Repeat;
            exportRT.name = "Export Render Texture";
            exportRT.enableRandomWrite = true;
            exportRT.Create();
        }

        Texture2D source = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA32, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = exportRT;
        Graphics.Blit(source, exportRT, planetSurfaceMaterial, 2);
        source.ReadPixels(new Rect(0, 0, textureSettings.textureWidth, textureSettings.textureHeight), 0, 0);
        source.Apply();
        RenderTexture.active = prevActive;
        Destroy(exportRT);
        exportRT = null;
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
        //string saveExtension = Path.GetExtension(savedFile);

        string baseFolder = Path.Combine(savePath, saveFile, "Surface");
        if (AppData.instance.SaveMainMap || AppData.instance.SaveLandMask)
            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

        string bumpBaseFolder = Path.Combine(savePath, saveFile, "Bump");
        if (AppData.instance.SaveHeightMap)
            if (!Directory.Exists(bumpBaseFolder))
                Directory.CreateDirectory(bumpBaseFolder);

        // Gets the Main Map.
        Texture2D mainMap = ShaderToTexture(SphereShaderDrawType.LandNormal);
        // Gets the Landmask.
        Texture2D landMask = ShaderToTexture(SphereShaderDrawType.InvertedLandMask);
        // Gets the Bump.
        Texture2D bumpMap = ShaderToTexture(SphereShaderDrawType.HeightMap);

        if (AppData.instance.TransparentOceans)
        {
            ApplyTransparency(mainMap, landMask);
        }

        //Texture2D mainMapCubemapResized = null;
        //Texture2D landMaskCubemapResized = null;
        //Texture2D bumpMapCubemapResized = null;
        if (AppData.instance.CubemapDivisions >= 1)
        {
            int cubemapResizeDimensions = (int)Mathf.Pow(2, AppData.instance.CubemapDivisions) * AppData.instance.CubemapDimension;

            //mainMapCubemapResized = mainMap.ResizePixels(cubemapResizeDimensions, cubemapResizeDimensions, true);

            //bumpMapCubemapResized = bumpMap.ResizePixels(cubemapResizeDimensions, cubemapResizeDimensions, true);
            //bumpMapCubemapResized.SaveAsPNG(Path.Combine(bumpBaseFolder, "base-bump-resized.png"));

            //if (!AppData.instance.TransparentOceans)
            //    landMaskCubemapResized = landMask.ResizePixels(cubemapResizeDimensions, cubemapResizeDimensions, true);

            TextureScale.Bilinear(mainMap, cubemapResizeDimensions, cubemapResizeDimensions);

            TextureScale.Bilinear(bumpMap, cubemapResizeDimensions, cubemapResizeDimensions);

            if (!AppData.instance.TransparentOceans)
            {
                TextureScale.Bilinear(landMask, cubemapResizeDimensions, cubemapResizeDimensions);
            }
        }

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
            if (AppData.instance.SaveMainMap || AppData.instance.SaveLandMask)
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

            string bumpFolder = Path.Combine(savePath, saveFile, "Bump", baseSubfolder);
            if (AppData.instance.SaveHeightMap)
                if (!Directory.Exists(bumpFolder))
                    Directory.CreateDirectory(bumpFolder);

            if (AppData.instance.SaveHeightMap)
                ExportCubeMapFile(ref bumpMap, bumpFolder, "", AppData.instance.CubemapDimension, faceCount, 0, 0, 0, true);

            if (AppData.instance.TransparentOceans || (AppData.instance.SaveMainMap && !AppData.instance.SaveLandMask))
                ExportCubeMapFile(ref mainMap, folder, "", AppData.instance.CubemapDimension, faceCount, 0, 0, 0, true);
            else
            {
                ExportCubeMapFile(ref mainMap, folder, "_c", AppData.instance.CubemapDimension, faceCount, 0, 0, 0);
                ExportCubeMapFile(ref landMask, folder, "_a", AppData.instance.CubemapDimension, faceCount, 0, 0, 0);
            }

            for (int i = 1; i < AppData.instance.CubemapDivisions; i++)
            {
                if (AppData.instance.SaveHeightMap)
                    SaveMapSubDivisions(ref bumpMap, faceCount, i, bumpFolder, "", true);

                if (AppData.instance.TransparentOceans || (AppData.instance.SaveMainMap && !AppData.instance.SaveLandMask))
                    SaveMapSubDivisions(ref mainMap, faceCount, i, folder, "", true);
                else
                {
                    SaveMapSubDivisions(ref mainMap, faceCount, i, folder, "_c");
                    SaveMapSubDivisions(ref landMask, faceCount, i, folder, "_a");
                }
            }
        }

        TextureScale.Bilinear(mainMap, AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);
        if (!AppData.instance.TransparentOceans)
        {
            TextureScale.Bilinear(landMask, AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);
        }
        TextureScale.Bilinear(bumpMap, AppData.instance.CubemapDimension, AppData.instance.CubemapDimension);

        if (AppData.instance.TransparentOceans || (AppData.instance.SaveMainMap && !AppData.instance.SaveLandMask))
            mainMap.SaveAsPNG(Path.Combine(baseFolder, "base.png"));
        else
        {
            mainMap.SaveAsJPG(Path.Combine(baseFolder, "base_c.jpg"));
            landMask.SaveAsJPG(Path.Combine(baseFolder, "base_a.jpg"));
        }

        if (AppData.instance.SaveHeightMap)
            bumpMap.SaveAsPNG(Path.Combine(bumpBaseFolder, "base.png"));

        UnityEngine.Object.Destroy(mainMap);
        UnityEngine.Object.Destroy(landMask);
        UnityEngine.Object.Destroy(bumpMap);

        mainMap = null;
        landMask = null;
        bumpMap = null;

        //if (AppData.instance.CubemapDivisions >= 1)
        //{
        //    UnityEngine.Object.Destroy(mainMapCubemapResized);
        //    mainMapCubemapResized = null;
        //    if (!AppData.instance.TransparentOceans)
        //    {
        //        UnityEngine.Object.Destroy(landMaskCubemapResized);
        //        landMaskCubemapResized = null;
        //    }
        //    UnityEngine.Object.Destroy(bumpMapCubemapResized);
        //    bumpMapCubemapResized = null;
        //}

        AppData.instance.LastSavedImageFolder = savePath;
        AppData.instance.Save();
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

        Destroy(transparencyRT);
        transparencyRT = null;
    }

    void ExportCubeMapFile(ref Texture2D map, string folder, string filePosfix, int dimension, int face, int division, int divisionX, int divisionY, bool saveAsPng = false)
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
            UnityEngine.Object.Destroy(resultTexture);
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

            UnityEngine.Object.Destroy(Equirectangular2Cubemap.instance.Result);
        }
    }

    void SaveMapSubDivisions(ref Texture2D tex, int faceCount, int subdivision, string folder, string filePosfix, bool saveAsPng = false)
    {
        int sizeDivisor = (int)Mathf.Pow(2, subdivision);

        for (int countX = 0; countX < sizeDivisor; countX++)
        {
            for (int countY = 0; countY < sizeDivisor; countY++)
            {
                ExportCubeMapFile(ref tex, folder, filePosfix, AppData.instance.CubemapDimension, faceCount, subdivision, countX, countY, saveAsPng);
            }
        }
    }

    void UpdateSurfaceMaterialProperties(bool resetEroded = true)
    {
        if (planetSurfaceMaterial == null)
            return;

        textureSettings.UpdateSurfaceMaterialProperties(planetSurfaceMaterial, showTemperature);

        if (mapSettings.UseImages)
        {
            //if (mapSettings.HeightMapPath == "" || !File.Exists(mapSettings.HeightMapPath))
                //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
            //else
            if (mapSettings.HeightMapPath != "" && File.Exists(mapSettings.HeightMapPath))
            {
                UpdateSurfaceMaterialHeightMap();
                //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
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
            //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
            planetSurfaceMaterial.SetInt("_IsMainmapSet", 0);
            planetSurfaceMaterial.SetInt("_IsLandmaskSet", 0);
        }

        if (resetEroded && !mapSettings.UseImages)
        {
            ResetEroded();
        }
    }

    public void ResetEroded()
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

    public void UpdateSurfaceMaterialHeightMap(bool isEroded = false)
    {
        if (planetSurfaceMaterial == null)
            return;

        if (!isEroded && mapSettings.HeightMapPath != "" && File.Exists(mapSettings.HeightMapPath))
        {
            Texture2D heightmap = LoadAnyImageFile(mapSettings.HeightMapPath);

            if (heightmapRT != null && (heightmapRT.width != heightmap.width || heightmapRT.height != heightmap.height))
            {
                Destroy(heightmapRT);
                heightmapRT = null;
            }

            if (heightmapRT == null)
            {
                heightmapRT = new RenderTexture(heightmap.width, heightmap.height, 16, RenderTextureFormat.ARGBHalf);
                heightmapRT.wrapMode = TextureWrapMode.Repeat;
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.Create();
            }

            Graphics.Blit(heightmap, heightmapRT);

            Texture2HeightMap(ref heightmapRT, ref originalHeightMap);

            if (erodedHeightMap == null || erodedHeightMap.Length != heightmapRT.width * heightmapRT.height)
                erodedHeightMap = new float[heightmapRT.width * heightmapRT.height];
            Array.Copy(originalHeightMap, erodedHeightMap, originalHeightMap.Length);

            if (mergedHeightMap == null || mergedHeightMap.Length != heightmapRT.width * heightmapRT.height)
                mergedHeightMap = new float[heightmapRT.width * heightmapRT.height];
            Array.Copy(originalHeightMap, mergedHeightMap, originalHeightMap.Length);

            if (textureSettings.textureWidth != heightmapRT.width)
            {
                textureSettings.textureWidth = heightmapRT.width;
                UpdateUIInputField(setupPanelTransform, "Texture Width Text Box", TextureWidth);
            }

            if (textureSettings.textureHeight != heightmapRT.height)
            {
                textureSettings.textureHeight = heightmapRT.height;
                UpdateUIInputField(setupPanelTransform, "Texture Height Text Box", TextureHeight);
            }
        }

        if (heightmapRT != null)
        { 
            planetSurfaceMaterial.SetTexture("_HeightMap", heightmapRT);
        }
        //else
        //    planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);

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
    public ComputeShader maxMinComputeShader;
    public ComputeShader erosionShader;
    public ComputeShader heightmap2TextureShader;
    public ComputeShader texture2HeightmapShader;
    float[] erodedHeightMap;
    float[] originalHeightMap;
    float[] mergedHeightMap;
    float[] humidityMap;

    void GenerateHeightMap(bool resetHeightLimits = false)
    {
        if (originalHeightMap == null)
        {
            originalHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
            erodedHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];
            mergedHeightMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];

            ComputeBuffer mapBuffer = new ComputeBuffer(originalHeightMap.Length, sizeof(float));
            mapBuffer.SetData(originalHeightMap);
            heightMapComputeShader.SetBuffer(0, "heightMap", mapBuffer);

            if (resetHeightLimits)
            {
                heightMapComputeShader.SetFloat("_MinimumHeight", 0);
                heightMapComputeShader.SetFloat("_MaximumHeight", 1);
            }
            else
            {
                heightMapComputeShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
                heightMapComputeShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);
            }
            heightMapComputeShader.SetInt("_MapWidth", textureSettings.textureWidth);
            heightMapComputeShader.SetInt("_MapHeight", textureSettings.textureHeight);

            heightMapComputeShader.SetFloat("_Seed", textureSettings.surfaceNoiseSettings.seed);
            heightMapComputeShader.SetFloat("_xOffset", textureSettings.surfaceNoiseSettings.noiseOffset.x);
            heightMapComputeShader.SetFloat("_yOffset", textureSettings.surfaceNoiseSettings.noiseOffset.y);
            heightMapComputeShader.SetFloat("_zOffset", textureSettings.surfaceNoiseSettings.noiseOffset.z);
            heightMapComputeShader.SetInt("_Octaves", textureSettings.surfaceNoiseSettings.octaves);
            heightMapComputeShader.SetFloat("_Lacunarity", textureSettings.surfaceNoiseSettings.lacunarity);
            heightMapComputeShader.SetFloat("_Persistence", textureSettings.surfaceNoiseSettings.persistence);
            heightMapComputeShader.SetFloat("_Multiplier", textureSettings.surfaceNoiseSettings.multiplier);
            heightMapComputeShader.SetInt("_RidgedNoise", textureSettings.surfaceNoiseSettings.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("_HeightExponent", textureSettings.surfaceNoiseSettings.heightExponent);
            heightMapComputeShader.SetFloat("_LayerStrength", textureSettings.surfaceNoiseSettings.layerStrength);
            heightMapComputeShader.SetFloat("_DomainWarping", textureSettings.surfaceNoiseSettings.domainWarping);

            heightMapComputeShader.SetFloat("_Seed2", textureSettings.surfaceNoiseSettings2.seed);
            heightMapComputeShader.SetFloat("_xOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.x);
            heightMapComputeShader.SetFloat("_yOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.y);
            heightMapComputeShader.SetFloat("_zOffset2", textureSettings.surfaceNoiseSettings2.noiseOffset.z);
            heightMapComputeShader.SetFloat("_Multiplier2", textureSettings.surfaceNoiseSettings2.multiplier);
            heightMapComputeShader.SetInt("_Octaves2", textureSettings.surfaceNoiseSettings2.octaves);
            heightMapComputeShader.SetFloat("_Lacunarity2", textureSettings.surfaceNoiseSettings2.lacunarity);
            heightMapComputeShader.SetFloat("_Persistence2", textureSettings.surfaceNoiseSettings2.persistence);
            heightMapComputeShader.SetInt("_RidgedNoise2", textureSettings.surfaceNoiseSettings2.ridged ? 1 : 0);
            heightMapComputeShader.SetFloat("_HeightExponent2", textureSettings.surfaceNoiseSettings2.heightExponent);
            heightMapComputeShader.SetFloat("_LayerStrength2", textureSettings.surfaceNoiseSettings2.layerStrength);
            heightMapComputeShader.SetFloat("_DomainWarping2", textureSettings.surfaceNoiseSettings2.domainWarping);

            heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 16f), Mathf.CeilToInt(textureSettings.textureHeight / 16f), 1);

            mapBuffer.GetData(originalHeightMap);

            if (resetHeightLimits)
            {
                //ImageTools.SaveTextureFloatArray(originalHeightMap, textureSettings.textureWidth, textureSettings.textureHeight, Path.Combine(Application.persistentDataPath, "Test-Heightmap.png"));

                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;

                for (int i = 0; i < originalHeightMap.Length; i += 1)
                {
                    if (minHeight > originalHeightMap[i])
                        minHeight = originalHeightMap[i];
                    if (maxHeight < originalHeightMap[i])
                        maxHeight = originalHeightMap[i];
                }

                if (minHeight != maxHeight)
                {
                    MapData.instance.LowestHeight = minHeight;
                    MapData.instance.HighestHeight = maxHeight;
                }

                if (MapData.instance.LowestHeight < 0)
                    MapData.instance.LowestHeight = 0;

                if (MapData.instance.HighestHeight > 1)
                    MapData.instance.HighestHeight = 1;

                SetHeightLimits();
            }

            // Gets the Max and Min Heights.
            //float[] minMaxMap = new float[2];
            //minMaxMap[0] = float.MaxValue;
            //minMaxMap[1] = float.MinValue;

            //ComputeBuffer minMaxOutputsBuffer = new ComputeBuffer(minMaxMap.Length, sizeof(float));
            //minMaxOutputsBuffer.SetData(minMaxMap);
            //maxMinComputeShader.SetInt("mapWidth", textureSettings.textureWidth);
            //maxMinComputeShader.SetInt("mapHeight", textureSettings.textureHeight);
            //maxMinComputeShader.SetBuffer(0, "map", mapBuffer);
            //maxMinComputeShader.SetBuffer(0, "outputs", minMaxOutputsBuffer);

            //maxMinComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);
            //maxMinComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            //minMaxOutputsBuffer.GetData(minMaxMap);

            //MapData.instance.LowestHeight = minMaxMap[0];
            //MapData.instance.HighestHeight = minMaxMap[1];

            //minHeights.Add(MapData.instance.LowestHeight);
            //maxHeights.Add(MapData.instance.HighestHeight);
            //Debug.Log("Map Height Limits: " + minHeights.Average().ToString("###0.#####") + " to " + maxHeights.Average().ToString("###0.#####"));

            //MapData.instance.LowestHeight = 0.15f;
            //MapData.instance.HighestHeight = 0.5f;

            //minMaxOutputsBuffer.Release();

            mapBuffer.Release();

            if (resetHeightLimits)
            {
                Array.Clear(originalHeightMap, 0, originalHeightMap.Length);
                originalHeightMap = null;
            }
            else
            {
                Array.Copy(originalHeightMap, erodedHeightMap, originalHeightMap.Length);
                Array.Copy(originalHeightMap, mergedHeightMap, originalHeightMap.Length);
            }

            HeightMap2Texture();

            //if (resetHeightLimits)
            //{
            //    textureSettings.UpdateSurfaceMaterialProperties(planetSurfaceMaterial, showTemperature);
            //    planetSurfaceMaterial.SetFloat("_MinHeight", 0);
            //    planetSurfaceMaterial.SetFloat("_MaxHeight", 1);
            //    SaveImageFile(Path.Combine(Application.persistentDataPath, "Test-Shader-Heightmap.png"), SphereShaderDrawType.HeightMap);
            //    planetSurfaceMaterial.SetFloat("_MinHeight", MapData.instance.LowestHeight);
            //    planetSurfaceMaterial.SetFloat("_MaxHeight", MapData.instance.HighestHeight);
            //}
        }
    }

    void GenerateHumiditytMap()
    {
        humidityMap = new float[textureSettings.textureWidth * textureSettings.textureHeight];

        ComputeBuffer humidityBuffer = new ComputeBuffer(originalHeightMap.Length, sizeof(float));
        humidityBuffer.SetData(originalHeightMap);
        heightMapComputeShader.SetBuffer(0, "heightMap", humidityBuffer);

        heightMapComputeShader.SetFloat("_Seed", textureSettings.humidityNoiseSettings.seed);
        heightMapComputeShader.SetFloat("_xOffset", textureSettings.humidityNoiseSettings.noiseOffset.x);
        heightMapComputeShader.SetFloat("_yOffset", textureSettings.humidityNoiseSettings.noiseOffset.y);
        heightMapComputeShader.SetFloat("_zOffset", textureSettings.humidityNoiseSettings.noiseOffset.z);
        heightMapComputeShader.SetFloat("_MinimumHeight", MapData.instance.LowestHeight);
        heightMapComputeShader.SetFloat("_MaximumHeight", MapData.instance.HighestHeight);
        heightMapComputeShader.SetInt("_MapWidth", textureSettings.textureWidth);
        heightMapComputeShader.SetInt("_MapHeight", textureSettings.textureHeight);
        heightMapComputeShader.SetInt("_Octaves", textureSettings.humidityNoiseSettings.octaves);
        heightMapComputeShader.SetFloat("_Lacunarity", textureSettings.humidityNoiseSettings.lacunarity);
        heightMapComputeShader.SetFloat("_Persistence", textureSettings.humidityNoiseSettings.persistence);
        heightMapComputeShader.SetFloat("_Multiplier", textureSettings.humidityNoiseSettings.multiplier);
        heightMapComputeShader.SetInt("_RidgedNoise", textureSettings.humidityNoiseSettings.ridged ? 1 : 0);
        heightMapComputeShader.SetFloat("_HeightExponent", textureSettings.humidityNoiseSettings.heightExponent);
        heightMapComputeShader.SetFloat("_LayerStrength", 1);
        heightMapComputeShader.SetFloat("_DomainWarping", 0);
        heightMapComputeShader.SetFloat("_xOffset2", textureSettings.humidityNoiseSettings.noiseOffset.x);
        heightMapComputeShader.SetFloat("_yOffset2", textureSettings.humidityNoiseSettings.noiseOffset.y);
        heightMapComputeShader.SetFloat("_zOffset2", textureSettings.humidityNoiseSettings.noiseOffset.z);
        heightMapComputeShader.SetFloat("_Seed2", textureSettings.humidityNoiseSettings.seed);
        heightMapComputeShader.SetFloat("_Multiplier2", textureSettings.humidityNoiseSettings.multiplier);
        heightMapComputeShader.SetInt("_Octaves2", textureSettings.humidityNoiseSettings.octaves);
        heightMapComputeShader.SetFloat("_Lacunarity2", textureSettings.humidityNoiseSettings.lacunarity);
        heightMapComputeShader.SetFloat("_Persistence2", textureSettings.humidityNoiseSettings.persistence);
        heightMapComputeShader.SetInt("_RidgedNoise2", textureSettings.humidityNoiseSettings.ridged ? 1 : 0);
        heightMapComputeShader.SetFloat("_HeightExponent2", textureSettings.humidityNoiseSettings.heightExponent);
        heightMapComputeShader.SetFloat("_LayerStrength2", 0);
        heightMapComputeShader.SetFloat("_DomainWarping2", 0);

        heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 8f), Mathf.CeilToInt(textureSettings.textureHeight / 8f), 1);

        humidityBuffer.GetData(humidityMap);
        humidityBuffer.Release();
    }

    RenderTexture heightmapRT;
    public void HeightMap2Texture()
    {
        if (erodedHeightMap == null || erodedHeightMap.Length == 0 || originalHeightMap == null || originalHeightMap.Length == 0 || mergedHeightMap == null || mergedHeightMap.Length == 0)
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
                heightmapRT = new RenderTexture(textureSettings.textureWidth, textureSettings.textureHeight, 16, RenderTextureFormat.ARGBHalf);
                heightmapRT.wrapMode = TextureWrapMode.Repeat;
                heightmapRT.name = "Heightmap Render Texture";
                heightmapRT.enableRandomWrite = true;
                heightmapRT.Create();
            }

            heightmap2TextureShader.SetTexture(0, "result", heightmapRT);
            heightmap2TextureShader.SetInt("mapWidth", textureSettings.textureWidth);
            heightmap2TextureShader.SetInt("mapHeight", textureSettings.textureHeight);
            heightmap2TextureShader.SetFloat("waterLevel", textureSettings.waterLevel);
            heightmap2TextureShader.SetFloat("normalScale", 50);
            heightmap2TextureShader.SetFloat("erosionNoiseMerge", textureSettings.erosionNoiseMerge);

            heightmap2TextureShader.Dispatch(0, Mathf.CeilToInt(textureSettings.textureWidth / 32f), Mathf.CeilToInt(textureSettings.textureHeight / 32f), 1);

            //if (heightmap == null || heightmap.width != textureSettings.textureWidth || heightmap.height != textureSettings.textureHeight)
            //    heightmap = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);

            //RenderTexture prevActive = RenderTexture.active;
            //RenderTexture.active = heightmapRT;
            //heightmap.ReadPixels(new Rect(0, 0, heightmapRT.width, heightmapRT.height), 0, 0);
            //heightmap.Apply();

            mapBufferMerged.GetData(mergedHeightMap);
            mapBuffer.Release();
            mapBufferEroded.Release();
            mapBufferMerged.Release();
            inciseFlowMapBuffer.Release();

            planetSurfaceMaterial.SetTexture("_HeightMap", heightmapRT);
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
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
            //if (heightmap == null || heightmap.width != textureSettings.textureWidth || heightmap.height != textureSettings.textureHeight)
            //    heightmap = new Texture2D(textureSettings.textureWidth, textureSettings.textureHeight, TextureFormat.RGBA64, false, true);
            //heightmap.SetPixels(colors);
            //heightmap.Apply();
        }
    }

    void Texture2HeightMap(ref RenderTexture heightmapTexture, ref float[] heightMap)
    {
        if (texture2HeightmapShader == null)
            return;

        heightMap = new float[heightmapTexture.width * heightmapTexture.height];

        ComputeBuffer mapBuffer = new ComputeBuffer(heightMap.Length, sizeof(float));
        mapBuffer.SetData(heightMap);
        texture2HeightmapShader.SetBuffer(0, "heightMap", mapBuffer);

        texture2HeightmapShader.SetTexture(0, "renderTexture", heightmapTexture);
        texture2HeightmapShader.SetInt("mapWidth", heightmapTexture.width);
        texture2HeightmapShader.SetInt("mapHeight", heightmapTexture.height);

        texture2HeightmapShader.Dispatch(0, Mathf.CeilToInt(heightmapTexture.width / 32f), Mathf.CeilToInt(heightmapTexture.height / 32f), 1);

        mapBuffer.GetData(heightMap);
        mapBuffer.Release();
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

    public void ResetImages()
    {
        planetSurfaceMaterial.SetInt("_IsMainMapSet", 0);
        planetSurfaceMaterial.SetInt("_IsLandMaskSet", 0);
        //planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
        heightmapRT.Release();
        if (heightmapRT != null)
            UnityEngine.Object.Destroy(heightmapRT);
        heightmapRT = null;
        if (landmap != null)
            UnityEngine.Object.Destroy(landmap);
        landmap = null;
        if (landmask != null)
            UnityEngine.Object.Destroy(landmask);
        landmask = null;
    }

    public void UndoErosion()
    {
        ResetEroded();
        flowTexture = null;
        flowTextureRandom = null;
        inciseFlowMap = null;
        flowMap = null;
        isInciseFlowApplied = false;

        //GenerateHeightMap();
        planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);

        if (mapSettings.UseImages && heightmapRT != null)
        {
            UpdateSurfaceMaterialHeightMap();
            planetSurfaceMaterial.SetInt("_IsHeightmapSet", 1);
        }
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
            //GenerateHeightMap();
            //if (connectivityMap == null) 
            //    EstablishHeightmapConnectivity();
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
                randomRiversRT.wrapMode = TextureWrapMode.Repeat;
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

            //float unNormalizedWaterLevel = (textureSettings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

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
        //planetSurfaceMaterial.SetTexture("_HeightMap", heightmap);
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

        //float unNormalizedWaterLevel = (textureSettings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

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

            //float unNormalizedWaterLevel = (textureSettings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

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
            plotRiversRT.wrapMode = TextureWrapMode.Repeat;
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

                float[] outputMap = new float[1];
                outputMap[0] = 1;

                ComputeBuffer heightBuffer = new ComputeBuffer(connectivityHeightMap.Length, sizeof(float));
                heightBuffer.SetData(connectivityHeightMap);

                ComputeBuffer connectivityIndexesBuffer = new ComputeBuffer(connectivityMap.Length, sizeof(int));
                connectivityIndexesBuffer.SetData(connectivityMap);

                ComputeBuffer distanceToWaterBuffer = new ComputeBuffer(distanceToWaterMap.Length, sizeof(float));
                distanceToWaterBuffer.SetData(distanceToWaterMap);

                ComputeBuffer outputBuffer = new ComputeBuffer(outputMap.Length, sizeof(float));
                outputBuffer.SetData(outputMap);

                //float unNormalizedWaterLevel = (textureSettings.waterLevel * (MapData.instance.HighestHeight - MapData.instance.LowestHeight)) + MapData.instance.LowestHeight;

                heightmapConnectivityShader.SetInt("mapWidth", textureSettings.textureWidth);
                heightmapConnectivityShader.SetInt("mapHeight", textureSettings.textureHeight);
                heightmapConnectivityShader.SetFloat("waterLevel", textureSettings.waterLevel);
                heightmapConnectivityShader.SetFloat("upwardWeight", inciseFlowSettings.upwardWeight);
                heightmapConnectivityShader.SetFloat("downwardWeight", inciseFlowSettings.downwardWeight);
                heightmapConnectivityShader.SetFloat("distanceWeight", inciseFlowSettings.distanceWeight);

                heightmapConnectivityShader.SetBuffer(0, "heightMap", heightBuffer);
                heightmapConnectivityShader.SetBuffer(0, "distanceMap", distanceToWaterBuffer);
                heightmapConnectivityShader.SetBuffer(0, "connectivityMap", connectivityIndexesBuffer);
                heightmapConnectivityShader.SetBuffer(0, "output", outputBuffer);

                for (int i = 0; i < numPasses; i++)
                {
                    outputMap[0] = 1;
                    outputBuffer.SetData(outputMap);
                    heightmapConnectivityShader.Dispatch(0, numThreadsX, numThreadsY, 1);
                    outputBuffer.GetData(outputMap);
                    if (outputMap[0] == 1)
                        break;
                }

                connectivityIndexesBuffer.GetData(connectivityMap);

                heightBuffer.Release();
                connectivityIndexesBuffer.Release();
                distanceToWaterBuffer.Release();
                outputBuffer.Release();
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
        //planetSurfaceMaterial.SetTexture("_HeightMap", heightmap);
        planetSurfaceMaterial.SetInt("_IsEroded", 1);
    }
}
