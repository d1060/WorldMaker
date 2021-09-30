Shader "Custom/Atmosphere Fresnel"
{
    Properties
    {
        _AtmosphericColor("Atmospheric Color", Color) = (1,1,1,1)
        _AtmosphericFalloff("Atmospheric Falloff", Range(0,5)) = 0.5
        _GlowPower("Glow Power", Range(0,50)) = 0.5
        _GlowIntensity("Glow Intensity", Range(0,10)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 worldPos;
        };

        float4 _AtmosphericColor;
        half _AtmosphericFalloff;
        half _GlowPower;
        half _Glossiness;
        half _Metallic;
        half _GlowIntensity;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 cameraVector = _WorldSpaceCameraPos - IN.worldPos;
            cameraVector = normalize(cameraVector);
            float normalDotCamera = dot(cameraVector, IN.worldNormal);

            float4 atmosphereColor = _AtmosphericColor;
            atmosphereColor.a = 1;

            // Fade In Interior:
            float inverseDot = 1 - normalDotCamera;
            inverseDot = clamp(inverseDot, 0, 1);
            float falloff = pow(inverseDot, _AtmosphericFalloff);
            atmosphereColor.a *= falloff;

            //Fade Out Exterior:
            float exteriorDot = normalDotCamera;
            exteriorDot *= _GlowIntensity;
            exteriorDot = clamp(exteriorDot, 0, 1);
            exteriorDot = pow(exteriorDot, _GlowPower);
            falloff *= exteriorDot;
            atmosphereColor.a *= exteriorDot;

            o.Albedo = atmosphereColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = falloff;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
