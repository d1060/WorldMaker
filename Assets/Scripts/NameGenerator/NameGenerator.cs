using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NameGenerator
{
    static readonly string baseMapDataFile = "NameGenerator.json";
    public NameGeneratorType[] Types;
    int seed = 2345908;
    System.Random random;

    #region Singleton
    static NameGenerator myInstance = null;

    NameGenerator()
    {
    }

    public void SetSeed(int seed)
    {
        this.seed = seed;
        random = new System.Random(seed);
    }

    public static NameGenerator instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new NameGenerator();
            return myInstance;
        }
    }
    #endregion

    public void Load()
    {
        try
        {
            // Saves for test.
            string filePath = Path.Combine(Application.streamingAssetsPath, baseMapDataFile);

            if (File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                NameGenerator ng = JsonUtility.FromJson<NameGenerator>(json);
                Types = ng.Types;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    public string GetName(NameGeneratorType.Types type = NameGeneratorType.Types.Town, System.Random nameRandom = null)
    {
        List<NameGeneratorType> applicableTypes = new List<NameGeneratorType>();
        foreach (NameGeneratorType nameGeneratorType in Types)
        {
            if (nameGeneratorType.Type == type)
                applicableTypes.Add(nameGeneratorType);
        }

        if (applicableTypes.Count == 0)
            return "";

        NameGeneratorType selectedNameGeneratorType = applicableTypes[0];

        System.Random randomToUse = random;
        if (nameRandom != null)
            randomToUse = nameRandom;

        if (applicableTypes.Count > 1)
        {
            int typesIndex = (int)(randomToUse.NextDouble() * applicableTypes.Count);
            selectedNameGeneratorType = applicableTypes[typesIndex];
        }

        return selectedNameGeneratorType.GetName(randomToUse);
    }
}
