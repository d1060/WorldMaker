Shader "Noise/Clouds Simplex Noise"
{
    Properties
    {
		_Color("Color", Color) = (1, 1, 1)
		_WaterLevel("Water Level", Range(0, 1)) = 0.66

        _Seed("Land Seed", Float) = 2343
		_Speed("Speed", Range(0,3)) = 1

        _XOffset("X Offset", Range(0, 1)) = 0
        _YOffset("Y Offset", Range(0, 1)) = 0
        _ZOffset("Z Offset", Range(0, 1)) = 0
		_WOffset("Z Offset", Range(0, 1)) = 0
		_MinHeight("Min Height", Range(0, 1)) = 0.15
        _MaxHeight("Max Height", Range(0, 1)) = 0.75
        _Multiplier("Noise Multiplier", Range(0.5, 10)) = 1
        _Octaves("Number of Octaves", Range(1, 30)) = 10
        _Lacunarity("Lacunarity", Range(1, 2)) = 1.5
        _Persistence("Persistance", Range(0, 1)) = 0.7
        _LayerStrength("Layer Strength", Range(0, 1)) = 1
        _HeightExponent("Height Exponent", Range(0, 10)) = 1
        _RidgedNoise("Ridged Noise", Int) = 0
        _DomainWarping("Domain Warping", Range(0, 4)) = 0

        _HeightMap("Heightmap", 2D) = "white" {}
        _Glossiness("Land Smoothness", Range(0,1)) = 0.25
        _Metallic("Land Metallicity", Range(0,1)) = 0.1

        _NormalScale("Normal Scale Multiplier", Range(0, 50)) = 50
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

            CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha
        #include "Assets/Shaders/Simplex4d.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #define LONGITUDE_STEP 0.000244140f
        #define LATITUDE_STEP 0.000488281f
        #define PI 3.14159265359
        #define TWO_PI 6.28318530718

        sampler2D _HeightMap;

        struct Input
        {
            float2 uv_HeightMap;
            float3 worldNormal; INTERNAL_DATA
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        float _Multiplier;
        float _Seed;
        float _HumiditySeed;
        int _Octaves;
        float _Lacunarity;
        float _Persistence;
        float _LayerStrength;
        float _HeightExponent;
        float _MinHeight;
        float _MaxHeight;
		float3 _Color;

        float _WaterLevel;
		float _Speed;

        float _NormalScale;
        float _UnderwaterNormalScale;

        float _XOffset;
        float _YOffset;
        float _ZOffset;
		float _WOffset;
        int _RidgedNoise;
        float _DomainWarping;

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

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_HeightMap;
            if (uv.x > 2) uv.x -= 2;
            else if (uv.x > 1) uv.x -= 1;

			float w = _Time.x * _Speed;

            float height = 0;
            float4 offset = float4(_XOffset, _YOffset, _ZOffset, _WOffset);

            height = sphereHeight(uv, w, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _RidgedNoise, _HeightExponent, _DomainWarping, _MinHeight, _MaxHeight);

			if (height <= _MinHeight)
				height = 0;
			else if (height >= _MaxHeight)
				height = 1;
			else if (height > _MinHeight && height < _MaxHeight)
				height = (height - _MinHeight) / (_MaxHeight - _MinHeight);

            float2 prevLongitude = float2(uv.x - LONGITUDE_STEP, uv.y);
            float2 prevLatitude = float2(uv.x, uv.y - LATITUDE_STEP);

            if (prevLongitude.x < 0) prevLongitude.x += 1;
            if (prevLatitude.y < 0)  prevLatitude.y = 0;

            float prevLongitudeHeight = 0;
            float prevLatitudeHeight = 0;

            prevLongitudeHeight = sphereHeight(prevLongitude, w, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _RidgedNoise > 0, _HeightExponent, _DomainWarping, _MinHeight, _MaxHeight);
            prevLatitudeHeight = sphereHeight(prevLatitude, w, offset, _Seed, _Multiplier, _Octaves, _Lacunarity, _Persistence, _RidgedNoise > 0, _HeightExponent, _DomainWarping, _MinHeight, _MaxHeight);

            float verticalDeltaHeight = prevLatitudeHeight - height;
            float horizontalDeltaHeight = prevLongitudeHeight - height;

            // Get Land Color
            float4 color = float4(_Color.xyz, height);

            float3 normal;

			float z = sqrt(1 - pow(horizontalDeltaHeight * _NormalScale, 2) - pow(verticalDeltaHeight * _NormalScale, 2));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            normal = float3(
                horizontalDeltaHeight * _NormalScale,
                verticalDeltaHeight * _NormalScale,
                z);

            o.Albedo = color;
            o.Normal = normal;

			o.Alpha = height;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
