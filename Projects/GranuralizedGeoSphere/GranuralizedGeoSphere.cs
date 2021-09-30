using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class GranuralizedGeoSphere
{
    public int Divisions = 50;
    public List<GeoSphereFace> BaseFaces;
    public List<GeoSpherePoint> Points;

    static public readonly string baseFile = "WorldGen.SpherePoints.json";
    List<int> indexesOfIndexesBorderLeft = new List<int>();
    List<int> indexesOfIndexesBorderRight = new List<int>();
    Dictionary<Duo<int, int>, List<Vector3>> sidesCache = new Dictionary<Duo<int, int>, List<Vector3>>();

    #region Singleton
    static GranuralizedGeoSphere myInstance = null;

    enum BorderIndexType {
        TOP,
        LEFT,
        BOTTOM_LEFT,
        BOTTOM,
        BOTTOM_RIGHT,
        RIGHT
    };

    GranuralizedGeoSphere()
    {
        Divisions = 50;
        BaseFaces = new List<GeoSphereFace>();
        Points = new List<GeoSpherePoint>();
    }

    public static GranuralizedGeoSphere instance
    {
        get
        {
            if (myInstance == null)
                myInstance = new GranuralizedGeoSphere();
            return myInstance;
        }
    }
    #endregion

    public void Init(int divisions = 5)
    {
        Divisions = divisions;

        indexesOfIndexesBorderLeft.Add(0);
        indexesOfIndexesBorderRight.Add(0);
        for (int i = 1; i <= Divisions + 1; i++)
        {
            indexesOfIndexesBorderLeft.Add(indexesOfIndexesBorderLeft[indexesOfIndexesBorderLeft.Count-1] + i);
            indexesOfIndexesBorderRight.Add(indexesOfIndexesBorderRight[indexesOfIndexesBorderRight.Count - 1] + i + 1);
        }

        string filePath = Path.Combine(Application.streamingAssetsPath, baseFile);
        if (File.Exists(filePath))
        {
            // Loads a saved Geosphere.
            Load();
        }

        if (!File.Exists(filePath) || BaseFaces.Count == 0 || Divisions != divisions)
        {
            // Calculates the Geosphere right now, and save.
            Divisions = divisions;
            Build();
            Save();
        }
    }

    void Build()
    {
        // Creates the base faces.
        for (int i = 0; i < Octodecahedron.baseFacesCount; i++)
        {
            int startIndex = i * 3;

            int vertex1 = Octodecahedron.triangles[startIndex];
            int vertex2 = Octodecahedron.triangles[startIndex+1];
            int vertex3 = Octodecahedron.triangles[startIndex+2];

            GeoSphereFace bf = new GeoSphereFace(Octodecahedron.vertices[vertex1].Round(),
                Octodecahedron.vertices[vertex2].Round(),
                Octodecahedron.vertices[vertex3].Round(),
                vertex1,
                vertex2,
                vertex3 );

            bf.Center = ((bf.p1 + bf.p2 + bf.p3) / 3).Round();
            bf.Neighbors = new List<int>();
            bf.Indexes = new List<int>();
            bf.Neighbors.Add(Octodecahedron.neighbors[startIndex]);
            bf.Neighbors.Add(Octodecahedron.neighbors[startIndex + 1]);
            bf.Neighbors.Add(Octodecahedron.neighbors[startIndex + 2]);

            Duo<int, int> d12 = new Duo<int, int>(vertex1, vertex2);
            Duo<int, int> d13 = new Duo<int, int>(vertex1, vertex3);
            Duo<int, int> d23 = new Duo<int, int>(vertex2, vertex3);
            Duo<int, int> d21 = new Duo<int, int>(vertex2, vertex1);
            Duo<int, int> d31 = new Duo<int, int>(vertex3, vertex1);
            Duo<int, int> d32 = new Duo<int, int>(vertex3, vertex2);

            if (!sidesCache.ContainsKey(d12))
            {
                if (sidesCache.ContainsKey(d21))
                {
                    List<Vector3> lineSplit21 = sidesCache[d21];
                    List<Vector3> lineSplit12 = new List<Vector3>(lineSplit21);
                    lineSplit12.Reverse();
                    sidesCache.Add(d12, lineSplit12);
                }
                else
                {
                    List<Vector3> lineSplit12 = SplitLine(bf.p1, bf.p2, Divisions);
                    sidesCache.Add(d12, lineSplit12);
                }
            }
            bf.Side12 = sidesCache[d12];
            if (!sidesCache.ContainsKey(d21))
            {
                List<Vector3> lineSplit12 = sidesCache[d12];
                List<Vector3> lineSplit21 = new List<Vector3>(lineSplit12);
                lineSplit21.Reverse();
                sidesCache.Add(d21, lineSplit21);
            }

            if (!sidesCache.ContainsKey(d13))
            {
                if (sidesCache.ContainsKey(d31))
                {
                    List<Vector3> lineSplit31 = sidesCache[d31];
                    List<Vector3> lineSplit13 = new List<Vector3>(lineSplit31);
                    lineSplit13.Reverse();
                    sidesCache.Add(d13, lineSplit13);
                }
                else
                {
                    List<Vector3> lineSplit13 = SplitLine(bf.p1, bf.p3, Divisions);
                    sidesCache.Add(d13, lineSplit13);
                }
            }
            bf.Side13 = sidesCache[d13];
            if (!sidesCache.ContainsKey(d31))
            {
                List<Vector3> lineSplit13 = sidesCache[d13];
                List<Vector3> lineSplit31 = new List<Vector3>(lineSplit13);
                lineSplit31.Reverse();
                sidesCache.Add(d31, lineSplit31);
            }

            if (!sidesCache.ContainsKey(d32))
            {
                if (sidesCache.ContainsKey(d23))
                {
                    List<Vector3> lineSplit23 = sidesCache[d23];
                    List<Vector3> lineSplit32 = new List<Vector3>(lineSplit23);
                    lineSplit32.Reverse();
                    sidesCache.Add(d32, lineSplit32);
                }
                else
                {
                    List<Vector3> lineSplit32 = SplitLine(bf.p3, bf.p2, Divisions);
                    sidesCache.Add(d32, lineSplit32);
                }
            }
            bf.Side32 = sidesCache[d32];
            if (!sidesCache.ContainsKey(d23))
            {
                List<Vector3> lineSplit32 = sidesCache[d32];
                List<Vector3> lineSplit23 = new List<Vector3>(lineSplit32);
                lineSplit23.Reverse();
                sidesCache.Add(d23, lineSplit23);
            }

            if (BaseFaces.Count > i)
                BaseFaces[i] = bf;
            else
                BaseFaces.Add(bf);
        }

        // Splits each face in its subdivisions.
        for (int i = 0; i < BaseFaces.Count; i++)
        {
            SplitFace(i);
        }
        //BuildBorderNeighbors();
    }

    public void SplitFace(int faceIndex)
    {
        Vector3 p1 = BaseFaces[faceIndex].p1;
        Vector3 p2 = BaseFaces[faceIndex].p2;
        Vector3 p3 = BaseFaces[faceIndex].p3;

        // Splits the sides.
        List<Vector3> lineSplit12 = BaseFaces[faceIndex].Side12;
        List<Vector3> lineSplit13 = BaseFaces[faceIndex].Side13;

        List<int> indexes = new List<int>();

        for (int i = 0; i <= Divisions + 1; i++)
        {
            if (i == 0)
            {
                int existingIndex = GetIndexOfPoint(p1);
                if (existingIndex == -1)
                {
                    GeoSpherePoint tf = new GeoSpherePoint(p1, Points.Count);
                    indexes.Add(Points.Count);
                    Points.Add(tf);
                }
                else
                {
                    indexes.Add(existingIndex);
                }
            }
            else
            {
                List<Vector3> lineSplit32 = null;
                if (i == Divisions + 1)
                    lineSplit32 = BaseFaces[faceIndex].Side32;
                else
                    lineSplit32 = SplitLine(lineSplit13[i], lineSplit12[i], (i - 1));

                for (int a = 0; a <= i; a++)
                {
                    int uvXoffssetNum = Divisions + 1 - i;
                    GeoSpherePoint tf = null;
                    Vector3 newPoint = Vector3.zero;

                    newPoint = lineSplit32[a];

                    if (a == 0 || a == i || i == Divisions + 1)
                    {
                        int existingIndex = GetIndexOfPoint(newPoint);
                        if (existingIndex == -1)
                        {
                            tf = new GeoSpherePoint(newPoint, Points.Count);
                            indexes.Add(Points.Count);
                            Points.Add(tf);
                        }
                        else
                        {
                            indexes.Add(existingIndex);
                        }
                    }
                    else
                    {
                        tf = new GeoSpherePoint(newPoint, Points.Count);
                        indexes.Add(Points.Count);
                        Points.Add(tf);
                    }
                }
            }
        }

        BaseFaces[faceIndex].Indexes = indexes;

        // Calculates the neighbors of the point within the face.
        int indexIndex = -1;
        for (int i = 0; i <= Divisions + 1; i++)
        {
            for (int a = 0; a <= i; a++)
            {
                indexIndex++;
                int actualIndex = indexes[indexIndex];

                if (a == 0 || a == i || i == Divisions + 1)
                {
                    if (indexIndex == 0)
                    {
                        // Top vertex.
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 1]);   // Bottom Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 2]);   // Bottom Right
                    }
                    else if (i == Divisions + 1 && a == 0)
                    {
                        // Bottom Left vertex
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - i]);       // Top Right
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 1]);       // Right
                    }
                    else if (i == Divisions + 1 && a == i)
                    {
                        // Bottom Right vertex
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - (i + 1)]); // Top Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - 1]);       // Left
                    }
                    else if (a == 0)
                    {
                        // Left side
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - i]);       // Top Right
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 1]);       // Right
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 2]);   // Bottom Right
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 1]);   // Bottom Left
                    }
                    else if (a == i)
                    {
                        // Right side
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - (i + 1)]); // Top Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - 1]);       // Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 1]);   // Bottom Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 2]);   // Bottom Right
                    }
                    else if (i == Divisions + 1)
                    {
                        // Bottom side
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - 1]);       // Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - (i + 1)]); // Top Left
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - i]);       // Top Right
                        Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 1]);       // Right
                    }
                    continue;
                }

                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - 1]);       // Left
                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + 1]);       // Right
                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - (i + 1)]); // Top Left
                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex - i]);       // Top Right
                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 1]);   // Bottom Left
                Points[actualIndex].Neighbors.AddUnique(indexes[indexIndex + i + 2]);   // Bottom Right
            }
        }
    }

    List<Vector3> SplitLine(Vector3 v1, Vector3 v2, int level, bool doNormalize = true)
    {
        Vector3 side = v2 - v1;
        float length = side.magnitude;
        float newLength = length / (level + 1);
        Vector3 step = side;
        step.Normalize();
        step *= newLength;
        step = step.Round();
        List<Vector3> newPoints = new List<Vector3>();
        newPoints.Add(v1);
        Vector3 point = v1 + step;
        point = point.Round();
        for (int i = 0; i < level; i++)
        {
            Vector3 newPointInSphere = new Vector3(point.x, point.y, point.z);
            if (!doNormalize)
                newPointInSphere.Normalize();
            newPoints.Add(newPointInSphere);
            point = point + step;
            point = point.Round();
        }
        newPoints.Add(v2);

        return newPoints;
    }

    int GetIndexOfPoint(Vector3 p)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            if (Mathf.Abs(Points[i].x - p.x) <= 0.00005 &&
                Mathf.Abs(Points[i].y - p.y) <= 0.00005 &&
                Mathf.Abs(Points[i].z - p.z) <= 0.00005)
                return i;
        }
        return -1;
    }

    BorderIndexType GetBorderIndexType(int indexOfIndex, GeoSphereFace face)
    {
        if (indexOfIndex == 0)
            return BorderIndexType.TOP;
        if (indexOfIndex == face.Indexes.Count - 1)
            return BorderIndexType.BOTTOM_RIGHT;
        if (indexOfIndex == face.Indexes.Count - ( Divisions + 2 ))
            return BorderIndexType.BOTTOM_LEFT;
        if (indexOfIndex >= face.Indexes.Count - (Divisions + 1))
            return BorderIndexType.BOTTOM;
        if (indexesOfIndexesBorderLeft.Contains(indexOfIndex))
            return BorderIndexType.LEFT;
        if (indexesOfIndexesBorderRight.Contains(indexOfIndex))
            return BorderIndexType.RIGHT;
        return BorderIndexType.TOP;
    }

    List<GeoSphereFace> GetNeighboringFaces(int index, GeoSphereFace currentFace)
    {
        List<GeoSphereFace> faces = new List<GeoSphereFace>();
        foreach (GeoSphereFace geoSphereFace in BaseFaces)
        {
            if (geoSphereFace == currentFace)
                continue;
            if (geoSphereFace.Indexes.Contains(index))
                faces.Add(geoSphereFace);
        }
        return faces;
    }

    void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(this, false);
            string filePath = Path.Combine(Application.streamingAssetsPath, baseFile);
            System.IO.File.WriteAllText(filePath, json);
        }
        catch (Exception e)
        {
            Log.Write("Error saving " + baseFile + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    void Load()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, baseFile);

        try
        {
            if (File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                GranuralizedGeoSphere ggs = JsonUtility.FromJson<GranuralizedGeoSphere>(json);
                Divisions = ggs.Divisions;
                BaseFaces = ggs.BaseFaces;
                Points = ggs.Points;
            }
        }
        catch (Exception e)
        {
            Log.Write("Error loading " + baseFile + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    public GeoSpherePoint GetClosestPointTo(Vector3 point)
    {
        float closestBaseFaceDistance = float.MaxValue;
        GeoSphereFace closestFace = null;
        foreach (GeoSphereFace geoSphereFace in BaseFaces)
        {
            float faceDistance = (geoSphereFace.Center - point).magnitude;
            if (faceDistance < closestBaseFaceDistance)
            {
                closestBaseFaceDistance = faceDistance;
                closestFace = geoSphereFace;
            }
        }

        float closestGeoSpherePointDistance = float.MaxValue;
        GeoSpherePoint closestPoint = null;
        foreach (int pointIndex in closestFace.Indexes)
        {
            GeoSpherePoint geoSpherePoint = Points[pointIndex];
            Vector3 pointCoords = geoSpherePoint.AsVector3();
            float pointDistance = (pointCoords - point).magnitude;
            if (pointDistance < closestGeoSpherePointDistance)
            {
                closestGeoSpherePointDistance = pointDistance;
                closestPoint = geoSpherePoint;
            }
        }

        return closestPoint;
    }

    public GeoSpherePoint GetPoint(int index)
    {
        if (index >= Points.Count || index < 0)
            return null;
        return Points[index];
    }
}
