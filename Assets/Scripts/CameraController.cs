using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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
    public float navigationKeySpeed = 4f;
    public float navigationKeyDelay = 1.0f;
    public float minNavigationSpeed = 0.04f;
    static float minCameraDistance = 4;
    static float maxCameraDistance = 190;
    public float zoomSpeed = 25;
    Vector3 cameraStartingPosition;
    Vector3 targetCameraPosition;
    Plane[] boundaryPlanes;
    Camera cam = null;
    //int zoomLevel = 1;
    public GameObject contextMenu;
    public GameObject zoomContextMenu;
    Canvas canvas;
    TMP_InputField mainTextureTextBox;
    TMP_InputField heightmapTextBox;
    TMP_InputField landMaskTextBox;

    DateTime navigationKeyStart;
    bool navigationKeyDown = false;
    bool isNavigating = false;
    float navigationSpeed = 0; // In a ratio from 0 to 1 (1 = navigationKeySpeed)
    float lastXAxisMovement = 0;
    float lastYAxisMovement = 0;
    public TMP_InputField worldNameText;
    public float smoothTime = 0.1f;
    private Vector3 velocity = Vector3.zero;

    double visibleLowerLongitude = 0;
    double visibleUpperLongitude = 1;
    double visibleLowerLatitude = 0;
    double visibleUpperLatitude = 1;

    //public int ZoomLevel { get { return zoomLevel; } }
    public double VisibleLowerLongitude { get { return visibleLowerLongitude; } }
    public double VisibleUpperLongitude { get { return visibleUpperLongitude; } }
    public double VisibleLowerLatitude { get { return visibleLowerLatitude; } }
    public double VisibleUpperLatitude { get { return visibleUpperLatitude; } }
    public static float MinCameraDistance { get { return minCameraDistance; } }
    public static float MaxCameraDistance { get { return maxCameraDistance; } }

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        boundaryPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        cameraStartingPosition = cam.transform.position;
        targetCameraPosition = cameraStartingPosition;

        canvas = this.gameObject.GetComponentInChildren<Canvas>();
        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = eventSystemObject.GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("FPS: " + 1.0f / Time.deltaTime);
        float xAxisMovement = Input.GetAxis("Mouse X");
        float yAxisMovement = Input.GetAxis("Mouse Y");
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            xAxisMovement = 0;
            yAxisMovement = 0;
        }
        bool isKeyPress = false;

        // Navigation Keys
        if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.RightArrow) || 
             Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow)) &&
            (!Input.GetKey(KeyCode.RightControl) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightShift) &&
             !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightAlt) && !Input.GetKey(KeyCode.LeftAlt)))
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                if (yAxisMovement < navigationKeySpeed)
                    yAxisMovement = navigationKeySpeed;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.RightArrow))
            {
                if (-xAxisMovement < navigationKeySpeed)
                    xAxisMovement = -navigationKeySpeed;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                if (-yAxisMovement < navigationKeySpeed)
                    yAxisMovement = -navigationKeySpeed;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow))
            {
                if (xAxisMovement < navigationKeySpeed)
                    xAxisMovement = navigationKeySpeed;
            }

            isKeyPress = true;
            if (navigationKeyDown == false)
            {
                navigationKeyStart = DateTime.Now;
                navigationKeyDown = true;
            }
            isNavigating = true;
        }
        else
        {
            if (navigationKeyDown == true)
            {
                navigationKeyStart = DateTime.Now;
                navigationKeyDown = false;
            }
        }

        if (navigationKeyDown)
        {
            TimeSpan ts = DateTime.Now - navigationKeyStart;
            if (ts.TotalSeconds >= navigationKeyDelay)
                navigationSpeed = 1;
            else
                navigationSpeed = (float)(ts.TotalSeconds / navigationKeyDelay);
            if (navigationSpeed < navigationKeySpeed / 10)
                navigationSpeed = navigationKeySpeed / 10;
        }
        else if (!navigationKeyDown && isNavigating)
        {
            TimeSpan ts = DateTime.Now - navigationKeyStart;
            if (ts.TotalSeconds >= navigationKeyDelay)
            {
                navigationSpeed = 0;
                isNavigating = false;
            }
            else
                navigationSpeed = 1 - (float)(ts.TotalSeconds / navigationKeyDelay);
            isKeyPress = true;

            xAxisMovement = lastXAxisMovement;
            yAxisMovement = lastYAxisMovement;
        }

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

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && mouseWheel != 0)
        {
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
                if (raycastResult.gameObject == contextMenu || raycastResult.gameObject == zoomContextMenu)
                    isClickingContextMenu = true;
            }
            if (!isClickingContextMenu)
            {
                //Debug.Log("Camera Controller: Close Context Menu - Not clicking on Context Menu.");
                CloseContextMenu();
            }
        }

        // Only moves camera if no UI element was pressed.
        if (graphicRaycastResults.Count == 0 && !isLeftMouseButtonDown && (xAxisMovement != 0 || yAxisMovement != 0 || mouseWheel != 0))
        {
            //Debug.Log("Moving camera by " + xAxisMovement + " x " + yAxisMovement + " y" + " wheel " + mouseWheel);
            //Debug.Log("Moving camera to " + targetCameraPosition);
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

            Vector3 planeHitPoint = new Vector3();
            if (mouseWheel != 0)
            {
                if (!isMapHit)
                {
                    float intersectionDistance = 0;
                    Plane zZero = new Plane(new Vector3(0, 0, -1), 0.0f);
                    zZero.Raycast(ray, out intersectionDistance);
                    Vector3 hitPoint = ray.GetPoint(intersectionDistance);
                }
                //Debug.Log("Doing Zoom.");
                DoZoom(mouseWheel, isMapHit ? mapHit.point : planeHitPoint);
            }

            if ((xAxisMovement != 0 || yAxisMovement != 0) && isMouseButtonDown && prevMousePosition.x != float.MinValue)
            {
                //Debug.Log("Doing Pan.");
                DoPan();
            }
            else if ((xAxisMovement != 0 || yAxisMovement != 0) && isKeyPress)
            {
                //Debug.Log("Doing Pan.");
                DoFixedPan(xAxisMovement * navigationSpeed, yAxisMovement * navigationSpeed);
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
            {
                //Debug.Log("Camera Controller: Open Context Menu.");
                OpenContextMenu();
            }
            else
            {
                //Debug.Log("Camera Controller: Close Context Menu - Not going to hit a waypoint Marker.");
                CloseContextMenu();
            }
        }
        else if (graphicRaycastResults.Count > 0)
        {
            if (mouseWheel != 0)
            {
                SliderMouseWheel(graphicRaycastResults[0], mouseWheel);
            }
        }

        lastXAxisMovement = xAxisMovement;
        lastYAxisMovement = yAxisMovement;
    }

    private void FixedUpdate()
    {
        if (targetCameraPosition != transform.position)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetCameraPosition, ref velocity, smoothTime);

            if (transform.position.x <= -map.mapWidth / 2)
            {
                transform.position = new Vector3(transform.position.x + map.mapWidth, transform.position.y, transform.position.z);
                targetCameraPosition = new Vector3(targetCameraPosition.x + map.mapWidth, targetCameraPosition.y, targetCameraPosition.z);
            }
            else if (transform.position.x > map.mapWidth / 2)
            {
                transform.position = new Vector3(transform.position.x - map.mapWidth, transform.position.y, transform.position.z);
                targetCameraPosition = new Vector3(targetCameraPosition.x - map.mapWidth, targetCameraPosition.y, targetCameraPosition.z);
            }

            if (transform.position.z > -minCameraDistance)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, -minCameraDistance);
                targetCameraPosition = new Vector3(targetCameraPosition.x, targetCameraPosition.y, -minCameraDistance);
            }
            if (transform.position.z < -maxCameraDistance)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, -maxCameraDistance);
                targetCameraPosition = new Vector3(targetCameraPosition.x, targetCameraPosition.y, -maxCameraDistance);
            }
        }
    }

    TMP_InputField MainTextureTextBox
    {
        get
        {
            if (mainTextureTextBox == null)
            {
                if (canvas != null)
                {
                    Transform tbTransform = canvas.transform.GetChildNamed_Recursive("MainTexture Text Box");
                    if (tbTransform != null)
                        mainTextureTextBox = tbTransform.GetComponent<TMP_InputField>();
                }
            }
            return mainTextureTextBox;
        }
    }

    TMP_InputField HeightmapTextBox
    {
        get
        {
            if (heightmapTextBox == null)
            {
                if (canvas != null)
                {
                    Transform tbTransform = canvas.transform.GetChildNamed_Recursive("Heightmap Text Box");
                    if (tbTransform != null)
                        heightmapTextBox = tbTransform.GetComponent<TMP_InputField>();
                }
            }
            return heightmapTextBox;
        }
    }

    TMP_InputField LandMaskTextBox
    {
        get
        {
            if (landMaskTextBox == null)
            {
                if (canvas != null)
                {
                    Transform tbTransform = canvas.transform.GetChildNamed_Recursive("LandMask Text Box");
                    if (tbTransform != null)
                        landMaskTextBox = tbTransform.GetComponent<TMP_InputField>();
                }
            }
            return landMaskTextBox;
        }
    }

    void SliderMouseWheel(RaycastResult raycastResult, float value)
    {
        if (raycastResult.gameObject.transform.parent == null)
            return;

        Transform parentTransform = raycastResult.gameObject.transform.parent;
        Slider slider = parentTransform.GetComponent<Slider>();

        if (slider == null)
        {
            if (parentTransform.parent == null)
                return;

            Transform grandParentTransform = parentTransform.parent;

            slider = grandParentTransform.GetComponent<Slider>();
            if (slider == null)
            {
                if (grandParentTransform.parent == null)
                    return;

                slider = grandParentTransform.parent.GetComponent<Slider>();
                if (slider == null)
                    return;
            }
        }

        float sliderValue = slider.value;
        float step = value * (slider.maxValue - slider.minValue) / 10;
        sliderValue += step;
        slider.value = sliderValue;
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

    void DoZoom(float zoomChange, Vector3 hitPoint)
    {
        zoomChange *= zoomSpeed;

        if (!map.ShowGlobe)
        {
            Vector3 zoomDirection = hitPoint - transform.position;

            zoomDirection.Normalize();
            zoomDirection *= zoomChange;
            Vector3 cameraPosition = targetCameraPosition;

            float zDelta = zoomDirection.z;
            Vector3 cameraPan = new Vector3(0, 0, zDelta);

            cameraPosition += cameraPan;

            if (cameraPosition.z > -minCameraDistance) cameraPosition.z = -minCameraDistance;
            if (cameraPosition.z < -maxCameraDistance) cameraPosition.z = -maxCameraDistance;

            if (!IsInsidePlanes(cameraPosition))
            {
                cameraPosition = MoveIntoPlanes(cameraPosition);
                CalculateVisibleFlatLatitudeAndLongitude();
                geosphere?.RotateCameraTo((visibleLowerLongitude + visibleUpperLongitude) / 2, (visibleLowerLatitude + visibleUpperLatitude) / 2);
            }

            cameraPan = cameraPosition - targetCameraPosition;
            targetCameraPosition += cameraPan;

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

            targetCameraPosition += cameraPosition;
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

        Vector3 cameraPan = new Vector3(xDelta, yDelta, 0);
        Vector3 cameraPosition = targetCameraPosition + cameraPan;

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
                }
                prevGlobePoint = globePoint;
            }
        }
        else
        {
            if (!IsInsidePlanes(cameraPosition))
                cameraPosition = MoveIntoPlanes(cameraPosition);
        }

        cameraPan = cameraPosition - targetCameraPosition;
        targetCameraPosition += cameraPan;
    }

    void DoFixedPan(float xAxisMovement, float yAxisMovement)
    {
        if (xAxisMovement == 0 && yAxisMovement == 0)
            return;

        //worldNameText.interactable = false;
        //MainTextureTextBox.interactable = true;
        //HeightmapTextBox.interactable = true;
        //LandMaskTextBox.interactable = true;

        float xDelta = xAxisMovement;
        float yDelta = yAxisMovement;

        Vector3 cameraPan = new Vector3(xDelta * 2, yDelta, 0);

        if (map.ShowGlobe)
        {
            if (geosphere != null)
            {
                EventSystem.current.SetSelectedGameObject(geosphere.gameObject);

                float latitude = 0;
                float longitude = 0;
                geosphere.GetCameraLatitudeLongitude(ref latitude, ref longitude);
                geosphere.MoveCameraBy(xDelta * 2, yDelta);
            }
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(map.gameObject);

            float cameraDistanceRatio = (-transform.position.z < minCameraDistance ? minCameraDistance : -transform.position.z) / maxCameraDistance;

            if (xDelta > 0)
                xDelta = (xDelta - minNavigationSpeed) * cameraDistanceRatio + minNavigationSpeed;
            else if (xDelta < 0)
                xDelta = (xDelta + minNavigationSpeed) * cameraDistanceRatio - minNavigationSpeed;

            if (yDelta > 0)
                yDelta = (yDelta - minNavigationSpeed) * cameraDistanceRatio + minNavigationSpeed;
            else if (yDelta < 0)
                yDelta = (yDelta + minNavigationSpeed) * cameraDistanceRatio - minNavigationSpeed;

            cameraPan = new Vector3(xDelta * 2, yDelta, 0);

            Vector3 cameraPosition = targetCameraPosition + cameraPan;

            if (!IsInsidePlanes(cameraPosition))
                cameraPosition = MoveIntoPlanes(cameraPosition);

            cameraPan = cameraPosition - targetCameraPosition;
        }

        targetCameraPosition += cameraPan;

        //worldNameText.interactable = true;
        //MainTextureTextBox.interactable = true;
        //HeightmapTextBox.interactable = true;
        //LandMaskTextBox.interactable = true;
    }

    public void BringCameraIntoViewPlanes()
    {
        Vector3 cameraPosition = cam.transform.position;
        if (!IsInsidePlanes(cameraPosition))
        {
            cameraPosition = MoveIntoPlanes(cameraPosition);

            transform.position = cameraPosition;
            targetCameraPosition = cameraPosition;
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
        float maxY = (map.mapHeight / 2 - cameraStartingPosition.y) * (position.z - cameraStartingPosition.z) / -cameraStartingPosition.z;
        float minY = (-map.mapHeight / 2 - cameraStartingPosition.y) * (position.z - cameraStartingPosition.z) / -cameraStartingPosition.z;

        return position.y <= maxY && position.y >= minY;

        //for (int i = 0; i <= 3; i++) // Left 0, Right 1, Down 2, Up 3, Near 4, Far 5
        //{
        //    Plane plane = boundaryPlanes[i];
        //    if (plane.GetDistanceToPoint(position) < 0)
        //        return false;
        //}
        //return true;
    }

    Vector3 MoveIntoPlanes(Vector3 point)
    {
        //if (boundaryPlanes[2].GetDistanceToPoint(point) >= 0 &&
        //    boundaryPlanes[3].GetDistanceToPoint(point) >= 0)
        //    return point;

        //Plane cameraPlane = new Plane(-transform.forward, point);

        //Line3d lineBottom = cameraPlane.IntersectionWith(boundaryPlanes[2]);
        //Line3d lineTop = cameraPlane.IntersectionWith(boundaryPlanes[3]);

        //float distanceToTop = lineTop.DistanceToPoint(point);
        //float distanceToBottom = lineBottom.DistanceToPoint(point);

        //Vector3 movedPoint = point;

        //if (distanceToTop < distanceToBottom)
        //{
        //    movedPoint = lineTop.ProjectionFrom(point);
        //}
        //else
        //{
        //    movedPoint = lineBottom.ProjectionFrom(point);
        //}
        //return movedPoint;

        if (point.z > -minCameraDistance) point.z = -minCameraDistance;
        if (point.z < -maxCameraDistance) point.z = -maxCameraDistance;

        float maxY = (map.mapHeight / 2 - cameraStartingPosition.y) * (point.z - cameraStartingPosition.z) / -cameraStartingPosition.z;
        float minY = (-map.mapHeight / 2 - cameraStartingPosition.y) * (point.z - cameraStartingPosition.z) / -cameraStartingPosition.z;

        if (point.y > maxY) point.y = maxY;
        if (point.y < minY) point.y = minY;

        return point;
    }

    int GetZoomLevel(float distance)
    {
        for (int i = 0; i < TextureManager.instance.Settings.zoomLevelDistances.Length; i++)
        {
            if (distance > TextureManager.instance.Settings.zoomLevelDistances[i])
                return i >= 1 ? i : 1;
        }
        if (distance <= TextureManager.instance.Settings.zoomLevelDistances[TextureManager.instance.Settings.zoomLevelDistances.Length - 1])
            return TextureManager.instance.Settings.zoomLevelDistances.Length - 1;

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
            ContextMenu contextMenuScript = map.DoingZoomBrush ? zoomContextMenu.GetComponent<ContextMenu>() : contextMenu.GetComponent<ContextMenu>();
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
        ContextMenu contextMenuScript = map.DoingZoomBrush ? zoomContextMenu.GetComponent<ContextMenu>() : contextMenu.GetComponent<ContextMenu>();
        if (contextMenuScript != null)
        {
            contextMenuScript.Close();
        }
    }

    public bool IsContextMenuOpen
    {
        get
        {
            ContextMenu contextMenuScript = map.DoingZoomBrush ? zoomContextMenu.GetComponent<ContextMenu>() : contextMenu.GetComponent<ContextMenu>();
            if (contextMenuScript != null)
            {
                return contextMenuScript.IsOpen;
            }
            return false;
        }
    }
}
