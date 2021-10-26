using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class InciseFlowSettings
{
    [Range(0, 1)]
    public float strength = 0.5f;
    public float exponent = 10;
    public float amount = 0.5f;
    public float maxFlowStrength = 100;
    public float chiselStrength = 1;
    [Range(0, 1)]
    public float heightInfluence = 0;
    [Range(0, 1)]
    public float minAmount = 0.01f;
    public float upwardWeight = 10;
    public float downwardWeight = 1;
    public float distanceWeight = 10;
    public bool plotRivers = true;
    public Color riverColor = new Color(0, 0, 0.6f);
    public float riverAmount1 = 0;
    public float riverAmount2 = 0.5f;
    public float preBlur = 0;
    public float postBlur = 0;
    public bool plotRiversRandomly = false;
    public int numberOfRivers = 1024;
    public float startingAlpha = 0.5f;
}
