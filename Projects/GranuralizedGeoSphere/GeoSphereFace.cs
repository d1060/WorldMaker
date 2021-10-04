using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class GeoSphereFace
{
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 Center;
    public List<int> Neighbors;
    public List<int> Indexes;

    int p1index = -1;
    int p2index = -1;
    int p3index = -1;

    List<Vector3> side12 = new List<Vector3>();
    List<Vector3> side13 = new List<Vector3>();
    List<Vector3> side32 = new List<Vector3>();

    public List<Vector3> Side12 { get { return side12; } set { side12 = value; } }
    public List<Vector3> Side13 { get { return side13; } set { side13 = value; } }
    public List<Vector3> Side32 { get { return side32; } set { side32 = value; } }

    public GeoSphereFace() { }
    public GeoSphereFace(Vector3 p1, Vector3 p2, Vector3 p3, int p1index, int p2index, int p3index)
    {
        this.p1 = p1.Round();
        this.p2 = p2.Round();
        this.p3 = p3.Round();
        Center = (p1 + p2 + p3) / 3;
        Center = Center.Round();
        Neighbors = new List<int>();
        Indexes = new List<int>();

        this.p1index = p1index;
        this.p2index = p2index;
        this.p3index = p3index;
    }

    public void WriteBinary(BinaryWriter writer)
    {
        p1.WriteBinary(writer);
        p2.WriteBinary(writer);
        p3.WriteBinary(writer);
        writer.Write(Neighbors.Count);
        foreach (int neighborId in Neighbors)
        {
            writer.Write(neighborId);
        }
        writer.Write(Indexes.Count);
        foreach (int indexId in Indexes)
        {
            writer.Write(indexId);
        }
    }

    public void ReadBinary(BinaryReader reader)
    {
        p1 = p1.ReadBinary(reader);
        p2 = p2.ReadBinary(reader);
        p3 = p3.ReadBinary(reader);
        Center = (p1 + p2 + p3) / 3;
        Center = Center.Round();
        int neighborsCount = reader.ReadInt32();
        Neighbors = new List<int>();
        for (int i = 0; i < neighborsCount; i++)
        {
            int neighborId = reader.ReadInt32();
            Neighbors.Add(neighborId);
        }
        int indexCount = reader.ReadInt32();
        Indexes = new List<int>();
        for (int i = 0; i < indexCount; i++)
        {
            int indexId = reader.ReadInt32();
            Indexes.Add(indexId);
        }
    }
}
