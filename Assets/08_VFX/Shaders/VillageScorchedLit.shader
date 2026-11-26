Shader "BattleInKingdoms/VillageScorchedLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _CharColor("Char / Soot Color", Color) = (0.045, 0.038, 0.035, 1)
        [HDR] _EmberColor("Ember Color", Color) = (2.6, 0.55, 0.08, 1)
        _BurnCenter("Burn Center (World)", Vector) = (0, 2, 8, 1)
        _BurnRadius("Burn Radius", Float) = 3.8
        _BurnIntensity("Burn Intensity", Range(0, 1)) = 0.85
        _SootRise("Soot Rise Stretch", Range(0.5, 4.0)) = 1.9
        _EmberStrength("Ember Strength", Range(0, 2)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _CharColor;
                half4 _EmberColor;
                float4 _BurnCenter;
                float _BurnRadius;
                float _BurnIntensity;
                float _SootRise;
                float _EmberStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3(i + float3(0, 0, 0));
                float n100 = Hash3(i + float3(1, 0, 0));
                float n010 = Hash3(i + float3(0, 1, 0));
                float n110 = Hash3(i + float3(1, 1, 0));
                float n001 = Hash3(i + float3(0, 0, 1));
                float n101 = Hash3(i + float3(1, 0, 1));
                float n011 = Hash3(i + float3(0, 1, 1));
                float n111 = Hash3(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            float Fbm3(float3 p)
            {
                return Noise3(p) * 0.58 + Noise3(p * 2.13 + 7.4) * 0.28 + Noise3(p * 4.31 + 19.2) * 0.14;
            }

            Varyings Vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(i.normalOS);
                o.positionCS = posInputs.positionCS;
                o.posWS = posInputs.positionWS;
                o.normalWS = normInputs.normalWS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;

                // World-space directional soot mask: smoke & char stretch upward above _BurnCenter
                float3 rel = i.posWS - _BurnCenter.xyz;
                float upBias = rel.y > 0.0 ? (rel.y / max(_SootRise, 0.5)) : (rel.y * 1.45);
                float3 shaped = float3(rel.x, upBias, rel.z);
                float dist = length(shaped) / max(_BurnRadius, 0.2);

                float n = Fbm3(i.posWS * 0.95);
                float streak = Fbm3(float3(i.posWS.x * 1.8, i.posWS.y * 0.45, i.posWS.z * 1.8));
                float sootField = dist + (n - 0.5) * 0.55 + (streak - 0.5) * 0.35;

                float scorchMask = (1.0 - smoothstep(0.32, 1.05, sootField)) * _BurnIntensity;
                float heavyChar = (1.0 - smoothstep(0.15, 0.62, sootField)) * _BurnIntensity;

                // Darken wall/roof realistically from light smoke stain to black char
                half3 stained = lerp(albedo, albedo * half3(0.32, 0.28, 0.25), scorchMask);
                half3 finalAlbedo = lerp(stained, _CharColor.rgb * (0.65 + n * 0.7), heavyChar);

                // Standard URP main light + ambient SH
                float3 normalWS = normalize(i.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.posWS));
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * (ndotl * mainLight.shadowAttenuation);
                half3 ambient = SampleSH(normalWS);

                half3 lit = finalAlbedo * (diffuse + ambient);

                // Subtle pulsing glowing embers inside the heaviest charred cracks
                float crackNoise = Fbm3(i.posWS * 3.4 + float3(0, -_Time.y * 0.12, 0));
                float pulse = 0.72 + 0.28 * sin(_Time.y * 3.4 + i.posWS.x * 2.1 + i.posWS.y * 1.7);
                float emberMask = smoothstep(0.64, 0.82, crackNoise) * smoothstep(0.28, 0.75, heavyChar);
                lit += _EmberColor.rgb * (emberMask * _EmberStrength * pulse);

                lit = MixFog(lit, i.fogCoord);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
