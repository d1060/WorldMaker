Shader "Noise/PlanetarySurfaceTexture"
{
    Properties
    {
        _WaterLevel("Water Level", Range(0, 1)) = 0.66
        [Enum(SphereShaderDrawType)] _DrawType("Draw Type", Float) = 0

        _TextureWidth("Texture Width", Float) = 2048

        _HeightMap("Heightmap", 2D) = "white" {}
        _MainMap("Main Map", 2D) = "white" {}
        _IsMainmapSet("Is Main Map Set", Int) = 0
        _LandMask("Land Mask", 2D) = "white" {}
        _IsLandmaskSet("Is Land Mask Set", Int) = 0
        _FlowTex("FlowMap", 2D) = "black" {}
        _IsFlowTexSet("Is Flow Texture Set", Int) = 0
        _NoiseMap("NoiseMap", 2D) = "black" {}
        _IsNoiseMapSet("Is Noise Texture Set", Int) = 0

        _Glossiness("Smoothness", Range(0,1)) = 1.0
        _Metallic("Metallicity", Range(0,1)) = 0.4
        _LandGlossiness("Land Smoothness", Range(0,1)) = 0.25
        _LandMetallic("Land Metallicity", Range(0,1)) = 0.1

        _ColorSteps("Color Steps", Int) = 6
        _ColorStep1("Color Step 1", Range(0, 1)) = 0.01
        _Color1("Color 1", Color) = (0.69, 0.63, 0.43)
        _ColorStep2("Color Step 2", Range(0, 1)) = 0.1
        _Color2("Color 2", Color) = (0.5, 0.5, 0.27)
        _ColorStep3("Color Step 3", Range(0, 1)) = 0.6
        _Color3("Color 3", Color) = (0.2, 0.27, 0.03)
        _ColorStep4("Color Step 4", Range(0, 1)) = 0.8
        _Color4("Color 4", Color) = (0.53, 0.54, 0.36)
        _ColorStep5("Color Step 5", Range(0, 1)) = 0.95
        _Color5("Color 5", Color) = (0.45, 0.41, 0.32)
        _ColorStep6("Color Step 6", Range(0, 1)) = 1.0
        _Color6("Color 6", Color) = (0.95, 0.95, 0.95)
        _ColorStep7("Color Step 7", Range(0, 1)) = 1.0
        _Color7("Color 7", Color) = (0.95, 0.95, 0.95)
        _ColorStep8("Color Step 8", Range(0, 1)) = 1.0
        _Color8("Color 8", Color) = (0.95, 0.95, 0.95)

        _OceanColorSteps("OceanColor Steps", Int) = 3
        _OceanColorStep1("Ocean Color Step 1", Range(0, 1)) = 0
        _OceanColor1("Ocean Color 1", Color) = (0.25, 0.33, 0.39)
        _OceanColorStep2("Ocean Color Step 2", Range(0, 1)) = 0.5
        _OceanColor2("Ocean Color 2", Color) = (0.19, 0.21, 0.29)
        _OceanColorStep3("Ocean Color Step 3", Range(0, 1)) = 1
        _OceanColor3("Ocean Color 3", Color) = (0.19, 0.2, 0.27)
        _OceanColorStep4("Ocean Color Step 4", Range(0, 1)) = 1
        _OceanColor4("Ocean Color 4", Color) = (0.19, 0.2, 0.27)

        _IceTemperatureThreshold1("Ice Threshold 1", Float) = 0
        _IceTemperatureThreshold2("Ice Threshold 2", Float) = -10
        _DesertThreshold1("Desert Threshold 1", Float) = 10
        _DesertThreshold2("Desert Threshold 2", Float) = 20
        _HighHumidityLightnessPercentage("Humidity Lightness", Range(0,1)) = 0.2
        _IceColor("Ice Color", Color) = (0.95, 0.95, 0.95)
        _DesertColor("Desert Color", Color) = (0.75, 0.88, 0.55)
        _NormalScale("Normal Scale Multiplier", Range(0, 100)) = 50
        _NormalInfluence("Normal Influence", Range(0, 1)) = 0
        _UnderwaterNormalScale("Underwater Normal Scale", Range(0, 100)) = 20
        _UnderwaterNormalInfluence("Underwater Normal Influence", Range(0, 1)) = 0
        _IceLandNormalScale("Ice Normal Scale on Land", Range(0, 100)) = 75
        _IceWaterNormalScale("Ice Normal Scale on Water", Range(0, 100)) = 50
        _GrayscaleGammaCorrection("Grayscale Gamma", Range(0, 5)) = 2.2
        _GrayscaleContrast("Grayscale Contrast", Range(0, 5)) = 1
        _BakedNormalIntensity("Baked Normal Intensity", Range(0, 5)) = 1

        _TemperatureExponent("Temperature Sigmoid Exponent", Range(0, 50)) = 2.718281
        _TemperatureRatio("Temperature Ratio", Range(0, 100)) = 20 // From 0 to 20
        _TemperatureElevationRatio("Temperature Elevation Ratio", Range(0, 50)) = 35 //World has 5km height, temperature falls 7 degrees for each km.
        _TemperatureWaterDrop("Temperature Water Drop", Range(0, 20)) = 5
        _TemperatureLatitudeMultiplier("Temperature Latitude Multiplier", Range(0, 100)) = 40
        _TemperatureLatitudeDrop("Temperature Latitude Drop", Range(0, 100)) = 15

        _HumidityExponent("Humidity Sigmoid Exponent", Range(0, 50)) = 2.718281
        _HumidityMultiplier("Humidity Sigmoid Multiplier", Range(0, 100)) = 20
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

            CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #include "Assets/Shaders/Simplex.cginc"
        #include "Assets/Shaders/Sphere.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #define LONGITUDE_STEP 0.000244140f
        #define LATITUDE_STEP 0.000488281f
        #define PI 3.14159265359
        #define TWO_PI 6.28318530718

        float _TextureWidth;

        sampler2D _HeightMap;

        sampler2D_float _MainMap;
        int _IsMainmapSet;
        sampler2D_float _LandMask;
        int _IsLandmaskSet;
        sampler2D_float _FlowTex;
        int _IsFlowTexSet;
        sampler2D_float _NoiseMap;
        int _IsNoiseMapSet;

        struct Input
        {
            float2 uv_HeightMap;
            float3 worldNormal; INTERNAL_DATA
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        half _LandGlossiness;
        half _LandMetallic;

        float _WaterLevel;
        float _ColorStep1;
        fixed4 _Color1;
        float _ColorStep2;
        fixed4 _Color2;
        float _ColorStep3;
        fixed4 _Color3;
        float _ColorStep4;
        fixed4 _Color4;
        float _ColorStep5;
        fixed4 _Color5;
        float _ColorStep6;
        fixed4 _Color6;
        float _ColorStep7;
        fixed4 _Color7;
        float _ColorStep8;
        fixed4 _Color8;

        float _OceanColorStep1;
        fixed4 _OceanColor1;
        float _OceanColorStep2;
        fixed4 _OceanColor2;
        float _OceanColorStep3;
        fixed4 _OceanColor3;
        float _OceanColorStep4;
        fixed4 _OceanColor4;

        float _IceTemperatureThreshold1;
        float _IceTemperatureThreshold2;
        float _DesertThreshold1;
        float _DesertThreshold2;
        float _HighHumidityLightnessPercentage;
        fixed4 _IceColor;
        fixed4 _DesertColor;
        float _NormalScale;
        float _NormalInfluence;
        float _UnderwaterNormalScale;
        float _UnderwaterNormalInfluence;
        float _IceLandNormalScale;
        float _IceWaterNormalScale;
        float _GrayscaleGammaCorrection;
        float _GrayscaleContrast;

        float _DrawType;
        float _XOffset;
        float _YOffset;
        float _ZOffset;
        float _BakedNormalIntensity;

        float _TemperatureExponent;
        float _TemperatureRatio;
        float _TemperatureElevationRatio;
        float _TemperatureWaterDrop;
        float _TemperatureLatitudeMultiplier;
        float _TemperatureLatitudeDrop;

        float _HumidityExponent;
        float _HumidityMultiplier;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float3x3 rotateAlign(float3 v1, float3 v2)
        {
            float3 axis = cross(v1, v2);

            const float cosA = dot(v1, v2);
            const float k = 1.0f / (1.0f + cosA);

            float3x3 result = { (axis.x * axis.x * k) + cosA,
                                 (axis.y * axis.x * k) - axis.z,
                                 (axis.z * axis.x * k) + axis.y,
                                 (axis.x * axis.y * k) + axis.z,
                                 (axis.y * axis.y * k) + cosA,
                                 (axis.z * axis.y * k) - axis.x,
                                 (axis.x * axis.z * k) - axis.y,
                                 (axis.y * axis.z * k) + axis.x,
                                 (axis.z * axis.z * k) + cosA
            };

            return result;
        }

        void getSphereCoordinatesOrthogonals(float3 sphereCoordinates, out float3 right, out float3 up)
        {
            float3 sphereTop = float3(0, 1, 0);

            if (sphereCoordinates.x == 0 && sphereCoordinates.y == 1 && sphereCoordinates.z == 0)
                sphereTop = float3(0, 0, 1);
            else if (sphereCoordinates.x == 0 && sphereCoordinates.y == -1 && sphereCoordinates.z == 0)
                sphereTop = float3(0, 0, -1);

            right = cross(sphereTop, sphereCoordinates);
            up = cross(sphereCoordinates, right);
        }

        float2 uvAdd(float3 sphereCoordinates, float3 vectorAdd)
        {
            float3 newSphereCoordinates = normalize(sphereCoordinates + (vectorAdd * LATITUDE_STEP));
            return SphereToUv(newSphereCoordinates);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_HeightMap;
            if (uv.x > 2) uv.x -= 2;
            else if (uv.x > 1) uv.x -= 1;

            float3 sphereCoordinates = UvToSphere(uv);
            float3 right;
            float3 up;
            getSphereCoordinatesOrthogonals(sphereCoordinates, right, up);

            float2 uvLeft = uvAdd(sphereCoordinates, -right);
            if (uvLeft.x < 0) uvLeft.x += 1;

            float2 uvRight = uvAdd(sphereCoordinates, right);
            if (uvRight.x > 1) uvRight.x -= 1;

            float2 uvTop = uvAdd(sphereCoordinates, up);
            if (uvTop.y > 1) uvTop.y = 1;

            float2 uvBottom = uvAdd(sphereCoordinates, -up);
            if (uvBottom.y < 0) uvBottom.y = 0;

            float height = 0;
            float3 offset = float3(_XOffset, _YOffset, _ZOffset);

            float4 c = tex2D(_HeightMap, uv);
            height = (c.r + c.g + c.b) / 3;
            float elevationRatio = ((height - _WaterLevel) / (1 - _WaterLevel));
            float depthRatio = 1 - (height / _WaterLevel);

            float temperature = 0;
            float humidity = 0;

            bool isAboveWater = height > _WaterLevel;

            if (_IsNoiseMapSet)
            {
                float4 n = tex2D(_NoiseMap, uv);
                float4 nLeft = tex2D(_NoiseMap, uvLeft);
                float4 nRight = tex2D(_NoiseMap, uvRight);
                float4 nTop = tex2D(_NoiseMap, uvTop);
                float4 nBottom = tex2D(_NoiseMap, uvBottom);

                temperature = (n.r + (nLeft.r + nRight.r + nTop.r + nBottom.r) / 4) / 2;
                humidity = n.g;

                // Temperature
                // Adjusts temperature with a sigmoid curve.
                temperature = 1 / (1 + pow(_TemperatureExponent, 5 * (-2 * (temperature - 0.5))));
                //temperature *= _TemperatureExponent;
                //if (temperature <= 0.5) temperature = 0;
                //else temperature = 1;

                temperature = (temperature - 0.5) * _TemperatureRatio; // From 0 to 20

                if (isAboveWater)
                {
                    // From -35 to 10
                    float temperatureDrop = elevationRatio * _TemperatureElevationRatio; //World has 5km height, temperature falls 7 degrees for each km.
                    temperature -= temperatureDrop;
                }
                else
                {
                    temperature -= _TemperatureWaterDrop;
                }

                float actualLatitude = (abs(uv.y - 0.5) * 2);
                float latitudeTemperature = 1 - actualLatitude; // 0 == 90 degrees, 1 = 0 degrees.
                latitudeTemperature *= _TemperatureLatitudeMultiplier; // From 0 to 10
                latitudeTemperature -= _TemperatureLatitudeDrop; // From -15 to 25
                temperature += latitudeTemperature; // -15 to 45


                // Adjusts humidity with a sigmoid curve.
                humidity = 1 / (1 + pow(_HumidityExponent, _HumidityMultiplier * (humidity - 0.5)));
            }

            if (_DrawType == 1) // Drawing a Heightmap.
            {
                //Gamma Correction. 
                float correctedHeight = pow(height, _GrayscaleGammaCorrection);
                correctedHeight = ((correctedHeight - 0.5) * _GrayscaleContrast) + 0.5;
                correctedHeight = clamp(correctedHeight, 0, 1);
                o.Albedo = float4(correctedHeight, correctedHeight, correctedHeight, 1);
                o.Metallic = 0;
                o.Smoothness = 0;
            }
            else if (_DrawType == 2) // Drawing a Landmask.
            {
                if (_IsLandmaskSet)
                {
                    float4 lmc = tex2D(_LandMask, uv);
                    o.Albedo = lmc;
                }
                else
                {
                    float2 uvDL = float2((int)(uv.x * _TextureWidth * 4) / (float)(_TextureWidth * 4), (int)(uv.y * _TextureWidth * 2) / (float)(_TextureWidth * 2));
                    float2 uvDR = float2(((int)(uv.x * _TextureWidth * 4) + 1) / (float)(_TextureWidth * 4), (int)(uv.y * _TextureWidth * 2) / (float)(_TextureWidth * 2));
                    float2 uvUL = float2((int)(uv.x * _TextureWidth * 4) / (float)(_TextureWidth * 4), ((int)(uv.y * _TextureWidth * 2) + 1) / (float)(_TextureWidth * 2));
                    float2 uvUR = float2(((int)(uv.x * _TextureWidth * 4) + 1) / (float)(_TextureWidth * 4), ((int)(uv.y * _TextureWidth * 2) + 1) / (float)(_TextureWidth * 2));

                    if (uvDR.x > 1)
                    {
                        uvDR.x -= 1;
                        uvUR.x -= 1;
                    }

                    if (uvUL.y > 1)
                    {
                        uvUL.y = 1;
                        uvUR.y = 1;
                    }

                    float4 cDL = tex2D(_HeightMap, uvDL);
                    float heightDL = (cDL.r + cDL.g + cDL.b) / 3;

                    float4 cDR = tex2D(_HeightMap, uvDR);
                    float heightDR = (cDR.r + cDR.g + cDR.b) / 3;

                    float4 cUL = tex2D(_HeightMap, uvUL);
                    float heightUL = (cUL.r + cUL.g + cUL.b) / 3;

                    float4 cUR = tex2D(_HeightMap, uvUR);
                    float heightUR = (cUR.r + cUR.g + cUR.b) / 3;

                    bool isAboveWaterDL = heightDL > _WaterLevel;
                    bool isAboveWaterDR = heightDR > _WaterLevel;
                    bool isAboveWaterUL = heightUL > _WaterLevel;
                    bool isAboveWaterUR = heightUR > _WaterLevel;

                    float greyScale = (isAboveWaterDL ? 0.25 : 0) + (isAboveWaterDR ? 0.25 : 0) + (isAboveWaterUL ? 0.25 : 0) + (isAboveWaterUR ? 0.25 : 0);

                    o.Albedo = float3(greyScale, greyScale, greyScale);
                }

                o.Metallic = 0;
                o.Smoothness = 0;
            }
            else if (_DrawType == 7) // Drawing an inverted Landmask.
            {
                if (_IsLandmaskSet)
                {
                    float4 lmc = tex2D(_LandMask, uv);
                    o.Albedo = float4(1 - lmc.r, 1 - lmc.g, 1 - lmc.b, 1);
                }
                else
                {
                    float2 uvDL = float2((int)(uv.x * _TextureWidth * 4) / (float)(_TextureWidth * 4), (int)(uv.y * _TextureWidth * 2) / (float)(_TextureWidth * 2));
                    float2 uvDR = float2(((int)(uv.x * _TextureWidth * 4) + 1) / (float)(_TextureWidth * 4), (int)(uv.y * _TextureWidth * 2) / (float)(_TextureWidth * 2));
                    float2 uvUL = float2((int)(uv.x * _TextureWidth * 4) / (float)(_TextureWidth * 4), ((int)(uv.y * _TextureWidth * 2) + 1) / (float)(_TextureWidth * 2));
                    float2 uvUR = float2(((int)(uv.x * _TextureWidth * 4) + 1) / (float)(_TextureWidth * 4), ((int)(uv.y * _TextureWidth * 2) + 1) / (float)(_TextureWidth * 2));

                    if (uvDR.x > 1)
                    {
                        uvDR.x -= 1;
                        uvUR.x -= 1;
                    }

                    if (uvUL.y > 1)
                    {
                        uvUL.y = 1;
                        uvUR.y = 1;
                    }

                    float4 cDL = tex2D(_HeightMap, uvDL);
                    float heightDL = (cDL.r + cDL.g + cDL.b) / 3;

                    float4 cDR = tex2D(_HeightMap, uvDR);
                    float heightDR = (cDR.r + cDR.g + cDR.b) / 3;

                    float4 cUL = tex2D(_HeightMap, uvUL);
                    float heightUL = (cUL.r + cUL.g + cUL.b) / 3;

                    float4 cUR = tex2D(_HeightMap, uvUR);
                    float heightUR = (cUR.r + cUR.g + cUR.b) / 3;

                    bool isAboveWaterDL = heightDL > _WaterLevel;
                    bool isAboveWaterDR = heightDR > _WaterLevel;
                    bool isAboveWaterUL = heightUL > _WaterLevel;
                    bool isAboveWaterUR = heightUR > _WaterLevel;

                    float greyScale = (isAboveWaterDL ? 0.25 : 0) + (isAboveWaterDR ? 0.25 : 0) + (isAboveWaterUL ? 0.25 : 0) + (isAboveWaterUR ? 0.25 : 0);

                    o.Albedo = float3(1 - greyScale, 1 - greyScale, 1 - greyScale);
                }

                o.Metallic = 0;
                o.Smoothness = 0;
            }
            else // DrawType != 1, 2, 7
            {
                if (_DrawType == 3) // Drawing a Temperature mask.
                {
                    o.Metallic = 0;
                    o.Smoothness = 0;
                    o.Albedo = temperatureColor(temperature);
                }
                else if (_DrawType == 5) // Drawing a Humidity mask.
                {
                    o.Metallic = 0;
                    o.Smoothness = 0;

                    if (!isAboveWater)
                        o.Albedo = float4(0, 0, 0.5, 1);
                    else
                    {
                        o.Albedo = humidityColor(humidity);
                    }
                }
                else // DrawType == 0, 4, 6 or 8
                {
                    //float2 prevLongitude = float2(uv.x - LONGITUDE_STEP, uv.y);
                    //float2 prevLatitude = float2(uv.x, uv.y - LATITUDE_STEP);

                    //if (prevLongitude.x < 0) prevLongitude.x += 1;
                    //if (prevLatitude.y < 0)  prevLatitude.y = 0;

                    //float prevLongitudeHeight = 0;
                    //float prevLatitudeHeight = 0;

                    float4 prevLongitudeColor = tex2D(_HeightMap, uvLeft);
                    float prevLongitudeHeight = (prevLongitudeColor.r + prevLongitudeColor.g + prevLongitudeColor.b) / 3;

                    float4 prevLatitudeColor = tex2D(_HeightMap, uvBottom);
                    float prevLatitudeHeight = (prevLatitudeColor.r + prevLatitudeColor.g + prevLatitudeColor.b) / 3;

                    float verticalDeltaHeight = prevLatitudeHeight - height;
                    float horizontalDeltaHeight = height - prevLongitudeHeight;

                    if (_DrawType == 4) // Drawing a Normal mask.
                    {
                        o.Metallic = 0;
                        o.Smoothness = 0;

                        float horizontalAngle = atan(horizontalDeltaHeight / (1 / (2 * _TextureWidth))) / PI;
                        float verticalAngle = atan(verticalDeltaHeight / (1 / _TextureWidth)) / PI;

                        float4 normalColor = float4(
                            horizontalAngle + 0.5,
                            verticalAngle + 0.5,
                            1,
                            1);

                        o.Albedo = normalColor;
                    }
                    else // Drawing the main map.
                    {
                        // Get Land Color
                        float4 color = float4(0, 0, 0, 1);
                        int isflowColor = 0;
                        float4 flowColor = float4(0, 0, 0, 0);

                        if (_IsFlowTexSet)
                        {
                            flowColor = tex2D(_FlowTex, uv);
                            if ((flowColor.r != 0 || flowColor.g != 0 || flowColor.b != 0) && isAboveWater)
                            {
                                if (flowColor.a >= 1)
                                {
                                    color = flowColor;
                                    isflowColor = 1;
                                }
                            }
                        }

                        if (isflowColor == 0)
                        {
                            if (_IsMainmapSet)
                            {
                                color = tex2D(_MainMap, uv);
                            }
                            else
                            {
                                color = colorAtElevation(height, _WaterLevel, temperature, humidity,
                                    _IceTemperatureThreshold1, _IceTemperatureThreshold2,
                                    _DesertThreshold1, _DesertThreshold2,
                                    _HighHumidityLightnessPercentage, _IceColor, _DesertColor,
                                    _IsLandmaskSet, isAboveWater,
                                    _ColorStep1, _Color1, _ColorStep2, _Color2, _ColorStep3, _Color3, _ColorStep4, _Color4, _ColorStep5, _Color5, _ColorStep6, _Color6, _ColorStep7, _Color7, _ColorStep8, _Color8,
                                    _OceanColorStep1, _OceanColor1, _OceanColorStep2, _OceanColor2, _OceanColorStep3, _OceanColor3, _OceanColorStep4, _OceanColor4);

                                if (flowColor.a > 0 && isAboveWater)
                                {
                                    color = color * (1 - flowColor.a) + flowColor * flowColor.a;
                                    color.a = 1;
                                }
                            }
                        }

                        float3 normal;
                        float normalScaleToUse = _NormalScale;
                        float normalInfluence = 1;
                        float metallicityToUse = _LandMetallic;
                        float glossinessToUse = _LandGlossiness;
                        if (!isAboveWater)
                        {
                            normalScaleToUse = _UnderwaterNormalScale;
                            metallicityToUse = _Metallic;
                            glossinessToUse = _Glossiness;

                            if (temperature < _IceTemperatureThreshold1)
                            {
                                float temperatureRatio = (_IceTemperatureThreshold1 - temperature) / (_IceTemperatureThreshold1 - _IceTemperatureThreshold2);
                                if (temperatureRatio > 1) temperatureRatio = 1;

                                normalScaleToUse = (_IceWaterNormalScale - _UnderwaterNormalScale) * temperatureRatio + _UnderwaterNormalScale;
                            }
                            normalInfluence = depthRatio * _UnderwaterNormalInfluence + (1 - _UnderwaterNormalInfluence);
                        }
                        else
                        {
                            if (temperature < _IceTemperatureThreshold1)
                            {
                                float temperatureRatio = (_IceTemperatureThreshold1 - temperature) / (_IceTemperatureThreshold1 - _IceTemperatureThreshold2);
                                if (temperatureRatio > 1) temperatureRatio = 1;

                                normalScaleToUse = (_IceLandNormalScale - _NormalScale) * temperatureRatio + _NormalScale;
                            }
                            normalInfluence = elevationRatio * _NormalInfluence + (1 - _NormalInfluence);
                        }

                        float z = sqrt(1 - pow(horizontalDeltaHeight * normalScaleToUse * normalInfluence, 2) - pow(verticalDeltaHeight * normalScaleToUse * normalInfluence, 2));
                        o.Metallic = metallicityToUse;
                        o.Smoothness = glossinessToUse;
                        normal = float3(
                            horizontalDeltaHeight * normalScaleToUse * normalInfluence,
                            verticalDeltaHeight * normalScaleToUse * normalInfluence,
                            z);

                        if (_DrawType == 8)
                        {
                            color *= 1.3;
                        }

                        if (_DrawType == 0 || _DrawType == 8) // Land
                        {
                            o.Albedo = color;
                            o.Normal = normal;
                        }
                        else if (_DrawType == 6) // Land with baked normals.
                        {
                            float3 lightRay = float3(0.657439604, 0.657439604, 0.368166178);
                            float projection = dot(normal, lightRay);
                            color *= projection * _BakedNormalIntensity;
                            o.Albedo = color;
                        }
                    }
                }
            }
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
