Shader "Rebirth/VisionFog"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Overlay"
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "VisionFog"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float4 _VisionCenter;
            float4 _VisionParams;
            float4 _VisionFogParams;
            float4 _VisionFogNoise;
            float4 _VisionColor;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i),                b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            static const float2x2 OCT_ROT = float2x2(0.80, -0.60, 0.60, 0.80);

            float fbm3(float2 p)
            {
                float v = 0.0, amp = 0.5, norm = 0.0;
                for (int i = 0; i < 3; i++)
                {
                    v += amp * vnoise(p);
                    norm += amp;
                    p = mul(OCT_ROT, p) * 2.03; amp *= 0.5;
                }
                return v / norm;
            }

            float2 fogWarp(float2 p)
            {
                return float2(fbm3(p), fbm3(p + float2(5.2, 1.3))) - 0.5;
            }

            float3 SampleSceneBlurred(float2 uv, float radiusPixels)
            {
                float3 c = SampleSceneColor(uv);
                if (radiusPixels < 0.01) return c;

                float2 r = _CameraOpaqueTexture_TexelSize.xy * radiusPixels;
                const float k = 0.7071;

                c += SampleSceneColor(uv + float2( r.x,  0.0));
                c += SampleSceneColor(uv + float2(-r.x,  0.0));
                c += SampleSceneColor(uv + float2( 0.0,  r.y));
                c += SampleSceneColor(uv + float2( 0.0, -r.y));
                c += SampleSceneColor(uv + float2( r.x * k,  r.y * k));
                c += SampleSceneColor(uv + float2(-r.x * k,  r.y * k));
                c += SampleSceneColor(uv + float2( r.x * k, -r.y * k));
                c += SampleSceneColor(uv + float2(-r.x * k, -r.y * k));
                return c / 9.0;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = float4(input.positionOS.xy * 2.0, UNITY_NEAR_CLIP_VALUE, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_VisionParams.z < 0.5) discard;

                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);

                float rawDepth = SampleSceneDepth(uv);
                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);

                float distanceFromCentre = distance(worldPosition.xz, _VisionCenter.xz);

                float turbulence = _VisionFogNoise.x;
                if (turbulence > 1e-4)
                {
                    float2 p = worldPosition.xz * _VisionFogNoise.z;
                    float time = _Time.y * _VisionFogNoise.y;

                    p += float2(0.06, -0.05) * time;
                    p += 0.45 * fogWarp(p * 0.55 + time * float2(-0.04, -0.05));

                    distanceFromCentre += (fbm3(p) - 0.5) * 2.0 * turbulence;
                }

                float fog = smoothstep(_VisionParams.x, _VisionParams.y, distanceFromCentre);

                float hasScene = _VisionFogParams.z;
                float drain = _VisionFogParams.y * hasScene;
                float retention = _VisionFogParams.w * hasScene;
                float blur = _VisionFogNoise.w * hasScene;

                float3 target = _VisionColor.rgb;
                if (drain > 0.0 || retention > 0.0 || blur > 0.0)
                {
                    float3 scene = SampleSceneBlurred(uv, blur * fog);

                    float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
                    float3 drained = lerp(scene, luma.xxx, drain);

                    target = _VisionColor.rgb * lerp(1.0, drained * 2.0, retention);
                }

                return half4(target, fog * _VisionFogParams.x);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
