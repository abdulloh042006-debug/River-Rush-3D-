Shader "RiverRush/WaterDropletURP"
{
    Properties
    {
        _BaseTint ("Water Tint", Color) = (0.15, 0.74, 0.96, 1)
        _Opacity ("Transparency", Range(0.25, 1)) = 0.77
        _RimStrength ("Glass Rim", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "WaterDroplet"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseTint;
                half _Opacity;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 view = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                half facing = saturate(dot(n, view));
                half fresnel = pow(1.0h - facing, 3.0h) * _RimStrength;
                // Stylized highlights without reflections/cubemaps, for mobile GPUs.
                half shine = pow(saturate(dot(n, normalize(view + half3(0.28h, 0.82h, 0.48h)))), 28.0h);
                half3 rgb = _BaseTint.rgb * (0.72h + 0.26h * facing);
                rgb = lerp(rgb, half3(0.84h, 0.97h, 1.0h), saturate(fresnel + shine * 0.4h));
                half alpha = saturate(_Opacity + fresnel * 0.18h);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
