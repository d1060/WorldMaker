using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Geosphere : MonoBehaviour
{
    List<Mesh> meshes = new List<Mesh>();
    public Map mainMap;
    public float Radius = 200.0f;
    public int divisions = 5;
    public float heightMultiplier = 1;
    public Material material;
    [HideInInspector]
    List<GeoSphereSector> sectors = new List<GeoSphereSector>();
    int prevDivisions = 5;
    float prevRadius = 200.0f;

    // Start is called before the first frame update
    void Start()
    {
        prevDivisions = divisions;
        BuildSectors();
        BuildGameObject();
    }

    // Update is called once per frame
    void Update()
    {
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

    public void MapHit()
    {
        Camera camera = transform.GetComponentInChildren<Camera>();
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
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
        Camera camera = transform.GetComponentInChildren<Camera>();
        Ray ray = camera.ScreenPointToRay(mousePosition);
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

    public void RotateCameraTo(double longitude, double latitude)
    {
        Camera camera = transform.GetComponentInChildren<Camera>();
        float currentDistance = camera.transform.localPosition.magnitude;

        float yAngle = (float)((latitude - 0.5) * Mathf.PI);
        float xAngle = (float)((longitude - 0.5) * Mathf.PI * 2);
        float cosYAngle = Mathf.Cos(yAngle);

        float x = Mathf.Cos(xAngle) * cosYAngle;
        float y = Mathf.Sin(yAngle);
        float z = Mathf.Sin(xAngle) * cosYAngle;
        Vector3 newCameraPosition = new Vector3(x, y, z);

        //newCameraPosition.Normalize();
        newCameraPosition *= currentDistance;
        camera.transform.localPosition = newCameraPosition;
        camera.transform.LookAt(transform);
        //HideHiddenFaces(camera);
    }

    public void ZoomCameraTo(double distance)
    {
        Camera camera = transform.GetComponentInChildren<Camera>();
        double newDistance = distance + Radius;
        Vector3 positionVector = camera.transform.localPosition;
        positionVector.Normalize();
        positionVector *= (float)newDistance;
        camera.transform.localPosition = positionVector;
        //SetLOD(camera);
        //HideHiddenFaces(camera);
    }

    public void Zoom(float zoomAmount, CameraController cameraController)
    {
        Camera camera = transform.GetComponentInChildren<Camera>();
        Vector3 positionVector = camera.transform.localPosition;
        float distance = positionVector.magnitude;
        distance -= zoomAmount;
        if (distance - Radius < cameraController.MinCameraDistance)
            distance = cameraController.MinCameraDistance + Radius;
        else if (distance - Radius > cameraController.MaxCameraDistance)
            distance = cameraController.MaxCameraDistance + Radius;

        positionVector.Normalize();
        positionVector *= distance;
        camera.transform.localPosition = positionVector;
    }

    public void GetCameraLatitudeLongitude(ref float latitude, ref float longitude)
    {
        Camera camera = transform.GetComponentInChildren<Camera>();
        Vector3 cameraPosition = camera.transform.localPosition;

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
        Camera camera = transform.GetComponentInChildren<Camera>();
        Vector3 cameraPosition = camera.transform.localPosition;
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

        EditorApplication.delayCall += () =>
        {
            meshFilter.sharedMesh = new Mesh();

            Vector3[] vertices = GetVertexes();
            meshFilter.sharedMesh.vertices = vertices;

            int[] tris = GetTriangles(vertices);
            meshFilter.sharedMesh.triangles = tris;
            meshFilter.sharedMesh.uv = GetUVs();
            meshFilter.sharedMesh.normals = GetNormals();
        };

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
