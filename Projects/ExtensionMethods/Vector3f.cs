using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct Vector3f
{
    float _x;
    float _y;
    float _z;

    public float x { get { return _x; } set { _x = value; } }
    public float y { get { return _y; } set { _y = value; } }
    public float z { get { return _z; } set { _z = value; } }
    public float magnitude { get { return (float)Math.Sqrt(_x * _x + _y * _y + _z * _z); } }

    public Vector3f(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    public void Set(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    public void Normalize()
    {
        float magnitude = (float)Math.Sqrt(_x * _x + _y * _y + _z * _z);
        _x /= magnitude;
        _y /= magnitude;
        _z /= magnitude;
    }

    public static Vector3f operator *(Vector3f i, float u)
    {
        i._x *= u;
        i._y *= u;
        i._z *= u;
        return i;
    }

    public static Vector3f operator /(Vector3f i, float u)
    {
        i._x /= u;
        i._y /= u;
        i._z /= u;
        return i;
    }

    public static Vector3f operator +(Vector3f i, Vector3f u)
    {
        Vector3f v = new Vector3f(i._x + u._x, i._y + u._y, i._z + u._z);
        return v;
    }

    public static Vector3f operator -(Vector3f i, Vector3f u)
    {
        Vector3f v = new Vector3f(i._x - u._x, i._y - u._y, i._z - u._z);
        return v;
    }

    public static Vector3f zero { get { return new Vector3f(0, 0, 0); } }
}
