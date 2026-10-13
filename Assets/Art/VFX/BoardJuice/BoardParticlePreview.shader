Shader "FuzzyFrenzy/BoardParticlePreview"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 frag(Output i) : SV_Target { return tex2D(_MainTex, i.uv) * i.color; }
            ENDHLSL
        }
    }
}
