using System;
using System.IO;
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

    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(index);
        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(Neighbors.Count);
        foreach (int neighborId in Neighbors)
        {
            writer.Write(neighborId);
        }
    }

    public void ReadBinary(BinaryReader reader)
    {
        index = reader.ReadInt32();
        x = reader.ReadSingle();
        y = reader.ReadSingle();
        z = reader.ReadSingle();
        int neighborsCount = reader.ReadInt32();
        Neighbors = new List<int>();
        for (int i = 0; i < neighborsCount; i++)
        {
            int neighborId = reader.ReadInt32();
            Neighbors.Add(neighborId);
        }
    }
}
