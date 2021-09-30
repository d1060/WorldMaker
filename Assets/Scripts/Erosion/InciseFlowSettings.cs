using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class InciseFlowSettings
{
    public int numIterations = 1024;
    public Color riverColor = new Color(0, 0, 1, 1);
    public float heightWeight = 500;
    [Range(0, 1)]
    public float startingAlpha = 0.5f;
    public float alphaStep = (2 / 255f);
    public float flowHeightDelta = 0.01f;
    [Range(0, 10)]
    public float brushSize = 3;
    [Range(0, 3)]
    public float brushExponent = 1.5f;

    public void Clear()
    {
        numIterations = 1024;
        riverColor = new Color(0, 0, 1, 1);
        heightWeight = 500;
        startingAlpha = 0.5f;
        alphaStep = (2 / 255f);
        flowHeightDelta = 0.01f;
        brushSize = 3;
        brushExponent = 1.5f;
    }
}
