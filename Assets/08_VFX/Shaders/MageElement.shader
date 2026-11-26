Shader "BattleInKingdoms/Mage Element"
{
 Properties { _Ice("Ice",Float)=1 _Facing("Facing",Float)=1 _Seed("Seed",Float)=0 _PreviewTime("Preview time (-1 = live)",Float)=-1 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Name "Element" Tags { "LightMode"="UniversalForward" }
 Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma target 3.0
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Ice, _Facing, _Seed, _PreviewTime;
 CBUFFER_END
 struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
 float Hash(float2 p) { p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
 float Noise(float2 p) { float2 i=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y); }
 float Fbm(float2 p) { return Noise(p)*.57+Noise(p*2.03+13.4)*.28+Noise(p*4.11+7.3)*.15; }
 float Cracks(float2 p) { float2 cell=floor(p),f=frac(p); float first=9,second=9;
 [unroll] for(int y=-1;y<=1;y++) [unroll] for(int x=-1;x<=1;x++) { float2 g=float2(x,y); float2 h=float2(Hash(cell+g),Hash(cell+g+17)); float2 r=g+h-f;float d=dot(r,r);if(d<first){second=first;first=d;}else second=min(second,d); }
 return 1-smoothstep(.015,.075,second-first); }
 half4 Frag(Varyings i):SV_Target {
 float t=(_PreviewTime>=0?_PreviewTime:_Time.y)+_Seed;
 float2 p=(i.uv-.5)*float2(2.6,1.25);p.x*=(_Facing<0?-1:1);
 float n=Fbm(p*6+float2(-t*4.7,t*.4));
 float radius=lerp(.245,.30,_Ice);float2 h=p;h.y+=sin(t*9)*.007;
 float r=length(h);float body=1-smoothstep(radius-.035,radius+.018+(n-.5)*.025,r);
 float behind=saturate(-p.x/1.25);float wave=sin(p.x*13+t*10)*.03+sin(p.x*23-t*7)*.012;
 float width=lerp(.18,.055,behind);float turbulence=(n-.5)*lerp(.15,.06,_Ice);
 float trail=saturate(1-abs(p.y-wave+turbulence)/width)*step(p.x,.03)*smoothstep(1.25,.1,-p.x);
 trail*=smoothstep(.24,.65,n+(.72-behind)*.3)*lerp(.95,.65,_Ice);
 trail*=1-body;
 float halo=exp(-r*r*15)*.24+trail*.12;
 float3 col,tail;
 if(_Ice>.5){
 float angle=t*.35;float2 q=float2(h.x*cos(angle)-h.y*sin(angle),h.x*sin(angle)+h.y*cos(angle));
 float cracks=Cracks(q*12+_Seed)*body;
 float rim=pow(saturate(r/radius),5);
 col=lerp(float3(.025,.21,.52),float3(.16,.75,1.05),saturate(.8-r+n*.3));
 col+=cracks*float3(.65,1.1,1.35)+rim*float3(.22,.68,.95);
 col+=exp(-dot(h-float2(-.07,.08),h-float2(-.07,.08))*190)*float3(.75,1,1.2);
 tail=float3(.1,.65,1.1)*(1.1-behind*.45);
 } else {
 float heat=saturate(.94-r*1.7+n*.24);
 col=lerp(float3(1,.075,.008),float3(1.9,.95,.16),heat);
 col+=exp(-r*r*85)*float3(.7,.65,.3);
 tail=lerp(float3(.8,.035,.003),float3(1.4,.48,.035),saturate(1-behind+n*.2));
 }
 float a=saturate(body+trail+halo);
 float3 rgb=(col*body+tail*trail+lerp(float3(1,.14,.015),float3(.08,.45,.9),_Ice)*halo)/(body+trail+halo+.001);
 return half4(rgb,a);
 }
 ENDHLSL
 }
 }
}
