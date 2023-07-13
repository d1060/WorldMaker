using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoomBrush : MonoBehaviour
{
    public int subdivisions = 20;
    float radius = 50;
    public float width = 128;
    public float height = 128;
    float zoomCamOriginalSize = 282.8427f;
    public Map map;
    public GameObject leftLineObject;
    public GameObject rightLineObject;
    public GameObject leftLeftLineObject;
    public GameObject leftRightLineObject;
    public GameObject rightLeftLineObject;
    public GameObject rightRightLineObject;
    public GameObject zoomMapGameObject;
    Material zoomCamMaterial = null;
    Vector3 previousMapPoint = Vector3.zero;
    Vector2 centerUV = Vector2.zero;
    Vector2 boundaryUV = Vector2.zero;

    List<Vector3[]> paths = new List<Vector3[]>();
    Camera cam;
    CameraController cameraController;

    // Start is called before the first frame update
    void Start()
    {
        cam = map.cam;
        cameraController = map.cam.GetComponent<CameraController>();
        RawImage zoomCamRawImage = zoomMapGameObject.GetComponent<RawImage>();
        zoomCamMaterial = zoomCamRawImage.material;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit mapHit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        ray = GetRayBeyondCanvas(ray);
        bool isMapHit = Physics.Raycast(ray, out mapHit);

        if (map.ShowGlobe)
        {
            //geosphere?.MapHit();
        }
        else if (mapHit.collider != null)
        {
            Vector3 hitPoint = mapHit.point;

            if (hitPoint != previousMapPoint)
            {
                if (!cameraController.IsContextMenuOpen)
                {
                    float u = ((hitPoint.x) / map.mapWidth) + 0.5f;
                    float v = ((hitPoint.y) / map.mapHeight) + 0.5f;
                    centerUV = new Vector2(u, v);

                    BuildMesh(u, v, hitPoint);
                }
            }
            previousMapPoint = hitPoint;
        }
    }

    public Vector2 CenterUV
    {
        get
        {
            return centerUV;
        }
    }

    public Vector2 BoundaryUV
    {
        get
        {
            return boundaryUV;
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

    public void ApplyResolution(float width, float height)
    {
        this.width = width;
        this.height = height;

        float ratio = height / width;

        float minimapWidth = Mathf.Sqrt(zoomCamOriginalSize * zoomCamOriginalSize / (1 + ratio * ratio));
        float minimapHeight = Mathf.Sqrt(zoomCamOriginalSize * zoomCamOriginalSize - minimapWidth * minimapWidth);

        RectTransform zoomMapGameObjectRectTransform = zoomMapGameObject.GetComponent<RectTransform>();
        zoomMapGameObjectRectTransform.sizeDelta = new Vector2(minimapWidth, minimapHeight);
        zoomMapGameObjectRectTransform.anchoredPosition = new Vector2(-minimapWidth / 2 - 10, minimapHeight / 2 + 10);
    }

    public void UpdateZoomMaterial(RenderTexture heightmapRT, RenderTexture noiseRT, MapSettings mapSettings, bool isEroded)
    {
        if (zoomCamMaterial == null)
        {
            RawImage zoomCamRawImage = zoomMapGameObject.GetComponent<RawImage>();
            zoomCamMaterial = zoomCamRawImage.material;
        }

        zoomCamMaterial.SetFloat("_TextureWidth", TextureManager.instance.Settings.textureWidth);
        zoomCamMaterial.SetFloat("_ZoomTextureWidth", width);
        zoomCamMaterial.SetFloat("_ZoomTextureHeight", height);

        zoomCamMaterial.SetFloat("_TemperatureExponent", MapData.instance.textureSettings.temperatureExponent);
        zoomCamMaterial.SetFloat("_TemperatureRatio", MapData.instance.textureSettings.temperatureRatio);
        zoomCamMaterial.SetFloat("_TemperatureElevationRatio", MapData.instance.textureSettings.temperatureElevationRatio);
        zoomCamMaterial.SetFloat("_TemperatureWaterDrop", MapData.instance.textureSettings.temperatureWaterDrop);
        zoomCamMaterial.SetFloat("_TemperatureLatitudeMultiplier", MapData.instance.textureSettings.temperatureLatitudeMultiplier);
        zoomCamMaterial.SetFloat("_TemperatureLatitudeDrop", MapData.instance.textureSettings.temperatureLatitudeDrop);
        zoomCamMaterial.SetFloat("_HumidityExponent", MapData.instance.textureSettings.humidityExponent);
        zoomCamMaterial.SetFloat("_HumidityMultiplier", MapData.instance.textureSettings.humidityMultiplier);

        int landColorSteps = TextureManager.instance.Settings.landColorStages.Length < TextureManager.instance.Settings.land1Color.Length ? TextureManager.instance.Settings.landColorStages.Length : TextureManager.instance.Settings.land1Color.Length;
        if (landColorSteps > 8) landColorSteps = 8;
        zoomCamMaterial.SetInt("_ColorSteps", landColorSteps);

        for (int i = 1; i <= 8; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= landColorSteps)
            {
                stage = TextureManager.instance.Settings.landColorStages[i - 1];
                color = TextureManager.instance.Settings.land1Color[i - 1];
            }
            else
            {
                stage = TextureManager.instance.Settings.landColorStages[landColorSteps - 1];
                color = TextureManager.instance.Settings.land1Color[landColorSteps - 1];
            }
            zoomCamMaterial.SetFloat("_ColorStep" + i, stage);
            zoomCamMaterial.SetColor("_Color" + i, color);
        }

        int oceanColorSteps = TextureManager.instance.Settings.oceanStages.Length < TextureManager.instance.Settings.oceanColors.Length ? TextureManager.instance.Settings.oceanStages.Length : TextureManager.instance.Settings.oceanColors.Length;
        if (oceanColorSteps > 4) oceanColorSteps = 4;
        zoomCamMaterial.SetInt("_OceanColorSteps", oceanColorSteps);

        for (int i = 1; i <= 4; i++)
        {
            float stage = 0;
            Color color = Color.white;

            if (i <= oceanColorSteps)
            {
                stage = TextureManager.instance.Settings.oceanStages[i - 1];
                color = TextureManager.instance.Settings.oceanColors[i - 1];
            }
            else
            {
                stage = TextureManager.instance.Settings.oceanStages[oceanColorSteps - 1];
                color = TextureManager.instance.Settings.oceanColors[oceanColorSteps - 1];
            }
            zoomCamMaterial.SetFloat("_OceanColorStep" + i, stage);
            zoomCamMaterial.SetColor("_OceanColor" + i, color);
        }

        zoomCamMaterial.SetFloat("_IceTemperatureThreshold1", TextureManager.instance.Settings.iceTemperatureThreshold);
        zoomCamMaterial.SetFloat("_IceTemperatureThreshold2", TextureManager.instance.Settings.iceTemperatureThreshold - TextureManager.instance.Settings.iceTransition);
        zoomCamMaterial.SetFloat("_DesertThreshold1", TextureManager.instance.Settings.desertThreshold);
        zoomCamMaterial.SetFloat("_DesertThreshold2", TextureManager.instance.Settings.desertThreshold + TextureManager.instance.Settings.humidityTransition);
        zoomCamMaterial.SetColor("_IceColor", TextureManager.instance.Settings.iceColor);
        zoomCamMaterial.SetColor("_DesertColor", TextureManager.instance.Settings.desertColor);
        zoomCamMaterial.SetFloat("_NormalScale", TextureManager.instance.Settings.normalScale * 100);
        zoomCamMaterial.SetFloat("_NormalInfluence", TextureManager.instance.Settings.normalInfluence);
        zoomCamMaterial.SetFloat("_UnderwaterNormalScale", TextureManager.instance.Settings.underwaterNormalScale * 100);
        zoomCamMaterial.SetFloat("_UnderwaterNormalInfluence", TextureManager.instance.Settings.underwaterNormalInfluence);

        zoomCamMaterial.SetTexture("_HeightMap", heightmapRT);
        if (isEroded && heightmapRT != null)
            zoomCamMaterial.SetInt("_IsEroded", 1);
        else
            zoomCamMaterial.SetInt("_IsEroded", 0);

        zoomCamMaterial.SetTexture("_MainMap", TextureManager.instance.Landmap);
        if (mapSettings.UseImages)
            zoomCamMaterial.SetInt("_IsMainmapSet", 1);

        zoomCamMaterial.SetTexture("_LandMask", TextureManager.instance.Landmask);
        if (mapSettings.UseImages)
            zoomCamMaterial.SetInt("_IsLandmaskSet", 1);

        zoomCamMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        zoomCamMaterial.SetInt("_IsFlowTexSet", 1);

        zoomCamMaterial.SetTexture("_NoiseMap", noiseRT);
        zoomCamMaterial.SetInt("_IsNoiseMapSet", 1);
    }

    public void SetRadius(float radius)
    {
        if (cameraController == null)
            cameraController = map.cam.GetComponent<CameraController>();

        if (!cameraController.IsContextMenuOpen)
        {
            this.radius = radius;
            float u = ((previousMapPoint.x) / map.mapWidth) + 0.5f;
            float v = ((previousMapPoint.y) / map.mapHeight) + 0.5f;
            centerUV = new Vector2(u, v);
            BuildMesh(u, v, previousMapPoint);
        }
    }

    public void UpdateZoomMaterialPosition(float u, float v, float uBoundary, float vBoundary)
    {
        if (zoomCamMaterial == null)
        {
            RawImage zoomCamRawImage = zoomMapGameObject.GetComponent<RawImage>();
            zoomCamMaterial = zoomCamRawImage.material;
        }

        zoomCamMaterial.SetFloat("_CenterU", u);
        zoomCamMaterial.SetFloat("_CenterV", v);

        zoomCamMaterial.SetFloat("_BoundaryU", uBoundary);
        zoomCamMaterial.SetFloat("_BoundaryV", vBoundary);
    }

    void BuildMesh(float u, float v, Vector3 mapPoint)
    {
        paths.Clear();

        Vector2 uv = new Vector2(0.5f, v);

        Vector3[] leftLine = new Vector3[subdivisions * 2];
        Vector3[] rightLine = new Vector3[subdivisions * 2];

        Vector3 position = uv.PolarRatioToCartesian(1);

        float uBoundary;
        float vBoundary;

        if (width > height)
        {
            uBoundary = radius / map.mapWidth;
            vBoundary = radius * (height / width) / map.mapHeight;
        }
        else
        {
            uBoundary = radius * (width / height) / map.mapWidth;
            vBoundary = radius / map.mapHeight;
        }
        boundaryUV = new Vector2(uBoundary, vBoundary);

        UpdateZoomMaterialPosition(u, v, uBoundary, vBoundary);

        float Ushift = uBoundary * 180;
        float Vshift = vBoundary * 90;

        // Upper and Lower Lines.
        for (int i = 0; i < subdivisions / 2; i++)
        {
            float Ustep = ((float)i / subdivisions) * uBoundary;
            float Uangle = Ustep * 360;

            Vector3 upperRight = ShiftPointInSphereBy(position, -Uangle, Vshift, v + vBoundary, mapPoint);
            Vector3 upperLeft = ShiftPointInSphereBy(position, Uangle, Vshift, v + vBoundary, mapPoint);
            Vector3 lowerRight = ShiftPointInSphereBy(position, -Uangle, -Vshift, v - vBoundary, mapPoint);
            Vector3 lowerLeft = ShiftPointInSphereBy(position, Uangle, -Vshift, v - vBoundary, mapPoint);

            if (upperLeft.x >= map.mapWidth / 2) upperLeft.x = -upperLeft.x;
            if (lowerLeft.x >= map.mapWidth / 2) lowerLeft.x = -lowerLeft.x;

            upperLeft.x += mapPoint.x;
            upperRight.x += mapPoint.x;
            lowerLeft.x += mapPoint.x;
            lowerRight.x += mapPoint.x;

            leftLine[i] = upperLeft;
            rightLine[i] = upperRight;
            leftLine[2 * subdivisions - i - 1] = lowerLeft;
            rightLine[2 * subdivisions - i - 1] = lowerRight;
        }

        // Left and Right lines
        for (int i = 0; i < subdivisions; i++)
        {
            float Vstep = ((float)(subdivisions / 2 - i) / subdivisions) * vBoundary;
            float Vangle = Vstep * 180;

            Vector3 right = ShiftPointInSphereBy(position, -Ushift, Vangle, v + Vstep, mapPoint);
            Vector3 left = ShiftPointInSphereBy(position, Ushift, Vangle, v + Vstep, mapPoint);

            if (left.x >= map.mapWidth / 2) left.x = -left.x;

            left.x += mapPoint.x;
            right.x += mapPoint.x;

            leftLine[i + subdivisions / 2] = left;
            rightLine[i + subdivisions / 2] = right;
        }

        SetLineRendererPositions(leftLineObject, leftLine, 0);
        SetLineRendererPositions(rightLineObject, rightLine, 0);

        SetLineRendererPositions(leftLeftLineObject, leftLine, -map.mapWidth);
        SetLineRendererPositions(leftRightLineObject, rightLine, -map.mapWidth);

        SetLineRendererPositions(rightLeftLineObject, leftLine, map.mapWidth);
        SetLineRendererPositions(rightRightLineObject, rightLine, map.mapWidth);
    }

    void SetLineRendererPositions(GameObject go, Vector3[] positions, float offset)
    {
        LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
        lineRenderer.positionCount = positions.Length;
        if (offset == 0)
            lineRenderer.SetPositions(positions);
        else
        {
            Vector3[] offsetPositions = new Vector3[positions.Length];

            for (int i = 0; i < positions.Length; i++)
            {
                offsetPositions[i] = new Vector3(positions[i].x + offset, positions[i].y, positions[i].z);
            }

            lineRenderer.SetPositions(offsetPositions);
        }
    }

    Vector3 ShiftPointInSphereBy(Vector3 point, float uAngle, float vAngle, float v, Vector3 mapPoint)
    {
        Vector3 rightVector = Vector3.Cross(Vector3.up, point);
        Vector3 upVector = Vector3.Cross(point, rightVector);

        Vector3 rotationX = Quaternion.AngleAxis(uAngle, upVector) * point;

        rightVector = Vector3.Cross(rotationX, upVector);

        Vector3 rotated = Quaternion.AngleAxis(vAngle, rightVector) * rotationX;

        //Quaternion rotation = Quaternion.Euler(vAngle, 0, uAngle);
        //Vector3 finalSpherePosition = rotation * point;
        Vector2 finalUVPosition = rotated.CartesianToPolarRatio(1);

        //if (v > 1 || v < 0)
        //    finalUVPosition.x = -finalUVPosition.x;

        Vector3 mapPosition = new Vector3((finalUVPosition.x - 0.5f) * map.mapWidth, (finalUVPosition.y - 0.5f) * map.mapHeight, -0.01f);

        return mapPosition;
    }
}
