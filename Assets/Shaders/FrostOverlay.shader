Shader "UI/FrostOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FrostTex ("Frost Texture", 2D) = "white" {}
        [Normal] _FrostNormals ("Frost Normals", 2D) = "bump" {}

        _BlendAmount ("Frost Amount", Range(0,1)) = 0.5
        _EdgeSharpness ("Edge Sharpness", Float) = 10
        _SeeThroughness ("See Throughness", Range(0,1)) = 0.1
        _Distortion ("Distortion", Range(0,1)) = 0.05

        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FrostTex;
            sampler2D _FrostNormals;

            fixed4 _Color;
            float4 _ClipRect;

            float _BlendAmount;
            float _EdgeSharpness;
            float _SeeThroughness;
            float _Distortion;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UI image base (usually a white sprite for fullscreen overlay)
                fixed4 baseColor = tex2D(_MainTex, i.uv) * i.color;

                // Frost mask/color
                fixed4 frost = tex2D(_FrostTex, i.uv);

                // Rebuild alpha like your original shader
                float frostAlpha = frost.a;
                frostAlpha = frostAlpha + (_BlendAmount * 2.0 - 1.0);
                frostAlpha = saturate(frostAlpha * _EdgeSharpness - (_EdgeSharpness - 1.0) * 0.5);

                // Fake distortion only inside the frost
                half2 bump = UnpackNormal(tex2D(_FrostNormals, i.uv)).rg;
                float2 distortedUV = i.uv + bump * frostAlpha * _Distortion;

                fixed4 distortedBase = tex2D(_MainTex, distortedUV) * i.color;
                fixed4 distortedFrost = tex2D(_FrostTex, distortedUV);

                fixed4 overlayColor = distortedFrost;
                overlayColor.rgb = distortedBase.rgb * (distortedFrost.rgb + 0.5) * (distortedFrost.rgb + 0.5);

                fixed4 frosted = lerp(distortedFrost, overlayColor, _SeeThroughness);

                fixed4 finalColor;
                finalColor.rgb = lerp(baseColor.rgb, frosted.rgb, frostAlpha);
                finalColor.a = baseColor.a * frostAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}