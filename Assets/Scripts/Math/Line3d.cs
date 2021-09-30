using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line3d
{
    Vector3 point;
    Vector3 vector;

    public Vector3 Point { get { return point; } set { point = value; } }
    public Vector3 Vector { get { return vector; } set { vector = value; } }

    public float DistanceToPoint(Vector3 point)
    {
        Vector3 m0m1 = this.point - point;

        Vector3 vCross = Vector3.Cross(m0m1, vector);

        float distance = vCross.magnitude / vector.magnitude;

        return distance;
    }

    public Vector3 ProjectionFrom(Vector3 point)
    {
        Vector3 pointB = this.point + vector;

        Vector3 ap = point - this.point;

        Vector3 projection = this.point + (Vector3.Dot(ap, vector) / Vector3.Dot(vector, vector)) * vector;

        return projection;
    }

    public float distanceFromSegmentToPoint(Vector3 point)
    {
        Vector3 p2 = this.point + vector;
        float distanceToP1 = (point - this.point).magnitude;
        float distanceToP2 = (point - p2).magnitude;
        float lineSize = (p2 - this.point).magnitude;

        if (Mathf.Abs(distanceToP1 - distanceToP2) > lineSize)
        {
            //Point is beyond the line segment.
            if (distanceToP1 <= distanceToP2)
                return distanceToP1;
            return distanceToP2;
        }

        float distanceToLine = DistanceToPoint(point);
        return distanceToLine;
    }
}
