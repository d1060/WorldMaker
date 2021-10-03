Shader "Noise/PlanetarySurface"
{
    Properties
    {
        _Seed("Land Seed", Int) = 2343
        _XOffset("X Offset", Range(0, 1)) = 0
        _YOffset("Y Offset", Range(0, 1)) = 0
        _ZOffset("Z Offset", Range(0, 1)) = 0
        _TemperatureSeed("Temperature Seed", Int) = 345
        _HumiditySeed("Humidity Seed", Int) = 987
        _Multiplier("Noise Multiplier", Range(0.5, 5)) = 1
        _Octaves("Number of Octaves", Range(1, 30)) = 10
        _Lacunarity("Lacunarity", Range(1, 2)) = 1.5
        _Persistence("Persistance", Range(0, 1)) = 0.7
        _LayerStrength("Layer Strength", Range(0, 1)) = 1
        _HeightExponent("Height Exponent", Range(0, 5)) = 1
        _WaterLevel("Water Level", Range(0, 1)) = 0.66
        _HeightRange("Height Range", Range(0, 1)) = 0.35
        _RidgedNoise("Ridged Noise", Int) = 0
        [Enum(SphereShaderDrawType)] _DrawType("Draw Type", Float) = 0

        _Seed2("Land Seed 2", Int) = 2343
        _XOffset2("X Offset 2", Range(0, 1)) = 0
        _YOffset2("Y Offset 2", Range(0, 1)) = 0
        _ZOffset2("Z Offset 2", Range(0, 1)) = 0
        _Multiplier2("Noise Multiplier 2", Range(0.5, 5)) = 1
        _Octaves2("Number of Octaves 2", Range(1, 30)) = 10
        _Lacunarity2("Lacunarity 2", Range(1, 2)) = 1.5
        _Persistence2("Persistance 2", Range(0, 1)) = 0.7
        _LayerStrength2("Layer Strength 2", Range(0, 1)) = 1
        _HeightExponent2("Height Exponent 2", Range(0, 5)) = 1
        _RidgedNoise2("Ridged Noise 2", Int) = 0
        _HeightRange2("Height Range 2", Range(0, 1)) = 0.35

        //_Color ("Color", Color) = (1,1,1,1)
        _MainTex("Heightmap", 2D) = "white" {}
        _IsHeightmapSet("Is Heightmap Set", Int) = 0
        _IsEroded("Is Eroded Heightmap", Int) = 0
        _MainMap("Main Map", 2D) = "white" {}
        _IsMainmapSet("Is Main Map Set", Int) = 0
        _LandMask("Land Mask", 2D) = "white" {}
        _IsLandmaskSet("Is Land Mask Set", Int) = 0
        _Glossiness("Smoothness", Range(0,1)) = 0.7
        _Metallic("Metallicity", Range(0,1)) = 0.4
        _LandGlossiness("Land Smoothness", Range(0,1)) = 0.25
        _LandMetallic("Land Metallicity", Range(0,1)) = 0.1

        _FlowTex("FlowMap", 2D) = "black" {}
        _IsFlowTexSet("Is Flow Texture Set", Int) = 0

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
        _NormalScale("Normal Scale Multiplier", Float) = 50
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

        sampler2D_float _MainTex;
        int _IsHeightmapSet;
        int _IsEroded;
        sampler2D _MainMap;
        int _IsMainmapSet;
        sampler2D _LandMask;
        int _IsLandmaskSet;
        sampler2D _FlowTex;
        int _IsFlowTexSet;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal; INTERNAL_DATA
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        half _LandGlossiness;
        half _LandMetallic;
        float _Multiplier;
        int _Seed;
        int _TemperatureSeed;
        int _HumiditySeed;
        int _Octaves;
        float _Lacunarity;
        float _Persistence;
        float _LayerStrength;
        float _HeightExponent;
        int _Seed2;
        float _XOffset2;
        float _YOffset2;
        float _ZOffset2;
        float _Multiplier2;
        int _Octaves2;
        float _Lacunarity2;
        float _Persistence2;
        float _LayerStrength2;
        float _HeightExponent2;
        int _RidgedNoise2;
        float _HeightRange2;
        //fixed4 _Color;

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
        float _HeightRange;

        float _DrawType;
        float _XOffset;
        float _YOffset;
        float _ZOffset;
        int _RidgedNoise;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float height = 0;
            float3 offset = float3(_XOffset, _YOffset, _ZOffset);
            float3 offset2 = float3(_XOffset2, _YOffset2, _ZOffset2);

            if (_IsHeightmapSet > 0 || _IsEroded > 0)
            {
                //float4 c = pow(tex2D(_MainTex, IN.uv_MainTex), 1/2.2);
                float4 c = tex2D(_MainTex, IN.uv_MainTex);
                height = (c.r + c.g + c.b) / 3;
            }
            else
            {
                height = sphereHeight(IN.uv_MainTex, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _HeightRange, _RidgedNoise, _HeightExponent, _LayerStrength,
                                                    offset2, _Seed2, _Multiplier2, _Octaves2, _Lacunarity2, _Persistence2, _HeightRange2, _RidgedNoise2, _HeightExponent2, _LayerStrength2);
            }

            bool isAboveWater = false;
            if (_IsLandmaskSet > 0)
            {
                //float4 c = pow(tex2D(_LandMask, IN.uv_MainTex), 1/2.2);
                float4 c = tex2D(_LandMask, IN.uv_MainTex);
                isAboveWater = (c.r + c.g + c.b) / 3 < 0.5;
            }
            else
            {
                isAboveWater = height > _WaterLevel;
            }

            if (_DrawType == 1) // Drawing a Heightmap.
            {
                o.Albedo = float4(height, height, height, 1);
                o.Metallic = 0;
                o.Smoothness = 0;
            }
            else if (_DrawType == 2) // Drawing a Landmask.
            {
                if (isAboveWater)
                {
                    o.Albedo = float3(1, 1, 1);
                }
                else
                {
                    o.Albedo = float3(0, 0, 0);
                }

                o.Metallic = 0;
                o.Smoothness = 0;
            }
            else
            {
                // Temperature
                float temperature = sphereNoise(IN.uv_MainTex, offset, _TemperatureSeed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _HeightRange, 0);

                temperature *= 20; // From 0 to 20

                if (isAboveWater)
                {
                    // From -35 to 10
                    float elevationRatio = ((height - _WaterLevel) / (1 - _WaterLevel));
                    float temperatureDrop = (elevationRatio * 5) * 7; //World has 5km height, temperature falls 7 degrees for each km.
                    temperature -= temperatureDrop;
                }
                else
                {
                    temperature -= 5;
                }

                float actualLatitude = (abs(IN.uv_MainTex.y - 0.5) * 2);
                float latitudeTemperature = 1 - actualLatitude;
                //latitudeTemperature = pow(latitudeTemperature, 0.5f);
                latitudeTemperature *= 40; // From 0 to 10
                latitudeTemperature -= 15; // From -15 to 25
                temperature += latitudeTemperature; // -15 to 45

                if (_DrawType == 3) // Drawing a Temperature mask.
                {
                    o.Metallic = 0;
                    o.Smoothness = 0;
                    o.Albedo = temperatureColor(temperature);
                }
                else
                {
                    float2 prevLongitude = float2(IN.uv_MainTex.x - LONGITUDE_STEP, IN.uv_MainTex.y);
                    float2 prevLatitude = float2(IN.uv_MainTex.x, IN.uv_MainTex.y - LATITUDE_STEP);
                    if (prevLongitude.x < 0)
                        prevLongitude.x += 1;
                    if (prevLatitude.y < 0)
                        prevLatitude.y = 0;

                    float prevLongitudeHeight = 0;
                    float prevLatitudeHeight = 0;

                    if (_IsHeightmapSet > 0 || _IsEroded > 0)
                    {
                        //float4 prevLongitudeColor = pow(tex2D(_MainTex, prevLongitude), 1/2.2);
                        float4 prevLongitudeColor = tex2D(_MainTex, prevLongitude);
                        prevLongitudeHeight = (prevLongitudeColor.r + prevLongitudeColor.g + prevLongitudeColor.b) / 3;
                        //float4 prevLatitudeColor = pow(tex2D(_MainTex, prevLatitude), 1/2.2);
                        float4 prevLatitudeColor = tex2D(_MainTex, prevLatitude);
                        prevLatitudeHeight = (prevLatitudeColor.r + prevLatitudeColor.g + prevLatitudeColor.b) / 3;
                    }
                    else
                    {
                        prevLongitudeHeight = sphereHeight(prevLongitude, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _HeightRange, _RidgedNoise > 0, _HeightExponent, _LayerStrength, offset2, _Seed2, _Multiplier2, _Octaves2, _Lacunarity2, _Persistence2, _HeightRange2, _RidgedNoise2 > 0, _HeightExponent2, _LayerStrength2);
                        prevLatitudeHeight = sphereHeight(prevLatitude, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _HeightRange, _RidgedNoise > 0, _HeightExponent, _LayerStrength, offset2, _Seed2, _Multiplier2, _Octaves2, _Lacunarity2, _Persistence2, _HeightRange2, _RidgedNoise2 > 0, _HeightExponent2, _LayerStrength2);
                    }

                    if (_DrawType == 4) // Drawing a Normal mask.
                    {
                        o.Metallic = 0;
                        o.Smoothness = 0;

                        if (!isAboveWater)
                        {
                            float4 normalColor = float4(0.5, 0.5, 1, 0.5);

                            o.Albedo = normalColor;
                        }
                        else
                        {
                            float4 normalColor = float4(
                                (prevLongitudeHeight - height) * _NormalScale + 0.5,
                                (prevLatitudeHeight - height) * _NormalScale + 0.5,
                                1,
                                (prevLongitudeHeight - height) * _NormalScale + 0.5);

                            o.Albedo = normalColor;
                        }
                    }
                    else // Drawing the main map.
                    {
                        // Humidity
                        float humidity = sphereNoise(IN.uv_MainTex, offset, _HumiditySeed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _HeightRange, 0);

                        // Get Land Color
                        float4 color = float4(0, 0, 0, 1);
                        int isflowColor = 0;
                        float4 flowColor = float4(0, 0, 0, 0);

                        if (_IsFlowTexSet)
                        {
                            flowColor = tex2D(_FlowTex, IN.uv_MainTex);
                            if (flowColor.r != 0 || flowColor.g != 0 || flowColor.b != 0)
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
                                color = tex2D(_MainMap, IN.uv_MainTex);
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

                                if (flowColor.a > 0)
                                {
                                    color = color * (1 - flowColor.a) + flowColor * flowColor.a;
                                    color.a = 1;
                                }
                            }
                        }
                        o.Albedo = color;

                        float3 normal = float3(0, 0, 1);
                        if (!isAboveWater)
                        {
                            o.Metallic = _Metallic;
                            o.Smoothness = _Glossiness;
                        }
                        else
                        {
                            o.Metallic = _LandMetallic;
                            o.Smoothness = _LandGlossiness;
                            normal = float3(
                                (prevLongitudeHeight - height) * _NormalScale + IN.worldNormal.x,
                                (prevLatitudeHeight - height) * _NormalScale + IN.worldNormal.y,
                                1);
                        }
                        normal = normalize(normal);
                        o.Normal = normal;
                    }
                }
            }
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
