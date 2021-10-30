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
    public InciseFlowSettings inciseFlowSettings;
    public TMP_InputField worldNameText;
    public GameObject debugText;
    public Camera cam;
    public Camera geoSphereCamera;
    CameraController cameraController;
    public MapSettings mapSettings;
    public ErosionSettings erosionSettings;
    public PlotRiversSettings plotRiversSettings;
    Vector2 centerScreenWorldPosition = new Vector2(0,0);
    public Geosphere geoSphere;
    public Material planetSurfaceMaterial;
    public TMPro.TMP_FontAsset pathLabelFont;
    public TMPro.TextMeshProUGUI totalPathLabel;
    int namesSeed = -1;
    bool updatingFieldCyclically = false;

    public float[,] heights = null;
    public GameObject waypointMarkerPrefab;
    public GameObject terrainBrushPrefab;
    Vector3 mouseMapHit = Vector3.zero;
    bool firstUpdate = true;
    public Material pathMaterial;

    //MapSector mainMap;
    GameObject waypointMarker;
    GameObject terrainBrush;
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
        MapData.instance.plotRiversSettings = plotRiversSettings;
        if (MapData.instance.Load())
        {
            mapSettings = MapData.instance.mapSettings;
            textureSettings = MapData.instance.textureSettings;
            erosionSettings = MapData.instance.erosionSettings;
            plotRiversSettings = MapData.instance.plotRiversSettings;
            inciseFlowSettings = MapData.instance.inciseFlowSettings;
        }
        GranuralizedGeoSphere.instance.Init(50);

        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
    }

    // Start is called before the first frame update
    void Start()
    {
        centerScreenWorldPosition = new Vector2(0.5f, 0.5f);
        UpdateMenuFields();
        UpdateRecentWorldsPanel();
        GenerateSeeds();
        UpdateSurfaceMaterialProperties(false);
        LoadTerrainTransformations();

        cameraController = cam.GetComponent<CameraController>();
        NameGenerator.instance.Load();
        NameGenerator.instance.SetSeed(namesSeed);
        if (MapData.instance.WorldName == null || MapData.instance.WorldName == "")
            MapData.instance.WorldName = NameGenerator.instance.GetName(NameGeneratorType.Types.World);
        //if (AppData.instance.LoadedWorld == null || AppData.instance.LoadedWorld == "")
        //{
        //    AppData.instance.LoadedWorld = Path.Combine(Application.persistentDataPath, MapData.baseMapDataFile);
        //    AppData.instance.Save();
        //}
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
            StartCoroutine(PerformErosionCycle());
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.P))
        {
            PerformPlotRiversRandomly();
        }

        //if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.V))
        //{
        //    StartCoroutine(PerformPluvialErosion());
        //}

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.U))
        {
            UndoErosion();
        }

        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.L))
        {
            if (LoadData())
            {
                AppData.instance.RecentWorlds.Add(MapData.instance.DataFile);
                AppData.instance.Save();
            }
        }

        UpdateDebugText();
    }

    void OnApplicationQuit()
    {
        SaveCurrentTerrainTransformations();

        planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);
        planetSurfaceMaterial.SetInt("_IsEroded", 0);
        planetSurfaceMaterial.SetInt("_IsHeightMapSet", 0);
        planetSurfaceMaterial.SetInt("_IsLandMaskSet", 0);
    }
    #endregion

    public void DoRuler(bool doRuler)
    {
        if (doRuler && doTerrainBrush)
        {
            TerrainToggleButton?.Disable();
            HideTerrainBrush();
            this.doTerrainBrush = false;
        }
        this.doRuler = doRuler;
        if (this.doRuler)
            ShowWaypointMarker();
        else
            HideWaypointMarker();
    }

    public void DoTerrainBrush(bool doTerrainBrush)
    {
        if (doTerrainBrush && this.doRuler)
        {
            WaypointToggleButton?.Disable();
            HideWaypointMarker();
            totalPathLabel.enabled = false;
            this.doRuler = false;
        }
        this.doTerrainBrush = doTerrainBrush;
        if (this.doTerrainBrush)
            ShowTerrainBrush();
        else
            HideTerrainBrush();
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

    void ShowTerrainBrush()
    {
        RaiseTerrainImage?.SetActive(true);
        LowerTerrainImage?.SetActive(true);
        BrushSizeSlider?.transform.gameObject.SetActive(true);
        BrushStrengthSlider?.transform.gameObject.SetActive(true);
        if (terrainBrush == null)
        {
            Vector3 position = Vector3.zero;
            if (mouseMapHit != Vector3.zero)
                position = new Vector3(mouseMapHit.x, mouseMapHit.y, mouseMapHit.z);

            terrainBrush = Instantiate(terrainBrushPrefab, position, Quaternion.identity);
            terrainBrush.name = "Terrain Brush";
            TerrainBrush terrainBrushScript = terrainBrush.GetComponent<TerrainBrush>();
            terrainBrushScript.map = this;
            terrainBrushScript.radius = BrushSizeSlider.value;
            terrainBrushScript.strength = BrushStrengthSlider.value;
        }
    }

    void HideTerrainBrush()
    {
        RaiseTerrainImage?.SetActive(false);
        LowerTerrainImage?.SetActive(false);
        BrushSizeSlider?.transform.gameObject.SetActive(false);
        BrushStrengthSlider?.transform.gameObject.SetActive(false);

        if (terrainBrush != null)
        {
            GameObject.DestroyImmediate(terrainBrush);
            terrainBrush = null;
        }
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

        float longitude = ((cam.transform.position.x - transform.position.x) / mapWidth) + 0.5f;
        float latitude = ((cam.transform.position.y - transform.position.y) / mapHeight) + 0.5f;

        textmeshPro.text =
            "Lon: " + longitude.ToString("####0.##") + ", Lat: " + latitude.ToString("####0.##");
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

        if (!keepSeed)
            GenerateHeightMap();

        UpdateSurfaceMaterialProperties();
    }

    public void SaveData()
    {
        MapData.instance.Save();

        string directory = System.IO.Path.GetDirectoryName(AppData.instance.RecentWorlds.Count > 0 ? AppData.instance.RecentWorlds[0] : AppData.instance.LastSavedImageFolder);
        string savedFile = StandaloneFileBrowser.SaveFilePanel("Save Map as JSON file", directory, MapData.instance.WorldName + ".json", new[] {
                new ExtensionFilter("JSon File", "json")
            });
        if (savedFile != null && savedFile != "")
        {
            SaveCurrentTerrainTransformations();
            AppData.instance.AddRecentWorld(savedFile);
            MapData.instance.IsSaved = true;
            MapData.instance.Save(savedFile);
            AppData.instance.Save();
            UpdateRecentWorldsPanel();
        }

        EventSystem eventSystem = cameraController.eventSystemObject.GetComponent<EventSystem>();
        eventSystem.SetSelectedGameObject(null);
    }

    public void LoadDataVoid()
    {
        LoadData();
        AppData.instance.Save();
    }

    public bool LoadData()
    {
        string directory = System.IO.Path.GetDirectoryName(AppData.instance.RecentWorlds.Count > 0 ? AppData.instance.RecentWorlds[0] : AppData.instance.LastSavedImageFolder);
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
            plotRiversSettings = MapData.instance.plotRiversSettings;
            MapData.instance.IsSaved = true;

            ResetImages();
            UndoErosion();
            GenerateSeeds();
            ReGenerateWorld(true);
            LoadTerrainTransformations();

            UpdateMenuFields();
            AppData.instance.AddRecentWorld(openFile);
            UpdateRecentWorldsPanel();
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
        textureSettings.surfaceNoiseSettings.seed = (int)(masterRandom.NextDouble() * 1000000); // Seeds of value greater than 8,388,600 may be lost due to the intrinsic conversion into float when passing into a material.
        textureSettings.surfaceNoiseSettings.noiseOffset = new Vector3((float)masterRandom.NextDouble(), (float)masterRandom.NextDouble(), (float)masterRandom.NextDouble());
        textureSettings.surfaceNoiseSettings2.seed = (int)(masterRandom.NextDouble() * 1000000); // Seeds of value greater than 8,388,600 may be lost due to the intrinsic conversion into float when passing into a material.
        textureSettings.surfaceNoiseSettings2.noiseOffset = new Vector3((float)masterRandom.NextDouble(), (float)masterRandom.NextDouble(), (float)masterRandom.NextDouble());
        textureSettings.TemperatureNoiseSeed = (int)(masterRandom.NextDouble() * 1000000);
        textureSettings.HumidityNoiseSeed = (int)(masterRandom.NextDouble() * 1000000);
        namesSeed = (int)(masterRandom.NextDouble() * 1000000);
    }

    public Vector2 CenterScreenWorldPosition { get { return centerScreenWorldPosition; }  set { centerScreenWorldPosition = value; } }
    public float MapWidth { get { return mapWidth; } set { mapWidth = value; } }
    public float MapHeight { get { return mapHeight; } set { mapHeight = value; } }
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

    public float HeightAtCoordinates(Vector2 polarRatioCoordinates)
    {
        GenerateHeightMap();

        int nextX = (int)polarRatioCoordinates.x + 1;
        if (nextX >= textureSettings.textureWidth) nextX -= textureSettings.textureWidth;
        int nextY = (int)polarRatioCoordinates.y + 1;
        if (nextY >= textureSettings.textureHeight) nextY = textureSettings.textureHeight - 1;

        int indexDL = (int)polarRatioCoordinates.x + (int)polarRatioCoordinates.y * textureSettings.textureWidth;
        int indexDR = nextX + (int)polarRatioCoordinates.y * textureSettings.textureWidth;
        int indexUL = (int)polarRatioCoordinates.x + nextY * textureSettings.textureWidth;
        int indexUR = nextX + nextY * textureSettings.textureWidth;

        float heightDL = erodedHeightMap[indexDL];
        float heightDR = erodedHeightMap[indexDR];
        float heightUL = erodedHeightMap[indexUL];
        float heightUR = erodedHeightMap[indexUR];

        float xDelta = polarRatioCoordinates.x - (int)polarRatioCoordinates.x;
        float yDelta = polarRatioCoordinates.y - (int)polarRatioCoordinates.y;

        float heightXdelta0 = (heightDR - heightDL) * xDelta + heightDL;
        float heightXdelta1 = (heightUR - heightUL) * xDelta + heightUL;

        float height = (heightXdelta1 - heightXdelta0) * yDelta + heightXdelta0;
        return height;
    }

    public float HeightAtCoordinatesUntilWaterLevel(Vector2 polarRatioCoordinates)
    {
        float height = HeightAtCoordinates(polarRatioCoordinates);
        if (height < textureSettings.waterLevel)
            return textureSettings.waterLevel;
        return height;
    }

    void SaveCurrentTerrainTransformations()
    {
        string tempDataFolder = Path.Combine(Application.persistentDataPath, "Temp", mapSettings.Seed.ToString());
        bool noFileExists = true;

        if (originalHeightMap != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            originalHeightMap.SaveBytes(Path.Combine(tempDataFolder, "originalHeightMap.raw"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "originalHeightMap.raw")))
            File.Delete(Path.Combine(tempDataFolder, "originalHeightMap.raw"));

        if (erodedHeightMap != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            erodedHeightMap.SaveBytes(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "erodedHeightMap.raw")))
            File.Delete(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));

        if (mergedHeightMap != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            mergedHeightMap.SaveBytes(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "mergedHeightMap.raw")))
            File.Delete(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));

        if (flowTexture != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            flowTexture.SaveAsPNG(Path.Combine(tempDataFolder, "flow.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "flow.png")))
            File.Delete(Path.Combine(tempDataFolder, "flow.png"));

        if (flowTextureRandom != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            flowTextureRandom.SaveAsPNG(Path.Combine(tempDataFolder, "flowRandom.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "flowRandom.png")))
            File.Delete(Path.Combine(tempDataFolder, "flowRandom.png"));

        if (inciseFlowMap != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            inciseFlowMap.SaveBytes(Path.Combine(tempDataFolder, "inciseFlow.raw"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "inciseFlow.raw")))
            File.Delete(Path.Combine(tempDataFolder, "inciseFlow.raw"));

        if (noFileExists && Directory.Exists(tempDataFolder))
            Directory.Delete(tempDataFolder);
    }

    void LoadTerrainTransformations()
    {
        string tempDataFolder = Path.Combine(Application.persistentDataPath, "Temp", mapSettings.Seed.ToString());
        if (!Directory.Exists(tempDataFolder))
            return;

        bool updateMaterial = false;
        bool updateFlow = false;
        if (File.Exists(Path.Combine(tempDataFolder, "originalHeightMap.raw")))
        {
            originalHeightMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "originalHeightMap.raw"), ref MapData.instance.LowestHeight, ref MapData.instance.HighestHeight);
            if (originalHeightMap != null && originalHeightMap.Length > 0)
                updateMaterial = true;
        }
        if (File.Exists(Path.Combine(tempDataFolder, "erodedHeightMap.raw")))
        {
            float lowest = 0;
            float highest = 0;
            erodedHeightMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "erodedHeightMap.raw"), ref lowest, ref highest);
            if (erodedHeightMap != null && erodedHeightMap.Length > 0)
                updateMaterial = true;
        }
        if (File.Exists(Path.Combine(tempDataFolder, "mergedHeightMap.raw")))
        {
            float lowest = 0;
            float highest = 0;
            mergedHeightMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "mergedHeightMap.raw"), ref lowest, ref highest);
            if (mergedHeightMap != null && mergedHeightMap.Length > 0)
                updateMaterial = true;
        }
        if (File.Exists(Path.Combine(tempDataFolder, "inciseFlow.raw")))
        {
            float lowest = 0;
            float highest = 0;
            inciseFlowMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "inciseFlow.raw"), ref lowest, ref highest);
            if (inciseFlowMap != null && inciseFlowMap.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "flow.png")))
        {
            flowTexture = LoadAnyImageFile(Path.Combine(tempDataFolder, "flow.png"));
            if (flowTexture != null)
                updateFlow = true;
        }
        if (File.Exists(Path.Combine(tempDataFolder, "flowRandom.png")))
        {
            flowTextureRandom = LoadAnyImageFile(Path.Combine(tempDataFolder, "flowRandom.png"));
            if (flowTextureRandom != null)
                updateFlow = true;
        }

        if (updateMaterial)
        {
            HeightMap2Texture();
            UpdateSurfaceMaterialHeightMap(true);
        }

        if (updateFlow)
        {
            if (flowTexture != null)
                planetSurfaceMaterial.SetTexture("_FlowTex", flowTexture);
            else
                planetSurfaceMaterial.SetTexture("_FlowTex", flowTextureRandom);
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
        }
    }

    float[] LoadFloatArrayFromFile(string fileName, ref float lowestFloat, ref float highestFloat)
    {
        try
        {
            lowestFloat = float.MaxValue;
            highestFloat = float.MinValue;

            long arrayLength = new System.IO.FileInfo(fileName).Length / sizeof(float);
            float[] array = new float[arrayLength];

            byte[] byteArray = File.ReadAllBytes(fileName);
            int byteIndex = 0;
            for (int i = 0; i < arrayLength; i++)
            {
                array[i] = BitConverter.ToSingle(byteArray, byteIndex);
                byteIndex += sizeof(float);
                if (array[i] > highestFloat) highestFloat = array[i];
                if (array[i] < lowestFloat) lowestFloat = array[i];
            }
            return array;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }
}
