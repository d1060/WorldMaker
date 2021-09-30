using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ErosionSettings
{
    public int numErosionIterations = 600000;
    public int erosionBrushRadius = 3;
    public ComputeShader erosion;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    [Range(0, 1)]
    public float depositSpeed = 0.3f;
    [Range(0, 1)]
    public float erodeSpeed = 0.3f;
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;

    public void Clear()
    {
         numErosionIterations = 600000;
         erosionBrushRadius = 3;
         maxLifetime = 30;
         sedimentCapacityFactor = 3;
         minSedimentCapacity = .01f;
         depositSpeed = 0.3f;
         erodeSpeed = 0.3f;
         evaporateSpeed = .01f;
         gravity = 4;
         startSpeed = 1;
         startWater = 1;
         inertia = 0.3f;
    }
}
