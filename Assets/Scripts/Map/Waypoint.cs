using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Waypoint
{
    MapWaypoint mapWaypoint = null;
    MapWaypoint globeWaypoint = null;
    List<Pathfinding.Node> path = null;
    //int index = -1;
    float pathLength = 0;

    //public int Index { get { return index; } set { index = value; } }
    public MapWaypoint MapWaypoint { get { return mapWaypoint; } set { mapWaypoint = value; } }
    public MapWaypoint GlobeWaypoint { get { return globeWaypoint; } set { globeWaypoint = value; } }
    public List<Pathfinding.Node> Path { get { return path; } set { path = value; } }
    public float PathLength { get { return pathLength; } set { pathLength = value; } }

    public Waypoint()
    {
        path = new List<Pathfinding.Node>();
    }

    public void CreatePathObjects(Map map, List<Pathfinding.Node> lastPath, int index)
    {
        CreatePathObject(map, lastPath, true, index);
        CreatePathObject(map, lastPath, false, index);
    }

    void CreatePathObject(Map map, List<Pathfinding.Node> lastPath, bool isGlobe, int index)
    {
        GameObject pathGameObject = new GameObject("Path " + (isGlobe ? "Globe " : "Map ") + index + "-" + (index + 1));
        Vector3 labelPosition = Vector3.zero;
        float currentDistance = 0;

        CameraController cameraController = null;
        if (map != null)
        {
            cameraController = map.cam.GetComponent<CameraController>();
        }

        if (isGlobe)
        {
            pathLength = 0;
            pathGameObject.transform.position = map.geoSphere.transform.position;
            pathGameObject.transform.parent = map.geoSphere.transform;

            currentDistance = -cameraController.transform.position.z;
        }
        else
        {
            pathGameObject.transform.position = map.transform.position;
            pathGameObject.transform.parent = map.transform;

            Camera camera = cameraController.map.geoSphere.transform.GetComponentInChildren<Camera>();
            Vector3 positionVector = camera.transform.localPosition;
            currentDistance = positionVector.magnitude - cameraController.map.geoSphere.Radius;
            if (currentDistance < CameraController.MinCameraDistance)
                currentDistance = CameraController.MinCameraDistance;
            else if (currentDistance > CameraController.MaxCameraDistance)
                currentDistance = CameraController.MaxCameraDistance;
        }
        LineRenderer lineRenderer = pathGameObject.AddComponent<LineRenderer>();
        lineRenderer.materials = new Material[] { map.pathMaterial };
        lineRenderer.startColor = new Color(1, 1, 1, 1);
        lineRenderer.endColor = new Color(1, 1, 1, 1);
        lineRenderer.useWorldSpace = false;
        int positionsCount = path.Count + (index > 0 ? 1 : 0);
        Vector3[] linePositions = new Vector3[positionsCount];
        int i = 0;
        if (index > 0)
        {
            MapWaypoint mapWaypoint = lastPath[lastPath.Count - 1] as MapWaypoint;
            Vector3 linePosition = mapWaypoint.Position;
            if (isGlobe)
            {
                linePosition.Normalize();
                linePosition *= map.geoSphere.Radius + 0.1f;
            }
            else
            {
                linePosition = map.UnityGlobeToMapPoint(linePosition);
                linePosition.z -= 0.1f;
                linePosition -= map.transform.position;
            }
            linePositions[i++] = linePosition;
            labelPosition += linePosition;
        }

        float lowestZ = 0;
        foreach (Pathfinding.Node node in path)
        {
            MapWaypoint mapWaypoint = node as MapWaypoint;
            if (mapWaypoint == null)
                continue;

            Vector3 linePosition = mapWaypoint.Position;
            if (isGlobe)
            {
                linePosition.Normalize();
                linePosition *= map.geoSphere.Radius + 0.1f;
            }
            else
            {
                linePosition = map.UnityGlobeToMapPoint(linePosition);
                linePosition.z -= 0.1f;
                linePosition -= map.transform.position;
                if (linePosition.z < lowestZ)
                    lowestZ = linePosition.z;
            }
            linePositions[i++] = linePosition;
            labelPosition += linePosition;

            if (i > 1 && isGlobe)
            {
                pathLength += (linePositions[i - 1] - linePositions[i - 2]).magnitude;
            }
        }
        labelPosition /= linePositions.Length;

        if (isGlobe)
        {
            linePositions = PathSmoother.instance.SmoothPath(linePositions, map.geoSphere.Radius + 0.1f);
            labelPosition.Normalize();
            labelPosition *= map.geoSphere.Radius + 0.2f;
        }
        else
        {
            linePositions = PathSmoother.instance.SmoothPath2D(linePositions);
            labelPosition.z = lowestZ - 0.2f;
        }
        lineRenderer.positionCount = linePositions.Length;
        lineRenderer.SetPositions(linePositions);

        float multiplierRatio = ((currentDistance - CameraController.MinCameraDistance) / (CameraController.MaxCameraDistance - CameraController.MinCameraDistance));
        lineRenderer.widthMultiplier = multiplierRatio;

        if (isGlobe)
        {
            if (globeWaypoint.PathGameObject != null)
                GameObject.DestroyImmediate(globeWaypoint.PathGameObject);
            globeWaypoint.PathGameObject = pathGameObject;
            pathLength *= (float)map.mapSettings.RadiusInKm / map.geoSphere.Radius;
            Vector3 labelForward = new Vector3(labelPosition.x, labelPosition.y, labelPosition.z);
            labelForward.Normalize();
            labelForward = -labelForward;
            globeWaypoint.CreateLengthLabel(pathLength, labelPosition + map.geoSphere.transform.position, labelForward, map.geoSphere.transform.up, map, map.geoSphereCamera, isGlobe);
        }
        else
        {
            if (mapWaypoint.PathGameObject != null)
                GameObject.DestroyImmediate(mapWaypoint.PathGameObject);
            mapWaypoint.PathGameObject = pathGameObject;
            Vector3 labelForward = new Vector3(0, 0, 1);
            mapWaypoint.CreateLengthLabel(pathLength, labelPosition + map.transform.position, labelForward, map.transform.up, map, map.cam, isGlobe);
        }
    }
}
