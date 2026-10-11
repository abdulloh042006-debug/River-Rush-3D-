Shader "RiverRush/StylizedRiverURP"
{
    Properties
    {
        _DeepColor ("Deep Water", Color) = (0.025, 0.20, 0.39, 1)
        _ShallowColor ("Shallow Water", Color) = (0.09, 0.63, 0.77, 1)
        _FoamColor ("Foam", Color) = (0.87, 0.98, 1, 1)
        _FlowSpeed ("Flow Speed", Range(0, 8)) = 1.8
        _FoamWidth ("Shore Foam", Range(0.01, 0.6)) = 0.19
        _WaveStrength ("Vertex Wave Height", Range(0, 0.4)) = 0.04
        _Opacity ("Opacity", Range(0, 1)) = 0.93
        _Detail ("Effect Quality", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "StylizedRiver"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _FoamColor;
            float _FlowSpeed;
            float _FoamWidth;
            float _WaveStrength;
            float _Opacity;
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
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float flowTime = _Time.y * _FlowSpeed;
                pos.y += sin(input.uv.y * 2.4 - flowTime) * _WaveStrength;
                output.positionCS = TransformObjectToHClip(pos);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = _Time.y * _FlowSpeed;
                float banks = saturate(abs(uv.x - 0.5) * 2.0);
                float broadWave = sin(uv.y * 7.0 - t * 3.0 + sin(uv.x * 13.0));
                float smallWave = sin(uv.y * 16.0 - t * 4.7 + uv.x * 22.0);
                float smoothRipples = broadWave * 0.6 + smallWave * 0.4 * _Detail;
                float shoreline = smoothstep(1.0 - _FoamWidth, 1.0,
                    banks + smoothRipples * 0.075);
                float crest = smoothstep(0.76, 0.99, broadWave * 0.5 + 0.5)
                             * (0.06 + 0.16 * _Detail);
                float foamLines = smoothstep(0.91, 0.995,
                    sin(uv.y * 21.0 - t * 5.6 + uv.x * 7.0) * 0.5 + 0.5)
                    * _Detail * 0.12;
                float3 rgb = lerp(_DeepColor.rgb, _ShallowColor.rgb,
                                  saturate(0.18 + banks * 0.63 + smoothRipples * 0.07));
                rgb = lerp(rgb, _FoamColor.rgb, saturate(shoreline * 0.9 + crest + foamLines));
                return half4(rgb, _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
