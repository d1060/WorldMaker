using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Int3
{
    public int x = 0;
    public int y = 0;
    public int z = 0;

    public Int3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Int3(int x, int y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = (int)z;
    }

    public Int3(Vector3 v)
    {
        this.x = (int)v.x;
        this.y = (int)v.y;
        this.z = (int)v.z;
    }

    public Int3(Int3 v)
    {
        this.x = v.x;
        this.y = v.y;
        this.z = v.z;
    }

    public override string ToString()
    {
        return x + ", " + y + ", " + z;
    }
}
