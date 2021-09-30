using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class NoiseSettings
{
    [Range(1, 40)]
    public int octaves = 8;
    [Range(0.1f, 2.0f)]
    public float persistence = 0.9f;
    [Range(0.1f, 50)]
    public float multiplier = 25;
    [Range(0.1f, 1.0f)]
    public float amplitude = 0.5f;
    [Range(0.1f, 4.0f)]
    public float lacunarity = 2;
    public Vector3 noiseOffset;
    public bool ridged = false;
}
