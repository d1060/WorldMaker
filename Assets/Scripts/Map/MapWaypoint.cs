using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapWaypoint : Pathfinding.Node
{
    GameObject gameObject = null;
    GeoSpherePoint geospherePoint = null;
    List<Pathfinding.Node> neighbors = null;
    Vector3 position;
    WaypointController controller;
    GameObject pathGameObject;
    GameObject lengthLabelGameObject;
    float height = 0;
    float heightInfluence = 1000;

    public Vector3 Position { get { return position;  } set { position = value; } }
    public Vector3 GameObjectPosition { get { return gameObject == null ? Vector3.zero : gameObject.transform.localPosition; } }
    public GameObject PathGameObject
    {
        get { return pathGameObject; }
        set
        {
            pathGameObject = value;

            WaypointSize gameObjectWaypointSize = gameObject.GetComponent<WaypointSize>();
            if (gameObjectWaypointSize != null)
                gameObjectWaypointSize.pathGameObject = pathGameObject;
        }
    }
    public GameObject LengthLabelGameObject { get { return lengthLabelGameObject; } set { lengthLabelGameObject = value; } }

    public MapWaypoint(GameObject prefab, Vector3 position, Vector3 forward, Vector3 up, Vector3 scale, Transform parent, int count, WaypointController controller, float height)
    {
        this.position = position;
        this.controller = controller;
        this.height = height;
        if (prefab != null)
        {
            gameObject = Object.Instantiate(prefab, position, Quaternion.identity);
            gameObject.transform.up = up;
            gameObject.transform.forward = forward;
            gameObject.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
            gameObject.name = "Map Waypoint " + count;
            gameObject.transform.SetParent(parent);

            WaypointSize gameObjectWaypointSize = gameObject.GetComponent<WaypointSize>();
            if (gameObjectWaypointSize != null)
            {
                gameObjectWaypointSize.height = height;
                gameObjectWaypointSize.map = controller.map;
                gameObjectWaypointSize.pathGameObject = pathGameObject;
            }
        }
    }

    ~MapWaypoint()
    {
        if (gameObject != null)
            GameObject.DestroyImmediate(gameObject);
    }

    public float Height
    {
        get { return height; }
    }

    public void Destroy()
    {
        GameObject.DestroyImmediate(gameObject);
        if (pathGameObject != null)
            GameObject.DestroyImmediate(pathGameObject);
        if (lengthLabelGameObject != null)
            GameObject.DestroyImmediate(lengthLabelGameObject);
        gameObject = null;
        pathGameObject = null;
        lengthLabelGameObject = null;
    }

    public string Name
    {
        get
        {
            if (gameObject == null)
                return "";

            return gameObject.name;
        }
    }

    public Vector3 Scale
    {
        set
        {
            gameObject.transform.localScale = value;
        }
    }

    public void FindClosestPoint()
    {
        Vector3 pointInUnitySphere = position - controller.map.geoSphere.transform.position;
        pointInUnitySphere.Normalize();
        geospherePoint = GranuralizedGeoSphere.instance.GetClosestPointTo(pointInUnitySphere);
    }

    public void CreateLengthLabel(float length, Vector3 position, Vector3 forward, Vector3 up, Map map, Camera cam, bool isInGlobe)
    {
        lengthLabelGameObject = new GameObject(length.ToString("#0") + " km label");
        if (isInGlobe)
            lengthLabelGameObject.transform.parent = map.geoSphere.transform;
        else
            lengthLabelGameObject.transform.parent = map.transform;
        lengthLabelGameObject.transform.up = up;
        lengthLabelGameObject.transform.position = position;
        lengthLabelGameObject.transform.forward = forward;
        //lengthLabelGameObject.transform.localScale = new Vector3(0.18f, 0.6f, 1);

        TMPro.TextMeshPro textMeshPro = lengthLabelGameObject.AddComponent<TMPro.TextMeshPro>();
        textMeshPro.text = length.ToString("#0") + " km";

        RectTransform rectTransform = lengthLabelGameObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(30, 5);

        //TMPro.TMP_FontAsset clonedFont = (TMPro.TMP_FontAsset)Object.Instantiate(map.pathLabelFont);
        //clonedFont.material.renderQueue = 2500;
        //textMeshPro.font = clonedFont;

        textMeshPro.fontSize = 60;
        textMeshPro.fontStyle = TMPro.FontStyles.Bold;
        textMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
        textMeshPro.overflowMode = TMPro.TextOverflowModes.Overflow;

        MapName distanceLabel = lengthLabelGameObject.AddComponent<MapName>();
        distanceLabel.cam = cam;
        distanceLabel.mainMap = map;
        distanceLabel.maxOutlineThickness = 0.4f;
        distanceLabel.minOutlineThickness = 0.4f;
        distanceLabel.maxOutlineThicknessDistance = 1;
        distanceLabel.minOutlineThicknessDistance = 0.5f;
        distanceLabel.maxDistanceAlphaZero = 1.1f;
        distanceLabel.minDistanceAlphaZero = 1;
        distanceLabel.maxDistanceAlpha = 0.5f;
        distanceLabel.minDistanceAlpha = 0.05f;
        distanceLabel.maxSize = 1.2f;
        distanceLabel.minSize = 0.1f;
        distanceLabel.isInGlobe = isInGlobe;
    }

    #region AStar
    public override List<Pathfinding.Node> GetNeighbours()
    {
        if (geospherePoint == null)
            return null;

        if (neighbors == null || neighbors.Count == 0)
        {
            neighbors = new List<Pathfinding.Node>();
            foreach (int pointIndex in geospherePoint.Neighbors)
            {
                GeoSpherePoint geoSpherePoint = GranuralizedGeoSphere.instance.GetPoint(pointIndex);
                if (geoSpherePoint != null)
                {
                    Vector3 waypointPosition = geoSpherePoint.AsVector3();
                    float height = controller.GetObjectHeightInUnityGlobe(waypointPosition);
                    //if (height < controller.map.WaterLevel)
                    //    height = controller.map.WaterLevel;
                    MapWaypoint mapWaypoint = new MapWaypoint(null, waypointPosition, Vector3.zero, Vector3.zero, Vector3.zero, null, 0, controller, height);
                    mapWaypoint.geospherePoint = geoSpherePoint;
                    neighbors.Add(mapWaypoint);
                }
            }
        }

        return neighbors;
    }

    public override bool IsPathfindingElligible() // OCEAN and NONE Tiles.
    {
        return true;
    }

    public override bool IsPathfindingSuddenEnd() // Tiles already with Roads.
    {
        if (geospherePoint == null)
            return false;
        return false;
    }

    public override float DistanceTo(Pathfinding.Node target)
    {
        if (geospherePoint == null)
            return float.MaxValue;

        if (target is MapWaypoint)
        {
            MapWaypoint targetMapWaypoint = target as MapWaypoint;
            float geoSphereDistance = (targetMapWaypoint.geospherePoint.AsVector3() - geospherePoint.AsVector3()).magnitude;
            float heightFactor = targetMapWaypoint.Height / height;
            if (heightFactor > 1)
                geoSphereDistance *= heightFactor * heightInfluence;
            else
                geoSphereDistance *= heightFactor;

            return geoSphereDistance;
        }
        else
            return 0;
    }

    public override bool Equals(object obj)
    {
        // False if the object is null
        if (obj == null)
            return false;

        // Try casting to a DistanceCell. If it fails, return false;
        MapWaypoint pMapWaypoint = obj as MapWaypoint;
        if (pMapWaypoint == null)
            return false;

        if (geospherePoint != null && pMapWaypoint.geospherePoint != null)
        {
            if (Mathf.Abs(geospherePoint.x - pMapWaypoint.geospherePoint.x) <= 0.00005 &&
                Mathf.Abs(geospherePoint.y - pMapWaypoint.geospherePoint.y) <= 0.00005 &&
                Mathf.Abs(geospherePoint.z - pMapWaypoint.geospherePoint.z) <= 0.00005)
                return true;
            return false;
        }

        if (Mathf.Abs(position.x - pMapWaypoint.position.x) <= 0.00005 &&
            Mathf.Abs(position.y - pMapWaypoint.position.y) <= 0.00005 &&
            Mathf.Abs(position.z - pMapWaypoint.position.z) <= 0.00005)
            return true;
        return false;
    }

    public override int GetHashCode()
    {
        int myHash = unchecked(position.GetHashCode() * 523 + geospherePoint.GetHashCode() * 541);
        return myHash;
    }
    #endregion
}
