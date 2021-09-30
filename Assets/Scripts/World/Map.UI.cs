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

public partial class Map : MonoBehaviour
{
    Transform setupPanelTransform = null;
    Transform worldNameTransform = null;
    Transform gradientPanelTransform = null;
    Transform contextMenuPanelTransform = null;
    Transform erosionPanelTransform = null;
    Transform inciseFlowPanelTransform = null;
    bool showTemperature = false;
    bool showGlobe = false;
    bool showBorders = false;
    bool doRuler = false;

    #region MapData
    public string UISeed
    {
        get
        {
            return mapSettings.Seed.ToString();
        }

        set
        {
            int iValue = 0;
            if (System.Int32.TryParse(value, out iValue))
            {
                mapSettings.Seed = iValue;
                GenerateSeeds();
                UpdateSurfaceMaterialProperties();
            }
            MapData.instance.Save();
        }
    }

    public string UIRadius
    {
        get
        {
            return mapSettings.RadiusInKm.ToString();
        }

        set
        {
            double dValue = 0;
            if (System.Double.TryParse(value, out dValue))
            {
                mapSettings.RadiusInKm = dValue;
            }
            MapData.instance.Save();
        }
    }

    public string UIHeightMap
    {
        get
        {
            return mapSettings.HeightMapPath;
        }

        set
        {
            mapSettings.HeightMapPath = value;
            if (mapSettings.UseImages)
            {
                UpdateSurfaceMaterialHeightMap();
            }
            MapData.instance.Save();
        }
    }

    public string UIMainTexture
    {
        get
        {
            return mapSettings.MainTexturePath;
        }

        set
        {
            mapSettings.MainTexturePath = value;
            if (mapSettings.UseImages)
            {
                UpdateSurfaceMaterialMainMap();
            }
            MapData.instance.Save();
        }
    }

    public string UILandMask
    {
        get
        {
            return mapSettings.LandMaskPath;
        }

        set
        {
            mapSettings.LandMaskPath = value;
            if (mapSettings.UseImages)
            {
                UpdateSurfaceMaterialLandMask();
            }
            MapData.instance.Save();
        }
    }

    [SerializeField]
    public Color DesertColor
    {
        get
        {
            return textureSettings.desertColor;
        }

        set
        {
            textureSettings.desertColor = value;
            UpdateSurfaceMaterialProperties();
            MapData.instance.Save();
        }
    }

    public string DesertRate1
    {
        get
        {
            return textureSettings.desertThreshold1.ToString();
        }

        set
        {
            double dValue = 0;
            if (System.Double.TryParse(value, out dValue))
            {
                textureSettings.desertThreshold1 = (float)dValue;
                UpdateSurfaceMaterialProperties();
                MapData.instance.Save();
            }
        }
    }

    public string DesertRate2
    {
        get
        {
            return textureSettings.desertThreshold2.ToString();
        }

        set
        {
            double dValue = 0;
            if (System.Double.TryParse(value, out dValue))
            {
                textureSettings.desertThreshold2 = (float)dValue;
                UpdateSurfaceMaterialProperties();
                MapData.instance.Save();
            }
        }
    }

    [SerializeField]
    public Color IceColor
    {
        get
        {
            return textureSettings.iceColor;
        }

        set
        {
            textureSettings.iceColor = value;
            UpdateSurfaceMaterialProperties();
            MapData.instance.Save();
        }
    }

    public string IceRate1
    {
        get
        {
            return textureSettings.iceTemperatureThreshold1.ToString();
        }

        set
        {
            double dValue = 0;
            if (System.Double.TryParse(value, out dValue))
            {
                textureSettings.iceTemperatureThreshold1 = (float)dValue;
                UpdateSurfaceMaterialProperties();
                MapData.instance.Save();
            }
        }
    }

    public string IceRate2
    {
        get
        {
            return textureSettings.iceTemperatureThreshold2.ToString();
        }

        set
        {
            double dValue = 0;
            if (System.Double.TryParse(value, out dValue))
            {
                textureSettings.iceTemperatureThreshold2 = (float)dValue;
                UpdateSurfaceMaterialProperties();
                MapData.instance.Save();
            }
        }
    }

