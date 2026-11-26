Shader "BattleInKingdoms/Mage Spark"
{
 Properties { _Ice("Ice shard",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Ice;
 CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;return o;}
 half4 Frag(V i):SV_Target{float2 p=i.uv*2-1;float d=lerp(length(p),abs(p.x)*.65+abs(p.y),_Ice);float alpha=saturate(1-d);alpha*=alpha;return half4(i.color.rgb*1.35,i.color.a*alpha);}
 ENDHLSL
 } }
}
