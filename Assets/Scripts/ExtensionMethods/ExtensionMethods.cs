using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum POLAR_ROUNDING_TYPE
{
    NONE,
    DOWN,
    UP
};

public static partial class ExtensionMethods
{
    static readonly int FloatingPointPrecision = 7;
    static readonly Vector3 horizontalPlaneNormal = new Vector3(0, 1, 0);
    static readonly Vector3 southPole = new Vector3(0, -1, 0);
    static readonly Plane horizontalPlane = new Plane(horizontalPlaneNormal, new Vector3(0, 0, 0));

    public static int ToInt(this string s)
    {
        int i = -1;
        if (int.TryParse(s, out i))
        {
            return i;
        }
        return int.MinValue;
    }

    public static float ToFloat(this string s)
    {
        float i = -1;
        if (float.TryParse(s, out i))
        {
            return i;
        }
        return float.MinValue;
    }

    public static float Max(this float[] a)
    {
        float highest = float.MinValue;
        foreach (float v in a)
        {
            if (v > highest)
                highest = v;
        }
        return highest;
    }

    public static void MoveForward(this Transform t, float amount)
    {
        Vector3 forward = t.forward;
        forward *= amount;
        t.position += forward;
    }

    public static Vector3 ReduceBy(this Vector3 v, float amount)
    {
        float magnitude = v.magnitude;
        v.Normalize();
        float newMagnitude = magnitude - amount;
        if (newMagnitude < 0)
        {
            v = Vector3.zero - v;
            v *= -newMagnitude;
        }
        else
        {
            v *= newMagnitude;
        }
        return v;
    }

    public static bool IsAlmost(this float f, float f2)
    {
        return f >= f2 - 0.00001f && f <= f2 + 0.00001f;
    }

    public static double GetClosestMultipleOf(this double d, double unit)
    {
        if (d <= unit)
            return d;

        int multiples = (int)System.Math.Round(d / unit, 0);
        double val = multiples * unit;
        return val;
    }

    public static int GetLogBase2(this int num)
    {
        if (num < 2) return 0;
        else if (num < 4) return 1;
        else if (num < 8) return 2;
        else if (num < 16) return 3;
        else if (num < 32) return 4;
        else if (num < 64) return 5;
        else if (num < 128) return 6;
        else if (num < 256) return 7;
        else if (num < 512) return 8;
        else if (num < 1024) return 9;
        else if (num < 2048) return 10;
        else if (num < 4096) return 11;
        else if (num < 8192) return 12;
        else if (num < 16384) return 13;
        else if (num < 16384) return 14;
        else if (num < 32768) return 15;
        else if (num < 65536) return 16;
        else if (num < 131072) return 17;
        else if (num < 262144) return 18;
        else if (num < 524288) return 19;
        else if (num < 1048576) return 20;
        else if (num < 2097152) return 21;
        else if (num < 4194304) return 22;
        else if (num < 8388608) return 23;
        else if (num < 16777216) return 24;
        else if (num < 33554432) return 25;
        else if (num < 67108864) return 26;
        else if (num < 134217728) return 27;
        else if (num < 268435456) return 28;
        else if (num < 536870912) return 29;
        else if (num < 1073741824) return 30;
        return 31;
    }

    public static int GetPowerOf2(this int num)
    {
        if (num < 2) return 1;
        else if (num < 4) return 2;
        else if (num < 8) return 4;
        else if (num < 16) return 8;
        else if (num < 32) return 16;
        else if (num < 64) return 32;
        else if (num < 128) return 64;
        else if (num < 256) return 128;
        else if (num < 512) return 256;
        else if (num < 1024) return 512;
        else if (num < 2048) return 1024;
        else if (num < 4096) return 2048;
        else if (num < 8192) return 4096;
        else if (num < 16384) return 8192;
        else if (num < 16384) return 16384;
        else if (num < 32768) return 16384;
        else if (num < 65536) return 32768;
        else if (num < 131072) return 65536;
        else if (num < 262144) return 131072;
        else if (num < 524288) return 262144;
        else if (num < 1048576) return 524288;
        else if (num < 2097152) return 1048576;
        else if (num < 4194304) return 2097152;
        else if (num < 8388608) return 4194304;
        else if (num < 16777216) return 8388608;
        else if (num < 33554432) return 16777216;
        else if (num < 67108864) return 33554432;
        else if (num < 134217728) return 67108864;
        else if (num < 268435456) return 134217728;
        else if (num < 536870912) return 268435456;
        else if (num < 1073741824) return 536870912;
        return 1073741824;
    }

    public static bool IsInsidePlanes(this Vector3 position, Plane[] planes)
    {
        for (int i = 0; i <= 3; i++) // Left 0, Right 1, Down 2, Up 3, Near 4, Far 5
        {
            Plane plane = planes[i];
            if (plane.GetDistanceToPoint(position) < 0)
                return false;
        }
        return true;
    }

    public static Line3d IntersectionWith(this Plane p1, Plane p2)
    {
        Vector3 lineVec = Vector3.Cross(p1.normal, p2.normal);
        Vector3 linePoint = Vector3.zero;

        Vector3 ldir = Vector3.Cross(p2.normal, lineVec);

        float numerator = Vector3.Dot(p1.normal, ldir);

        //Prevent divide by zero.
        if (Mathf.Abs(numerator) > 0.000001f)
        {
            Vector3 p1Position = p1.normal;
            Vector3 p2Position = p2.normal;

            p1Position *= -p1.distance;
            p2Position *= -p2.distance;

            Vector3 plane1ToPlane2 = p1Position - p2Position;
            float t = Vector3.Dot(p1.normal, plane1ToPlane2) / numerator;
            linePoint = p2Position + t * ldir;
        }

        Line3d line = new Line3d();
        line.Point = linePoint;
        line.Vector = lineVec;
        return line;
    }

