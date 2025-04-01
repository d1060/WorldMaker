Shader "Custom/Cloud Sampler"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Cloud Texture 1", 2D) = "white" {}
		_MainNormal("Cloud Normal 1", 2D) = "white" {}
		_Speed1("Speed 1", Range(0,3)) = 1
		_MainTex2("Cloud Texture 2", 2D) = "white" {}
		_MainNormal2("Cloud Normal 2", 2D) = "white" {}
		_Speed2("Speed 2", Range(0,3)) = 1
		_MainTex3("Cloud Texture 3", 2D) = "white" {}
		_MainNormal3("Cloud Normal 3", 2D) = "white" {}
		_Speed3("Speed 3", Range(0,3)) = 1

		_Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_NormalScale("Normal Scale", Range(-50,50)) = 0.0
		_CloudExponent("Clouds Exponent", Range(0,2)) = 0.25
		_Opacity("Opacity", Range(0,1)) = 1
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha
		#include "Assets/Shaders/Simplex.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		#define LONGITUDE_STEP 0.000244140f
		#define LATITUDE_STEP 0.000488281f

        sampler2D _MainTex;
		sampler2D _MainTex2;
		sampler2D _MainTex3;
		sampler2D _MainNormal;
		sampler2D _MainNormal2;
		sampler2D _MainNormal3;

        struct Input
        {
            float2 uv_MainTex;
        };

		float _Speed1;
		float _Speed2;
		float _Speed3;
		float _DisplacementAmount;

		float _NormalScale;
		float _CloudExponent;
		float _Opacity;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

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

		float2 fixUV(float2 uv)
		{
			if (uv.y > 1)
				uv.y = 2 - uv.y;
			if (uv.y < 0)
				uv.y = -uv.y;

			if (uv.x > 1)
				uv.x = frac(uv.x);
			if (uv.x < 0)
				uv.x = 1-frac(uv.x);

			return uv;
		}

		float4 getColor(float2 uv1, float2 uv2, float2 uv3)
		{
			float4 c1 = tex2D(_MainTex, fixUV(uv1));
			float4 c2 = tex2D(_MainTex2, fixUV(uv2));
			float4 c3 = tex2D(_MainTex3, fixUV(uv3));

			float4 c = c1 * c2 * c3;
			c = pow(c, _CloudExponent);
			return c;
		}

		float4 getNormal(float2 uv1, float2 uv2, float2 uv3)
		{
			float4 n1 = tex2D(_MainNormal, fixUV(uv1));
			float4 n2 = tex2D(_MainNormal2, fixUV(uv2));
			float4 n3 = tex2D(_MainNormal3, fixUV(uv3));

			float normalR = (((n1.r) - 0.5) + ((n2.r) - 0.5) + ((n3.r) - 0.5)) * _NormalScale / 3;
			float normalG = (((n1.g) - 0.5) + ((n2.g) - 0.5) + ((n3.g) - 0.5)) * _NormalScale / 3;
			float normalB = (n1.b + n2.b + n3.b) / 3;
			float normalA = (n1.a + n2.a + n3.a) / 3;

			normalR += 0.5;
			normalG += 0.5;

			float4 n = float4(normalR, normalG, normalB, normalA);
			return n;
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float2 uv = IN.uv_MainTex;
			if (uv.x > 1) uv.x = frac(uv.x);

			float time1 = _Time.x * _Speed1;
			float time2 = _Time.x * _Speed2;
			float time3 = _Time.x * _Speed3;

			float2 uv1 = float2(uv.x + time1, uv.y);
			float2 uv2 = float2(uv.x + time2, uv.y);
			float2 uv3 = float2(uv.x + time3, uv.y);

			uv1 = fixUV(uv1);
			uv2 = fixUV(uv2);
			uv3 = fixUV(uv3);

			float4 c = getColor(uv1, uv2, uv3);
			float4 n = getNormal(uv1, uv2, uv3);

			o.Albedo = c.rgb * _Color;
			o.Alpha = c.a * _Opacity * _Color.a;
			o.Normal = n;
			o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
