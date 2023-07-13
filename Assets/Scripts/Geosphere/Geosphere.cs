using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Geosphere : MonoBehaviour
{
    public Map mainMap;
    public float Radius = 200.0f;
    public int divisions = 5;
    public float heightMultiplier = 1;
    public float panDivisor = 1250;
    public Material material;
    [HideInInspector]
    List<GeoSphereSector> sectors = new List<GeoSphereSector>();
    int prevDivisions = 5;
    float prevRadius = 200.0f;
    Vector3 targetCameraPosition = Vector3.zero;
    Camera geoSphereCamera = null;
    public float minNavigationSpeed = 0.04f;
    public float smoothTime = 0.1f;
    private Vector3 velocity = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        prevDivisions = divisions;
        BuildSectors();
        BuildGameObject();
        geoSphereCamera = transform.GetComponentInChildren<Camera>();
        if (geoSphereCamera != null)
            targetCameraPosition = geoSphereCamera.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        if (geoSphereCamera != null)
        {
            if (targetCameraPosition != geoSphereCamera.transform.localPosition)
            {
                geoSphereCamera.transform.localPosition = Vector3.SmoothDamp(geoSphereCamera.transform.localPosition, targetCameraPosition, ref velocity, smoothTime);
                geoSphereCamera.transform.LookAt(transform);

                if (geoSphereCamera.transform.localPosition.x < - Radius - CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(- Radius - CameraController.MaxCameraDistance, geoSphereCamera.transform.localPosition.y, geoSphereCamera.transform.localPosition.z);
                    targetCameraPosition = new Vector3(- Radius - CameraController.MaxCameraDistance, targetCameraPosition.y, targetCameraPosition.z);
                }
                else if (geoSphereCamera.transform.localPosition.x > Radius + CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(Radius + CameraController.MaxCameraDistance, geoSphereCamera.transform.localPosition.y, geoSphereCamera.transform.localPosition.z);
                    targetCameraPosition = new Vector3(Radius + CameraController.MaxCameraDistance, targetCameraPosition.y, targetCameraPosition.z);
                }

                if (geoSphereCamera.transform.localPosition.y < - Radius - CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(transform.position.x, - Radius - CameraController.MaxCameraDistance, geoSphereCamera.transform.localPosition.z);
                    targetCameraPosition = new Vector3(transform.position.x, - Radius - CameraController.MaxCameraDistance, targetCameraPosition.z);
                }
                else if (geoSphereCamera.transform.localPosition.y > Radius + CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(transform.position.x, Radius + CameraController.MaxCameraDistance, geoSphereCamera.transform.localPosition.z);
                    targetCameraPosition = new Vector3(transform.position.x, Radius + CameraController.MaxCameraDistance, targetCameraPosition.z);
                }

                if (geoSphereCamera.transform.localPosition.z < - Radius - CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(transform.position.x, geoSphereCamera.transform.localPosition.y, - Radius - CameraController.MaxCameraDistance);
                    targetCameraPosition = new Vector3(transform.position.x, targetCameraPosition.y, - Radius - CameraController.MaxCameraDistance);
                }
                else if (geoSphereCamera.transform.localPosition.z > + Radius + CameraController.MaxCameraDistance)
                {
                    geoSphereCamera.transform.localPosition = new Vector3(transform.position.x, geoSphereCamera.transform.localPosition.y, Radius + CameraController.MaxCameraDistance);
                    targetCameraPosition = new Vector3(transform.position.x, targetCameraPosition.y, Radius + CameraController.MaxCameraDistance);
                }
            }
        }
    }

    void OnValidate()
    {
        if (prevDivisions != divisions || prevRadius != Radius)
        {
            BuildSectors();
            BuildGameObject();

            prevDivisions = divisions;
            prevRadius = Radius;
        }
    }

    public void ResetCameraTargetPosition()
    {
        if (geoSphereCamera != null)
            targetCameraPosition = geoSphereCamera.transform.localPosition;
    }

    public void MapHit()
    {
        if (geoSphereCamera == null)
            return;

        Ray ray = geoSphereCamera.ScreenPointToRay(Input.mousePosition);
        ray = CameraController.GetRayBeyondCanvas(ray);
        RaycastHit mapHit;
        bool isMapHit = Physics.Raycast(ray, out mapHit);
        if (mapHit.collider != null)
        {
            Vector3 hitPoint = mapHit.point;
            mainMap.MouseMapHit = hitPoint;
        }
    }

    public Vector3 MapHit(Vector3 mousePosition)
    {
        if (geoSphereCamera == null)
            return new Vector3();

        Ray ray = geoSphereCamera.ScreenPointToRay(mousePosition);
        //ray = CameraController.GetRayBeyondCanvas(ray);

        RaycastHit[] hits = Physics.RaycastAll(ray);
        Vector3 hitPoint = Vector3.zero;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is SphereCollider)
            {
                hitPoint = hit.point - transform.position;
                break;
            }
        }
        return hitPoint;
    }

    public void PanCameraTo(Vector3 from, Vector3 to)
    {
        float latitudeFrom = 0;
        float longitudeFrom = 0;
        GetPointLatitudeLongitude(from, ref latitudeFrom, ref longitudeFrom);
        float latitudeTo = 0;
        float longitudeTo = 0;
        GetPointLatitudeLongitude(to, ref latitudeTo, ref longitudeTo);

        float latitudeDelta = latitudeFrom - latitudeTo;
        float longitudeDelta = longitudeFrom - longitudeTo;

        float latitude = 0;
        float longitude = 0;
        GetCameraLatitudeLongitude(ref latitude, ref longitude);

        latitude += latitudeDelta;
        longitude += longitudeDelta;

        if (latitude > 0.9999f)
            latitude = 0.9999f;
        if (latitude < -0.9999f)
            latitude = -0.9999f;

        if (longitude > 1)
            longitude -= 1;
        if (longitude < 0)
            longitude += 1;

        RotateCameraTo(longitude, latitude);
    }

    public void MoveCameraBy(float x, float y)
    {
        float latitudeDelta = y / panDivisor;
        float longitudeDelta = x / panDivisor;

        Vector3 positionVector = targetCameraPosition;
        float distance = positionVector.magnitude - Radius;

        float cameraDistanceRatio = (distance < CameraController.MinCameraDistance ? CameraController.MinCameraDistance : distance) / CameraController.MaxCameraDistance;

        if (longitudeDelta > 0)
            longitudeDelta = (longitudeDelta - minNavigationSpeed) * cameraDistanceRatio + minNavigationSpeed;
        else if (longitudeDelta < 0)
            longitudeDelta = (longitudeDelta + minNavigationSpeed) * cameraDistanceRatio - minNavigationSpeed;

        if (latitudeDelta > 0)
            latitudeDelta = (latitudeDelta - minNavigationSpeed) * cameraDistanceRatio + minNavigationSpeed;
        else if (latitudeDelta < 0)
            latitudeDelta = (latitudeDelta + minNavigationSpeed) * cameraDistanceRatio - minNavigationSpeed;

        float latitude = 0;
        float longitude = 0;
        GetCameraLatitudeLongitude(ref latitude, ref longitude);

        latitude += latitudeDelta;
        longitude += longitudeDelta;

        if (latitude > 0.9999f) latitude = 0.9999f;
        if (latitude < 0.0001f) latitude = 0.0001f;
        if (longitude > 1) longitude = 1;
        if (longitude < 0) longitude = 0;

        RotateCameraTo(longitude, latitude);
    }

    public void RotateCameraTo(double longitude, double latitude)
    {
        float currentDistance = targetCameraPosition.magnitude;

        float yAngle = (float)((latitude - 0.5) * Mathf.PI);
        float xAngle = (float)((longitude - 0.5) * Mathf.PI * 2);
        float cosYAngle = Mathf.Cos(yAngle);

        float x = Mathf.Cos(xAngle) * cosYAngle;
        float y = Mathf.Sin(yAngle);
        float z = Mathf.Sin(xAngle) * cosYAngle;
        Vector3 newCameraPosition = new Vector3(x, y, z);

        //newCameraPosition.Normalize();
        newCameraPosition *= currentDistance;
        targetCameraPosition = newCameraPosition;
        //HideHiddenFaces(geoSphereCamera);
    }

    public void ZoomCameraTo(double distance)
    {
        double newDistance = distance + Radius;
        Vector3 positionVector = targetCameraPosition;
        positionVector.Normalize();
        positionVector *= (float)newDistance;
        targetCameraPosition = positionVector;
        //SetLOD(geoSphereCamera);
        //HideHiddenFaces(geoSphereCamera);
    }

    public void Zoom(float zoomAmount, CameraController cameraController)
    {
        Vector3 positionVector = targetCameraPosition;
        float distance = positionVector.magnitude;
        distance -= zoomAmount;
        if (distance - Radius < CameraController.MinCameraDistance)
            distance = CameraController.MinCameraDistance + Radius;
        else if (distance - Radius > CameraController.MaxCameraDistance)
            distance = CameraController.MaxCameraDistance + Radius;

        positionVector.Normalize();
        positionVector *= distance;
        targetCameraPosition = positionVector;
    }

    public void GetCameraLatitudeLongitude(ref float latitude, ref float longitude)
    {
        Vector3 cameraPosition = targetCameraPosition;

        GetPointLatitudeLongitude(cameraPosition, ref latitude, ref longitude);
    }

    public void GetPointLatitudeLongitude(Vector3 point, ref float latitude, ref float longitude)
    {
        float x2 = point.x * point.x;
        //float y2 = point.y * point.y;
        float z2 = point.z * point.z;

        longitude = Mathf.Atan2(point.z, point.x) / (Mathf.PI * 2);
        longitude += 0.5f;

        float hipXZ = Mathf.Sqrt(x2 + z2);

        latitude = Mathf.Atan2(point.y, hipXZ) / Mathf.PI;
        latitude += 0.5f;
    }

    public float GetCameraDistance()
    {
        Vector3 cameraPosition = targetCameraPosition;
        return cameraPosition.magnitude - Radius;
    }

    void BuildSectors()
    {
        sectors.Clear();
        for (int i = 0; i < GeoSphereBaseFaces.baseFacesCount; i++)
        {
            GeoSphereSector gss = new GeoSphereSector(i, Radius);
            gss.SplitFace(Radius, divisions);
            sectors.Add(gss);
        }
    }

    void BuildGameObject()
    {
        // Builds Game Object
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
#endif
            if (meshFilter == null)
                return;

            meshFilter.sharedMesh = new Mesh();

            Vector3[] vertices = GetVertexes();
            meshFilter.sharedMesh.vertices = vertices;

            int[] tris = GetTriangles(vertices);
            meshFilter.sharedMesh.triangles = tris;
            meshFilter.sharedMesh.uv = GetUVs();
            meshFilter.sharedMesh.normals = GetNormals();
            meshFilter.sharedMesh.tangents = GetTangents();
#if UNITY_EDITOR
        };
