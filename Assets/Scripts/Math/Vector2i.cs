using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public struct Vector2i
{
    public static Vector2i zero;

    public int x;
    public int y;

    float myMagnitude;
    public float magnitude
    {
        get
        {
            if (float.IsNaN(myMagnitude))
            {
                myMagnitude = (float)Math.Sqrt(x * x + y * y);
            }

            return myMagnitude;
        }
    }

    public float sqrMagnitude
    {
        get { return x * x + y * y; }
    }

    public Vector2i(int x, int y)
    {
        this.x = x;
        this.y = y;
        myMagnitude = float.NaN;
    }

    public Vector2i(float x, float y)
    {
        this.x = (int)x;
        this.y = (int)y;
        myMagnitude = float.NaN;
    }

    public Vector2i(Vector2i v)
    {
        this.x = v.x;
        this.y = v.y;
        myMagnitude = float.NaN;
    }

    public Vector2i(Vector2 v)
    {
        this.x = (int)v.x;
        this.y = (int)v.y;
        myMagnitude = float.NaN;
    }

    public Vector2i(Vector3 v)
    {
        this.x = (int)v.x;
        this.y = (int)v.y;
        myMagnitude = float.NaN;
    }

    public static Vector2i operator +(Vector2i v, Vector2i v2)
    {
        Vector2i v3 = new Vector2i(v.x + v2.x, v.y + v2.y);
        return v3;
    }

    public static Vector2i operator -(Vector2i v, Vector2i v2)
    {
        Vector2i v3 = new Vector2i(v.x - v2.x, v.y - v2.y);
        return v3;
    }

    public static Vector2i operator /(Vector2i v, float div)
    {
        Vector2i v2 = new Vector2i(v.x, v.y);
        v2.x = (int)(v2.x / div);
        v2.y = (int)(v2.y / div);

        return v2;
    }

    public static Vector2i operator *(Vector2i v, float div)
    {
        Vector2i v2 = new Vector2i(v.x, v.y);
        v2.x = (int)(v2.x * div);
        v2.y = (int)(v2.y * div);

        return v2;
    }

    public static implicit operator Vector2i(Vector2 v2)
    {
        Vector2i v = new Vector2i(v2);
        return v;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Vector2i))
            return false;

        Vector2i mys = (Vector2i)obj;
        // compare elements here
        return mys.x == x && mys.y == y;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = unchecked((521 * x.GetHashCode()) + (523 * y.GetHashCode()));
            return hash;
        }
    }

    public static bool operator ==(Vector2i v, Vector2i v2)
    {
        return v.x == v2.x && v.y == v2.y;
    }
    public static bool operator !=(Vector2i v, Vector2i v2)
    {
        return v.x != v2.x || v.y != v2.y;
    }

    public static implicit operator Vector2i(Vector3 v3)
    {
        Vector2i v2 = new Vector2i((int)v3.x, (int)v3.y);
        return v2;
    }

    public static float Distance(Vector2i v1, Vector2i v2)
    {
        Vector2i v3 = v1 - v2;
        return (float)Math.Sqrt(v3.x * v3.x + v3.y * v3.y);
    }

    public int ToIndex(int width)
    {
        return x + y * width;
    }

    public override string ToString()
    {
        return x.ToString() + "," + y.ToString();
    }

    public void FromString(string str)
    {
        string[] parts = str.Split(',');
        if (parts.Length > 0)
            x = int.Parse(parts[0]);
        if (parts.Length > 1)
            y = int.Parse(parts[1]);
    }

    public void WriteBinary(BinaryWriter bw)
    {
        bw.Write(x);
        bw.Write(y);
    }

    public void ReadBinary(BinaryReader br)
    {
        x = br.ReadInt32();
        y = br.ReadInt32();
    }
}
