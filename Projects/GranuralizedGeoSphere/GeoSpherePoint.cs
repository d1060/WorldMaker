using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class GeoSpherePoint
{
    public float x = 0;
    public float y = 0;
    public float z = 0;
    public List<int> Neighbors;
    int index = -1;
    public int Index { get { return index; } set { index = value; } }

    public GeoSpherePoint() { }
    public GeoSpherePoint(Vector3 p1, int index)
    {
        this.x = p1.x.Round();
        this.y = p1.y.Round();
        this.z = p1.z.Round();
        this.index = index;
        Neighbors = new List<int>();
    }

    public Vector3 AsVector3()
    {
        return new Vector3(x, y, z);
    }

    public override int GetHashCode()
    {
        int myHash = unchecked(x.GetHashCode() * 523 + y.GetHashCode() * 541 + z.GetHashCode() * 547);
        return myHash;
    }
}
