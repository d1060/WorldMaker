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
    public bool ExportAsCubemap = false;
    public bool TransparentOceans = false;
    public int CubemapDimension = 256;
    public int CubemapDivisions = 2;
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
                ExportAsCubemap = ad.ExportAsCubemap;
                CubemapDimension = ad.CubemapDimension;
                CubemapDivisions = ad.CubemapDivisions;
                TransparentOceans = ad.TransparentOceans;
                RemoveRepeatedRecentWorlds();
                return true;
            }
        }
        catch (Exception)
        {

        }
        return false;
        //#endif
    }

    void RemoveRepeatedRecentWorlds()
    {
        if (RecentWorlds == null || RecentWorlds.Count < 2)
            return;

        for (int i = 0; i < RecentWorlds.Count; i++)
        {
            for (int a = i + 1; a < RecentWorlds.Count; a++)
            {
                if (RecentWorlds[i] == RecentWorlds[a])
                {
                    RecentWorlds.RemoveAt(a);
                    a--;
                }
            }
        }
    }

    public void AddRecentWorld(string fileName)
    {
        if (RecentWorlds.Contains(fileName))
        {
            RecentWorlds.Remove(fileName);
            RecentWorlds.Insert(0, fileName);
        }
        else if (RecentWorlds.Count < MaxRecentWorlds)
        {
            RecentWorlds.Insert(0, fileName);
        }
        else if (!RecentWorlds.Contains(fileName))
        {
            RecentWorlds.RemoveAt(RecentWorlds.Count - 1);
            RecentWorlds.Insert(0, fileName);
        }
    }

    public void RemoveRecentWorld(string fileName)
    {
        if (RecentWorlds.Contains(fileName))
        {
            RecentWorlds.Remove(fileName);
        }
    }
}
