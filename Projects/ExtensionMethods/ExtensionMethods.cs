using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum POLAR_ROUNDING_TYPE
{
    NONE,
    DOWN,
    UP
};

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

    public static Vector2 CartesianToPolarDegrees(this Vector3 cartesian, float radius)
    {
        cartesian.Normalize();
        cartesian *= radius;

        Vector2 retVal;
        float xzAtan2 = 0;

        if (cartesian.x == 0)
        {
            if (cartesian.z > 0)
                xzAtan2 = Mathf.PI / 2.0f;
            else
                xzAtan2 = -Mathf.PI / 2.0f;
        }
        else
            xzAtan2 = Mathf.Atan2(cartesian.z, cartesian.x);

        retVal.x = xzAtan2;

        retVal.y = Mathf.Asin(cartesian.y / radius);

        retVal.x *= Mathf.Rad2Deg;
        retVal.y *= Mathf.Rad2Deg;

        return retVal;
    }

    public static Vector2 CartesianToPolarRatio(this Vector3 cartesian, float radius)
    {
        Vector2 polar = cartesian.CartesianToPolarDegrees(radius);
        polar.x /= 360;
        polar.y /= 180;
        polar.x += 0.5f;
        polar.y += 0.5f;
        return polar;
    }

    public static Vector2 CartesianToPolarRatio(this Vector3 cartesian, float radius, POLAR_ROUNDING_TYPE rounding)
    {
        Vector2 polar = cartesian.CartesianToPolarDegrees(radius);
        polar.x /= 360;
        polar.y /= 180;
        polar.x += 0.5f;
        polar.y += 0.5f;
        if (rounding == POLAR_ROUNDING_TYPE.DOWN && polar.x >= 1)
            polar.x -= 1;
        else if (rounding == POLAR_ROUNDING_TYPE.UP && polar.x <= 0)
            polar.x += 1;
        return polar;
    }

    public static Vector3 PolarRatioToCartesian(this Vector2 polar, float radius)
    {
        polar.x -= 0.5f;
        polar.y -= 0.5f;
        polar.x *= 360;
        polar.y *= 180;

        float a = radius * Mathf.Cos(polar.y * Mathf.Deg2Rad);
        float y = radius * Mathf.Sin(polar.y * Mathf.Deg2Rad);
        float z = a * Mathf.Sin(polar.x * Mathf.Deg2Rad);
        float x = a * Mathf.Cos(polar.x * Mathf.Deg2Rad);

        return new Vector3(x, y, z);
    }

    public static Vector3 PolarDegreesToCartesian(this Vector2 polar, float radius)
    {
        float a = radius * Mathf.Cos(polar.y * Mathf.Deg2Rad);
        float y = (float)System.Math.Round(radius * Mathf.Sin(polar.y * Mathf.Deg2Rad), 7);
        float z = (float)System.Math.Round(a * Mathf.Sin(polar.x * Mathf.Deg2Rad), 7);
        float x = (float)System.Math.Round(a * Mathf.Cos(polar.x * Mathf.Deg2Rad), 7);

        return new Vector3(x, y, z);
    }

    public static Vector3 CartesianToCubemap(this Vector3 cartesian)
    {
        Vector3 cubeMap = new Vector3();

        float tanZx = cartesian.z / cartesian.x;
        float tanYx = cartesian.y / cartesian.x;

        float tanZy = cartesian.z / cartesian.y;
        float tanXy = cartesian.x / cartesian.y;

        float tanXz = cartesian.x / cartesian.z;
        float tanYz = cartesian.y / cartesian.z;

        if (cartesian.x < 0 && (tanYx >= -1 && tanYx <= 1) && (tanZx >= -1 && tanZx <= 1)) // neg_x - Left
        {
            cubeMap.x = (1 - tanZx) / 2;
            cubeMap.y = (1 - tanYx) / 2;
            cubeMap.z = 0;
        }
        else if (cartesian.y < 0 && (tanXy >= -1 && tanXy <= 1) && (tanZy >= -1 && tanZy <= 1)) // neg_y - Bottom
        {
            cubeMap.x = (1 - tanXy) / 2;
            cubeMap.y = (1 - tanZy) / 2;
            cubeMap.z = 5;
        }
        else if (cartesian.z < 0 && (tanYz >= -1 && tanYz <= 1) && (tanXz >= -1 && tanXz <= 1)) // neg_z - Back
        {
            cubeMap.x = (tanXz + 1) / 2;
            cubeMap.y = (1 - tanYz) / 2;
            cubeMap.z = 3;
        }
        else if (cartesian.z > 0 && (tanYz >= -1 && tanYz <= 1) && (tanXz >= -1 && tanXz <= 1)) // pos_z - Front
        {
            cubeMap.x = (tanXz + 1) / 2;
            cubeMap.y = (tanYz + 1) / 2;
            cubeMap.z = 1;
        }
        else if (cartesian.x > 0 && (tanYx >= -1 && tanYx <= 1) && (tanZx >= -1 && tanZx <= 1)) // pos_x - Right
        {
            cubeMap.x = (1 - tanZx) / 2;
            cubeMap.y = (tanYx + 1) / 2;
            cubeMap.z = 2;
        }
        else if (cartesian.y > 0 && (tanXy >= -1 && tanXy <= 1) && (tanZy >= -1 && tanZy <= 1)) // pos_y - Top
        {
            cubeMap.x = (tanXy + 1) / 2;
            cubeMap.y = (1 - tanZy) / 2;
            cubeMap.z = 4;
        }
        return cubeMap;
    }

    static public void SaveToFile(this RenderTexture renderTexture, string filePath)
    {
        Texture2D tex;
        tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false, true);
        var oldRt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = oldRt;
        try
        {
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
        }
        catch
        {

        }

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(tex);
        else
            UnityEngine.Object.DestroyImmediate(tex);
    }
}
