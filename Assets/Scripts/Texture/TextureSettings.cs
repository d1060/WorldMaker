using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TextureSettings
{
    public int textureWidth = 512;
    //public int textureHeight = 128;
    public Color32 iceColor = new Color32(244, 244, 244, 255);
    [Range(0, -20)]
    public float iceTemperatureThreshold = 0;
    public Color32 desertColor = new Color32(192, 226, 142, 255);
    public float desertThreshold = 10;
    public Color32[] land1Color = new Color32[] 
    {
        new Color32(176, 163, 110, 255),
        new Color32(130, 128, 69, 255),
        new Color32(52, 70, 10, 255),
        new Color32(135, 138, 93, 255),
        new Color32(117, 105, 83, 255),
        new Color32(244, 244, 244, 255)
    };
    public float[] landColorStages = new float[]
    {
        0.01f,
        0.1f,
        0.3f,
        0.6f,
        0.95f,
        1.0f
    };
    public Color32[] oceanColors = new Color32[]
    {
        new Color32(64, 85, 100, 255),
        new Color32(49, 54, 73, 255),
        new Color32(50, 51, 71, 255)
    };
    public float[] oceanStages = new float[]
    {
        0.0f,
        0.5f,
        1.0f
    };
    public float[] zoomLevelDistances = new float[] { 200, 95, 47.5f, 23.75f, 11.875f, 5.9375f, float.MinValue };
    float[] textureSteps = null; // In Longitudes per pixel

    public Color32 riverTileColor = new Color32(0, 0, 200, 255);

    [Range(0, 1)]
    public float waterLevel = 0.6f;
    [Range(0, 1)]
    public float minHeight = 0.15f;
    [Range(0, 1)]
    public float maxHeight = 0.75f;
    [Header("Terrain Settings")]
    public NoiseSettings surfaceNoiseSettings;
    public NoiseSettings surfaceNoiseSettings2;
    public NoiseSettings temperatureNoiseSettings;
    public float temperatureExponent = 2.718281f;
    public float temperatureRatio = 16;
    public float temperatureElevationRatio = 10;
    public float iceTransition = 0.25f;
    public float temperatureWaterDrop = 2;
    public float temperatureLatitudeMultiplier = 15;
    public float temperatureLatitudeDrop = 0;
    public NoiseSettings humidityNoiseSettings;
    public float humidityExponent = 2.718281f;
    public float humidityMultiplier = 11;
    public float humidityTransition = 10;
    [Range(0, 1)]
    public float highHumidityLightnessPercentage = 0.2f;
    [Range(0, 1)]
    public float erosionNoiseMerge = 0.5f;

    int selectedLayer = 1;
    public float[] TextureSteps { get { return textureSteps; } set { textureSteps = value; } }
    public int MaxZoomLevel { get { return zoomLevelDistances.Length - 1; } }
    public float XOffset { get { return selectedLayer == 1 ? surfaceNoiseSettings.noiseOffset.x : surfaceNoiseSettings2.noiseOffset.x; } set { if (selectedLayer == 1) surfaceNoiseSettings.noiseOffset.x = value; else surfaceNoiseSettings2.noiseOffset.x = value; } }
    public float YOffset { get { return selectedLayer == 1 ? surfaceNoiseSettings.noiseOffset.y : surfaceNoiseSettings2.noiseOffset.y; } set { if (selectedLayer == 1) surfaceNoiseSettings.noiseOffset.y = value; else surfaceNoiseSettings2.noiseOffset.y = value; } }
    public float ZOffset { get { return selectedLayer == 1 ? surfaceNoiseSettings.noiseOffset.z : surfaceNoiseSettings2.noiseOffset.z; } set { if (selectedLayer == 1) surfaceNoiseSettings.noiseOffset.z = value; else surfaceNoiseSettings2.noiseOffset.z = value; } }
    public float Detail { get { return selectedLayer == 1 ? surfaceNoiseSettings.octaves : surfaceNoiseSettings2.octaves; } set { if (selectedLayer == 1) surfaceNoiseSettings.octaves = (int)value; else surfaceNoiseSettings2.octaves = (int)value; } }
    public float Scale { get { return selectedLayer == 1 ? surfaceNoiseSettings.lacunarity : surfaceNoiseSettings2.lacunarity; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.lacunarity = value; else surfaceNoiseSettings2.lacunarity = value; } }
    public float Multiplier { get { return selectedLayer == 1 ? surfaceNoiseSettings.multiplier : surfaceNoiseSettings2.multiplier; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.multiplier = value; else surfaceNoiseSettings2.multiplier = value; } }
    public float Persistence { get { return selectedLayer == 1 ? surfaceNoiseSettings.persistence : surfaceNoiseSettings2.persistence; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.persistence = value; else surfaceNoiseSettings2.persistence = value; } }
    public float LayerStrength { get { return selectedLayer == 1 ? surfaceNoiseSettings.layerStrength : surfaceNoiseSettings2.layerStrength; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.layerStrength = value; else surfaceNoiseSettings2.layerStrength = value; } }
    public float HeightExponent { get { return selectedLayer == 1 ? surfaceNoiseSettings.heightExponent : surfaceNoiseSettings2.heightExponent; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.heightExponent = value; else surfaceNoiseSettings2.heightExponent = value; } }
    public bool Ridged { get { return selectedLayer == 1 ? surfaceNoiseSettings.ridged : surfaceNoiseSettings2.ridged; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.ridged = value; else surfaceNoiseSettings2.ridged = value; } }
    public float DomainWarping { get { return selectedLayer == 1 ? surfaceNoiseSettings.domainWarping : surfaceNoiseSettings2.domainWarping; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.domainWarping = value; else surfaceNoiseSettings2.domainWarping = value; } }
    public int SelectedLayer { get { return selectedLayer; } set { selectedLayer = value; } }

    public TextureSettings()
    {
        Clear();
    }

    public void Clear()
    {
        textureWidth = 512;
        iceColor = new Color32(244, 244, 244, 255);
        iceTemperatureThreshold = 0;
        desertColor = new Color32(192, 226, 142, 255);
        desertThreshold = 10;
        land1Color = new Color32[]
        {
            new Color32(176, 163, 110, 255),
            new Color32(130, 128, 69, 255),
            new Color32(52, 70, 10, 255),
            new Color32(135, 138, 93, 255),
            new Color32(117, 105, 83, 255),
            new Color32(244, 244, 244, 255)
        };
        landColorStages = new float[]
        {
            0.01f,
            0.1f,
            0.3f,
            0.6f,
            0.95f,
            1.0f
        };
        oceanColors = new Color32[]
        {
            new Color32(64, 85, 100, 255),
            new Color32(49, 54, 73, 255),
            new Color32(50, 51, 71, 255)
        };
        oceanStages = new float[]
        {
            0.0f,
            0.5f,
            1.0f
        };
        zoomLevelDistances = new float[] { 200, 95, 47.5f, 23.75f, 11.875f, 5.9375f, float.MinValue };
        textureSteps = null;

        riverTileColor = new Color32(0, 0, 200, 255);

        waterLevel = 0.6f;
        surfaceNoiseSettings = new NoiseSettings();
        surfaceNoiseSettings2 = new NoiseSettings();
        surfaceNoiseSettings2.layerStrength = 0;
        temperatureNoiseSettings = new NoiseSettings();
        humidityNoiseSettings = new NoiseSettings();
        highHumidityLightnessPercentage = 0.2f;
        erosionNoiseMerge = 0.5f;
    }

    public void UpdateSurfaceMaterialProperties(Material surfaceMaterial, bool showTemperature)
    {
        if (surfaceMaterial == null)
            return;

        surfaceMaterial.SetFloat("_TextureWidth", textureWidth);
        surfaceMaterial.SetFloat("_TextureHeight", textureWidth);

        surfaceMaterial.SetFloat("_Seed", surfaceNoiseSettings.seed);
        surfaceMaterial.SetFloat("_XOffset", surfaceNoiseSettings.noiseOffset.x);
        surfaceMaterial.SetFloat("_YOffset", surfaceNoiseSettings.noiseOffset.y);
        surfaceMaterial.SetFloat("_ZOffset", surfaceNoiseSettings.noiseOffset.z);
        if (MapData.instance.LowestHeight <= MapData.instance.HighestHeight)
        {
            surfaceMaterial.SetFloat("_MinHeight", MapData.instance.LowestHeight);
            surfaceMaterial.SetFloat("_MaxHeight", MapData.instance.HighestHeight);
        }
        else
        {
            surfaceMaterial.SetFloat("_MinHeight", MapData.instance.HighestHeight);
            surfaceMaterial.SetFloat("_MaxHeight", MapData.instance.LowestHeight);
        }
        surfaceMaterial.SetFloat("_TemperatureSeed", temperatureNoiseSettings.seed);
        surfaceMaterial.SetFloat("_HumiditySeed", humidityNoiseSettings.seed);
        surfaceMaterial.SetFloat("_Multiplier", surfaceNoiseSettings.multiplier);
        surfaceMaterial.SetInt("_Octaves", surfaceNoiseSettings.octaves);
        surfaceMaterial.SetFloat("_Lacunarity", surfaceNoiseSettings.lacunarity);
        surfaceMaterial.SetFloat("_Persistence", surfaceNoiseSettings.persistence);
        surfaceMaterial.SetFloat("_WaterLevel", waterLevel);
        surfaceMaterial.SetFloat("_DrawType", showTemperature ? 3 : 0);
        surfaceMaterial.SetInt("_RidgedNoise", surfaceNoiseSettings.ridged ? 1 : 0);
        surfaceMaterial.SetFloat("_HeightExponent", surfaceNoiseSettings.heightExponent);
        surfaceMaterial.SetFloat("_LayerStrength", surfaceNoiseSettings.layerStrength);
        surfaceMaterial.SetFloat("_DomainWarping", surfaceNoiseSettings.domainWarping);

        surfaceMaterial.SetFloat("_Seed2", surfaceNoiseSettings2.seed);
        surfaceMaterial.SetFloat("_XOffset2", surfaceNoiseSettings2.noiseOffset.x);
        surfaceMaterial.SetFloat("_YOffset2", surfaceNoiseSettings2.noiseOffset.y);
        surfaceMaterial.SetFloat("_ZOffset2", surfaceNoiseSettings2.noiseOffset.z);
        surfaceMaterial.SetFloat("_Multiplier2", surfaceNoiseSettings2.multiplier);
        surfaceMaterial.SetInt("_Octaves2", surfaceNoiseSettings2.octaves);
        surfaceMaterial.SetFloat("_Lacunarity2", surfaceNoiseSettings2.lacunarity);
        surfaceMaterial.SetFloat("_Persistence2", surfaceNoiseSettings2.persistence);
        surfaceMaterial.SetInt("_RidgedNoise2", surfaceNoiseSettings2.ridged ? 1 : 0);
        surfaceMaterial.SetFloat("_HeightExponent2", surfaceNoiseSettings2.heightExponent);
        surfaceMaterial.SetFloat("_LayerStrength2", surfaceNoiseSettings2.layerStrength);
        surfaceMaterial.SetFloat("_DomainWarping2", surfaceNoiseSettings2.domainWarping);

        surfaceMaterial.SetFloat("_TemperatureExponent", MapData.instance.textureSettings.temperatureExponent);
        surfaceMaterial.SetFloat("_TemperatureRatio", MapData.instance.textureSettings.temperatureRatio);
        surfaceMaterial.SetFloat("_TemperatureElevationRatio", MapData.instance.textureSettings.temperatureElevationRatio);
        surfaceMaterial.SetFloat("_TemperatureWaterDrop", MapData.instance.textureSettings.temperatureWaterDrop);
        surfaceMaterial.SetFloat("_TemperatureLatitudeMultiplier", MapData.instance.textureSettings.temperatureLatitudeMultiplier);
        surfaceMaterial.SetFloat("_TemperatureLatitudeDrop", MapData.instance.textureSettings.temperatureLatitudeDrop);
        surfaceMaterial.SetFloat("_HumidityExponent", MapData.instance.textureSettings.humidityExponent);
        surfaceMaterial.SetFloat("_HumidityMultiplier", MapData.instance.textureSettings.humidityMultiplier);

        int landColorSteps = landColorStages.Length < land1Color.Length ? landColorStages.Length : land1Color.Length;
        if (landColorSteps > 8) landColorSteps = 8;
        surfaceMaterial.SetInt("_ColorSteps", landColorSteps);

        for (int i = 1; i <= 8; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= landColorSteps)
            {
                stage = landColorStages[i - 1];
                color = land1Color[i - 1];
            }
            else
            {
                stage = landColorStages[landColorSteps - 1];
                color = land1Color[landColorSteps - 1];
            }
            surfaceMaterial.SetFloat("_ColorStep" + i, stage);
            surfaceMaterial.SetColor("_Color" + i, color);
        }

        int oceanColorSteps = oceanStages.Length < oceanColors.Length ? oceanStages.Length : oceanColors.Length;
        if (oceanColorSteps > 4) oceanColorSteps = 4;
        surfaceMaterial.SetInt("_OceanColorSteps", oceanColorSteps);

        for (int i = 1; i <= 4; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= oceanColorSteps)
            {
                stage = oceanStages[i - 1];
                color = oceanColors[i - 1];
            }
            else
            {
                stage = oceanStages[oceanColorSteps - 1];
                color = oceanColors[oceanColorSteps - 1];
            }
            surfaceMaterial.SetFloat("_OceanColorStep" + i, stage);
            surfaceMaterial.SetColor("_OceanColor" + i, color);
        }

        surfaceMaterial.SetFloat("_IceTemperatureThreshold1", iceTemperatureThreshold);
        surfaceMaterial.SetFloat("_IceTemperatureThreshold2", iceTemperatureThreshold - iceTransition);
        surfaceMaterial.SetFloat("_DesertThreshold1", desertThreshold);
        surfaceMaterial.SetFloat("_DesertThreshold2", desertThreshold + humidityTransition);
        //surfaceMaterial.SetFloat("_HighHumidityLightnessPercentage", );
        surfaceMaterial.SetColor("_IceColor", iceColor);
        surfaceMaterial.SetColor("_DesertColor", desertColor);
        //surfaceMaterial.SetFloat("_NormalScale", 50);
    }
}
