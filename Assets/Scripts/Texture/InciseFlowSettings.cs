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
    [Range(0, 1)]
    public float minAmount = 0.01f;
}
