Shader "PoolPatrol/UnderwaterShadow"
{
    // Sprite shader for the shadow a floating object casts on the pool floor. It takes the sprite's
    // silhouette (or a soft blob sprite), blurs it, and wobbles it the way light through moving water
    // would. Drawn between the floor and the water surface, so the surface highlights stay on top.
    // SpriteRenderer.color.a scales the shadow, so each object can tune its own strength.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _ShadowColor ("Shadow Color (alpha = strength)", Color) = (0.0, 0.16, 0.32, 0.6)
        _Blur ("Blur (uv)", Range(0, 0.15)) = 0.05
        _Wobble ("Wobble (uv)", Range(0, 0.05)) = 0.012
        _WobbleSpeed ("Wobble Speed", Float) = 2.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _ShadowColor;
                float  _Blur, _Wobble, _WobbleSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 worldXY : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = v.uv;
                o.color = v.color;
                o.worldXY = ws.xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WobbleSpeed;
                float2 uv = i.uv + float2(sin(i.worldXY.y * 3.1 + t), cos(i.worldXY.x * 2.7 + t * 1.3)) * _Wobble;

                float b = _Blur;
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * 0.28;
                a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( b,  b)).a * 0.18;
                a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-b,  b)).a * 0.18;
                a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( b, -b)).a * 0.18;
                a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-b, -b)).a * 0.18;

                return half4(_ShadowColor.rgb, a * _ShadowColor.a * i.color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
