using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct Vector2f
{
    float _x;
    float _y;

    public float x { get { return _x; } set { _x = value; } }
    public float y { get { return _y; } set { _y = value; } }

    public Vector2f(float x, float y)
    {
        _x = x;
        _y = y;
    }

    public void Set(float x, float y)
    {
        _x = x;
        _y = y;
    }

    public void Normalize()
    {
        float magnitude = (float)Math.Sqrt(_x * _x + _y * _y);
        _x /= magnitude;
        _y /= magnitude;
    }
}
