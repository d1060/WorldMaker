using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppData
{
    static public readonly string baseAppDataFile = "WorldGen.AppData.json";
    public bool KeepSeedOnRegenerate = true;
    public bool AutoRegenerate = false;
    public bool SaveMainMap = true;
    public bool SaveHeightMap = true;
    public bool SaveLandMask = true;
    public bool SaveNormalMap = true;
    public bool SaveSpecularMap = true;
    public bool SaveTemperature = true;
    public bool SaveRivers = true;
    public string LastSavedImageFolder = "";
    public List<string> RecentWorlds = new List<string>();
    int MaxRecentWorlds = 8;

    #region Singleton
    static AppData myInstance = null;

    AppData()
    {
    }

    public static AppData instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new AppData();
            return myInstance;
        }
    }
    #endregion

    public void Save()
    {
        string json = JsonUtility.ToJson(this, true);
        string filePath = Path.Combine(Application.persistentDataPath, baseAppDataFile);
        System.IO.File.WriteAllText(filePath, json);
    }

    public bool Load()
    {
        //#if DEBUG
        //#else
        string filePath = Path.Combine(Application.persistentDataPath, baseAppDataFile);

        try
        {
            if (File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                AppData ad = JsonUtility.FromJson<AppData>(json);
                KeepSeedOnRegenerate = ad.KeepSeedOnRegenerate;
                SaveMainMap = ad.SaveMainMap;
                SaveHeightMap = ad.SaveHeightMap;
                SaveLandMask = ad.SaveLandMask;
                SaveNormalMap = ad.SaveNormalMap;
                SaveSpecularMap = ad.SaveSpecularMap;
                SaveTemperature = ad.SaveTemperature;
                SaveRivers = ad.SaveRivers;
                LastSavedImageFolder = ad.LastSavedImageFolder;
                AutoRegenerate = ad.AutoRegenerate;
                RecentWorlds = ad.RecentWorlds;
                return true;
            }
        }
        catch (Exception)
        {

        }
        return false;
        //#endif
    }

    public void AddRecentWorld(string fileName)
    {
        if (AppData.instance.RecentWorlds.Contains(fileName))
        {
            AppData.instance.RecentWorlds.Remove(fileName);
            AppData.instance.RecentWorlds.Insert(0, fileName);
        }
        else if (AppData.instance.RecentWorlds.Count < MaxRecentWorlds)
        {
            AppData.instance.RecentWorlds.Insert(0, fileName);
        }
        else if (!AppData.instance.RecentWorlds.Contains(fileName))
        {
            AppData.instance.RecentWorlds.RemoveAt(AppData.instance.RecentWorlds.Count - 1);
            AppData.instance.RecentWorlds.Insert(0, fileName);
        }
    }

    public void RemoveRecentWorld(string fileName)
    {
        if (AppData.instance.RecentWorlds.Contains(fileName))
        {
            AppData.instance.RecentWorlds.Remove(fileName);
        }
    }
}
