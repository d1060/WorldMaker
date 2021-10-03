using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TextureSettings
{
    public int textureWidth = 256;
    public int textureHeight = 128;
    public Color32 iceColor = new Color32(244, 244, 244, 255);
    [Range(0, -20)]
    public float iceTemperatureThreshold1 = 0;
    [Range(0, -50)]
    public float iceTemperatureThreshold2 = -10;
    public Color32 desertColor = new Color32(192, 226, 142, 255);
    public float desertThreshold1 = 10;
    public float desertThreshold2 = 15;
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
    [Header("Terrain Settings")]
    public NoiseSettings surfaceNoiseSettings;
    public NoiseSettings surfaceNoiseSettings2;
    public NoiseSettings temperatureNoiseSettings;
    public NoiseSettings humidityNoiseSettings;
    [Range(0, 1)]
    public float highHumidityLightnessPercentage = 0.2f;
    [Range(0, 1)]
    public float erosionNoiseMerge = 0.5f;

    int selectedLayer = 1;
    public float[] TextureSteps { get { return textureSteps; } set { textureSteps = value; } }
    public int MaxZoomLevel { get { return zoomLevelDistances.Length - 1; } }
    public int TemperatureNoiseSeed { get; set; }
    public int HumidityNoiseSeed { get; set; }
    public float Detail { get { return selectedLayer == 1 ? surfaceNoiseSettings.octaves : surfaceNoiseSettings2.octaves; } set { if (selectedLayer == 1) surfaceNoiseSettings.octaves = (int)value; else surfaceNoiseSettings2.octaves = (int)value; } }
    public float Scale { get { return selectedLayer == 1 ? surfaceNoiseSettings.lacunarity : surfaceNoiseSettings2.lacunarity; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.lacunarity = value; else surfaceNoiseSettings2.lacunarity = value; } }
    public float Multiplier { get { return selectedLayer == 1 ? surfaceNoiseSettings.multiplier : surfaceNoiseSettings2.multiplier; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.multiplier = value; else surfaceNoiseSettings2.multiplier = value; } }
    public float Persistence { get { return selectedLayer == 1 ? surfaceNoiseSettings.persistence : surfaceNoiseSettings2.persistence; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.persistence = value; else surfaceNoiseSettings2.persistence = value; } }
    public float LayerStrength { get { return selectedLayer == 1 ? surfaceNoiseSettings.layerStrength : surfaceNoiseSettings2.layerStrength; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.layerStrength = value; else surfaceNoiseSettings2.layerStrength = value; } }
    public float HeightExponent { get { return selectedLayer == 1 ? surfaceNoiseSettings.heightExponent : surfaceNoiseSettings2.heightExponent; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.heightExponent = value; else surfaceNoiseSettings2.heightExponent = value; } }
    public bool Ridged { get { return selectedLayer == 1 ? surfaceNoiseSettings.ridged : surfaceNoiseSettings2.ridged; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.ridged = value; else surfaceNoiseSettings2.ridged = value; } }
    public float HeightScale { get { return selectedLayer == 1 ? surfaceNoiseSettings.heightScale : surfaceNoiseSettings2.heightScale; ; } set { if (selectedLayer == 1) surfaceNoiseSettings.heightScale = value; else surfaceNoiseSettings2.heightScale = value; } }
    public int SelectedLayer { get { return selectedLayer; } set { selectedLayer = value; } }

    public void Clear()
    {
        textureWidth = 256;
        textureHeight = 128;
        iceColor = new Color32(244, 244, 244, 255);
        iceTemperatureThreshold1 = 0;
        iceTemperatureThreshold2 = -10;
        desertColor = new Color32(192, 226, 142, 255);
        desertThreshold1 = 10;
        desertThreshold2 = 15;
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
        temperatureNoiseSettings = new NoiseSettings();
        humidityNoiseSettings = new NoiseSettings();
        highHumidityLightnessPercentage = 0.2f;
        erosionNoiseMerge = 0.5f;
    }
}
