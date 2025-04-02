using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ColorSchemes
{
    static readonly string colorSchemesFolder = "ColorSchemes";
    static readonly string colorSchemesFile = "Color.schemes";
    Dictionary<string, List<ColorScheme>> colorSchemes = new Dictionary<string, List<ColorScheme>>();

    public Dictionary<string, List<ColorScheme>> Schemes { get { return colorSchemes; } set { colorSchemes = value; } }

    #region Singleton
    static ColorSchemes myInstance = null;

    ColorSchemes()
    {
    }

    public static ColorSchemes instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new ColorSchemes();
            return myInstance;
        }
    }
    #endregion

    public void Load()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, colorSchemesFolder);
        using (BinaryReader reader = new BinaryReader(File.Open(Path.Combine(Application.streamingAssetsPath, colorSchemesFolder, colorSchemesFile), FileMode.Open)))
        {
            int schemesListCount = reader.ReadInt32();
            for (int i = 0; i < schemesListCount; i++)
            {
                string schemeName = reader.ReadString();
                List<ColorScheme> colorSchemeList = new List<ColorScheme>();
                int colorSchemesCount = reader.ReadInt32(); // Number of color schemes in this file.
                for (int a = 0; a < colorSchemesCount; a++)
                {
                    ColorScheme cs = new ColorScheme();
                    cs.Index = a;
                    cs.ReadBinary(reader);
                    colorSchemeList.Add(cs);
                }

                colorSchemes.Add(schemeName, colorSchemeList);
            }
        }

        //string[] clutFiles = Directory.GetFiles(folderPath, "*.clut");
        //foreach (string file in clutFiles)
        //{
        //    string schemeName = Path.GetFileNameWithoutExtension(file);
        //    List<ColorScheme> colorSchemeList = new List<ColorScheme>();

        //    using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
        //    {
        //        int count = reader.ReadInt32(); // Number of color schemes in this file.

        //        for (int i = 0; i < count; i++)
        //        {
        //            ColorScheme cs = new ColorScheme();
        //            cs.Index = i;
        //            cs.ReadBinary(reader);
        //            colorSchemeList.Add(cs);
        //        }
        //    }

        //    colorSchemes.Add(schemeName, colorSchemeList);
        //}

        //using (BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(Application.streamingAssetsPath, colorSchemesFolder, colorSchemesFile), FileMode.Create)))
        //{
        //    writer.Write(colorSchemes.Count);
        //    List<string> schemeNames = colorSchemes.Keys.ToList();
        //    for (int i = 0; i < colorSchemes.Count; i++)
        //    {
        //        string schemeName = schemeNames[i];
        //        writer.Write(schemeName);

        //        List<ColorScheme> schemes = colorSchemes[schemeName];
        //        writer.Write(schemes.Count);

        //        foreach (ColorScheme cs in schemes)
        //        {
        //            cs.WriteBinary(writer);
        //        }
        //    }
        //}
    }

    public ColorScheme GetColorScheme(string label, int index)
    {
        if (colorSchemes == null || colorSchemes.Count == 0)
            return null;

        if (colorSchemes.ContainsKey(label) && colorSchemes[label].Count > index)
        {
            return colorSchemes[label].ElementAt(index);
        }

        if (!colorSchemes.ContainsKey(label))
        {
            return colorSchemes.ElementAt(0).Value.ElementAt(0);
        }

        if (colorSchemes[label].Count <= index)
        {
            return colorSchemes[label].ElementAt(0);
        }

        return null;
    }

    public float ActivateLabel(string label)
    {
        var keys = colorSchemes.Keys;
        float minHeight = 0;

        foreach (string key in keys)
        {
            List<ColorScheme> schemes = colorSchemes[key];
            foreach (ColorScheme cs in schemes)
            {
                cs.SetActive(key == label);
                if (key == label)
                {
                    RectTransform rectTransform = cs.GameObject.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float height = rectTransform.anchoredPosition.y - rectTransform.sizeDelta.y;
                        if (height < minHeight)
                            minHeight = height;
                    }
                }
            }
        }

        return minHeight;
    }
}
