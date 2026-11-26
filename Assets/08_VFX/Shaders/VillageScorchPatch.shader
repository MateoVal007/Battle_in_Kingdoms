Shader "BattleInKingdoms/VillageScorchPatch"
{
    Properties
    {
        _CharColor("Char Color", Color) = (0.055, 0.045, 0.04, 1)
        _EdgeColor("Scorched Grass Edge", Color) = (0.18, 0.15, 0.09, 1)
        [HDR] _EmberColor("Ember Color", Color) = (2.5, 0.55, 0.06, 1)
        _EmberStrength("Ember Strength", Range(0, 2)) = 0.75
        _Seed("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-1"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "ScorchPatch"
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
                half4 _CharColor;
                half4 _EdgeColor;
                half4 _EmberColor;
                float _EmberStrength;
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
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 34.23);
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
                return Noise(p) * 0.56 + Noise(p * 2.1 + 4.2) * 0.29 + Noise(p * 4.3 + 9.8) * 0.15;
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
                float2 p = (i.uv - 0.5) * 2.0;
                float n = Fbm(p * 3.1 + _Seed * 5.3);
                float r = length(p) + (n - 0.5) * 0.42;

                float outer = 1.0 - smoothstep(0.45, 0.96, r);
                float inner = 1.0 - smoothstep(0.18, 0.64, r);

                half3 col = lerp(_EdgeColor.rgb, _CharColor.rgb, inner);
                col *= (0.78 + n * 0.44);

                float cracks = Fbm(p * 7.5 + _Seed);
                float pulse = 0.72 + 0.28 * sin(_Time.y * 3.1 + _Seed + p.x * 4.0);
                float ember = smoothstep(0.64, 0.80, cracks) * smoothstep(0.20, 0.65, inner) * (1.0 - smoothstep(0.0, 0.22, r) * 0.35);
                col += _EmberColor.rgb * (ember * _EmberStrength * pulse);

                return half4(col, outer * 0.92);
            }
            ENDHLSL
        }
    }
}
