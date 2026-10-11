Shader "Rebirth/Grass"
{
    Properties
    {
        [Header(Appearance)]
        _RootColor ("Root colour", Color) = (0.09, 0.13, 0.05, 1)
        _TipColor ("Tip colour", Color) = (0.44, 0.48, 0.21, 1)
        _TintVariation ("Tint variation", Range(0, 1)) = 0.4

        [Header(Lighting)]
        _AmbientStrength ("Ambient strength (sky/GI fill, 0 = black shadows)", Range(0, 2)) = 0.85
        _NormalUpBias ("Normal up bias (softens lighting toward upright)", Range(0, 1)) = 0.5

        [Header(Shape)]
        _LeanAngleRange ("Lean angle range, min/max degrees (Y, Z unused)", Vector) = (14, 30, 0, 0)

        [Header(Wind)]
        _WindDirection ("Wind direction (XZ)", Vector) = (1, 0, 0.35, 0)
        _WindStrength ("Wind strength", Range(0, 1)) = 0.11
        _WindFrequency ("Wind frequency", Range(0, 2)) = 0.35
        _WindSpeed ("Wind speed", Range(0, 8)) = 1.15
        _WindSwayRange ("Wind sway range (min, max per blade)", Vector) = (0.7, 1.3, 0, 0)
        _WindWander ("Wind wavefront wander", Range(0, 3)) = 1.4

        [Header(Interaction)]
        _BendStrength ("Bend strength", Range(0, 2)) = 0.9

        [Header(Growth)]
        _BladeHeightRange ("Blade height range (min, max)", Vector) = (0.25, 0.5, 0, 0)
        _BladeWidthRange ("Blade width range (min, max)", Vector) = (0.09, 0.16, 0, 0)
        _PatchSizeFalloff ("Patch size falloff (short and thin blades at patch edges)", Range(0, 1)) = 0.6
        _Density ("Density", Range(0, 1)) = 1.0
        _CellSize ("Cell size", Range(0.01, 1)) = 0.15
        _FalloffStartDistance ("Falloff start distance", Range(0, 200)) = 20
        _DrawDistance ("Max draw distance", Range(1, 200)) = 40
        _BladeSegments ("Blade segments", Range(1, 6)) = 3

        [Header(Performance)]
        [Enum(Off, 0, Back, 2)] _Cull ("Face culling (Back is cheaper)", Float) = 2
        [Toggle(_GRASS_SOFT_SHADOWS)] _SoftShadows ("Soft main-light shadows", Float) = 0
        [Toggle(_GRASS_ADDITIONAL_LIGHT_SHADOWS)] _AdditionalLightShadows ("Point/spot light shadows", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma shader_feature_local_fragment _ _GRASS_SOFT_SHADOWS
            #pragma shader_feature_local_fragment _ _GRASS_ADDITIONAL_LIGHT_SHADOWS
            #if defined(_GRASS_SOFT_SHADOWS)
                #define _SHADOWS_SOFT
            #endif
            #if defined(_GRASS_ADDITIONAL_LIGHT_SHADOWS)
                #define _ADDITIONAL_LIGHT_SHADOWS
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct GrassBlade
            {
                float3 positionWS;
                float  rotation;
                float2 size;
                float2 hash;
            };

            StructuredBuffer<GrassBlade> _Blades;

            TEXTURE2D(_BendMap);
            SAMPLER(sampler_BendMap);
            float4 _BendMapOrigin;
            float  _BendMapWorldSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _RootColor;
                float4 _TipColor;
                float  _NormalUpBias;
                float  _TintVariation;
                float  _AmbientStrength;
                float4 _WindDirection;
                float  _WindStrength;
                float  _WindFrequency;
                float  _WindSpeed;
                float4 _WindSwayRange;
                float  _WindWander;
                float  _BendStrength;
                float4 _LeanAngleRange;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  tint       : TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                float3 direct     : TEXCOORD5;
                float3 ambient    : TEXCOORD6;
            };

            float LeanAngleForBlade(float2 hash)
            {
                float magnitudeHash = frac(hash.x * 12.9898 + hash.y * 78.233 + 0.37);
                float signHash = frac(hash.x * 39.3468 + hash.y * 11.135 + 0.91);

                float magnitude = radians(lerp(_LeanAngleRange.x, _LeanAngleRange.y, magnitudeHash));
                float direction = signHash < 0.5 ? -1.0 : 1.0;

                return magnitude * direction;
            }

            float SampleAmbientOcclusion(float4 positionCS)
            {
            #if defined(_SCREEN_SPACE_OCCLUSION)
                float2 screenUV = positionCS.xy / _ScaledScreenParams.xy;
                return SAMPLE_TEXTURE2D_X(_ScreenSpaceOcclusionTexture, sampler_ScreenSpaceOcclusionTexture, screenUV).r;
            #else
                return 1.0;
            #endif
            }

            float3 ApplyWind(float3 positionWS, float3 rootWS, float heightFraction, float stiffnessHash)
            {
                float2 direction = normalize(_WindDirection.xz + float2(1e-5, 1e-5));
                float2 acrossDirection = float2(-direction.y, direction.x);

                float alongWind = dot(rootWS.xz, direction) * _WindFrequency;
                float acrossWind = dot(rootWS.xz, acrossDirection) * _WindFrequency;

                float wander = sin(acrossWind * 0.7 + _Time.y * _WindSpeed * 0.25)
                             + sin(acrossWind * 1.9 - _Time.y * _WindSpeed * 0.4) * 0.5;

                float travel = alongWind - _Time.y * _WindSpeed + wander * _WindWander;

                float gust = sin(travel);
                float flutter = sin(travel * 2.7 + 1.3) * 0.35;
                float sway = gust + flutter;

                float stiffness = lerp(_WindSwayRange.x, _WindSwayRange.y, stiffnessHash);
                float bend = sway * _WindStrength * stiffness * heightFraction * heightFraction;

                positionWS.xz += direction * bend;
                return positionWS;
            }

            float3 ApplyBend(float3 positionWS, float3 rootWS, float heightFraction)
            {
                float2 bendMapUV = (rootWS.xz - _BendMapOrigin.xy) / _BendMapWorldSize;
                float2 push = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, bendMapUV, 0).xy;

                positionWS.xz += push * _BendStrength * heightFraction * heightFraction;
                return positionWS;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                GrassBlade blade = _Blades[IN.instanceID];

                float s, c;
                sincos(blade.rotation, s, c);

                float leanAngle = LeanAngleForBlade(blade.hash);
                float sl, cl;
                sincos(leanAngle, sl, cl);

                float3 scaled = float3(IN.positionOS.x * blade.size.x,
                                       IN.positionOS.y * blade.size.y,
                                       IN.positionOS.z * blade.size.x);

                float3 leaned = float3(scaled.x,
                                       scaled.y * cl - scaled.z * sl,
                                       scaled.y * sl + scaled.z * cl);

                float3 rotated = float3(leaned.x * c - leaned.z * s,
                                        leaned.y,
                                        leaned.x * s + leaned.z * c);

                float3 leanedNormal = float3(IN.normalOS.x,
                                             IN.normalOS.y * cl - IN.normalOS.z * sl,
                                             IN.normalOS.y * sl + IN.normalOS.z * cl);

                float3 positionWS = blade.positionWS + rotated;
                positionWS = ApplyWind(positionWS, blade.positionWS, IN.uv.y, blade.hash.y);
                positionWS = ApplyBend(positionWS, blade.positionWS, IN.uv.y);

                float3 normalWS = float3(leanedNormal.x * c - leanedNormal.z * s,
                                         leanedNormal.y,
                                         leanedNormal.x * s + leanedNormal.z * c);

                normalWS = normalize(lerp(normalWS, float3(0.0, 1.0, 0.0), _NormalUpBias));

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndotl = dot(normalWS, mainLight.direction) * 0.5 + 0.5;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;
                OUT.direct = mainLight.color * ndotl * mainLight.shadowAttenuation;
                OUT.ambient = SampleSH(normalWS) * _AmbientStrength;
                OUT.uv = IN.uv;
                OUT.tint = blade.hash.x;
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            float3 WrapDiffuse(Light light, float3 normalWS)
            {
                float ndotl = dot(normalWS, light.direction) * 0.5 + 0.5;
                return light.color * ndotl
                     * (light.distanceAttenuation * light.shadowAttenuation);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 albedo = lerp(_RootColor.rgb, _TipColor.rgb, IN.uv.y);
                albedo *= lerp(1.0 - _TintVariation, 1.0 + _TintVariation, IN.tint);

                float ao = SampleAmbientOcclusion(IN.positionCS);

                float3 lighting = IN.direct;

            #if defined(_ADDITIONAL_LIGHTS)
                float3 normalWS = normalize(IN.normalWS);
                uint additionalLightCount = GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                LIGHT_LOOP_BEGIN(additionalLightCount)
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    lighting += WrapDiffuse(light, normalWS);
                LIGHT_LOOP_END
            #endif

                float3 colour = albedo * (lighting + IN.ambient * ao);
                colour = MixFog(colour, IN.fogCoord);

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
