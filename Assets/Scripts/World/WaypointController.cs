using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaypointController : MonoBehaviour
{
    public Map map;
    CameraController cameraController = null;
    float lastDistance = 0;
    float sizeMultiplierAtMaxDistance = 20;
    float sizeMultiplierAtMinDistance = 0.8f;
    static List<Waypoint> waypoints = new List<Waypoint>();
    bool isLeftMouseButtonDown = false;
    bool isRightMouseButtonDown = false;
    bool hasMoved = false;
    static int count = 1;
    float height = 0;
    public TMPro.TextMeshProUGUI totalPathLabel = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (map != null)
        {
            float xAxisMovement = Input.GetAxis("Mouse X");
            float yAxisMovement = Input.GetAxis("Mouse Y");
            //bool leftMouseButtonClick = false;
            bool leftMouseButtonUp = false;
            bool rightMouseButtonClick = false;
            //bool rightMouseButtonUp = false;
            bool isUIClick = false;
            if (Input.GetMouseButton(0))
            {
                //if (!isLeftMouseButtonDown)
                //    leftMouseButtonClick = true;
                isLeftMouseButtonDown = true;
            }
            else
            {
                if (isLeftMouseButtonDown)
                    leftMouseButtonUp = true;
                isLeftMouseButtonDown = false;
            }
            if (Input.GetMouseButton(1))
            {
                if (!isRightMouseButtonDown)
                    rightMouseButtonClick = true;
                isRightMouseButtonDown = true;
            }
            else
            {
                //if (isRightMouseButtonDown)
                //    rightMouseButtonUp = true;
                isRightMouseButtonDown = false;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                isUIClick = true;
            }

            hasMoved |= xAxisMovement != 0 || yAxisMovement != 0;

            if (cameraController == null && map.cam != null)
            {
                cameraController = map.cam.GetComponent<CameraController>();
            }

            if (cameraController != null)
            {
                Camera activeCamera = null;
                if (!map.ShowGlobe)
                    activeCamera = map.cam;
                else
                    activeCamera = cameraController.map.geoSphere.transform.GetComponentInChildren<Camera>();

                Ray mouseRay = activeCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(mouseRay);
                //bool foundMeshCollider = false;
                //RaycastHit colliderHit;
                Vector3 hitPoint = Vector3.zero;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider != null)
                    {
                        //colliderHit = hit;
                        //foundMeshCollider = true;
                        hitPoint = hit.point;
                        break;
                    }
                }

                float currentDistance = 0;
                Vector3 globePoint = Vector3.zero;
                if (!map.ShowGlobe)
                {
                    transform.position = hitPoint;
                    transform.forward = new Vector3(0, 0, 1);

                    currentDistance = -cameraController.transform.position.z;
                    globePoint = map.MapToGlobePoint(transform.position);
                }
                else
                {
                    Camera camera = cameraController.map.geoSphere.transform.GetComponentInChildren<Camera>();

                    Vector3 positionVector = camera.transform.localPosition;
                    currentDistance = positionVector.magnitude - cameraController.map.geoSphere.Radius;
                    if (currentDistance < cameraController.MinCameraDistance)
                        currentDistance = cameraController.MinCameraDistance;
                    else if (currentDistance > cameraController.MaxCameraDistance)
                        currentDistance = cameraController.MaxCameraDistance;
                    globePoint = hitPoint;
                }
                height = GetObjectHeightInUnityGlobe(globePoint);

                if (lastDistance != currentDistance)
                {
                    lastDistance = currentDistance;
                    float multiplier = ((currentDistance - cameraController.MinCameraDistance) / (cameraController.MaxCameraDistance - cameraController.MinCameraDistance)) * (sizeMultiplierAtMaxDistance - sizeMultiplierAtMinDistance) + sizeMultiplierAtMinDistance;
                    transform.localScale = new Vector3(multiplier*0.32f, multiplier, 1);
                }

                if (!hasMoved && leftMouseButtonUp && !isUIClick)
                {
                    Transform parent = null;
                    Transform otherParent = null;
                    Waypoint waypoint = new Waypoint();

                    // Pin a Waypoint.
                    if (!map.ShowGlobe)
                    {
                        parent = map.transform;
                        otherParent = map.geoSphere.transform;
                        MapWaypoint mapWaypoint = new MapWaypoint(map.waypointMarkerPrefab, transform.position, transform.forward, transform.up, transform.localScale, parent, count, this, height);

                        // Also plots a waypoint on the globe.
                        Vector3 forward = map.geoSphere.transform.position - globePoint;
                        forward.Normalize();
                        MapWaypoint globeWaypoint = new MapWaypoint(map.waypointMarkerPrefab, globePoint, forward, transform.up, transform.localScale, otherParent, count, this, height);
                        globeWaypoint.FindClosestPoint();

                        waypoint.MapWaypoint = mapWaypoint;
                        waypoint.GlobeWaypoint = globeWaypoint;
                    }
                    else
                    {
                        parent = map.geoSphere.transform;
                        otherParent = map.transform;
                        MapWaypoint globeWaypoint = new MapWaypoint(map.waypointMarkerPrefab, transform.position, transform.forward, transform.up, transform.localScale, parent, count, this, height);
                        globeWaypoint.FindClosestPoint();

                        // Also plots a waypoint on the map.
                        Vector3 mapPoint = map.GlobeToMapPoint(transform.position);
                        Vector3 forward = new Vector3(0, 0, 1);
                        forward.Normalize();
                        MapWaypoint mapWaypoint = new MapWaypoint(map.waypointMarkerPrefab, mapPoint, forward, transform.up, transform.localScale, otherParent, count, this, height);

                        waypoint.MapWaypoint = mapWaypoint;
                        waypoint.GlobeWaypoint = globeWaypoint;
                    }
                    //waypoint.Index = waypoints.Count;
                    waypoints.Add(waypoint);

                    if (waypoints.Count >= 2)
                    {
                        PlotNewPath(waypoints.Count - 2, waypoints.Count - 1);
                    }
                    count++;
                }
                else if (!hasMoved && rightMouseButtonClick && !isUIClick)
                {
                    // Unpin a Waypoint.
                    Camera camera = null;
                    if (!map.ShowGlobe)
                    {
                        camera = map.cam;
                    }
                    else
                    {
                        camera = map.geoSphere.transform.GetComponentInChildren<Camera>();
                    }

                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit[] mapHits = Physics.RaycastAll(ray, 1000);

                    foreach (RaycastHit mapHit in mapHits)
                    {
                        if (mapHit.collider != null)
                        {
                            GameObject colliderGO = mapHit.collider.gameObject;
                            if (colliderGO.name.StartsWith("Map Waypoint "))
                            {
                                RemoveAllWaypointsNamed(colliderGO.name);
                            }
                        }
                    }
                }
            }

            if (!isLeftMouseButtonDown)
                hasMoved = false;
        }
    }

    static public int WaypointsCount
    {
        get
        {
            return waypoints == null ? 0 : waypoints.Count;
        }
    }

    void RemoveAllWaypointsNamed(string name)
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            MapWaypoint mapWaypoint = waypoints[i].MapWaypoint;
            bool bRemove = false;
            if (mapWaypoint != null && mapWaypoint.Name == name)
            {
                mapWaypoint.Destroy();
                bRemove = true;
            }
            MapWaypoint globeWaypoint = waypoints[i].GlobeWaypoint;
            if (globeWaypoint != null && globeWaypoint.Name == name)
            {
                globeWaypoint.Destroy();
                bRemove = true;
            }

            if (bRemove)
            {
                waypoints[i].Path.Clear();
                waypoints.RemoveAt(i);
                if (i > 0)
                {
                    waypoints[i - 1].Path.Clear();
                    if (waypoints[i - 1].MapWaypoint.LengthLabelGameObject != null)
                        GameObject.DestroyImmediate(waypoints[i - 1].MapWaypoint.LengthLabelGameObject);
                    if (waypoints[i - 1].GlobeWaypoint.LengthLabelGameObject != null)
                        GameObject.DestroyImmediate(waypoints[i - 1].GlobeWaypoint.LengthLabelGameObject);
                    if (waypoints[i - 1].MapWaypoint.PathGameObject != null)
                        GameObject.DestroyImmediate(waypoints[i - 1].MapWaypoint.PathGameObject);
                    if (waypoints[i - 1].GlobeWaypoint.PathGameObject != null)
                        GameObject.DestroyImmediate(waypoints[i - 1].GlobeWaypoint.PathGameObject);

                    if (waypoints.Count >= 2 && waypoints.Count > i)
                    {
                        PlotNewPath(i - 1, i);
                    }
                }
                i--;
            }
        }

        float totalPathLength = 0;
        foreach (Waypoint waypoint in waypoints)
        {
            totalPathLength += waypoint.PathLength;
        }
        totalPathLabel.text = "Total Path Length: " + totalPathLength.ToString("#0") + " km";
    }

    //public float GetObjectHeightInFlatMap(Vector3 mapPosition)
    //{
        //float lat = 0;
        //float lon = 0;
        //float waypointWidth = transform.localScale.x * (1 / map.MapWidth);
        //float waypointHeight = transform.localScale.y * (1 / map.MapHeight);

        //map.GetPointLatLon(mapPosition, out lat, out lon);
        //Vector2 polarRatio = new Vector2(lon, lat);
        //float height = map.GetHeight(polarRatio);

        //Vector2 llLL = new Vector2(polarRatio.x - waypointWidth / 2, polarRatio.y - waypointHeight / 2);
        //Vector2 llLR = new Vector2(polarRatio.x + waypointWidth / 2, polarRatio.y - waypointHeight / 2);
        //Vector2 llUL = new Vector2(polarRatio.x - waypointHeight / 2, polarRatio.y + waypointHeight / 2);
        //Vector2 llUR = new Vector2(polarRatio.x + waypointHeight / 2, polarRatio.y + waypointHeight / 2);

        //float heightLL = map.GetHeight(llLL);
        //float heightLR = map.GetHeight(llLR);
        //float heightUL = map.GetHeight(llUL);
        //float heightUR = map.GetHeight(llUR);

        //float highestHeight = (new float[] { height, heightLL, heightLR, heightUL, heightUR }).Max();

        //if (highestHeight < map.WaterLevel)
        //    highestHeight = map.WaterLevel;
        //highestHeight -= map.WaterLevel;
        //highestHeight *= map.HeightRatio;
        //transform.position = new Vector3(mapPosition.x, mapPosition.y, -(float)highestHeight);
        //transform.forward = new Vector3(0, 0, 1);
        //return height;
    //}

    //public float GetObjectHeightInGlobe(Vector3 globePosition)
    //{
    //    globePosition -= map.geoSphere.transform.position;
    //    Vector2 polar = globePosition.CartesianToPolarRatio(map.geoSphere.Radius);

    //    float x = ((polar.x - 0.5f) * map.MapWidth) + map.transform.position.x;
    //    float y = ((polar.y - 0.5f) * map.MapHeight) + map.transform.position.y;
    //    float height = map.GetHeight(polar);
    //    return height;
    //}

    public float GetObjectHeightInUnityGlobe(Vector3 globePosition)
    {
        Vector2 polar = globePosition.CartesianToPolarRatio(1);
        float height = map.HeightAtCoordinatesUntilWaterLevel(polar);
        return height;
    }

    void PlotNewPath(int indexStart, int indexEnd)
    {
        MapWaypoint from = waypoints[indexStart].GlobeWaypoint;
        MapWaypoint to = waypoints[indexEnd].GlobeWaypoint;

        if (!from.Equals(to))
        {
            List<Pathfinding.Node> path = Pathfinding.AStar.FindPath(from, to);
            if (path.Count > 0)
            {
                ((MapWaypoint)path[0]).Position = from.GameObjectPosition;
                ((MapWaypoint)path[0]).Position.Normalize();
                if (path.Count > 1)
                {
                    ((MapWaypoint)path[path.Count - 1]).Position = to.GameObjectPosition;
                    ((MapWaypoint)path[path.Count - 1]).Position.Normalize();
                }

                waypoints[indexStart].Path = path;
                List<Pathfinding.Node> lastPath = null;
                if (indexStart > 0)
                    lastPath = waypoints[indexStart - 1].Path;
                waypoints[indexStart].CreatePathObjects(map, lastPath, indexStart);
            }
        }

        float totalPathLength = 0;
        foreach (Waypoint waypoint in waypoints)
        {
            totalPathLength += waypoint.PathLength;
        }
        totalPathLabel.text = "Total Path Length: " + totalPathLength.ToString("#0") + " km";
    }

    static public void ClearAll()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            MapWaypoint mapWaypoint = waypoints[i].MapWaypoint;
            if (mapWaypoint != null)
            {
                mapWaypoint.Destroy();
            }
            MapWaypoint globeWaypoint = waypoints[i].GlobeWaypoint;
            if (globeWaypoint != null)
            {
                globeWaypoint.Destroy();
            }
            waypoints[i].Path.Clear();
            waypoints.RemoveAt(i);
            i--;
        }
    }
}
