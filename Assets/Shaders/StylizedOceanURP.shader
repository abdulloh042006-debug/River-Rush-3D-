Shader "RiverRush/StylizedOceanURP"
{
    Properties
    {
        _OceanColor ("Ocean Color", Color) = (0.015, 0.22, 0.39, 1)
        _Highlight ("Wave Highlights", Color) = (0.18, 0.65, 0.77, 1)
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 0.7
        _WaveHeight ("Wave Height", Range(0, 0.4)) = 0.035
        _Detail ("Effect Quality", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Ocean"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _OceanColor;
            float4 _Highlight;
            float _WaveSpeed;
            float _WaveHeight;
            float _Detail;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 p = input.positionOS.xyz;
                p.y += sin(p.x * 0.025 + _Time.y * _WaveSpeed) *
                       cos(p.z * 0.018 - _Time.y * _WaveSpeed) * _WaveHeight;
                o.positionCS = TransformObjectToHClip(p);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * 60.0;
                float t = _Time.y * _WaveSpeed;
                float waves = sin(uv.x * 0.78 + t) *
                              cos(uv.y * 0.90 - t * 0.85);
                float ripples = sin(uv.x * 2.4 - t * 2.2 + sin(uv.y)) * _Detail;
                float glint = smoothstep(0.62, 0.92, waves * 0.65 + ripples * 0.2);
                return half4(lerp(_OceanColor.rgb, _Highlight.rgb,
                                  glint * (0.2 + _Detail * 0.23)), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
