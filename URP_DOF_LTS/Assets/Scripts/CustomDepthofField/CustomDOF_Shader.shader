Shader "Hidden/Custom/CustomDOF_Bokeh" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex, _CameraDepthTexture;
		float4 _MainTex_TexelSize;
        float _FocusDistance, _FocusRange;

		struct VertexData {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Interpolators {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		Interpolators VertexProgram (VertexData v) {
			Interpolators i;
			i.pos = UnityObjectToClipPos(v.vertex);
			i.uv = v.uv;
			return i;
		}

	ENDCG

	SubShader {
		Cull Off
		ZTest Always
		ZWrite Off

		Pass {
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				half4 FragmentProgram (Interpolators i) : SV_Target {
                    half depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
					depth = LinearEyeDepth(depth);
                    float coc = (depth - _FocusDistance) / _FocusRange;
					return coc;
				}
			ENDCG
		}
	}
}



// Shader "Hidden/Custom/CustomDOF_Bokeh"
// {
    // Properties
    // {
        // _MainTex ("Source", 2D) = "white" {}
    // }

    // SubShader
    // {
        // Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        // Pass
        // {
            // Name "CustomDOF_Bokeh"
            // ZTest Always ZWrite Off Cull Off

            // HLSLPROGRAM

            // #pragma vertex vert
            // #pragma fragment frag

            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // // Color texture
            // TEXTURE2D(_MainTex);
            // SAMPLER(sampler_MainTex);

            // // Camera depth texture (need setting enabled in URP don't forget that sir)
            // TEXTURE2D_X(_CameraDepthTexture);
            // SAMPLER(sampler_CameraDepthTexture);

            // float _FocusDistance;
            // float _Aperture;
            // float _FocalLength;

            // // TODO: these are not used, but kept for completeness (probably should use them?)
            // int   _BladeCount;
            // float _BladeCurvature;
            // float _BladeRotation;

            // struct Attributes
            // {
                // float4 positionOS : POSITION;
                // float2 uv         : TEXCOORD0;
            // };

            // struct Varyings
            // {
                // float4 positionHCS : SV_POSITION;
                // float2 uv          : TEXCOORD0;
            // };

            // Varyings vert (Attributes v)
            // {
                // Varyings o;
                // o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                // o.uv = v.uv;
                // return o;
            // }

            // // Helper: sample scene depth and convert to linear eye depth
            // float GetLinearEyeDepth(float2 uv)
            // {
                // float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                // return LinearEyeDepth(raw, _ZBufferParams);
            // }

            // float ComputeCoC(float eyeDepth)
            // {
                // // Very simple CoC model: proportional to distance from focus plane
                // // TODO: replace this with a more physically-accurate model
                // float focus = _FocusDistance;
                // float coc = abs(eyeDepth - focus) * _Aperture * 0.01;   // arbitrary scale
                // // Clamp to prevent insane radii
                // return saturate(coc);
            // }

            // // TODO: REPLACE IMMEDIATLY!
            // // Hardcoded small Poisson disk as a placeholder for future custom sampling
            // static const int SAMPLE_COUNT = 8;
            // static const float2 SAMPLE_OFFSETS[SAMPLE_COUNT] = {
                // float2( 0.0,  0.0),
                // float2( 0.4,  0.1),
                // float2(-0.3,  0.3),
                // float2( 0.2, -0.4),
                // float2(-0.4, -0.2),
                // float2( 0.1,  0.5),
                // float2( 0.5, -0.1),
                // float2(-0.5,  0.0)
            // };

            // float4 frag (Varyings i) : SV_Target
            // {
                // // return float4(1, 0, 0, 1);
                // float2 uv = i.uv;

                // // Depth & CoC
                // float eyeDepth = GetLinearEyeDepth(uv);
                // float coc      = ComputeCoC(eyeDepth);

                // // Convert CoC (0–1) to a radius in pixels
                // float maxRadiusPixels = 12.0; // should probably expose as a parm? tweakable I think...
                // float radiusPixels = coc * maxRadiusPixels;

                // // If in focus, just return original color
                // if (radiusPixels < 0.5)
                // {
                    // return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                // }

                // float2 texelSize = 1.0 / _ScreenParams.xy;

                // float3 accum = 0.0;
                // float  weight = 0.0;

                // // Simple gather blur over small disk using our sample offsets
                // [unroll]
                // for (int s = 0; s < SAMPLE_COUNT; s++)
                // {
                    // float2 dir = SAMPLE_OFFSETS[s];
                    // float2 sampleUv = uv + dir * radiusPixels * texelSize;

                    // float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv);

                    // // Weight: center samples more, outer samples slightly less
                    // float w = 1.0; // could be length-based
                    // accum += c.rgb * w;
                    // weight += w;
                // }

                // float3 blurred = accum / max(weight, 1e-4);
                // return float4(blurred, 1.0);
            // }

            // ENDHLSL
        // }
    // }

    // FallBack Off
// }