    public string TextureWidth
    {
        get
        {
            return textureSettings.textureWidth.ToString();
        }
    }

    public void SetTextureWidth(string value)
    {
        if (value != null && value.Length > 0 && !updatingFieldCyclically)
        {
            int width = 1;
            try
            {
                width = Int32.Parse(value);
            }
            catch
            {
            }
            if (width > SystemInfo.maxTextureSize)
            {
                width = SystemInfo.maxTextureSize;
                UpdateUIInputField(setupPanelTransform, "Texture Width Text Box", width.ToString());
            }
            textureSettings.textureWidth = width;
            MapData.instance.Save();
        }
    }

    public string TextureHeight
    {
        get
        {
            return textureSettings.textureHeight.ToString();
        }
    }

    bool cyclicalNoiseTypeUpdate = false;
    public void SetNormalNoiseType(bool normalNoise)
    {
        textureSettings.surfaceNoiseSettings.ridged = !normalNoise;
        if (!cyclicalNoiseTypeUpdate)
        {
            cyclicalNoiseTypeUpdate = true;
            UpdateUIToggle(setupPanelTransform, "Toggle Ridged Noise", textureSettings.surfaceNoiseSettings.ridged);
            cyclicalNoiseTypeUpdate = false;
        }
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void SetRidgedNoiseType(bool ridged)
    {
        textureSettings.surfaceNoiseSettings.ridged = ridged;
        if (!cyclicalNoiseTypeUpdate)
        {
            cyclicalNoiseTypeUpdate = true;
            UpdateUIToggle(setupPanelTransform, "Toggle Regular Noise", !textureSettings.surfaceNoiseSettings.ridged);
            cyclicalNoiseTypeUpdate = false;
        }
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void SetTextureHeight(string value)
    {
        if (value != null && value.Length > 0 && !updatingFieldCyclically)
        {
            int height = 1;
            try
            {
                height = Int32.Parse(value);
            }
            catch
            {
            }
            if (height > SystemInfo.maxTextureSize)
            {
                height = SystemInfo.maxTextureSize;
                UpdateUIInputField(setupPanelTransform, "Texture Height Text Box", height.ToString());
            }
            textureSettings.textureHeight = height;
            MapData.instance.Save();
        }
    }

    public void NewMapDetail(float value)
    {
        textureSettings.Detail = value;
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void NewMapScaling(float value)
    {
        textureSettings.Scale = value;
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void NewSmoothness(float value)
    {
        textureSettings.Persistence = value;
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void NewHeightScale(float value)
    {
        textureSettings.heightScale = value;
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void NewLandMasses(float value)
    {
        textureSettings.Multiplier = (4.01f - value);
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void NewWaterLevel(float value)
    {
        textureSettings.waterLevel = value;
        MapData.instance.textureSettings = textureSettings;
        UpdateSurfaceMaterialProperties();
        MapData.instance.Save();
    }

    public void SliderDeselect()
    {
        UpdateSurfaceMaterialProperties();
    }

    public void WaterLevelDeselect()
    {
        UpdateSurfaceMaterialProperties(false);
    }
    #endregion

    #region Erosion
    public void SetErosionNoiseMerge(float value)
    {
        textureSettings.erosionNoiseMerge = value;
        if (erodedHeightMap == null || originalHeightMap == null || mergedHeightMap == null)
            return;

        HeightMap2Texture();
        UpdateSurfaceMaterialHeightMap(true);
    }

    public void SetNumErosionInterations(string value)
    {
        erosionSettings.numErosionIterations = value.ToInt();
    }

    public void SetErosionBrushRadius(float value)
    {
        erosionSettings.erosionBrushRadius = (int)value;
    }

    public void SetErosionMaxDropletLifetime(string value)
    {
        erosionSettings.maxLifetime = value.ToInt();
    }

    public void SetErosionSedimentCapacity(string value)
    {
        erosionSettings.sedimentCapacityFactor = value.ToFloat();
    }

    public void SetErosionMinSedimentCapacity(string value)
    {
        erosionSettings.minSedimentCapacity = value.ToFloat();
    }

    public void SetErosionDepositSpeed(float value)
    {
        erosionSettings.depositSpeed = value;
    }

    public void SetErosionErodeSpeed(float value)
    {
        erosionSettings.erodeSpeed = value;
    }

    public void SetErosionEvaporateSpeed(float value)
    {
        erosionSettings.evaporateSpeed = value;
    }

    public void SetErosionGravity(string value)
    {
        erosionSettings.gravity = value.ToFloat();
    }

    public void SetErosionStartSpeed(float value)
    {
        erosionSettings.startSpeed = value;
    }

    public void SetErosionWaterSpeed(float value)
    {
        erosionSettings.startWater = value;
    }

    public void SetErosionInertia(float value)
    {
        erosionSettings.inertia = value;
    }
    #endregion

    #region InciseFlow
    public void SetNumRivers(string rivers)
    {
        inciseFlowSettings.numIterations = rivers.ToInt();
    }

    public Color RiverColor
    {
        get
        {
            return inciseFlowSettings.riverColor;
        }

        set
        {
            inciseFlowSettings.riverColor = value;
        }
    }

    public void SetStartingAlpha(float startingAlpha)
    {
        inciseFlowSettings.startingAlpha = startingAlpha;
    }

    public void SetFlowHeightDelta(string flowHeightDelta)
    {
        inciseFlowSettings.flowHeightDelta = flowHeightDelta.ToFloat();
    }

    public void SetRiverBrushSize(float brushSize)
    {
        inciseFlowSettings.brushSize = brushSize;
    }

    public void SetRiverBrushExponent(float brushExponent)
    {
        inciseFlowSettings.brushExponent = brushExponent;
    }
    #endregion

    public void DeselectTextureSize()
    {
        Canvas canvas = null;
        if (cam != null)
            canvas = cam.GetComponentInChildren<Canvas>();

        if (setupPanelTransform == null)
            return;

        string strWidth  = GetUIInputField(setupPanelTransform, "Texture Width Text Box");

        int width = 0;
        try
        {
            width = Int32.Parse(strWidth);
        }
        catch
        {
        }

        if (textureSettings.textureWidth != width)
        {
            textureSettings.textureWidth = width;
        }
    }

    public void DoPlayGenerateTextures(bool playGenerateTextures)
    {
        if (!playGenerateTextures)
        {
            //Textures.instance.ResumeMatrixTextureBuild();

            Canvas canvas = null;
            if (cam == null)
                return;

            canvas = cam.GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            ButtonToggle[] buttonToggles = canvas.transform.GetComponentsInChildren<ButtonToggle>();
            foreach (ButtonToggle buttonToggle in buttonToggles)
            {
                if (buttonToggle.transform.name == "Button Pause")
                {
                    buttonToggle.Enable();
                }
            }
        }
    }

    public void DoPauseGenerateTextures(bool pauseGenerateTextures)
    {
        if (!pauseGenerateTextures)
        {
            //Textures.instance.PauseMatrixTextureBuild();

            Canvas canvas = null;
            if (cam == null)
                return;

            canvas = cam.GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            ButtonToggle[] buttonToggles = canvas.transform.GetComponentsInChildren<ButtonToggle>();
            foreach (ButtonToggle buttonToggle in buttonToggles)
            {
                if (buttonToggle.transform.name == "Button Play")
                {
                    buttonToggle.Enable();
                }
            }
        }
    }

    public void DoStopGenerateTextures(bool stopGenerateTextures)
    {
        if (!stopGenerateTextures)
        {
            //Textures.instance.StopTextureMatrixBuild();

            Canvas canvas = null;
            if (cam == null)
                return;

            canvas = cam.GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            ButtonToggle[] buttonToggles = canvas.transform.GetComponentsInChildren<ButtonToggle>();
            foreach (ButtonToggle buttonToggle in buttonToggles)
            {
                if (buttonToggle.transform.name == "Button Play")
                {
                    buttonToggle.Disable();
                }
                else if (buttonToggle.transform.name == "Button Pause")
                {
                    buttonToggle.Disable();
                }
            }

            if (setupPanelTransform == null)
                return;

            //textureSettings.textureSize = Textures.instance.CurrentTextureSize;
            UpdateUIInputField(setupPanelTransform, "Texture Width Text Box", textureSettings.textureWidth.ToString());
            UpdateUIInputField(setupPanelTransform, "Texture Height Text Box", textureSettings.textureHeight.ToString());
        }
    }

    public bool GetUseImages()
    {
        return mapSettings.UseImages;
    }

    public void UseImages(Boolean useImages)
    {
        mapSettings.UseImages = useImages;
        if (!firstUpdate)
        {
            MapData.instance.Save();
            if (mapSettings.UseImages)
            {
                UpdateSurfaceMaterialHeightMap();
                UpdateSurfaceMaterialMainMap();
                UpdateSurfaceMaterialLandMask();
            }
            ReGenerateWorld(true);
        }
    }

    public void SaveMainMap(bool doSave)
    {
        AppData.instance.SaveMainMap = doSave;
        AppData.instance.Save();
    }

    public void SaveHeightMap(bool doSave)
    {
        AppData.instance.SaveHeightMap = doSave;
        AppData.instance.Save();
    }

    public void SaveLandMask(bool doSave)
    {
        AppData.instance.SaveLandMask = doSave;
        AppData.instance.Save();
    }

    public void SaveNormalMap(bool doSave)
    {
        AppData.instance.SaveNormalMap = doSave;
        AppData.instance.Save();
    }

    public void SaveSpecularMap(bool doSave)
    {
        AppData.instance.SaveMainMap = doSave;
        AppData.instance.Save();
    }

    public void SaveTemperature(bool doSave)
    {
        AppData.instance.SaveTemperature = doSave;
        AppData.instance.Save();
    }

    public void SaveRivers(bool doSave)
    {
        AppData.instance.SaveRivers = doSave;
        AppData.instance.Save();
    }

    public void KeepSeedOnRegenerate(bool keepSeed)
    {
        AppData.instance.KeepSeedOnRegenerate = keepSeed;
        AppData.instance.Save();
    }

    public void AutoRegenerateOnParameterChange(bool autoRegenerate)
    {
        AppData.instance.AutoRegenerate = autoRegenerate;
        AppData.instance.Save();
    }

    public void OnLandGradientChanged(GradientSlider gradientSlider)
    {
        textureSettings.landColorStages = gradientSlider.Stages;
        textureSettings.land1Color = gradientSlider.Colors;
        UpdateSurfaceMaterialProperties();
    }

    public void OnOnceanGradientChanged(GradientSlider gradientSlider)
    {
        textureSettings.oceanStages = gradientSlider.Stages;
        textureSettings.oceanColors = gradientSlider.Colors;
        UpdateSurfaceMaterialProperties();
    }

    private void SetupPanelVariables()
    {
        if (cam == null)
            return;

        Canvas canvas = cam.GetComponentInChildren<Canvas>();
        if (canvas == null)
            return;

        setupPanelTransform = null;
        worldNameTransform = null;
        gradientPanelTransform = null;
        erosionPanelTransform = null;
        inciseFlowPanelTransform = null;

        foreach (Transform canvasChildTransform in canvas.transform)
        {
            if (canvasChildTransform.name == "World Menu Panel")
            {
                setupPanelTransform = canvasChildTransform;
            }
            else if (canvasChildTransform.name == "Erosion Panel")
            {
                erosionPanelTransform = canvasChildTransform;
            }
            else if (canvasChildTransform.name == "Rivers Panel")
            {
                inciseFlowPanelTransform = canvasChildTransform;
            }
            else if (canvasChildTransform.name == "World Name")
            {
                worldNameTransform = canvasChildTransform;
            }
            else if (canvasChildTransform.name == "Color Gradient Menu Panel")
            {
                gradientPanelTransform = canvasChildTransform;
            }
            else if (canvasChildTransform.name == "Context Menu")
            {
                contextMenuPanelTransform = canvasChildTransform;
            }
        }
    }

    private void UpdateMenuFields()
    {
        if (worldNameTransform != null)
        {
            GameObject worldName = worldNameTransform.gameObject;
            TMP_InputField worldNameTextMeshPro = worldName.GetComponent<TMP_InputField>();
            if (worldNameTextMeshPro != null)
                worldNameTextMeshPro.text = MapData.instance.WorldName;
        }

        if (setupPanelTransform != null)
        {
            UpdateUIInputField(setupPanelTransform, "Texture Width Text Box", TextureWidth);
            UpdateUIInputField(setupPanelTransform, "Texture Height Text Box", TextureHeight);
            // Setup the Seed Field
            UpdateUIInputField(setupPanelTransform, "Seed Text Box", mapSettings.Seed.ToString());
            // Setup the Radius Field.
            UpdateUIInputField(setupPanelTransform, "Radius Text Box", mapSettings.RadiusInKm.ToString("######0"));
            // Setup the Heightmap Field
            UpdateUIInputField(setupPanelTransform, "Heightmap Text Box", System.IO.Path.GetFileName(mapSettings.HeightMapPath));
            // Setup the Texture Field
            UpdateUIInputField(setupPanelTransform, "MainTexture Text Box", System.IO.Path.GetFileName(mapSettings.MainTexturePath));
            // Setup the Landmask Field
            UpdateUIInputField(setupPanelTransform, "LandMask Text Box", System.IO.Path.GetFileName(mapSettings.LandMaskPath));
            UpdateUISlider(setupPanelTransform, "Water Level Slider", textureSettings.waterLevel);
            UpdateUIToggle(setupPanelTransform, "Toggle Keep Seed", AppData.instance.KeepSeedOnRegenerate);
            UpdateUIToggle(setupPanelTransform, "Toggle Auto Regenerate", AppData.instance.AutoRegenerate);
            UpdateUIToggle(setupPanelTransform, "Toggle Use Images", mapSettings.UseImages);
            UpdateUIToggle(setupPanelTransform, "Toggle Regular Noise", !textureSettings.surfaceNoiseSettings.ridged);
            UpdateUIToggle(setupPanelTransform, "Toggle Ridged Noise", textureSettings.surfaceNoiseSettings.ridged);
            UpdateUISlider(setupPanelTransform, "Map Detail Slider", textureSettings.Detail);
            UpdateUISlider(setupPanelTransform, "Map Scaling Slider", textureSettings.Scale);
            UpdateUISlider(setupPanelTransform, "Smoothness Slider", textureSettings.Persistence);
            UpdateUISlider(setupPanelTransform, "Height Range Slider", textureSettings.heightScale);
            UpdateUISlider(setupPanelTransform, "Landmasses Slider", 4.01f - textureSettings.Multiplier);

            UpdateUIGradientSlider(gradientPanelTransform, textureSettings.landColorStages, textureSettings.land1Color, textureSettings.oceanStages, textureSettings.oceanColors);

            UpdateUIInputField(gradientPanelTransform, "Desert Range 1 Text Box", textureSettings.desertThreshold1.ToString());
            UpdateUIInputField(gradientPanelTransform, "Desert Range 2 Text Box", textureSettings.desertThreshold2.ToString());
            UpdateUIInputField(gradientPanelTransform, "Ice Range 1 Text Box", textureSettings.iceTemperatureThreshold1.ToString());
            UpdateUIInputField(gradientPanelTransform, "Ice Range 2 Text Box", textureSettings.iceTemperatureThreshold2.ToString());
        }

        if (gradientPanelTransform != null)
        {
            UpdateUIColorPanel(gradientPanelTransform, "Desert Color Panel", textureSettings.desertColor);
            UpdateUIColorPanel(gradientPanelTransform, "Ice Color Panel", textureSettings.iceColor);
        }

        if (contextMenuPanelTransform != null)
        {
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Main Map", AppData.instance.SaveMainMap);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Height Map", AppData.instance.SaveHeightMap);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Land Mask", AppData.instance.SaveLandMask);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Normal Map", AppData.instance.SaveNormalMap);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Specular Map", AppData.instance.SaveSpecularMap);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Temperature", AppData.instance.SaveTemperature);
            UpdateUIToggle(contextMenuPanelTransform, "Toggle Rivers", AppData.instance.SaveRivers);
        }

        if (erosionPanelTransform != null)
        {
            UpdateUISlider(erosionPanelTransform, "Noise Merge Slider", textureSettings.erosionNoiseMerge);
            UpdateUIInputField(erosionPanelTransform, "Iterations Text Box", erosionSettings.numErosionIterations.ToString());
            UpdateUISlider(erosionPanelTransform, "Brush Radius Slider", erosionSettings.erosionBrushRadius);
            UpdateUIInputField(erosionPanelTransform, "Lifetime Text Box", erosionSettings.maxLifetime.ToString());
            UpdateUIInputField(erosionPanelTransform, "Sediment Capacity Text Box", erosionSettings.sedimentCapacityFactor.ToString());
            UpdateUIInputField(erosionPanelTransform, "Min Sediment Capacity Text Box", erosionSettings.minSedimentCapacity.ToString());
            UpdateUISlider(erosionPanelTransform, "Deposit Speed Slider", erosionSettings.depositSpeed);
            UpdateUISlider(erosionPanelTransform, "Erode Speed Slider", erosionSettings.erodeSpeed);
            UpdateUISlider(erosionPanelTransform, "Evaporate Speed Slider", erosionSettings.evaporateSpeed);
            UpdateUIInputField(erosionPanelTransform, "Gravity Text Box", erosionSettings.gravity.ToString());
            UpdateUISlider(erosionPanelTransform, "Start Speed Slider", erosionSettings.startSpeed);
            UpdateUISlider(erosionPanelTransform, "Start Water Slider", erosionSettings.startWater);
            UpdateUISlider(erosionPanelTransform, "Inertia Slider", erosionSettings.inertia);
        }

        if (inciseFlowPanelTransform != null)
        {
            UpdateUIInputField(inciseFlowPanelTransform, "Rivers Text Box", inciseFlowSettings.numIterations.ToString());
            UpdateUIInputField(inciseFlowPanelTransform, "River Erosion Text Box", inciseFlowSettings.flowHeightDelta.ToString());
            UpdateUISlider(inciseFlowPanelTransform, "Starting Alpha Slider", inciseFlowSettings.startingAlpha);
            UpdateUISlider(inciseFlowPanelTransform, "River Brush Size Slider", inciseFlowSettings.brushSize);
            UpdateUISlider(inciseFlowPanelTransform, "River Brush Exponent Slider", inciseFlowSettings.brushExponent);
            UpdateUIColorPanel(inciseFlowPanelTransform, "River Color Panel", inciseFlowSettings.riverColor);
        }
    }

    private string GetUIInputField(Transform panelTransform, string textBoxName)
    {
        Transform textBoxTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == textBoxName)
            {
                textBoxTransform = setupPanelChildTransform;
                break;
            }
        }

        if (textBoxTransform == null)
            return null;

        GameObject textBox = textBoxTransform.gameObject;
        TMP_InputField inputField = textBox.GetComponent<TMP_InputField>();

        if (inputField == null)
            return null;

        return inputField.text;
    }

    private void UpdateUIInputField(Transform panelTransform, string textBoxName, string newValue)
    {
        Transform textBoxTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == textBoxName)
            {
                textBoxTransform = setupPanelChildTransform;
                break;
            }
        }

        if (textBoxTransform == null)
            return;

        GameObject textBox = textBoxTransform.gameObject;
        TMP_InputField inputField = textBox.GetComponent<TMP_InputField>();

        if (inputField == null)
            return;

        inputField.text = newValue;
    }

