using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TerrainBrush : MonoBehaviour
{
    [Range(3, 100)]
    public int numberOfDivisions = 10;
    public float radius = 100;
    public float strength = 1;
    public float thickness = 1; // Thickness at distance 190
    public Map map;
    public GameObject eventSystemObject;
    public Canvas canvas;
    public GameObject brushLeft;
    public GameObject brushRight;

    int prevDivisions = 10;
    float prevRadius = 100;
    float prevThickness = 100;
    bool hasMoved = false;
    CameraController cameraController = null;
    float currentDistance = 0;
    float lastDistance = 0;
    float startingThickness = 0;
    //bool movedWhileRightMouseButtonWasDown = false;
    Vector2 currentCoordinates = new Vector2(0.5f, 0.5f);
    bool isLeftMouseButtonDown = false;
    bool isRightMouseButtonDown = false;
    EventSystem eventSystem;
    GraphicRaycaster graphicRaycaster;

    // Start is called before the first frame update
    void Start()
    {
        startingThickness = thickness;
        BuildMesh();
        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = eventSystemObject.GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        float xAxisMovement = Input.GetAxis("Mouse X");
        float yAxisMovement = Input.GetAxis("Mouse Y");
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        hasMoved = xAxisMovement != 0 || yAxisMovement != 0 || mouseWheel != 0;

        if (Input.GetMouseButton(0))
        {
            isLeftMouseButtonDown = true;
        }
        else
        {
            //if (isLeftMouseButtonDown)
            //    leftMouseButtonUp = true;
            isLeftMouseButtonDown = false;
        }
        if (Input.GetMouseButton(1))
        {
            //if (!isRightMouseButtonDown)
            //    rightMouseButtonClick = true;
            isRightMouseButtonDown = true;
        }
        else
        {
            isRightMouseButtonDown = false;
        }

        if (map == null)
            return;

        Camera activeCamera = null;
        if (!map.ShowGlobe)
        {
            activeCamera = map.cam;
            if (brushLeft != null && !brushLeft.activeSelf) brushLeft.SetActive(true);
            if (brushRight != null && !brushRight.activeSelf) brushRight.SetActive(true);
        }
        else
        {
            activeCamera = map.geoSphere.transform.GetComponentInChildren<Camera>();
            if (brushLeft != null && brushLeft.activeSelf) brushLeft.SetActive(false);
            if (brushRight != null && brushRight.activeSelf) brushRight.SetActive(false);
        }

        Ray mouseRay = activeCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(mouseRay);

        if (cameraController == null && map != null && map.cam != null)
        {
            cameraController = map.cam.GetComponent<CameraController>();
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> graphicRaycastResults = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, graphicRaycastResults);

        if (graphicRaycastResults.Count == 0)
        {
            if (isLeftMouseButtonDown)
            {
                //if (!movedWhileRightMouseButtonWasDown)
                //{
                RaiseTerrain();
                //}

                //movedWhileRightMouseButtonWasDown = false;
            }
            else if (isRightMouseButtonDown)
            {
                LowerTerrain();
            }
        }

        Vector3 hitPoint = Vector3.zero;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != cameraController.gameObject)
            {
                hitPoint = hit.point;
                break;
            }
        }

        //if (hasMoved && isRightMouseButtonDown)
        //    movedWhileRightMouseButtonWasDown = true;

        if (hasMoved && hitPoint != Vector3.zero)
        {
            if (!map.ShowGlobe)
            {
                transform.position = new Vector3(hitPoint.x, hitPoint.y, -0.1f);
                transform.forward = new Vector3(0, 0, 1);
                currentDistance = -activeCamera.transform.position.z;

                currentCoordinates.x = ((hitPoint.x - map.transform.position.x) / map.mapWidth) + 0.5f;
                currentCoordinates.y = ((hitPoint.y - map.transform.position.y) / map.mapHeight) + 0.5f;
            }
            else
            {
                Camera camera = map.geoSphere.transform.GetComponentInChildren<Camera>();

                Vector3 positionVector = camera.transform.localPosition;
                currentDistance = positionVector.magnitude - map.geoSphere.Radius;
                if (currentDistance < CameraController.MinCameraDistance)
                    currentDistance = CameraController.MinCameraDistance;
                else if (currentDistance > CameraController.MaxCameraDistance)
                    currentDistance = CameraController.MaxCameraDistance;

                Vector3 forward = map.geoSphere.transform.position - hitPoint;
                forward.Normalize();

                // Moves the circle inward based on the radius.
                float radiusRatio = radius / map.geoSphere.Radius;
                if (radiusRatio > 1)
                    radiusRatio = 1;
                float angle = Mathf.Asin(radius / map.geoSphere.Radius);
                float depth = 1 - Mathf.Cos(angle);
                depth *= map.geoSphere.Radius;

                Vector3 globePoint = hitPoint - map.geoSphere.transform.position;
                currentCoordinates = globePoint.CartesianToPolarRatio(map.geoSphere.Radius);

                hitPoint += forward * depth * 0.95f;

                transform.position = hitPoint;
                transform.forward = forward;
            }
        }

        if (lastDistance != currentDistance)
        {
            lastDistance = currentDistance;
            thickness = (currentDistance / CameraController.MaxCameraDistance) * startingThickness;
        }

        if (prevDivisions != numberOfDivisions || prevRadius != radius || prevThickness != thickness)
        {
            BuildMesh();

            prevDivisions = numberOfDivisions;
            prevRadius = radius;
            prevThickness = thickness;
        }
    }

    void OnValidate()
    {
        if (prevDivisions != numberOfDivisions || prevRadius != radius || prevThickness != thickness)
        {
            BuildMesh();

            prevDivisions = numberOfDivisions;
            prevRadius = radius;
            prevThickness = thickness;
        }
    }

    void BuildMesh()
    {
        BuildCircle(gameObject);
        if (brushLeft != null) BuildCircle(brushLeft);
        if (brushRight != null) BuildCircle(brushRight);
    }

    void BuildCircle(GameObject gameObject)
    {
        float anglePerStep = 2 * Mathf.PI / numberOfDivisions;
        float innerRadius = radius - thickness;

        Vector3[] vertexes = new Vector3[numberOfDivisions * 2];
        int[] triangles = new int[numberOfDivisions * 6];
        Vector3[] normals = new Vector3[numberOfDivisions * 2];

        int triangleIndex = 0;
        for (int step = 0; step < numberOfDivisions; step++)
        {
            float angle = step * anglePerStep;

            Vector2 vec0 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); //      vec1 (index+1)            vec0 (index)
            Vector2 vec1 = new Vector2(vec0.x, vec0.y);                     //      PrevVec1 (index-1)        PrevVec0 (index-2)
            vec0 *= radius;
            vec1 *= innerRadius;
            try
            {
                vertexes[step * 2] = new Vector3(vec0.x, vec0.y, -0.01f);
                vertexes[step * 2 + 1] = new Vector3(vec1.x, vec1.y, -0.01f);
                normals[step * 2] = new Vector3(0, -1, 0);
                normals[step * 2 + 1] = new Vector3(0, -1, 0);

                if (step > 0)
                {
                    triangles[triangleIndex++] = step * 2;
                    triangles[triangleIndex++] = step * 2 - 2;
                    triangles[triangleIndex++] = step * 2 - 1;

                    triangles[triangleIndex++] = step * 2 + 1;
                    triangles[triangleIndex++] = step * 2;
                    triangles[triangleIndex++] = step * 2 - 1;
                }
                if (step == numberOfDivisions - 1)
                {
                    triangles[triangleIndex++] = step * 2;
                    triangles[triangleIndex++] = step * 2 + 1;
                    triangles[triangleIndex++] = 0;

                    triangles[triangleIndex++] = step * 2 + 1;
                    triangles[triangleIndex++] = 1;
                    triangles[triangleIndex++] = 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("Error creating mesh at step " + step + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
#endif
            if (meshFilter == null)
                return;

            meshFilter.sharedMesh = new Mesh();

            meshFilter.sharedMesh.vertices = vertexes;
            meshFilter.sharedMesh.triangles = triangles;
            meshFilter.sharedMesh.normals = normals;
#if UNITY_EDITOR
        };
#endif
    }

    void RaiseTerrain()
    {
        map.AlterTerrain(currentCoordinates, radius, strength);
    }

    void LowerTerrain()
    {
        map.AlterTerrain(currentCoordinates, radius, -strength);
    }
}
