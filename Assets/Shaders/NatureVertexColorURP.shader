Shader "RiverRush/NatureVertexColorURP"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Ambient ("Ambient Brightness", Range(0.2, 1.0)) = 0.58
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "NatureLowPoly"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half lighting : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                half3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                half3 lightDirection = normalize(half3(0.32h, 0.82h, 0.48h));
                o.lighting = lerp(_Ambient, 1.0h,
                    saturate(dot(normalWS, lightDirection)));
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb * _BaseColor.rgb * input.lighting, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
