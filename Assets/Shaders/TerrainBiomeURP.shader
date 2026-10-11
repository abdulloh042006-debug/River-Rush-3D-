Shader "RiverRush/TerrainBiomeURP"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _SeaLevel ("Sea Height", Float) = 3.0
        _MountainHeight ("Mountain Scale", Float) = 95.0
        _GrassColor ("Meadow Grass", Color) = (0.26, 0.53, 0.26, 1)
        _ForestColor ("Forest Floor", Color) = (0.12, 0.32, 0.18, 1)
        _SandColor ("Beach Sand", Color) = (0.74, 0.69, 0.48, 1)
        _RockColor ("Rocky Mountain", Color) = (0.43, 0.46, 0.48, 1)
        _SnowColor ("Snowcaps", Color) = (0.83, 0.89, 0.91, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "TerrainBiome"
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
                float _SeaLevel;
                float _MountainHeight;
                half4 _GrassColor;
                half4 _ForestColor;
                half4 _SandColor;
                half4 _RockColor;
                half4 _SnowColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float heightAboveSea = max(0.0, input.positionOS.y - _SeaLevel);
                float elevation = saturate(heightAboveSea / max(1.0, _MountainHeight));
                half3 normalWorld = normalize(TransformObjectToWorldNormal(input.normalOS));
                half steepness = 1.0h - saturate(normalWorld.y);
                half macroVariation = saturate(
                    0.48h + 0.24h * sin(input.positionOS.x * 0.026h +
                                        input.positionOS.z * 0.036h) +
                    0.22h * cos(input.positionOS.z * 0.055h));
                half3 ground = lerp(_GrassColor.rgb, _ForestColor.rgb, macroVariation);

                // Mountain cliffs depend on slope and altitude.
                half stoneMix = saturate(
                    smoothstep(0.22h, 0.63h, elevation) * 0.55h +
                    smoothstep(0.16h, 0.54h, steepness) * 0.70h);
                ground = lerp(ground, _RockColor.rgb, stoneMix);
                half snowMix = smoothstep(0.54h, 0.84h, elevation) *
                               (1.0h - saturate(steepness * 1.5h));
                ground = lerp(ground, _SnowColor.rgb, snowMix);
                half beachMix = 1.0h - smoothstep(2.0h, 9.0h, (half)heightAboveSea);
                ground = lerp(ground, _SandColor.rgb, beachMix);
                half diffuse = lerp(0.65h, 1.0h,
                    saturate(dot(normalWorld, normalize(half3(0.32h, 0.85h, 0.35h)))));
                output.color = half4(ground * _BaseColor.rgb * diffuse, 1.0h);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
