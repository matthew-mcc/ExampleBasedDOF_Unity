Shader "Unlit/URPDoF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        // LOD 100

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.h"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            sampler2D _MainTex;
            float _Intensity;
            float4 _OverlayColor;

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v){
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target{
                float4 col = tex2D(_MainTex, i.uv) * _OverlayColor;
                col.rgb *= _Intensity;
                return col;
            }
            ENDHLSL
        }
    }
}

// Shader "Unlit/URPDoF"
// {
    // Properties
    // {
        // _MainTex ("Texture", 2D) = "white" {}
        // _Intensity ("Intensity", Range(0, 2)) = 1
        // _OverlayColor ("Overlay Color", Color) = (1, 1, 1, 1)
    // }

    // SubShader
    // {
        // Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // Pass
        // {
            // HLSLPROGRAM

            // #pragma vertex vert
            // #pragma fragment frag

            // // Core URP helpers (gives you TransformObjectToHClip, _ScreenParams, etc.)
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // // Use URP texture macros instead of raw sampler2D/tex2D
            // TEXTURE2D(_MainTex);
            // SAMPLER(sampler_MainTex);

            // float _Intensity;
            // float4 _OverlayColor;

            // struct appdata
            // {
                // float4 vertex : POSITION;
                // float2 uv     : TEXCOORD0;
            // };

            // struct v2f
            // {
                // float4 vertex : SV_POSITION;
                // float2 uv     : TEXCOORD0;
            // }; // <- needed semicolon

            // v2f vert(appdata v)
            // {
                // v2f o;
                // o.vertex = TransformObjectToHClip(v.vertex.xyz);
                // o.uv = v.uv;
                // return o;
            // }

            // float4 frag(v2f i) : SV_Target
            // {
                // // Sample the main texture using URP macros
                // float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // // Apply overlay color and intensity
                // col *= _OverlayColor;
                // col.rgb *= _Intensity;

                // return col;
            // }

            // ENDHLSL
        // }
    // }
// }
