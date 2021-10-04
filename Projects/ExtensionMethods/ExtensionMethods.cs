using System;
using System.IO;
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

    public static void WriteBinary(this Vector3 vector, BinaryWriter writer)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    public static Vector3 ReadBinary(this Vector3 vector, BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        vector.Set(x, y, z);
        return vector;
    }
}
