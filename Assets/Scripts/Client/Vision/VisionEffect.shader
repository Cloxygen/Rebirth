Shader "Rebirth/VisionEffect"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _UseVertexColor("Tint By Vertex Colour", Float) = 0
        [HideInInspector] _VisionFade("Vision Fade", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Scripts/Client/Vision/VisionDither.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _UseVertexColor;
                float  _VisionFade;
            CBUFFER_END

            float4 _VisionCenter;
            float4 _VisionParams;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 color      : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 vertexTint = lerp(float4(1.0, 1.0, 1.0, 1.0), input.color, _UseVertexColor);
                float4 color = vertexTint * _BaseColor;

                float hidden = _VisionFade;

                if (_VisionParams.z >= 0.5)
                {
                    float fromCentre = distance(input.positionWS.xz, _VisionCenter.xz);
                    hidden = max(hidden, smoothstep(_VisionParams.x, _VisionParams.y, fromCentre));
                }

                hidden = hidden * (1.0 - saturate(_VisionEditorBypass));
                color.a = color.a * (1.0 - hidden);

                clip(color.a - 0.0001);

                return half4(color);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
