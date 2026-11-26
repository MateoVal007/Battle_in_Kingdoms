Shader "BattleInKingdoms/VillageAmbientFire"
{
    Properties
    {
        [HDR] _CoreColor("Core Color", Color) = (3.4, 2.2, 0.55, 1)
        [HDR] _FlameColor("Flame Color", Color) = (2.4, 0.42, 0.04, 1)
        _SmokeColor("Smoke Color", Color) = (0.11, 0.10, 0.10, 1)
        _Speed("Anim Speed", Float) = 2.4
        _SmokeAmount("Smoke Amount", Range(0, 1)) = 0.72
        _FlameHeight("Flame Height", Range(0.2, 0.85)) = 0.52
        _Seed("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "AmbientFire"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _FlameColor;
                half4 _SmokeColor;
                float _Speed;
                float _SmokeAmount;
                float _FlameHeight;
                float _Seed;
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

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x),
                    f.y
                );
            }

            float Fbm(float2 p)
            {
                return Noise(p) * 0.55 + Noise(p * 2.07 + 11.3) * 0.30 + Noise(p * 4.19 + 5.7) * 0.15;
            }

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed + _Seed * 7.13;
                float2 uv = i.uv;
                float cx = (uv.x - 0.5) * 2.0;

                // Gentle wind sway increasing with height
                float sway = sin(t * 1.15 + uv.y * 4.5 + _Seed) * 0.14 * uv.y;
                float x = cx + sway;

                // Animated rising turbulence
                float nFlame = Fbm(float2(x * 2.3 + _Seed, uv.y * 3.8 - t * 1.35));
                float nDetail = Fbm(float2(x * 5.0 - _Seed, uv.y * 6.5 - t * 2.2));
                float turb = nFlame * 0.65 + nDetail * 0.35;

                // Flame tongue silhouette in lower part of quad
                float yNorm = uv.y / max(_FlameHeight, 0.15);
                float flameWidth = lerp(0.76, 0.08, saturate(yNorm));
                float flameEdge = abs(x) + (turb - 0.5) * (0.35 + yNorm * 0.55);
                float flameMask = (1.0 - smoothstep(flameWidth - 0.22, flameWidth + 0.06, flameEdge));
                flameMask *= smoothstep(0.0, 0.10, uv.y) * (1.0 - smoothstep(0.65, 1.15, yNorm + (turb - 0.5) * 0.45));
                flameMask = saturate(flameMask);

                float coreMask = saturate(flameMask * (1.0 - smoothstep(0.0, 0.62, yNorm)) * smoothstep(0.32, 0.78, 1.0 - abs(x) + nDetail * 0.35));
                half3 flameCol = lerp(_FlameColor.rgb, _CoreColor.rgb, coreMask);

                // Rising wispy smoke plume above the flames
                float nSmoke = Fbm(float2((x - uv.y * 0.12) * 2.2 + _Seed * 1.7, uv.y * 2.4 - t * 0.48));
                float smokeWidth = lerp(0.55, 0.92, uv.y);
                float smokeField = abs(x - uv.y * 0.12) + (nSmoke - 0.5) * 0.62;
                float smokeMask = (1.0 - smoothstep(smokeWidth - 0.35, smokeWidth + 0.10, smokeField));
                smokeMask *= smoothstep(0.18, 0.46, uv.y) * (1.0 - smoothstep(0.68, 0.98, uv.y));
                smokeMask *= smoothstep(0.28, 0.74, nSmoke) * _SmokeAmount;

                // Subtle warm under-glow from fire illuminating bottom of smoke
                half3 smokeCol = _SmokeColor.rgb * (0.75 + nSmoke * 0.5) + _FlameColor.rgb * (0.18 * (1.0 - uv.y));

                float outAlpha = saturate(flameMask + smokeMask * (1.0 - flameMask * 0.75));
                half3 outColor = lerp(smokeCol, flameCol, saturate(flameMask / max(outAlpha, 0.001)));

                return half4(outColor, outAlpha);
            }
            ENDHLSL
        }
    }
}
