Shader "Kingdoms/PlazaAdoquin"
{
    Properties { _BaseColor("Color de piedra",Color)=(.62,.58,.49,1) _TileScale("Escala",Float)=.9 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _TileScale;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;};
            Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;}
            half4 frag(Varyings i):SV_Target
            {
                float2 p=i.positionWS.xz*_TileScale*float2(1,1.6);
                p.x+=fmod(floor(p.y),2)*.5;
                float2 cell=floor(p),uv=frac(p);
                float h=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
                float2 edge=min(uv,1-uv);
                float grout=smoothstep(.018,.055,min(edge.x,edge.y));
                half3 color=lerp(_BaseColor.rgb*.57,_BaseColor.rgb*(.87+h*.24),grout);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse=saturate(dot(normalize(i.normalWS),light.direction));
                half3 lighting=SampleSH(normalize(i.normalWS))*.65+light.color*(.3+diffuse*.7)*light.shadowAttenuation;
                return half4(color*lighting,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
