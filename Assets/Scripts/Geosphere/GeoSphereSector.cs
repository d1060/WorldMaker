using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoSphereSector
{
    float radius;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 center;
    public Vector2 uv1;
    public Vector2 uv2;
    public Vector2 uv3;
    public Vector3 p1Normal;
    public Vector3 p2Normal;
    public Vector3 p3Normal;

    public int level = 0;
    public int index = -1;
    float baseProjection;
    List<GeoSphereSector> subFaces = new List<GeoSphereSector>();

    float lowestLat; // From 0 to 1
    float highestLat; // From 0 to 1
    float lowestLon; // From 0 to 1
    float highestLon; // From 0 to 1

    public GeoSphereSector(int index, float radius)
    {
        this.radius = radius;

        Vector3 v1 = GeoSphereBaseFaces.vertices[GeoSphereBaseFaces.triangles[index * 3]];
        Vector3 v2 = GeoSphereBaseFaces.vertices[GeoSphereBaseFaces.triangles[index * 3 + 1]];
        Vector3 v3 = GeoSphereBaseFaces.vertices[GeoSphereBaseFaces.triangles[index * 3 + 2]];

        v1.Normalize();
        v2.Normalize();
        v3.Normalize();

        v1 *= radius;
        v2 *= radius;
        v3 *= radius;

        Init(v1, v2, v3);
    }

    public Vector3 Center { get { return center; } }

    public GeoSphereSector(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Init(v1, v2, v3);
    }

    void Init(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        p1 = new Vector3(v1.x, v1.y, v1.z);
        p2 = new Vector3(v2.x, v2.y, v2.z);
        p3 = new Vector3(v3.x, v3.y, v3.z);
        center = (p1 + p2 + p3) / 3;

        Vector3 vec1 = v2 - v1;
        vec1.Normalize();
        Vector3 vec2 = v3 - v1;
        vec2.Normalize();

        p1Normal = v1;
        p2Normal = v2;
        p3Normal = v3;

        p1Normal.Normalize();
        p2Normal.Normalize();
        p3Normal.Normalize();

        /////////////////////
        // UV Calculations //
        /////////////////////
        uv1 = p1.CartesianToPolarRatio(radius);
        uv2 = p2.CartesianToPolarRatio(radius);
        uv3 = p3.CartesianToPolarRatio(radius);

        // Adjust for the north and south poles.
        if (v1.x.IsAlmost(0) && v1.z.IsAlmost(0))
            uv1.x = (uv2.x + uv3.x) / 2;
        if (v2.x.IsAlmost(0) && v2.z.IsAlmost(0))
            uv2.x = (uv1.x + uv3.x) / 2;
        if (v3.x.IsAlmost(0) && v3.z.IsAlmost(0))
            uv3.x = (uv1.x + uv2.x) / 2;

        Vector3 vecBase = p2 - p3; // X vector
        Vector3 vecBaseUnit = vecBase;

        Vector3 vec13 = p1 - p3;
        Vector3 bottomMidpoint = ((Vector3.Dot(vec13, vecBaseUnit) / Vector3.Dot(vecBaseUnit, vecBaseUnit)) * vecBaseUnit);
        Vector3 vecHeight = p1 - bottomMidpoint; // Y vector
    }

    public void SplitFace(float radius, int newLevel)
    {
        subFaces.Clear();

        // Splits the sides.
        List<Vector3> lineSplit12 = SplitLine(p1, p2, newLevel, radius);
        List<Vector3> lineSplit13 = SplitLine(p1, p3, newLevel, radius);

        //Splits the bottoms for each "side" line.
        List<Vector3> prevLineSplit32 = new List<Vector3>();

        for (int i = 0; i <= newLevel; i++)
        {
            if (i == 0)
            {
                GeoSphereSector tf = new GeoSphereSector(p1, lineSplit12[0], lineSplit13[0]);
                subFaces.Add(tf);
                prevLineSplit32.Clear();
                prevLineSplit32.Add(lineSplit13[0]);
                prevLineSplit32.Add(lineSplit12[0]);
            }
            else
            {
                List<Vector3> lineSplit32 = SplitLine(lineSplit13[i], lineSplit12[i], i, radius);
                //List<Vector3> lineSplit32straight = SplitLine(lineSplit13[i], lineSplit12[i], i, float.MinValue);
                for (int a = 0; a <= i; a++)
                {
                    int uvXoffssetNum = newLevel + 1 - i;

                    if (a == 0)
                    {
                        GeoSphereSector tf = new GeoSphereSector(lineSplit13[i - 1], lineSplit32[a], lineSplit13[i]);
                        subFaces.Add(tf);
                    }
                    else
                    {
                        GeoSphereSector tf = new GeoSphereSector(prevLineSplit32[a - 1], prevLineSplit32[a], lineSplit32[a - 1]);
                        subFaces.Add(tf);

                        GeoSphereSector tf2 = new GeoSphereSector(lineSplit32[a - 1], prevLineSplit32[a], lineSplit32[a]);
                        subFaces.Add(tf2);
                    }
                }
                prevLineSplit32.Clear();
                prevLineSplit32.Add(lineSplit13[i]);
                prevLineSplit32.AddRange(lineSplit32);
            }
        }
        level = newLevel;
        //Debug.Log("Added " + subFaces.Count + " subfaces to face " + index + ".");
    }

    public Vector3[] GetVertexes()
    {
        Vector3[] vertexes = new Vector3[3];
        int i = 0;
        vertexes[i++] = p1;
        vertexes[i++] = p2;
        vertexes[i++] = p3;
        return vertexes;
    }

    public Vector3[] GetNormals()
    {
        //if (level == 0 || subFaces.Count == 0)
        //{
        Vector3[] normals = new Vector3[3];
        int i = 0;
        normals[i++] = p1Normal;
        normals[i++] = p2Normal;
        normals[i++] = p3Normal;
        return normals;
    }

    public Vector2[] GetUVs()
    {
        //Vector3 vec32 = p2 - p3; // X vector
        //Vector3 bottomMidpoint = ((p2 + p3) / 2);
        //Vector3 vec123 = p1 - bottomMidpoint; // Y vector

        //if (level == 0 || subFaces.Count == 0)
        //{
        uv1 = p1.CartesianToPolarRatio(1);
        uv2 = p2.CartesianToPolarRatio(1);
        uv3 = p3.CartesianToPolarRatio(1);
        Vector3 pCenter = (p1 + p2 + p3) / 3;
        Vector2 uvCenter = pCenter.CartesianToPolarRatio(1);

        if (uv1.x < 0.25f && uvCenter.x >= 0.75f) uv1.x += 1;
        if (uv2.x < 0.25f && uvCenter.x >= 0.75f) uv2.x += 1;
        if (uv3.x < 0.25f && uvCenter.x >= 0.75f) uv3.x += 1;

        if (uv1.x > 0.75f && uvCenter.x <= 0.25f) uv1.x -= 1;
        if (uv2.x > 0.75f && uvCenter.x <= 0.25f) uv2.x -= 1;
        if (uv3.x > 0.75f && uvCenter.x <= 0.25f) uv3.x -= 1;

        Vector2[] uvs = new Vector2[3];
        int i = 0;
        uvs[i++] = uv1;
        uvs[i++] = uv2;
        uvs[i++] = uv3;

        return uvs;
    }

    public Vector3[] GetSubFacesVertexes()
    {
        if (subFaces.Count == 0)
            return GetVertexes();
        List<Vector3> vertexes = new List<Vector3>();
        foreach (GeoSphereSector face in subFaces)
        {
            vertexes.AddRange(face.GetVertexes());
        }
        return vertexes.ToArray();
    }

    public Vector3[] GetSubFacesNormals()
    {
        if (subFaces.Count == 0)
            return GetNormals();
        List<Vector3> normals = new List<Vector3>();
        foreach (GeoSphereSector face in subFaces)
        {
            normals.AddRange(face.GetNormals());
        }
        return normals.ToArray();
    }

    public Vector2[] GetSubFacesUVs()
    {
        if (subFaces.Count == 0)
            return GetUVs();
        List<Vector2> uvs = new List<Vector2>();
        foreach (GeoSphereSector face in subFaces)
        {
            uvs.AddRange(face.GetUVs());
        }
        return uvs.ToArray();
    }

    List<Vector3> SplitLine(Vector3 v1, Vector3 v2, int level, float radius)
    {
        Vector3 side = v2 - v1;
        float length = side.magnitude;
        float newLength = length / (level + 1);
        Vector3 step = side;
        step.Normalize();
        step *= newLength;
        step = step.Round();
        List<Vector3> newPoints = new List<Vector3>();
        Vector3 point = v1 + step;
        point = point.Round();
        for (int i = 0; i <= level; i++)
        {
            Vector3 newPointInSphere = new Vector3(point.x, point.y, point.z);
            if (radius != float.MinValue)
            {
                newPointInSphere.Normalize();
                newPointInSphere *= radius;
            }
            newPoints.Add(newPointInSphere);

            point = point + step;
            point = point.Round();
        }

        return newPoints;
    }

    public Vector3 GetClosestPointTo(Vector3 point)
    {
        float closestGeoSpherePointDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        Vector3[] vertexes = GetVertexes();
        foreach (Vector3 vertex in vertexes)
        {
            float pointDistance = (vertex - point).magnitude;
            if (pointDistance < closestGeoSpherePointDistance)
            {
                closestGeoSpherePointDistance = pointDistance;
                closestPoint = vertex;
            }
        }
        return closestPoint;
    }
}
