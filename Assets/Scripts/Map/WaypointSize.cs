using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointSize : MonoBehaviour
{
    public float height = 0;
    public float sizeMultiplierAtMaxDistance = 20;
    public float sizeMultiplierAtMinDistance = 0.8f;
    float lastDistance = 0;
    CameraController cameraController = null;
    public Map map;
    bool isInGlobe = false;
    bool isInGlobeSet = false;
    public GameObject pathGameObject = null;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!isInGlobeSet)
        {
            isInGlobeSet = true;
            if (transform.parent != null && transform.parent.gameObject != null)
            {
                //SectorizedGeoSphere.Sphere parentSphere = transform.parent.gameObject.GetComponent<SectorizedGeoSphere.Sphere>();
                Geosphere geosphere = transform.parent.gameObject.GetComponent<Geosphere>();
                if (geosphere != null)
                    isInGlobe = true;
            }
        }

        if (cameraController == null && map != null)
        {
            cameraController = map.cam.GetComponent<CameraController>();
        }

        if (cameraController != null && map != null)
        {
            float currentDistance = 0;
            Vector3 globePoint = Vector3.zero;
            if (!isInGlobe)
            {
                currentDistance = -cameraController.transform.position.z;
                //height = GetObjectHeightInFlatMap(transform.position);
                globePoint = map.MapToGlobePoint(transform.position);
            }
            else
            {
                Camera camera = cameraController.map.geoSphere.transform.GetComponentInChildren<Camera>();
                Vector3 positionVector = camera.transform.localPosition;
                currentDistance = positionVector.magnitude - cameraController.map.geoSphere.Radius;
                if (currentDistance < CameraController.MinCameraDistance)
                    currentDistance = CameraController.MinCameraDistance;
                else if (currentDistance > CameraController.MaxCameraDistance)
                    currentDistance = CameraController.MaxCameraDistance;
                globePoint = map.MapToGlobePoint(transform.position);
                //height = GetObjectHeightInGlobe(globePoint);
            }
            //if (height < map.WaterLevel)
            //    height = map.WaterLevel;

            if (lastDistance != currentDistance)
            {
                lastDistance = currentDistance;
                float multiplierRatio = ((currentDistance - CameraController.MinCameraDistance) / (CameraController.MaxCameraDistance - CameraController.MinCameraDistance));
                float multiplier = multiplierRatio * (sizeMultiplierAtMaxDistance - sizeMultiplierAtMinDistance) + sizeMultiplierAtMinDistance;
                transform.localScale = new Vector3(multiplier * 0.32f, multiplier, 1);

                if (pathGameObject != null)
                {
                    LineRenderer lineRenderer = pathGameObject.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                        lineRenderer.widthMultiplier = multiplierRatio;
                }
            }
        }
    }

    //public float GetObjectHeightInFlatMap(Vector3 mapPosition)
    //{
    //    float lat = 0;
    //    float lon = 0;
    //    float waypointWidth = transform.localScale.x * (1 / map.MapWidth);
    //    float waypointHeight = transform.localScale.y * (1 / map.MapHeight);

    //    map.GetPointLatLon(mapPosition, out lat, out lon);
    //    Vector2 polarRatio = new Vector2(lon, lat);
    //    float height = map.GetHeight(polarRatio);

    //    Vector2 llLL = new Vector2(polarRatio.x - waypointWidth / 2, polarRatio.y - waypointHeight / 2);
    //    Vector2 llLR = new Vector2(polarRatio.x + waypointWidth / 2, polarRatio.y - waypointHeight / 2);
    //    Vector2 llUL = new Vector2(polarRatio.x - waypointHeight / 2, polarRatio.y + waypointHeight / 2);
    //    Vector2 llUR = new Vector2(polarRatio.x + waypointHeight / 2, polarRatio.y + waypointHeight / 2);

    //    float heightLL = map.GetHeight(llLL);
    //    float heightLR = map.GetHeight(llLR);
    //    float heightUL = map.GetHeight(llUL);
    //    float heightUR = map.GetHeight(llUR);

    //    float highestHeight = (new float[] { height, heightLL, heightLR, heightUL, heightUR }).Max();

    //    //if (highestHeight < map.WaterLevel)
    //    //    highestHeight = map.WaterLevel;
    //    //highestHeight -= map.WaterLevel;
    //    highestHeight *= map.HeightRatio;
    //    //transform.position = new Vector3(mapPosition.x, mapPosition.y, -(float)highestHeight);
    //    //transform.forward = new Vector3(0, 0, 1);
    //    return height;
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
}
