Shader "Kingdoms/BombExplosion"
{
    Properties
    {
        [HDR] _FireColor("Fire",Color)=(3,0.55,0.025,1)
        [HDR] _CoreColor("Flash",Color)=(6,3,0.5,1)
        _SmokeColor("Smoke",Color)=(0.13,0.105,0.09,1)
        _Progress("Progress",Range(0,1))=0
        _Seed("Seed",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _FireColor,_CoreColor,_SmokeColor;
                float _Progress,_Seed;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float t=saturate(_Progress),fade=1-smoothstep(.52,1,t);
                float2 p=(i.uv-.5)*2;
                p.y-=t*.18;
                float2 drift=float2(_Seed,-t*2);
                float noise=Noise(p*4+drift)*.65+Noise(p*9+drift*1.7)*.35;
                float radius=lerp(.2,.88,1-pow(1-t,4));
                float edge=length(p)+(noise-.5)*.24;
                float mask=1-smoothstep(radius-.12,radius+.04,edge);
                float heat=saturate(1-length(p)/max(radius,.01));
                half3 fire=lerp(_FireColor.rgb,_CoreColor.rgb,pow(heat,2.4));
                float smoke=smoothstep(.25,.78,t);
                half3 color=lerp(fire,_SmokeColor.rgb*(.7+noise*.6),smoke);
                return half4(color,mask*fade*lerp(1,.72,smoke));
            }
            ENDHLSL
        }
    }
}
