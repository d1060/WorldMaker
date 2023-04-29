using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MapData
{
    static public readonly string baseMapDataFile = "WorldGen.json";
    string mapDataFile = "";
    public string WorldName;
    public TextureSettings textureSettings;
    public MapSettings mapSettings;
    public ErosionSettings erosionSettings;
    public PlotRiversSettings plotRiversSettings;
    public InciseFlowSettings inciseFlowSettings;
    public bool IsSaved = false;
    public float LowestHeight = 0.15f;
    public float HighestHeight = 0.5f;
    public float TemperatureExponent = 2.718281f;
    public float TemperatureRatio = 16;
    public float TemperatureElevationRatio = 6;
    public float TemperatureWaterDrop = 2;
    public float TemperatureLatitudeMultiplier = 30;
    public float TemperatureLatitudeDrop = 0;
    public float HumidityExponent = 2.718281f;
    public float HumidityMultiplier = 11;

    #region Singleton
    static MapData myInstance = null;

    MapData()
    {
    }

    public static MapData instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new MapData();
            return myInstance;
        }
    }
    #endregion

    public string DataFile { get { return mapDataFile; } }

    public void Clear()
    {
        WorldName = "";
        textureSettings.Clear();
        mapSettings.Clear();
    }

    public void Save(string filePath = "")
    {
        string json = JsonUtility.ToJson(this, true);
        if (filePath == "")
            filePath = Path.Combine(Application.persistentDataPath, baseMapDataFile);
        System.IO.File.WriteAllText(filePath, json);
    }

    public bool Load(string filePath = "")
    {
//#if DEBUG
//#else
        if (filePath == "" || filePath == null)
            filePath = Path.Combine(Application.persistentDataPath, baseMapDataFile);

        try
        {
            if (File.Exists(filePath))
            {
                mapDataFile = filePath;
                string json = System.IO.File.ReadAllText(filePath);
                MapData md = JsonUtility.FromJson<MapData>(json);
                WorldName = md.WorldName;
                textureSettings = md.textureSettings;
                mapSettings = md.mapSettings;
                erosionSettings = md.erosionSettings;
                plotRiversSettings = md.plotRiversSettings;
                inciseFlowSettings = md.inciseFlowSettings;
                LowestHeight = md.LowestHeight;
                HighestHeight = md.HighestHeight;
                TemperatureExponent = md.TemperatureExponent;
                TemperatureRatio = md.TemperatureRatio;
                TemperatureElevationRatio = md.TemperatureElevationRatio;
                TemperatureWaterDrop = md.TemperatureWaterDrop;
                TemperatureLatitudeMultiplier = md.TemperatureLatitudeMultiplier;
                TemperatureLatitudeDrop = md.TemperatureLatitudeDrop;
                HumidityExponent = md.HumidityExponent;
                HumidityMultiplier = md.HumidityMultiplier;
                return true;
            }
        }
        catch (Exception)
        {

        }
        return false;
        //#endif
    }
}
