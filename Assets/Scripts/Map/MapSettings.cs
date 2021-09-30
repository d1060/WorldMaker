using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class MapSettings
{
    public int Seed = 1473223211;
    public double RadiusInKm = 6000;
    public string HeightMapPath = "";
    public string MainTexturePath = "";
    public string LandMaskPath = "";
    public bool UseImages;

    public void Clear()
    {
        Seed = 1473223211;
        RadiusInKm = 6000;
        HeightMapPath = "";
        MainTexturePath = "";
        LandMaskPath = "";
        UseImages = false;
    }
}
