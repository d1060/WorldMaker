using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    Ray prevRay;
    RaycastHit prevMapHit;
    Ray ray;
    RaycastHit mapHit;
    GraphicRaycaster graphicRaycaster;
    public GameObject eventSystemObject;
    EventSystem eventSystem;
    Vector3 prevMousePosition;
    public Map map;
    //public Globe globe;
    //public SectorizedGeoSphere.Sphere sectorizedGeoSphere;
    public Geosphere geosphere;
    float minCameraDistance = 4;
    float maxCameraDistance = 190;
    float zoomMultiplier = 10;
    Vector3 cameraStartingPosition;
    Plane[] boundaryPlanes;
    Camera cam = null;
    int zoomLevel = 1;
    public GameObject contextMenu;
    Canvas canvas;

    double visibleLowerLongitude = 0;
    double visibleUpperLongitude = 1;
    double visibleLowerLatitude = 0;
    double visibleUpperLatitude = 1;

    public int ZoomLevel { get { return zoomLevel; } }
    public double VisibleLowerLongitude { get { return visibleLowerLongitude; } }
    public double VisibleUpperLongitude { get { return visibleUpperLongitude; } }
    public double VisibleLowerLatitude { get { return visibleLowerLatitude; } }
    public double VisibleUpperLatitude { get { return visibleUpperLatitude; } }
    public float MinCameraDistance { get { return minCameraDistance; } }
    public float MaxCameraDistance { get { return maxCameraDistance; } }

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        boundaryPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        cameraStartingPosition = cam.transform.position;

        canvas = this.gameObject.GetComponentInChildren<Canvas>();
        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = eventSystemObject.GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        float xAxisMovement = Input.GetAxis("Mouse X");
        float yAxisMovement = Input.GetAxis("Mouse Y");
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        bool isMouseButtonDown = false;
        bool isLeftMouseButtonDown = false;
        //bool isMouseButton2Down = false;
        if (Input.GetMouseButton(0))
            isMouseButtonDown = true;
        //if (Input.GetMouseButton(1))
        //    isMouseButton2Down = true;
        if(Input.GetKeyDown(KeyCode.Mouse1))
            isLeftMouseButtonDown = true;


        bool performZoom = true;
        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && mouseWheel != 0)
        {
            if (map.ChangeTerrainBrushSize(mouseWheel))
                performZoom = false;
        }

        if ((Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)) && mouseWheel != 0)
        {
            if (map.ChangeTerrainBrushStrength(mouseWheel))
                performZoom = false;
        }

        if (!performZoom)
            mouseWheel = 0;

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> graphicRaycastResults = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, graphicRaycastResults);

        if (isMouseButtonDown)
        {
            bool isClickingContextMenu = false;
            foreach (RaycastResult raycastResult in graphicRaycastResults)
            {
                if (raycastResult.gameObject == contextMenu)
                    isClickingContextMenu = true;
            }
            if (!isClickingContextMenu)
                CloseContextMenu();
        }

        // Only moves camera if no UI element was pressed.
        if (graphicRaycastResults.Count == 0 && !isLeftMouseButtonDown)
        {
            cam = GetComponent<Camera>();
            ray = cam.ScreenPointToRay(Input.mousePosition);
            ray = GetRayBeyondCanvas(ray);
            bool isMapHit = Physics.Raycast(ray, out mapHit);
            //Debug.Log("Is map hit: " + isMapHit);

            if (map.ShowGlobe)
            {
                geosphere?.MapHit();
            }
            else if (mapHit.collider != null)
            {
                Vector3 hitPoint = mapHit.point;
                map.MouseMapHit = hitPoint;
            }

            if (mouseWheel != 0)
            {
                //Debug.Log("Doing Zoom.");
                DoZoom(mouseWheel, isMapHit, mapHit);
            }

            if ((xAxisMovement != 0 || yAxisMovement != 0) && isMouseButtonDown && prevMousePosition.x != float.MinValue)
            {
                //Debug.Log("Doing Pan.");
                DoPan();
            }

            if (isMouseButtonDown)
            {
                prevMousePosition = Input.mousePosition;
                prevMapHit = mapHit;
                prevRay = ray;
            }
            else
            {
                prevMousePosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }

        // Context Menu Opening.
        else if (graphicRaycastResults.Count == 0 && isLeftMouseButtonDown)
        {
            if (!IsClickGoingToHitAWaypointMarker())
                OpenContextMenu();
            else
                CloseContextMenu();
        }
    }

    bool IsClickGoingToHitAWaypointMarker()
    {
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
                    return true;
                }
            }
        }
        return false;
    }

    void DoZoom(float zoomChange, bool isMapHit, RaycastHit hit)
    {
        zoomChange *= zoomMultiplier;

        if (isMapHit)
        {
            Vector3 zoomDirection = hit.point - transform.position;
            zoomDirection.Normalize();
            zoomDirection *= zoomChange;
            Vector3 cameraPosition = transform.position;

            float xDelta = zoomDirection.x;
            float yDelta = zoomDirection.y;
            float zDelta = zoomDirection.z;
            Vector3 cameraPan = new Vector3(0, yDelta, zDelta);

            cameraPosition += cameraPan;

            if (xDelta != 0)
            {
                map.Shift(-xDelta);
                //globe.Shift(map.CenterScreenWorldPosition, cam);
            }

            CalculateVisibleFlatLatitudeAndLongitude();

            zoomLevel = GetZoomLevel(-transform.position.z);

            if (-cameraPosition.z >= minCameraDistance && -cameraPosition.z <= maxCameraDistance)
            {
                if (!map.ShowGlobe)
                {
                    if (!IsInsidePlanes(cameraPosition))
                    {
                        cameraPosition = MoveIntoPlanes(cameraPosition);
                        CalculateVisibleFlatLatitudeAndLongitude();
                        geosphere?.RotateCameraTo((visibleLowerLongitude + visibleUpperLongitude) / 2, (visibleLowerLatitude + visibleUpperLatitude) / 2);
                    }
                }

                if (cameraPosition.z > -minCameraDistance)
                    cameraPosition.z = -minCameraDistance;
                transform.position = cameraPosition;
            }

            geosphere?.ZoomCameraTo(-transform.position.z);
        }
        else if (geosphere != null)
        {
            geosphere.Zoom(zoomChange, this);

            float latitude = 0;
            float longitude = 0;
            geosphere.GetCameraLatitudeLongitude(ref latitude, ref longitude);

            Vector3 cameraPosition = Vector3.zero;
            cameraPosition.x = 0;
            cameraPosition.y = (float)(latitude * map.MapHeight - map.MapHeight / 2);
            cameraPosition.z = 0 - geosphere.GetCameraDistance();
            if (cameraPosition.z > -minCameraDistance)
                cameraPosition.z = -minCameraDistance;
            transform.position = cameraPosition;
        }
    }

    void DoPan()
    {
        if (prevMousePosition.x == float.MinValue)
            return;

        Plane z = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));

        Ray rayBeyondCanvas = GetRayBeyondCanvas(ray);
        float hitDistance;
        z.Raycast(rayBeyondCanvas, out hitDistance);

        cam = GetComponent<Camera>();
        Ray prevRay = cam.ScreenPointToRay(prevMousePosition);
        Ray prevRayBeyondCanvas = GetRayBeyondCanvas(prevRay);

        float prevHitDistance;
        z.Raycast(prevRayBeyondCanvas, out prevHitDistance);

        Vector3 from = prevRayBeyondCanvas.GetPoint(prevHitDistance);
        Vector3 to = rayBeyondCanvas.GetPoint(hitDistance);

        float xDelta = from.x - to.x;
        float yDelta = from.y - to.y;

        Vector3 cameraPan = new Vector3(0, yDelta, 0);
        Vector3 cameraPosition = cam.transform.position + cameraPan;

        if (map.ShowGlobe)
        {
            if (geosphere != null)
            {
                Vector3 prevGlobePoint = geosphere.MapHit(prevMousePosition);
                Vector3 globePoint = geosphere.MapHit(Input.mousePosition);
                if (globePoint != Vector3.zero && prevGlobePoint != Vector3.zero)
                {
                    geosphere.PanCameraTo(prevGlobePoint, globePoint);

                    float latitude = 0;
                    float longitude = 0;
                    geosphere.GetCameraLatitudeLongitude(ref latitude, ref longitude);
                    map.ShiftTo(longitude);
                }
                prevGlobePoint = globePoint;
            }
        }
        else
        {
            if (!IsInsidePlanes(cameraPosition))
                cameraPosition = MoveIntoPlanes(cameraPosition);

            if (xDelta != 0)
            {
                map.Shift(-xDelta);
            }
        }

        cameraPosition.z = cam.transform.position.z;
        cam.transform.position = cameraPosition;

        CalculateVisibleFlatLatitudeAndLongitude();

        if (!map.ShowGlobe)
            geosphere?.RotateCameraTo((visibleLowerLongitude + visibleUpperLongitude)/2, (visibleLowerLatitude + visibleUpperLatitude)/2);
    }

    public void BringCameraIntoViewPlanes()
    {
        Vector3 cameraPosition = cam.transform.position;
        if (!IsInsidePlanes(cameraPosition))
        {
            cameraPosition = MoveIntoPlanes(cameraPosition);
            cam.transform.position = cameraPosition;
        }
    }

    public static Ray GetRayBeyondCanvas(Ray ray)
    {
        Vector3 direction = ray.direction;
        Vector3 origin = ray.origin;

        direction.Normalize();
        direction *= 2;
        origin += direction;

        Ray outputRay = new Ray(origin, direction);
        return outputRay;
    }

    bool IsInsidePlanes(Vector3 position)
    {
        for (int i = 0; i <= 3; i++) // Left 0, Right 1, Down 2, Up 3, Near 4, Far 5
        {
            Plane plane = boundaryPlanes[i];
            if (plane.GetDistanceToPoint(position) < 0)
                return false;
        }
        return true;
    }

    Vector3 MoveIntoPlanes(Vector3 point)
    {
        if (boundaryPlanes[2].GetDistanceToPoint(point) >= 0 &&
            boundaryPlanes[3].GetDistanceToPoint(point) >= 0)
            return point;

        Plane cameraPlane = new Plane(-transform.forward, point);

        Line3d lineBottom = cameraPlane.IntersectionWith(boundaryPlanes[2]);
        Line3d lineTop = cameraPlane.IntersectionWith(boundaryPlanes[3]);

        float distanceToTop = lineTop.DistanceToPoint(point);
        float distanceToBottom = lineBottom.DistanceToPoint(point);

        Vector3 movedPoint = point;

        if (distanceToTop < distanceToBottom)
        {
            movedPoint = lineTop.ProjectionFrom(point);
        }
        else
        {
            movedPoint = lineBottom.ProjectionFrom(point);
        }
        return movedPoint;
    }

    int GetZoomLevel(float distance)
    {
        for (int i = 0; i < map.textureSettings.zoomLevelDistances.Length; i++)
        {
            if (distance > map.textureSettings.zoomLevelDistances[i])
                return i >= 1 ? i : 1;
        }
        if (distance <= map.textureSettings.zoomLevelDistances[map.textureSettings.zoomLevelDistances.Length - 1])
            return map.textureSettings.zoomLevelDistances.Length - 1;

        return 1;
    }

    public void CalculateVisibleFlatLatitudeAndLongitude()
    {
        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(cam); // Left 0, Right 1, Down 2, Up 3, Near 4, Far 5
        Plane mapPlane = new Plane(new Vector3(0, 0, -1), 0);

        Vector3 lowerLeft = new Vector3(0, 0, 0);
        Vector3 upperRight = new Vector3(0, 0, 0);

        PlanesIntersectAtSinglePoint(mapPlane, cameraPlanes[0], cameraPlanes[2], out lowerLeft);
        PlanesIntersectAtSinglePoint(mapPlane, cameraPlanes[1], cameraPlanes[3], out upperRight);

        visibleLowerLongitude = ((lowerLeft.x - map.transform.position.x) / map.MapWidth) + 0.5;
        visibleUpperLongitude = ((upperRight.x - map.transform.position.x) / map.MapWidth) + 0.5;
        visibleLowerLatitude = ((lowerLeft.y - map.transform.position.y) / map.MapHeight) + 0.5;
        visibleUpperLatitude = ((upperRight.y - map.transform.position.y) / map.MapHeight) + 0.5;

        if (!map.ShowGlobe)
        {
            if (visibleLowerLatitude < 0) visibleLowerLatitude = 0;
            if (visibleUpperLatitude > 1) visibleUpperLatitude = 1;
        }
    }

    private static bool PlanesIntersectAtSinglePoint(Plane p0, Plane p1, Plane p2, out Vector3 intersectionPoint)
    {
        const float EPSILON = 1e-4f;

        float det = Vector3.Dot(Vector3.Cross(p0.normal, p1.normal), p2.normal);
        if (Mathf.Abs(det) < EPSILON)
        {
            intersectionPoint = Vector3.zero;
            return false;
        }

        intersectionPoint =
            (-(p0.distance * Vector3.Cross(p1.normal, p2.normal)) -
            (p1.distance * Vector3.Cross(p2.normal, p0.normal)) -
            (p2.distance * Vector3.Cross(p0.normal, p1.normal))) / det;

        return true;
    }

    public void OpenContextMenu()
    {
        if (!map.DoingTerrainBrush)
        {
            ContextMenu contextMenuScript = contextMenu.GetComponent<ContextMenu>();
            if (contextMenuScript != null)
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                contextMenuScript.Open(new Vector3(Input.mousePosition.x - canvasRect.sizeDelta.x / 2,
                                                   Input.mousePosition.y - canvasRect.sizeDelta.y / 2,
                                                   Input.mousePosition.z));
            }
        }
    }

    public void CloseContextMenu()
    {
        ContextMenu contextMenuScript = contextMenu.GetComponent<ContextMenu>();
        if (contextMenuScript != null)
        {
            contextMenuScript.Close();
        }
    }
}
