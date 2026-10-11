Shader "RiverRush/StylizedRiverURP"
{
    Properties
    {
        _DeepColor ("Deep Water", Color) = (0.035, 0.25, 0.48, 1)
        _ShallowColor ("Shallow Water", Color) = (0.07, 0.68, 0.78, 1)
        _FoamColor ("Foam", Color) = (0.85, 0.98, 1.0, 1)
        _FlowSpeed ("Flow Speed", Range(0, 8)) = 1.8
        _FoamWidth ("Bank Foam Width", Range(0.02, 0.6)) = 0.18
        _WaveStrength ("Wave Strength", Range(0, 0.4)) = 0.045
        _Opacity ("Opacity", Range(0, 1)) = 0.91
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "RiverSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
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
                float3 p = input.positionOS.xyz;
                // Very small displacement to keep mobile vertex cost low.
                p.y += sin(input.uv.y * 2.4 - _Time.y * _FlowSpeed) * _WaveStrength;
                output.positionCS = TransformObjectToHClip(p);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float edge = saturate(abs(uv.x - 0.5) * 2.0);
                float wave1 = sin(uv.y * 8.0 - _Time.y * _FlowSpeed * 3.2 + sin(uv.x * 15.0));
                float wave2 = sin(uv.y * 13.0 - _Time.y * _FlowSpeed * 2.1 - uv.x * 19.0);
                float ripple = (wave1 + wave2) * 0.5;
                float foam = smoothstep(1.0 - _FoamWidth, 1.0,
                                        edge + ripple * 0.07);
                float highlights = smoothstep(0.67, 0.95, wave1 * 0.5 + 0.5) * 0.11;
                float3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, edge * 0.6);
                water += highlights;
                water = lerp(water, _FoamColor.rgb, foam * 0.88);
                return half4(water, _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