#endif

        SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.radius = Radius;
        }
    }

    Vector3[] GetVertexes()
    {
        List<Vector3> vertexes = new List<Vector3>();
        foreach (GeoSphereSector geoSphereSector in sectors)
        {
            vertexes.AddRange(geoSphereSector.GetSubFacesVertexes());
        }
        return vertexes.ToArray();
    }

    int[] GetTriangles(Vector3[] vertices)
    {
        int[] triangles = new int[vertices.Length];

        for (int i = 0; i < vertices.Length; i += 3)
        {
            triangles[i] = i;
            triangles[i+1] = i+1;
            triangles[i+2] = i+2;
        }

        return triangles;
    }

    Vector2[] GetUVs()
    {
        List<Vector2> uvs = new List<Vector2>();
        foreach (GeoSphereSector geoSphereSector in sectors)
        {
            uvs.AddRange(geoSphereSector.GetSubFacesUVs());
        }
        return uvs.ToArray();
    }

    Vector4[] GetTangents()
    {
        List<Vector4> tangents = new List<Vector4>();
        foreach (GeoSphereSector geoSphereSector in sectors)
        {
            tangents.AddRange(geoSphereSector.GetSubFacesTangents());
        }
        return tangents.ToArray();
    }

    Vector3[] GetNormals()
    {
        List<Vector3> normals = new List<Vector3>();
        foreach (GeoSphereSector geoSphereSector in sectors)
        {
            normals.AddRange(geoSphereSector.GetSubFacesNormals());
        }
        return normals.ToArray();
    }

    public Vector3 GetClosestPointTo(Vector3 point)
    {
        float closestBaseFaceDistance = float.MaxValue;
        GeoSphereSector closestFace = null;
        foreach (GeoSphereSector geoSphereSector in sectors)
        {
            float faceDistance = (geoSphereSector.Center - point).magnitude;
            if (faceDistance < closestBaseFaceDistance)
            {
                closestBaseFaceDistance = faceDistance;
                closestFace = geoSphereSector;
            }
        }

        Vector3 closestPoint = closestFace.GetClosestPointTo(point);
        return closestPoint;
    }
}