    private void UpdateUIColorPanel(Transform panelTransform, string panelName, Color color)
    {
        Transform colorPanelTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == panelName)
            {
                colorPanelTransform = setupPanelChildTransform;
                break;
            }
        }

        if (colorPanelTransform == null)
            return;

        GameObject colorPanel = colorPanelTransform.gameObject;
        ColorBox colorBox = colorPanel.GetComponent<ColorBox>();

        if (colorBox == null)
            return;

        colorBox.Color = color;
    }

    private void UpdateUIInputFieldDirect(Transform panelTransform, string textBoxName, string newValue)
    {
        Transform textBoxTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == textBoxName)
            {
                textBoxTransform = setupPanelChildTransform;
                break;
            }
        }

        if (textBoxTransform == null)
            return;

        foreach (Transform setupPanelChildChildTransform in textBoxTransform)
        {
            GameObject childGO = setupPanelChildChildTransform.gameObject;
            TMPro.TextMeshProUGUI textMeshProUGUI = childGO.GetComponent<TMPro.TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = newValue;
                break;
            }
        }
    }

    private void UpdateUISlider(Transform panelTransform, string sliderName, float sliderValue)
    {
        Transform sliderTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == sliderName)
            {
                sliderTransform = setupPanelChildTransform;
                break;
            }
        }

        if (sliderTransform == null)
            return;

        GameObject sliderGO = sliderTransform.gameObject;
        Slider slider = sliderGO.GetComponent<Slider>();

        if (slider == null)
            return;

        MapSliderField mapSliderField = sliderGO.GetComponent<MapSliderField>();
        if (mapSliderField)
        {
            mapSliderField.Init(sliderValue);
        }
        else
        {
            slider.value = sliderValue;
        }
    }

    private void UpdateUIToggle(Transform panelTransform, string toggleName, bool toggleValue)
    {
        Transform toggleTransform = null;
        foreach (Transform setupPanelChildTransform in panelTransform.transform)
        {
            if (setupPanelChildTransform.name == toggleName)
            {
                toggleTransform = setupPanelChildTransform;
                break;
            }
        }

        if (toggleTransform == null)
            return;

        GameObject toggleGO = toggleTransform.gameObject;
        Toggle toggle = toggleGO.GetComponent<Toggle>();

        if (toggle == null)
            return;

        toggle.isOn = toggleValue;
    }

    private void UpdateUIGradientSlider(Transform gradientPanelTransform, float[] landColorStages, Color32[] land1Color, float[] oceanStages, Color32[] oceanColors)
    {
        foreach (Transform childTransform in gradientPanelTransform)
        {
            if (childTransform.name == "Base Slider")
            {
                GradientSlider gradientSlider = childTransform.gameObject.GetComponent<GradientSlider>();
                for (int i = 0; i < landColorStages.Length; i++)
                {
                    if (land1Color.Length <= i)
                        continue;

                    float level = landColorStages[i];
                    Color32 color = land1Color[i];

                    gradientSlider.CreateNewGradientPoint(level, color);
                }
            }
            else if (childTransform.name == "Ocean Slider")
            {
                GradientSlider gradientSlider = childTransform.gameObject.GetComponent<GradientSlider>();
                for (int i = 0; i < oceanStages.Length; i++)
                {
                    if (oceanColors.Length <= i)
                        continue;

                    float level = oceanStages[i];
                    Color32 color = oceanColors[i];

                    gradientSlider.CreateNewGradientPoint(level, color);
                }
            }
        }
    }

    void ShowPlottingRiversPanel()
    {
        Canvas canvas = cam.GetComponentInChildren<Canvas>();
        Component childComponent = canvas.GetChildWithName("Plotting Rivers Panel");
        if (childComponent == null || !(childComponent is RectTransform))
            return;

        RectTransform rectTransform = childComponent as RectTransform;
        Vector3 newPosition = new Vector3(0, 0, rectTransform.localPosition.z);
        rectTransform.localPosition = newPosition;
    }

    void HidePlottingRiversPanel()
    {
        Canvas canvas = cam.GetComponentInChildren<Canvas>();
        Component childComponent = canvas.GetChildWithName("Plotting Rivers Panel");
        if (childComponent == null || !(childComponent is RectTransform))
            return;

        RectTransform rectTransform = childComponent as RectTransform;
        Vector3 newPosition = new Vector3(0, -1680, rectTransform.localPosition.z);
        rectTransform.localPosition = newPosition;
    }
}
