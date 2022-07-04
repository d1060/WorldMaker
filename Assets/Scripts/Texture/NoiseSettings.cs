using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class NoiseSettings
{
    public float seed = 1.324653f;
    [Range(1, 40)]
    public int octaves = 8;
    [Range(0.1f, 2.0f)]
    public float persistence = 0.9f;
    [Range(0.1f, 50)]
    public float multiplier = 4;
    [Range(0.1f, 1.0f)]
    public float amplitude = 0.5f;
    [Range(0.1f, 4.0f)]
    public float lacunarity = 2;
    [Range(0, 1)]
    public float layerStrength = 1;
    [Range(0, 5)]
    public float heightExponent = 1;
    public Vector3 noiseOffset;
    public bool ridged = false;
    public float domainWarping = 1;
}
