Shader "PoolPatrol/UIWaterBlob"
{
    // A UI (uGUI) shader for the pause menu's water-blob panel background. Adds a slow, subtle
    // ripple to the sprite's own UVs plus a drifting diagonal sheen, so the blob reads as a living
    // body of water instead of a static image.
    //
    // Time comes from _UnscaledTime, pushed in by UI.InGame.WaterBlobRipple, NOT from Unity's
    // built-in _Time: the panel is on screen exactly when Time.timeScale is 0, which freezes _Time.
    //
    // The blob's sprite lives in a multi-sprite sheet, so the mesh UVs are sheet-space, not 0..1.
    // _SpriteRect (pushed in by the same script) carries the sprite's own uv rect: the ripple is
    // phased and faded in that local space, and the sampled UV is clamped to the rect, so the
    // distortion can never reach across into a neighbouring sprite.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _UnscaledTime ("Unscaled Time", Float) = 0
        _SpriteRect ("Sprite UV Rect (uMin, vMin, uMax, vMax)", Vector) = (0,0,1,1)
        _RippleStrength ("Ripple Strength", Range(0, 0.03)) = 0.006
        _RippleFrequency ("Ripple Frequency", Range(0.5, 12)) = 4.5
        _RippleSpeed ("Ripple Speed", Range(0, 4)) = 0.9
        _SheenStrength ("Sheen Strength", Range(0, 0.4)) = 0.12

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
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _UnscaledTime;
            float4 _SpriteRect;
            float _RippleStrength;
            float _RippleFrequency;
            float _RippleSpeed;
            float _SheenStrength;

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

            fixed4 frag(v2f IN) : SV_Target
            {
                float t = _UnscaledTime * _RippleSpeed;

                float2 uv = IN.texcoord;
                float2 rectSize = max(_SpriteRect.zw - _SpriteRect.xy, 1e-5);

                // 0..1 across this sprite, whatever corner of the sheet it was packed into.
                float2 local = saturate((uv - _SpriteRect.xy) / rectSize);

                // Two crossed sine waves at slightly different rates so the motion never visibly
                // loops. Faded out towards the sprite edges, so the blob's silhouette stays put and
                // only its interior appears to move.
                float2 edgeFade = saturate(min(local, 1.0 - local) * 6.0);
                float fade = edgeFade.x * edgeFade.y;

                float2 offset;
                offset.x = sin((local.y * _RippleFrequency) + t) * _RippleStrength;
                offset.y = cos((local.x * _RippleFrequency * 0.83) + t * 1.17) * _RippleStrength;

                // Offset in sprite-local units, then clamped inside the rect as a hard guarantee.
                float2 sampleUv = clamp(uv + offset * fade * rectSize, _SpriteRect.xy, _SpriteRect.zw);

                half4 color = (tex2D(_MainTex, sampleUv) + _TextureSampleAdd) * IN.color;

                // Broad diagonal highlight drifting across the surface, scaled by the sprite's own
                // alpha so it never shows outside the blob's silhouette.
                float sheen = sin((local.x + local.y) * 2.2 - t * 0.55) * 0.5 + 0.5;
                color.rgb += sheen * sheen * _SheenStrength * color.a;

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
