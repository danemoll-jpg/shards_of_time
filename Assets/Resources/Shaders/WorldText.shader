// Fix 13.2 item 2: world text (signs, labels, neon words). The font's own material (GUI/Text Shader) draws over everything and from
// both sides: this one is hidden by walls like any other object (depth tested) and shows only on its front (a TextMesh reads from
// its -z side: from behind, nothing is drawn).
Shader "ShardsOfTime/WorldText"
{
    Properties { _MainTex ("Font Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Off Lighting Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float side : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 cam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
                o.side = cam.z - v.vertex.z;   // < 0: the camera is on the text's front (-z) side
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                clip(-i.side);
                fixed4 c = i.color; c.a *= tex2D(_MainTex, i.uv).a; return c;
            }
            ENDCG
        }
    }
}
