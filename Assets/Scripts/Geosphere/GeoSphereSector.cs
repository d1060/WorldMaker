using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoSphereSector
{
    float radius;
    public Vector3[] p;
    public Vector3 center;
    public Vector2[] uv;
    public Vector3[] normal;

    public int level = 0;
    public int index = -1;
    List<GeoSphereSector> subFaces = new List<GeoSphereSector>();

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

    public GeoSphereSector(float radius, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        this.radius = radius;
        Init(v1, v2, v3);
    }

    void Init(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        p = new Vector3[] {
            new Vector3(v1.x, v1.y, v1.z),
            new Vector3(v2.x, v2.y, v2.z),
            new Vector3(v3.x, v3.y, v3.z)
        };

        center = (p[0] + p[1] + p[2]) / 3;

        Vector3 vec1 = v2 - v1;
        vec1.Normalize();
        Vector3 vec2 = v3 - v1;
        vec2.Normalize();

        normal = new Vector3[] { v1, v2, v3 };

        normal[0].Normalize();
        normal[1].Normalize();
        normal[2].Normalize();

        /////////////////////
        // UV Calculations //
        /////////////////////
        uv = new Vector2[] { p[0].CartesianToPolarRatio(radius),
                             p[1].CartesianToPolarRatio(radius),
                             p[2].CartesianToPolarRatio(radius) };

        // Adjust for the north and south poles.
        if (v1.x.IsAlmost(0) && v1.z.IsAlmost(0))
            uv[0].x = (uv[1].x + uv[2].x) / 2;
        if (v2.x.IsAlmost(0) && v2.z.IsAlmost(0))
            uv[1].x = (uv[0].x + uv[2].x) / 2;
        if (v3.x.IsAlmost(0) && v3.z.IsAlmost(0))
            uv[2].x = (uv[0].x + uv[1].x) / 2;

        Vector3 vecBase = p[1] - p[2]; // X vector
        Vector3 vecBaseUnit = vecBase;

        Vector3 vec13 = p[0] - p[2];
        Vector3 bottomMidpoint = ((Vector3.Dot(vec13, vecBaseUnit) / Vector3.Dot(vecBaseUnit, vecBaseUnit)) * vecBaseUnit);
        //Vector3 vecHeight = p[0] - bottomMidpoint; // Y vector
    }

    public void SplitFace(float radius, int newLevel)
    {
        subFaces.Clear();

        // Splits the sides.
        List<Vector3> lineSplit12 = SplitLine(p[0], p[1], newLevel, radius);
        List<Vector3> lineSplit13 = SplitLine(p[0], p[2], newLevel, radius);

        //Splits the bottoms for each "side" line.
        List<Vector3> prevLineSplit32 = new List<Vector3>();

        for (int i = 0; i <= newLevel; i++)
        {
            if (i == 0)
            {
                GeoSphereSector tf = new GeoSphereSector(radius, p[0], lineSplit12[0], lineSplit13[0]);
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
                        GeoSphereSector tf = new GeoSphereSector(radius, lineSplit13[i - 1], lineSplit32[a], lineSplit13[i]);
                        subFaces.Add(tf);
                    }
                    else
                    {
                        GeoSphereSector tf = new GeoSphereSector(radius, prevLineSplit32[a - 1], prevLineSplit32[a], lineSplit32[a - 1]);
                        subFaces.Add(tf);

                        GeoSphereSector tf2 = new GeoSphereSector(radius, lineSplit32[a - 1], prevLineSplit32[a], lineSplit32[a]);
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
        vertexes[i++] = p[0];
        vertexes[i++] = p[1];
        vertexes[i++] = p[2];
        return vertexes;
    }

    public Vector3[] GetNormals()
    {
        //if (level == 0 || subFaces.Count == 0)
        //{
        Vector3[] normals = new Vector3[3];
        int i = 0;
        normals[i++] = normal[0];
        normals[i++] = normal[1];
        normals[i++] = normal[2];
        return normals;
    }

    public Vector2[] GetUVs()
    {
        //Vector3 vec32 = p[1] - p[2]; // X vector
        //Vector3 bottomMidpoint = ((p[1] + p[2]) / 2);
        //Vector3 vec123 = p[0] - bottomMidpoint; // Y vector

        //if (level == 0 || subFaces.Count == 0)
        //{
        uv[0] = p[0].CartesianToPolarRatio(1);
        uv[1] = p[1].CartesianToPolarRatio(1);
        uv[2] = p[2].CartesianToPolarRatio(1);

        Vector3 pCenter = (p[0] + p[1] + p[2]) / 3;
        Vector2 uvCenter = pCenter.CartesianToPolarRatio(1);

        if (uv[0].x < 0.25f && uvCenter.x >= 0.75f) uv[0].x += 1;
        if (uv[1].x < 0.25f && uvCenter.x >= 0.75f) uv[1].x += 1;
        if (uv[2].x < 0.25f && uvCenter.x >= 0.75f) uv[2].x += 1;

        if (uv[0].x > 0.75f && uvCenter.x <= 0.25f) uv[0].x -= 1;
        if (uv[1].x > 0.75f && uvCenter.x <= 0.25f) uv[1].x -= 1;
        if (uv[2].x > 0.75f && uvCenter.x <= 0.25f) uv[2].x -= 1;

        // Adjust for the north and south poles.
        if (p[0].y == radius || p[0].y == -radius)
            uv[0].x = (uv[1].x + uv[2].x) / 2;
        if (p[1].y == radius || p[1].y == -radius)
            uv[1].x = (uv[0].x + uv[2].x) / 2;
        if (p[2].y == radius || p[2].y == -radius)
            uv[2].x = (uv[0].x + uv[1].x) / 2;

        Vector2[] uvs = new Vector2[3];
        int i = 0;
        uvs[i++] = uv[0];
        uvs[i++] = uv[1];
        uvs[i++] = uv[2];

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
