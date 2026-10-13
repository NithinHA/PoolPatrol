Shader "PoolPatrol/UIBlur"
{
    // A UI (uGUI) shader that blurs the Image's own sprite with a tweenable _BlurSize. Intended for a
    // static background Image (e.g. the menu MainBG) — it samples _MainTex, so it does not blur what is
    // drawn behind the Image. Keeps standard UI masking/clipping support.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0,12)) = 4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _BlurSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 3x3 gaussian ( [1 2 1; 2 4 2; 1 2 1] / 16 ).
            fixed4 BlurSample(float2 uv)
            {
                float2 o = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 c = fixed4(0,0,0,0);
                c += tex2D(_MainTex, uv + float2(-o.x, -o.y)) * 0.0625;
                c += tex2D(_MainTex, uv + float2( 0.0, -o.y)) * 0.125;
                c += tex2D(_MainTex, uv + float2( o.x, -o.y)) * 0.0625;
                c += tex2D(_MainTex, uv + float2(-o.x,  0.0)) * 0.125;
                c += tex2D(_MainTex, uv + float2( 0.0,  0.0)) * 0.25;
                c += tex2D(_MainTex, uv + float2( o.x,  0.0)) * 0.125;
                c += tex2D(_MainTex, uv + float2(-o.x,  o.y)) * 0.0625;
                c += tex2D(_MainTex, uv + float2( 0.0,  o.y)) * 0.125;
                c += tex2D(_MainTex, uv + float2( o.x,  o.y)) * 0.0625;
                return c;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (BlurSample(IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
