using System;
using SFB;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public partial class Map : MonoBehaviour
{
    public float mapWidth = 400;
    public float mapHeight = 200;
    public TextureSettings textureSettings;
    public TMP_InputField worldNameText;
    public GameObject debugText;
    public Camera cam;
    public Camera geoSphereCamera;
    CameraController cameraController;
    public MapSettings mapSettings;
    public ErosionSettings erosionSettings;
    public InciseFlowSettings inciseFlowSettings;
    Vector2 centerScreenWorldPosition = new Vector2(0,0);
    public Geosphere geoSphere;
    public Material planetSurfaceMaterial;
    public TMPro.TMP_FontAsset pathLabelFont;
    public TMPro.TextMeshProUGUI totalPathLabel;
    int namesSeed = -1;
    bool updatingFieldCyclically = false;

    public float[,] heights = null;
    float[] heightsLat0 = null;
    float[] heightsLon0 = null;
    float lowestHeight = float.MaxValue;
    float highestHeight = float.MinValue;
    float lowestHumidity = float.MaxValue;
    float highestHumidity = float.MinValue;
    float lowestTemperature = float.MaxValue;
    float highestTemperature = float.MinValue;
    List<float> allHeights = new List<float>();
    float waterHeight; // Actual water height within the Noise variation
    int mapInfosWidth;
    int mapInfosHeight;
    public GameObject waypointMarkerPrefab;
    float heightRatio = 10;
    Vector3 mouseMapHit = Vector3.zero;
    bool firstUpdate = true;
    public Material pathMaterial;

    //MapSector mainMap;
    GameObject waypointMarker;
    WaypointController waypointController = null;

    public bool ShowBorders { get { return showBorders; } }
    public bool ShowTemperature { get { return showTemperature; } }
    public bool ShowGlobe { get { return showGlobe; } }

    #region Mono
    void Awake()
    {
        SetupPanelVariables();
        totalPathLabel.enabled = false;
        Log.Reset();
        AppData.instance.Load();

        if (mapSettings.Seed == -1)
        {
            System.Random random = new System.Random();
            mapSettings.Seed = random.Next();
        }
        MapData.instance.mapSettings = mapSettings;
        MapData.instance.textureSettings = textureSettings;
        MapData.instance.erosionSettings = erosionSettings;
        MapData.instance.inciseFlowSettings = inciseFlowSettings;
        if (MapData.instance.Load(AppData.instance.LoadedWorld))
        {
            mapSettings = MapData.instance.mapSettings;
            textureSettings = MapData.instance.textureSettings;
            erosionSettings = MapData.instance.erosionSettings;
            inciseFlowSettings = MapData.instance.inciseFlowSettings;
        }
        GranuralizedGeoSphere.instance.Init(30);
    }

    // Start is called before the first frame update
    void Start()
    {
        centerScreenWorldPosition = new Vector2(0.5f, 0.5f);
        UpdateMenuFields();
        GenerateSeeds();
        UpdateSurfaceMaterialProperties();

        cameraController = cam.GetComponent<CameraController>();
        NameGenerator.instance.Load();
        NameGenerator.instance.SetSeed(namesSeed);
        if (MapData.instance.WorldName == null || MapData.instance.WorldName == "")
            MapData.instance.WorldName = NameGenerator.instance.GetName(NameGeneratorType.Types.World);
        if (AppData.instance.LoadedWorld == null || AppData.instance.LoadedWorld == "")
        {
            AppData.instance.LoadedWorld = Path.Combine(Application.persistentDataPath, MapData.baseMapDataFile);
            AppData.instance.Save();
        }
        ApplyWorldName();
    }

    // Update is called once per frame
    void Update()
    {
        if (firstUpdate)
        {
            firstUpdate = false;
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.S))
        {
            SaveData();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.I))
        {
            SaveImages();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.R))
        {
            ReGenerateWorld();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.E))
        {
            PerformErosionCycle();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(PerformInciseFlow());
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.U))
        {
            UndoErosion();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.L))
        {
            if (LoadData())
            {
                AppData.instance.LoadedWorld = MapData.instance.DataFile;
                AppData.instance.Save();
            }
        }

        UpdateDebugText();
    }

    void OnApplicationQuit()
    {

    }
    #endregion

    public void DoRuler(bool doRuler)
    {
        this.doRuler = doRuler;
        if (this.doRuler)
            ShowWaypointMarker();
        else
            HideWaypointMarker();
    }

    void ShowWaypointMarker()
    {
        if (waypointMarker == null && mouseMapHit != Vector3.zero)
        {
            Vector3 position = new Vector3(mouseMapHit.x, mouseMapHit.y, mouseMapHit.z);
            waypointMarker = Instantiate(waypointMarkerPrefab, position, Quaternion.identity);
            WaypointController.ClearAll();
            waypointController = waypointMarker.GetComponent<WaypointController>();
            if (waypointController != null)
            {
                totalPathLabel.text = "Total Path Length: 0 km";
                waypointController.totalPathLabel = totalPathLabel;
                waypointController.map = this;
            }
            waypointMarker.name = "Waypoint Marker";
            totalPathLabel.enabled = true;
        }
    }

    void HideWaypointMarker()
    {
        if (waypointMarker != null)
        {
            GameObject.DestroyImmediate(waypointMarker);
            waypointMarker = null;
        }

        if (WaypointController.WaypointsCount <= 0)
            totalPathLabel.enabled = false;
    }

    public void DoShowBorders(bool showBorders)
    {
        this.showBorders = showBorders;
        //mainMap.ShowTextures();
        //geoSphere.ShowTextures();
    }

    public void DoShowTemperature(bool showTemperature)
    {
        this.showTemperature = showTemperature;
        UpdateSurfaceMaterialProperties();
    }

    public void DoShowGlobe(bool showGlobe)
    {
        this.showGlobe = showGlobe;
        if (!showGlobe)
        {
            ShowPlaneMap();
            cameraController.CalculateVisibleFlatLatitudeAndLongitude();
            cameraController.BringCameraIntoViewPlanes();
            geoSphereCamera.enabled = false;
            Light geolight = geoSphereCamera.GetComponentInChildren<Light>();
            geolight.enabled = false;

            cam.enabled = true;
            Canvas ui = cam.GetComponentInChildren<Canvas>();
            ui.worldCamera = cam;
            Light light = cam.GetComponentInChildren<Light>();
            light.enabled = true;
        }
        else
        {
            HidePlaneMap();
            geoSphereCamera.enabled = true;
            Light geolight = geoSphereCamera.GetComponentInChildren<Light>();
            geolight.enabled = true;

            cam.enabled = false;
            Canvas ui = cam.GetComponentInChildren<Canvas>();
            ui.worldCamera = geoSphereCamera;
            Light light = cam.GetComponentInChildren<Light>();
            light.enabled = false;
        }
        //mainMap.ShowTextures();
    }

    public void UpdateDebugText()
    {
        TextMeshProUGUI textmeshPro = debugText.GetComponent<TextMeshProUGUI>();
        if (textmeshPro == null)
            return;
        textmeshPro.text =
            "Height: " + (-cam.transform.position.z).ToString("####0.##") + ", Zoom Level: " + cameraController.ZoomLevel;
    }

    public void Shift(float x)
    {
        Vector3 position = transform.position;
        position.x += x;

        if (position.x < -mapWidth / 2)
        {
            position.x += mapWidth;
        }
        else if (position.x > mapWidth / 2)
        {
            position.x -= mapWidth;
        }

        transform.position = position;

        centerScreenWorldPosition = new Vector2((mapWidth / 2 - transform.position.x) / mapWidth, (cam.transform.position.y + mapHeight/2) / mapHeight);
    }

    public void ShiftTo(float longitude)
    {
        Vector3 position = transform.position;
        position.x = (float)((1 - longitude) * mapWidth - mapWidth / 2);

        if (position.x < -mapWidth / 2)
        {
            position.x += mapWidth;
        }
        else if (position.x > mapWidth / 2)
        {
            position.x -= mapWidth;
        }

        transform.position = position;

        centerScreenWorldPosition = new Vector2((mapWidth / 2 - transform.position.x) / mapWidth, (cam.transform.position.y + mapHeight / 2) / mapHeight);
    }

    public void ReGenerateLandColors()
    {
        UpdateSurfaceMaterialProperties();
    }

    public void ReGenerateWorld()
    {
        ReGenerateWorld(AppData.instance.KeepSeedOnRegenerate);
    }

    public void ReGenerateWorld(bool keepSeed)
    {
        WaypointController.ClearAll();

        if (!keepSeed)
        {
            System.Random random = new System.Random();
            mapSettings.Seed = random.Next();
            GenerateSeeds();
            NameGenerator.instance.SetSeed(namesSeed);
            MapData.instance.WorldName = NameGenerator.instance.GetName(NameGeneratorType.Types.World);
            ApplyWorldName();
        }

        EventSystem eventSystem = cameraController.eventSystemObject.GetComponent<EventSystem>();
        eventSystem.SetSelectedGameObject(null);
        erodedHeightMap = null;
        originalHeightMap = null;
        mergedHeightMap = null;
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

        UpdateSurfaceMaterialProperties();
    }

    public void SaveData()
    {
        MapData.instance.Save();

        string directory = System.IO.Path.GetDirectoryName(AppData.instance.LoadedWorld);
        string savedFile = StandaloneFileBrowser.SaveFilePanel("Save Map as JSON file", directory, "", new[] {
                new ExtensionFilter("JSon File", "json")
            });
        if (savedFile != null && savedFile != "")
        {
            AppData.instance.LoadedWorld = savedFile;
            MapData.instance.Save(savedFile);
            AppData.instance.Save();
        }

        EventSystem eventSystem = cameraController.eventSystemObject.GetComponent<EventSystem>();
        eventSystem.SetSelectedGameObject(null);
    }

    public bool LoadData()
    {
        string directory = System.IO.Path.GetDirectoryName(AppData.instance.LoadedWorld);
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Load Map from a JSON file", directory,
            new[] {
                new ExtensionFilter("JSON file", "json" )
            }
            , false);
        if (paths.Length == 0)
            return false;

        string openFile = paths[0];

        if (openFile == "" || openFile == null)
            return false;

        if (MapData.instance.Load(openFile))
        {
            textureSettings = MapData.instance.textureSettings;
            mapSettings = MapData.instance.mapSettings;
            erosionSettings = MapData.instance.erosionSettings;
            inciseFlowSettings = MapData.instance.inciseFlowSettings;

            GenerateSeeds();
            ReGenerateWorld(true);

            UpdateMenuFields();
            return true;
        }
        return false;
    }

    void ShowPlaneMap()
    {
        //mainMap.Enabled = true;
    }

    void HidePlaneMap()
    {
        //mainMap.Enabled = false;
    }

    public void WorldNameChanged(String name)
    {
        MapData.instance.WorldName = worldNameText.text;
        MapData.instance.Save();
    }

    void ApplyWorldName()
    {
        if (worldNameText == null)
            return;

        worldNameText.text = MapData.instance.WorldName;
    }

    void GenerateSeeds()
    {
        System.Random masterRandom = new System.Random(mapSettings.Seed);
        textureSettings.SurfaceNoiseSeed = (int)(masterRandom.NextDouble() * 1000000); // Seeds of value greater than 8,388,600 may be lost due to the intrinsic conversion into float when passing into a material.
        textureSettings.surfaceNoiseSettings.noiseOffset = new Vector3((float)masterRandom.NextDouble(), (float)masterRandom.NextDouble(), (float)masterRandom.NextDouble());
        textureSettings.TemperatureNoiseSeed = (int)(masterRandom.NextDouble() * 1000000);
        textureSettings.HumidityNoiseSeed = (int)(masterRandom.NextDouble() * 1000000);
        namesSeed = (int)(masterRandom.NextDouble() * 1000000);
    }

    public int CurrentZoomLevel { get { return cameraController != null ? cameraController.ZoomLevel : 1; } }
    public Vector2 CenterScreenWorldPosition { get { return centerScreenWorldPosition; }  set { centerScreenWorldPosition = value; } }
    public float MapWidth { get { return mapWidth; } set { mapWidth = value; } }
    public float MapHeight { get { return mapHeight; } set { mapHeight = value; } }
    public float[] HeightsLat0 { get { return heightsLat0; } set { heightsLat0 = value; } }
    public float[] HeightsLon0 { get { return heightsLon0; } set { heightsLon0 = value; } }
    public float LowestHeight { get { return lowestHeight; } set { lowestHeight = value; } }
    public float HighestHeight { get { return highestHeight; } set { highestHeight = value; } }
    public float LowestHumidity { get { return lowestHumidity; } set { lowestHumidity = value; } }
    public float HighestHumidity { get { return highestHumidity; } set { highestHumidity = value; } }
    public float LowestTemperature { get { return lowestTemperature; } set { lowestTemperature = value; } }
    public float HighestTemperature { get { return highestTemperature; } set { highestTemperature = value; } }
    public float HeightRatio { get { return heightRatio; } set { heightRatio = value; } }
    public Vector3 MouseMapHit
    {
        get { return mouseMapHit; }
        set
        {
            mouseMapHit = value;
            if (this.doRuler && waypointMarker != null)
            {
                //Debug.Log("Hit: "+ mouseMapHit);
                if (!showGlobe)
                {
                    waypointMarker.transform.position = new Vector3(mouseMapHit.x, mouseMapHit.y, mouseMapHit.z);
                }
                else
                {
                    Vector3 pointRelativeToGlobe = mouseMapHit - geoSphere.transform.position;
                    pointRelativeToGlobe.Normalize();
                    Vector3 newForward = new Vector3(-pointRelativeToGlobe.x, -pointRelativeToGlobe.y, -pointRelativeToGlobe.z);

                    Vector2 polar = pointRelativeToGlobe.CartesianToPolarRatio(1);
                    //float height = GetHeight(polar);
                    //float heightAboveWaver = ((height > waterHeight ? height : waterHeight) - waterHeight) / (1 - waterHeight);
                    pointRelativeToGlobe *= geoSphere.Radius + geoSphere.heightMultiplier + 0.1f;

                    waypointMarker.transform.position = pointRelativeToGlobe + geoSphere.transform.position;
                    waypointMarker.transform.forward = newForward;
                }
            }
        }
    }

    public Vector3 MapToGlobePoint(Vector3 point)
    {
        float longitude = ((point.x - transform.position.x) / mapWidth) + 0.5f;
        float latitude = ((point.y - transform.position.y) / mapHeight) + 0.5f;

        Vector2 polar = new Vector2(longitude, latitude);
        Vector3 globePoint = polar.PolarRatioToCartesian(geoSphere.Radius);
        globePoint.Normalize();
        globePoint *= geoSphere.Radius + geoSphere.heightMultiplier + 0.1f;
        return globePoint + geoSphere.transform.position;
    }

    public Vector3 GlobeToMapPoint(Vector3 point)
    {
        point -= geoSphere.transform.position;
        Vector2 polar = point.CartesianToPolarRatio(geoSphere.Radius);

        float x = ((polar.x - 0.5f) * mapWidth) + transform.position.x;
        float y = ((polar.y - 0.5f) * mapHeight) + transform.position.y;
        float z = GetFlatMapZ(x, y);

        return new Vector3(x, y, z);
    }

    float GetFlatMapZ(float x, float y)
    {
        Ray ray = new Ray(new Vector3(x, y, -100), new Vector3(0, 0, 1));
        RaycastHit[] hits = Physics.RaycastAll(ray);
        Vector3 hitPoint = Vector3.zero;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is MeshCollider)
            {
                hitPoint = hit.point;
                break;
            }
        }
        return hitPoint.z;
    }

    public Vector3 UnityGlobeToMapPoint(Vector3 point)
    {
        Vector2 polar = point.CartesianToPolarRatio(1);

        float x = ((polar.x - 0.5f) * mapWidth) + transform.position.x;
        float y = ((polar.y - 0.5f) * mapHeight) + transform.position.y;
        float z = GetFlatMapZ(x, y);

        return new Vector3(x, y, z);
    }
}
