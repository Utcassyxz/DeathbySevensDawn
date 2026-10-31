Shader "SevenDay/SoftRoomGlow"
{
    Properties { _MainTex ("Glow", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,0.1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; fixed4 color:COLOR; };
            sampler2D _MainTex; float4 _ClipRect; fixed4 _Tint;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o; }
            fixed4 frag(v2f i):SV_Target { clip(min(min(i.world.x-_ClipRect.x,_ClipRect.z-i.world.x),min(i.world.y-_ClipRect.y,_ClipRect.w-i.world.y)));return tex2D(_MainTex,i.uv)*_Tint; }
            ENDCG
        }
    }
}
