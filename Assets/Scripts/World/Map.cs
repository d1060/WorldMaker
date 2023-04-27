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
    public GameObject terrainBrush;
    Vector3 mouseMapHit = Vector3.zero;
    bool firstUpdate = true;
    public Material pathMaterial;
    public float smoothTime = 0.1f;
    private Vector3 velocity = Vector3.zero;

    //MapSector mainMap;
    GameObject waypointMarker;
    WaypointController waypointController = null;

    List<float> minHeights = new List<float>();
    List<float> maxHeights = new List<float>();

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
        MapData.instance.textureSettings = TextureManager.instance.Settings;
        MapData.instance.erosionSettings = erosionSettings;
        MapData.instance.inciseFlowSettings = inciseFlowSettings;
        MapData.instance.plotRiversSettings = plotRiversSettings;
        if (MapData.instance.Load())
        {
            mapSettings = MapData.instance.mapSettings;
            TextureManager.instance.Settings = MapData.instance.textureSettings;
            erosionSettings = MapData.instance.erosionSettings;
            plotRiversSettings = MapData.instance.plotRiversSettings;
            inciseFlowSettings = MapData.instance.inciseFlowSettings;
        }
        GranuralizedGeoSphere.instance.Init(50);
        MapData.instance.LowestHeight = TextureManager.instance.Settings.minHeight;
        MapData.instance.HighestHeight = TextureManager.instance.Settings.maxHeight;

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
        if (!LoadTerrainTransformations())
        {
            GenerateHeightMap();
            HeightMap2Texture();
            UpdateSurfaceMaterialHeightMap(true);
        }

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

        bool isCTRLPressed = Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl);

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.S))
        {
            SaveData();
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.I))
        {
            SaveImages();
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.R))
        {
            ReGenerateWorld();
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(PerformErosionCycle());
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.P))
        {
            PerformPlotRiversRandomly();
        }

        //if (isCTRLPressed && Input.GetKeyDown(KeyCode.V))
        //{
        //    StartCoroutine(PerformPluvialErosion());
        //}

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.U))
        {
            UndoErosion();
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.T))
        {
            //Show Temperature
            TemperatureToggleButton?.Toggle();
            DoShowTemperature(!showTemperature);
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.G))
        {
            //Show Globe
            ShowGlobeToggleButton?.Toggle();
            DoShowGlobe(!showGlobe);
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.W))
        {
            //Edit Waypoints
            WaypointToggleButton?.Toggle();
            DoRuler(!doRuler);
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.A))
        {
            //Alter Terrain
            TerrainToggleButton?.Toggle();
            DoTerrainBrush(!doTerrainBrush);
        }

        if (isCTRLPressed && Input.GetKeyDown(KeyCode.L))
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
        planetSurfaceMaterial.SetInt("_IsHeightmapSet", 0);
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
        if (terrainBrush != null)
        {
            terrainBrush.SetActive(true);
            Vector3 position = Vector3.zero;
            if (mouseMapHit != Vector3.zero)
                position = new Vector3(mouseMapHit.x, mouseMapHit.y, mouseMapHit.z);

            TerrainBrush terrainBrushScript = terrainBrush.GetComponent<TerrainBrush>();
            terrainBrushScript.map = this;
            terrainBrushScript.radius = BrushSizeSlider.value;
            terrainBrushScript.strength = BrushStrengthSlider.value / 100;
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
            terrainBrush.SetActive(false);
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
        worldNameText.interactable = false;
        this.showGlobe = showGlobe;
        if (!showGlobe)
        {
            ShowPlaneMap();
            EventSystem.current.SetSelectedGameObject(this.gameObject);
            //cameraController.CalculateVisibleFlatLatitudeAndLongitude();
            cameraController.BringCameraIntoViewPlanes();
            geoSphereCamera.enabled = false;
            Component geolightComponent = geoSphereCamera.GetChildWithName("Geosphere Directional Light");
            if (geolightComponent != null)
                geolightComponent.gameObject.SetActive(false);

            cam.enabled = true;
            Canvas ui = cam.GetComponentInChildren<Canvas>();
            ui.worldCamera = cam;
            Component lightComponent = cam.GetChildWithName("Directional Light");
            if (lightComponent != null)
                lightComponent.gameObject.SetActive(true);
            geoSphere.gameObject.SetActive(false);
        }
        else
        {
            HidePlaneMap();
            geoSphereCamera.enabled = true;
            geoSphere.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(geoSphere.gameObject);

            cameraController.CalculateVisibleFlatLatitudeAndLongitude();
            geoSphere.RotateCameraTo((cameraController.VisibleLowerLongitude + cameraController.VisibleUpperLongitude) / 2, (cameraController.VisibleLowerLatitude + cameraController.VisibleUpperLatitude) / 2);
            geoSphere.ResetCameraTargetPosition();

            Component geolightComponent = geoSphereCamera.GetChildWithName("Geosphere Directional Light");
            if (geolightComponent != null)
                geolightComponent.gameObject.SetActive(true);

            cam.enabled = false;
            Canvas ui = cam.GetComponentInChildren<Canvas>();
            ui.worldCamera = geoSphereCamera;
            Component lightComponent = cam.GetChildWithName("Directional Light");
            if (lightComponent != null)
                lightComponent.gameObject.SetActive(false);
        }
        worldNameText.interactable = true;
    }

    int frameCounter = 0;
    float timeCounter = 0.0f;
    float lastFramerate = 0.0f;
    public float refreshTime = 0.5f;
    public void UpdateDebugText()
    {
        if (!debugText.activeSelf)
            return;

        TextMeshProUGUI textmeshPro = debugText.GetComponent<TextMeshProUGUI>();
        if (textmeshPro == null)
            return;

        if (timeCounter < refreshTime)
        {
            timeCounter += Time.deltaTime;
            frameCounter++;
        }
        else
        {
            lastFramerate = (float)frameCounter / timeCounter;
            frameCounter = 0;
            timeCounter = 0.0f;
        }

        float longitude = ((cam.transform.position.x - transform.position.x) / mapWidth) + 0.5f;
        float latitude = ((cam.transform.position.y - transform.position.y) / mapHeight) + 0.5f;

        textmeshPro.text =
            "Lon: " + longitude.ToString("####0.##") + ", Lat: " + latitude.ToString("####0.##") + " FPS: " + lastFramerate.ToString("####0.##");
    }

    public void ReGenerateLandColors()
    {
        UpdateSurfaceMaterialProperties();
    }

    public void ReGenerateWorld()
    {
        ReGenerateWorld(AppData.instance.KeepSeedOnRegenerate);
    }

    public void ReGenerateWorld(bool keepSeed, bool reGenerateHeightMap = false)
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
            UpdateUIInputField(setupPanelTransform, "Seed Text Box", mapSettings.Seed.ToString());

            if (MapData.instance.textureSettings.surfaceNoiseSettings.ridged && MapData.instance.textureSettings.surfaceNoiseSettings2.ridged)
            {
                MapData.instance.LowestHeight = 0;
                MapData.instance.HighestHeight = 1;
            }
            else if (MapData.instance.textureSettings.surfaceNoiseSettings.ridged != MapData.instance.textureSettings.surfaceNoiseSettings2.ridged)
            {
                MapData.instance.LowestHeight = 0.15f;
                MapData.instance.HighestHeight = 0.85f;
            }
            else
            {
                MapData.instance.LowestHeight = 0.15f;
                MapData.instance.HighestHeight = 0.7f;
            }

            MapData.instance.textureSettings.surfaceNoiseSettings.noiseOffset.x = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings.noiseOffset.y = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings.noiseOffset.z = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings.multiplier = (float)random.NextDouble() * 3 + 2; //Landmasses: 5 - 2
            MapData.instance.textureSettings.surfaceNoiseSettings.octaves = random.Next(13, 17); //Map Detail: 13-17
            MapData.instance.textureSettings.surfaceNoiseSettings.lacunarity = (float)random.NextDouble() * 0.15f + 1.6f; //Map Scaling: 1.6 - 1.75
            MapData.instance.textureSettings.surfaceNoiseSettings.persistence = (float)random.NextDouble() * 0.06f + 0.67f; //Smoothness: 0.67 - 0.73
            MapData.instance.textureSettings.surfaceNoiseSettings.heightExponent = 2; //Height Exponent: 2
            MapData.instance.textureSettings.surfaceNoiseSettings.domainWarping = (float)random.NextDouble() + 0.5f; //Domain Warping: 0.5 - 1.5
            MapData.instance.textureSettings.surfaceNoiseSettings.layerStrength = (float)random.NextDouble();

            MapData.instance.textureSettings.surfaceNoiseSettings2.noiseOffset.x = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings2.noiseOffset.y = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings2.noiseOffset.z = (float)random.NextDouble();
            MapData.instance.textureSettings.surfaceNoiseSettings2.multiplier = (float)random.NextDouble() * 3 + 2; //Landmasses: 5 - 2
            MapData.instance.textureSettings.surfaceNoiseSettings2.octaves = random.Next(13, 17); //Map Detail: 13-17
            MapData.instance.textureSettings.surfaceNoiseSettings2.lacunarity = (float)random.NextDouble() * 0.15f + 1.6f; //Map Scaling: 1.6 - 1.75
            MapData.instance.textureSettings.surfaceNoiseSettings2.persistence = (float)random.NextDouble() * 0.06f + 0.67f; //Smoothness: 0.67 - 0.73
            MapData.instance.textureSettings.surfaceNoiseSettings2.heightExponent = 2; //Height Exponent: 2
            MapData.instance.textureSettings.surfaceNoiseSettings2.domainWarping = (float)random.NextDouble() + 0.5f; //Domain Warping: 0.5 - 1.5
            MapData.instance.textureSettings.surfaceNoiseSettings2.layerStrength = 1 - MapData.instance.textureSettings.surfaceNoiseSettings.layerStrength;
        }

        EventSystem eventSystem = cameraController.eventSystemObject.GetComponent<EventSystem>();
        eventSystem.SetSelectedGameObject(null);

        if (!keepSeed || reGenerateHeightMap)
        {
            ResetEroded();

            planetSurfaceMaterial.SetInt("_IsEroded", 0);
            planetSurfaceMaterial.SetInt("_IsFlowTexSet", 0);

            GenerateHeightMap();

            HeightMap2Texture();

            //float minHeight = 0, maxHeight = 0;
            //TextureManager.instance.HeightMapMinMaxHeights(ref minHeight, ref maxHeight);

            //MapData.instance.LowestHeight = minHeight;
            //MapData.instance.HighestHeight = maxHeight;

            SetHeightLimits();
        }

        UpdateSurfaceMaterialProperties();
        UpdateNoiseLayerFields();
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
            TextureManager.instance.Settings = MapData.instance.textureSettings;
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

    public void WorldNameSelected(string param)
    {

    }

    public void WorldNameDeSelected(string param)
    {

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
        TextureManager.instance.Settings.surfaceNoiseSettings.seed = (float)(masterRandom.NextDouble() * 1000000);
        //TextureManager.instance.Settings.surfaceNoiseSettings.noiseOffset = new Vector3((float)masterRandom.NextDouble(), (float)masterRandom.NextDouble(), (float)masterRandom.NextDouble());
        TextureManager.instance.Settings.surfaceNoiseSettings2.seed = (float)(masterRandom.NextDouble() * 1000000);
        //TextureManager.instance.Settings.surfaceNoiseSettings2.noiseOffset = new Vector3((float)masterRandom.NextDouble(), (float)masterRandom.NextDouble(), (float)masterRandom.NextDouble());
        TextureManager.instance.Settings.TemperatureNoiseSeed = (float)(masterRandom.NextDouble() * 1000000);
        TextureManager.instance.Settings.HumidityNoiseSeed = (float)(masterRandom.NextDouble() * 1000000);
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

    public float HeightAtCoordinates(Vector2 uv)
    {
        GenerateHeightMap();

        float nextX = uv.x + 1 / TextureManager.instance.Settings.textureWidth;
        if (nextX >= 1) nextX -= 1.0f;
        float nextY = uv.y + 1 / TextureManager.instance.Settings.textureWidth;
        if (nextY >= 1) nextY = uv.y;

        Vector2 uvDL = new Vector2(uv.x, uv.y);
        Vector2 uvDR = new Vector2(uv.x, uv.y);
        Vector2 uvUL = new Vector2(uv.x, uv.y);
        Vector2 uvUR = new Vector2(uv.x, uv.y);

        float heightDL = TextureManager.instance.HeightMapValueAtUV(uvDL);
        float heightDR = TextureManager.instance.HeightMapValueAtUV(uvDR);
        float heightUL = TextureManager.instance.HeightMapValueAtUV(uvUL);
        float heightUR = TextureManager.instance.HeightMapValueAtUV(uvUR);

        float xDelta = uv.x - (int)uv.x;
        float yDelta = uv.y - (int)uv.y;

        float heightXdelta0 = (heightDR - heightDL) * xDelta + heightDL;
        float heightXdelta1 = (heightUR - heightUL) * xDelta + heightUL;

        float height = (heightXdelta1 - heightXdelta0) * yDelta + heightXdelta0;
        return height;
    }

    public float HeightAtCoordinatesUntilWaterLevel(Vector2 polarRatioCoordinates)
    {
        float height = HeightAtCoordinates(polarRatioCoordinates);
        if (height < TextureManager.instance.Settings.waterLevel)
            return TextureManager.instance.Settings.waterLevel;
        return height;
    }

    void SaveCurrentTerrainTransformations()
    {
        string tempDataFolder = Path.Combine(Application.persistentDataPath, "Temp", mapSettings.Seed.ToString());
        bool noFileExists = true;

        if (TextureManager.instance.HeightMap1 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap1.SaveBytes(Path.Combine(tempDataFolder, "HeightMap1.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap1-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap1.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap1.raw"));

        if (TextureManager.instance.HeightMap2 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap2.SaveBytes(Path.Combine(tempDataFolder, "HeightMap2.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap2-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap2.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap2.raw"));

        if (TextureManager.instance.HeightMap3 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap3.SaveBytes(Path.Combine(tempDataFolder, "HeightMap3.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap3-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap3.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap3.raw"));

        if (TextureManager.instance.HeightMap4 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap4.SaveBytes(Path.Combine(tempDataFolder, "HeightMap4.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap4-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap4.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap4.raw"));

        if (TextureManager.instance.HeightMap5 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap5.SaveBytes(Path.Combine(tempDataFolder, "HeightMap5.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap5-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap5.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap5.raw"));

        if (TextureManager.instance.HeightMap6 != null)
        {
            if (!Directory.Exists(tempDataFolder))
                Directory.CreateDirectory(tempDataFolder);
            noFileExists = false;
            TextureManager.instance.HeightMap6.SaveBytes(Path.Combine(tempDataFolder, "HeightMap6.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap6-save.png"));
        }
        else if (File.Exists(Path.Combine(tempDataFolder, "HeightMap6.raw")))
            File.Delete(Path.Combine(tempDataFolder, "HeightMap6.raw"));

        //if (TextureManager.instance.ErodedHeightMap != null)
        //{
        //    if (!Directory.Exists(tempDataFolder))
        //        Directory.CreateDirectory(tempDataFolder);
        //    noFileExists = false;
        //    TextureManager.instance.ErodedHeightMap.SaveBytes(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));
        //}
        //else if (File.Exists(Path.Combine(tempDataFolder, "erodedHeightMap.raw")))
        //    File.Delete(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));

        //if (TextureManager.instance.MergedHeightMap != null)
        //{
        //    if (!Directory.Exists(tempDataFolder))
        //        Directory.CreateDirectory(tempDataFolder);
        //    noFileExists = false;
        //    TextureManager.instance.MergedHeightMap.SaveBytes(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));
        //}
        //else if (File.Exists(Path.Combine(tempDataFolder, "mergedHeightMap.raw")))
        //    File.Delete(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));

        //if (TextureManager.instance.FlowTexture != null)
        //{
        //    if (!Directory.Exists(tempDataFolder))
        //        Directory.CreateDirectory(tempDataFolder);
        //    noFileExists = false;
        //    TextureManager.instance.FlowTexture.SaveAsPNG(Path.Combine(tempDataFolder, "flow.png"));
        //}
        //else if (File.Exists(Path.Combine(tempDataFolder, "flow.png")))
        //    File.Delete(Path.Combine(tempDataFolder, "flow.png"));

        //if (TextureManager.instance.FlowTextureRandom != null)
        //{
        //    if (!Directory.Exists(tempDataFolder))
        //        Directory.CreateDirectory(tempDataFolder);
        //    noFileExists = false;
        //    TextureManager.instance.FlowTextureRandom.SaveAsPNG(Path.Combine(tempDataFolder, "flowRandom.png"));
        //}
        //else if (File.Exists(Path.Combine(tempDataFolder, "flowRandom.png")))
        //    File.Delete(Path.Combine(tempDataFolder, "flowRandom.png"));

        //if (TextureManager.instance.InciseFlowMap != null)
        //{
        //    if (!Directory.Exists(tempDataFolder))
        //        Directory.CreateDirectory(tempDataFolder);
        //    noFileExists = false;
        //    TextureManager.instance.InciseFlowMap.SaveBytes(Path.Combine(tempDataFolder, "inciseFlow.raw"));
        //}
        //else if (File.Exists(Path.Combine(tempDataFolder, "inciseFlow.raw")))
        //    File.Delete(Path.Combine(tempDataFolder, "inciseFlow.raw"));

        if (noFileExists && Directory.Exists(tempDataFolder))
            Directory.Delete(tempDataFolder);
    }

    bool LoadTerrainTransformations()
    {
        string tempDataFolder = Path.Combine(Application.persistentDataPath, "Temp", mapSettings.Seed.ToString());
        if (!Directory.Exists(tempDataFolder))
            return false;

        bool updateMaterial = false;
        //bool updateFlow = false;
        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap1.raw")))
        {
            TextureManager.instance.HeightMap1 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap1.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap1, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap1-load.png"));
            if (TextureManager.instance.HeightMap1.Length > 0 && TextureManager.instance.HeightMap1.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap1 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap1.raw"));
            }

            if (TextureManager.instance.HeightMap1 != null && TextureManager.instance.HeightMap1.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap2.raw")))
        {
            TextureManager.instance.HeightMap2 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap2.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap2, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap2-load.png"));
            if (TextureManager.instance.HeightMap2.Length > 0 && TextureManager.instance.HeightMap2.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap2 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap2.raw"));
            }

            if (TextureManager.instance.HeightMap2 != null && TextureManager.instance.HeightMap2.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap3.raw")))
        {
            TextureManager.instance.HeightMap3 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap3.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap3, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap3-load.png"));
            if (TextureManager.instance.HeightMap3.Length > 0 && TextureManager.instance.HeightMap3.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap3 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap3.raw"));
            }

            if (TextureManager.instance.HeightMap3 != null && TextureManager.instance.HeightMap3.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap4.raw")))
        {
            TextureManager.instance.HeightMap4 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap4.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap4, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap4-load.png"));
            if (TextureManager.instance.HeightMap4.Length > 0 && TextureManager.instance.HeightMap4.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap4 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap4.raw"));
            }

            if (TextureManager.instance.HeightMap4 != null && TextureManager.instance.HeightMap4.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap5.raw")))
        {
            TextureManager.instance.HeightMap5 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap5.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap5, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap5-load.png"));
            if (TextureManager.instance.HeightMap5.Length > 0 && TextureManager.instance.HeightMap5.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap5 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap5.raw"));
            }

            if (TextureManager.instance.HeightMap5 != null && TextureManager.instance.HeightMap5.Length > 0)
                updateMaterial = true;
        }

        if (File.Exists(Path.Combine(tempDataFolder, "HeightMap6.raw")))
        {
            TextureManager.instance.HeightMap6 = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "HeightMap6.raw"));
            //ImageTools.SaveTextureCubemapFaceFloatArray(TextureManager.instance.HeightMap6, TextureManager.instance.Settings.textureWidth, Path.Combine(tempDataFolder, "HeightMap6-load.png"));
            if (TextureManager.instance.HeightMap6.Length > 0 && TextureManager.instance.HeightMap6.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
            {
                TextureManager.instance.HeightMap6 = null;
                File.Delete(Path.Combine(tempDataFolder, "HeightMap6.raw"));
            }

            if (TextureManager.instance.HeightMap6 != null && TextureManager.instance.HeightMap6.Length > 0)
                updateMaterial = true;
        }

        //if (File.Exists(Path.Combine(tempDataFolder, "erodedHeightMap.raw")))
        //{
        //    //float lowest = 0;
        //    //float highest = 0;
        //    TextureManager.instance.ErodedHeightMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));
        //    if (TextureManager.instance.ErodedHeightMap.Length > 0 && TextureManager.instance.ErodedHeightMap.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
        //    {
        //        TextureManager.instance.ErodedHeightMap = null;
        //        File.Delete(Path.Combine(tempDataFolder, "erodedHeightMap.raw"));
        //    }

        //    if (TextureManager.instance.ErodedHeightMap != null && TextureManager.instance.ErodedHeightMap.Length > 0)
        //        updateMaterial = true;
        //}

        //if (File.Exists(Path.Combine(tempDataFolder, "mergedHeightMap.raw")))
        //{
        //    //float lowest = 0;
        //    //float highest = 0;
        //    TextureManager.instance.MergedHeightMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));
        //    if (TextureManager.instance.MergedHeightMap.Length > 0 && TextureManager.instance.MergedHeightMap.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
        //    {
        //        TextureManager.instance.MergedHeightMap = null;
        //        File.Delete(Path.Combine(tempDataFolder, "mergedHeightMap.raw"));
        //    }

        //    if (TextureManager.instance.MergedHeightMap != null && TextureManager.instance.MergedHeightMap.Length > 0)
        //        updateMaterial = true;
        //}

        //if (File.Exists(Path.Combine(tempDataFolder, "inciseFlow.raw")))
        //{
        //    //float lowest = 0;
        //    //float highest = 0;
        //    TextureManager.instance.InciseFlowMap = LoadFloatArrayFromFile(Path.Combine(tempDataFolder, "inciseFlow.raw"));
        //    if (TextureManager.instance.InciseFlowMap.Length > 0 && TextureManager.instance.InciseFlowMap.Length != TextureManager.instance.Settings.textureWidth * TextureManager.instance.Settings.textureWidth)
        //    {
        //        TextureManager.instance.InciseFlowMap = null;
        //        File.Delete(Path.Combine(tempDataFolder, "inciseFlow.raw"));
        //    }

        //    if (TextureManager.instance.InciseFlowMap != null && TextureManager.instance.InciseFlowMap.Length > 0)
        //        updateMaterial = true;
        //}

        //if (File.Exists(Path.Combine(tempDataFolder, "flow.png")))
        //{
        //    TextureManager.instance.FlowTexture = LoadAnyImageFile(Path.Combine(tempDataFolder, "flow.png"));
        //    if (TextureManager.instance.FlowTexture.width != TextureManager.instance.Settings.textureWidth || TextureManager.instance.FlowTexture.height != TextureManager.instance.Settings.textureWidth)
        //    {
        //        TextureManager.instance.FlowTexture = null;
        //        File.Delete(Path.Combine(tempDataFolder, "flow.png"));
        //    }

        //    if (TextureManager.instance.FlowTexture != null)
        //        updateFlow = true;
        //}
        //if (File.Exists(Path.Combine(tempDataFolder, "flowRandom.png")))
        //{
        //    TextureManager.instance.FlowTextureRandom = LoadAnyImageFile(Path.Combine(tempDataFolder, "flowRandom.png"));
        //    if (TextureManager.instance.FlowTextureRandom.width != TextureManager.instance.Settings.textureWidth || TextureManager.instance.FlowTextureRandom.height != TextureManager.instance.Settings.textureWidth)
        //    {
        //        TextureManager.instance.FlowTextureRandom = null;
        //        File.Delete(Path.Combine(tempDataFolder, "flowRandom.png"));
        //    }

        //    if (TextureManager.instance.FlowTextureRandom != null)
        //        updateFlow = true;
        //}

        if (updateMaterial)
        {
            HeightMap2Texture();
            UpdateSurfaceMaterialHeightMap(true);
        }

        //if (updateFlow)
        //{
        //    if (TextureManager.instance.FlowTexture != null)
        //        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTexture);
        //    else
        //        planetSurfaceMaterial.SetTexture("_FlowTex", TextureManager.instance.FlowTextureRandom);
        //    planetSurfaceMaterial.SetInt("_IsFlowTexSet", 1);
        //}
        return updateMaterial;
    }

    float[] LoadFloatArrayFromFile(string fileName)
    {
        try
        {
            //lowestFloat = float.MaxValue;
            //highestFloat = float.MinValue;

            long arrayLength = new System.IO.FileInfo(fileName).Length / sizeof(float);
            float[] array = new float[arrayLength];

            byte[] byteArray = File.ReadAllBytes(fileName);
            int byteIndex = 0;
            for (int i = 0; i < arrayLength; i++)
            {
                array[i] = BitConverter.ToSingle(byteArray, byteIndex);
                byteIndex += sizeof(float);
                //if (array[i] > highestFloat) highestFloat = array[i];
                //if (array[i] < lowestFloat) lowestFloat = array[i];
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
