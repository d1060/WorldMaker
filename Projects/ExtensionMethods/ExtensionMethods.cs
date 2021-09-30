using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static partial class ExtensionMethods
{
    //static readonly int FloatingPointPrecision = 7;
    public static float Round(this float f)
    {
        float rounded = Mathf.Round(f * 100000) / 100000;
        return rounded;
    }

    public static Vector3 Round(this Vector3 v)
    {
        Vector3 vRounded = new Vector3(v.x, v.y, v.z);
        vRounded.x = vRounded.x.Round();
        vRounded.y = vRounded.y.Round();
        vRounded.z = vRounded.z.Round();
        return vRounded;
    }

    public static void AddUnique<T>(this List<T> l, T val)
    {
        if (!l.Contains(val))
            l.Add(val);
    }
}