    public static bool ContainsCloseTo(this List<Vector2> lv, Vector2 v, double tolerance)
    {
        foreach (Vector2 v2 in lv)
        {
            if (v2.x >= v.x - tolerance &&
                v2.x <= v.x + tolerance &&
                v2.y >= v.y - tolerance &&
                v2.y <= v.y + tolerance)
                return true;
        }
        return false;
    }

    public static void AddMultipleOf(this List<Vector2> lv, Vector2 v, double step)
    {
        Vector2 v2 = new Vector2(v.x, v.y);
        v2.x = (float)((int)(v2.x / step) * step);
        v2.y = (float)((int)(v2.y / (step*2)) * (step*2));
        lv.Add(v2);
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
        float y = (float)System.Math.Round(radius * Mathf.Sin(polar.y * Mathf.Deg2Rad), FloatingPointPrecision);
        float z = (float)System.Math.Round(a * Mathf.Sin(polar.x * Mathf.Deg2Rad), FloatingPointPrecision);
        float x = (float)System.Math.Round(a * Mathf.Cos(polar.x * Mathf.Deg2Rad), FloatingPointPrecision);

        return new Vector3(x, y, z);
    }

    //public static Vector2 PolarRatioToStereographic(this Vector2 polar)
    //{
    //    // x is longitude. y is latitude.
    //    float angleInRads = polar.x * Mathf.PI * 2;
    //    float x = Mathf.Cos(angleInRads);
    //    float y = Mathf.Sin(angleInRads);

    //    // Adjusts for latitude (distance from Center)
    //    float distance = 1 - polar.y;
    //    x *= distance;
    //    y *= distance;

    //    return new Vector2(x, y);
    //}

    public static Vector2 CartesianToStereographic(this Vector3 cartesian)
    {
        Vector3 line = cartesian - southPole;
        line.Normalize();
        // Horizontal plane equation is P => 0x + 1y + 0z = 0

        float t = 1 / line.y;
        //float t = (Vector3.Dot(horizontalPlaneNormal, Vector3.zero) - Vector3.Dot(horizontalPlaneNormal, southPole)) / Vector3.Dot(horizontalPlaneNormal, line);
        Vector3 projection = southPole + (line * t);
        return new Vector2(projection.x, projection.z);
    }

    public static Vector3 StereographicToCartesian(this Vector2 stereographic)
    {
        Vector3 line = new Vector3(stereographic.x, 0, stereographic.y) - southPole;
        line.Normalize();
        // Unit sphere equation is x2 + y2 + z2 = 1
        float b = -2 * line.y;

        float t = -b;
        line *= t;
        return southPole + line;
    }

    //public static Vector2 StereographicToPolarRatio(this Vector2 stereographic)
    //{
    //    float distance = Mathf.Sqrt(stereographic.x * stereographic.x + stereographic.y * stereographic.y);
    //    float y = 1 - distance;

    //    float atan2 = Mathf.Atan2(stereographic.y, stereographic.x);
    //    float x = atan2 / (2 * Mathf.PI);

    //    if (x < 0) x += 1;
    //    else if (x > 1) x -= 1;

    //    return new Vector2(x, y);
    //}

    //public static Vector2 StereographicToPolarRatioWithoutAdjustments(this Vector2 stereographic)
    //{
    //    float distance = Mathf.Sqrt(stereographic.x * stereographic.x + stereographic.y * stereographic.y);
    //    float y = 1 - distance;

    //    float atan2 = Mathf.Atan2(stereographic.y, stereographic.x);
    //    float x = atan2 / (2 * Mathf.PI);

    //    return new Vector2(x, y);
    //}

    public static Vector2 FlatMapCoordinatesToPolarRatio(this Vector3 flatMap, float mapWidth, float mapHeight)
    {
        return new Vector2((flatMap.x + mapWidth / 2) / mapWidth, (flatMap.y + mapHeight / 2) / mapHeight);
    }

    public static Vector3 PolarRatioToFlatMapCoordinates(this Vector2 polar, float mapWidth, float mapHeight)
    {
        float mapX = polar.x * mapWidth - (mapWidth / 2);
        float mapY = polar.y * mapHeight - (mapHeight / 2);

        Vector3 coord = new Vector3(mapX, mapY, -2);
        return coord;
    }

    public static Vector3 Average(this Vector3[] collection)
    {
        Vector3 avgVec = Vector3.zero;
        foreach (Vector3 v in collection)
        {
            avgVec += v;
        }
        avgVec /= collection.Length;
        return avgVec;
    }

    public static void Save(this RenderTexture renderTexture, string file)
    {
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        System.IO.File.WriteAllBytes(file, texture.EncodeToPNG());
        UnityEngine.Object.Destroy(texture);
    }

    public static Component GetChildWithName(this Component obj, string name)
    {
        Transform trans = obj.transform;
        Transform childTrans = trans.Find(name);
        if (childTrans != null)
        {
            return childTrans;
        }
        else
        {
            return null;
        }
    }

    public static Transform GetChildTransformNamed(this Component obj, string name)
    {
        Transform trans = obj.transform;
        Transform childTrans = trans.Find(name);
        if (childTrans != null)
        {
            return childTrans;
        }
        else
        {
            return null;
        }
    }

    public static List<Transform> GetAllChildrenNamed(this Transform transform, string name)
    {
        List<Transform> children = new List<Transform>();
        foreach(Transform child in transform)
        {
            if (child.name.Contains(name))
                children.Add(child);
        }
        return children;
    }

    public static void SaveAsPNG(this Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
    }
}
