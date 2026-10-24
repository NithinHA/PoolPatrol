Shader "PoolPatrol/PoolSurface"
{
    // The water surface itself, drawn over the floor and the underwater shadows. Mostly transparent:
    // it only adds what lives *on* the surface.
    //   - sun glints  : two drifting copies of the caustics texture multiplied, so only rare
    //                   overlaps survive the threshold and sparkle.
    //   - ripple light: the ripple sim's slope is lit from the sun, so every ring gets a bright
    //                   leading side and a dark trailing side, which is what makes it read as relief.
    //   - edge lapping: a foam line hugging the pool wall plus thin bands moving inwards from it.
    Properties
    {
        [Header(Surface)]
        _SurfaceTint ("Surface Tint (alpha = amount)", Color) = (0.55, 0.9, 1.0, 0.08)

        [Header(Ripples)]
        _RippleSlope ("Ripple Slope Scale", Float) = 35
        _RippleHighlight ("Ripple Highlight", Range(0, 2)) = 0.7
        _RippleShade ("Ripple Shade", Range(0, 1)) = 0.25

        [Header(Glints)]
        _CausticsTex ("Glint Pattern (use the caustics texture)", 2D) = "black" {}
        _GlintScale ("Glint Pattern Size (world units)", Float) = 3.5
        _GlintSpeed ("Glint Speed", Float) = 0.05
        _GlintThreshold ("Glint Threshold", Range(0, 1)) = 0.3
        _GlintStrength ("Glint Strength", Range(0, 2)) = 0.5

        [Header(Edge)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth ("Foam Line Width", Float) = 0.12
        _FoamBreakup ("Foam Breakup", Range(0, 1)) = 0.6
        _EdgeBandSpacing ("Lapping Band Spacing", Float) = 0.45
        _EdgeBandSpeed ("Lapping Band Speed", Float) = 0.35
        _EdgeBandWidth ("Lapping Band Sharpness", Range(0.5, 0.99)) = 0.86
        _EdgeFalloff ("Lapping Reach", Float) = 1.1
        _EdgeStrength ("Lapping Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _POOL_LOW   // global, set by PoolEnvironment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PoolCommon.hlsl"

            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _SurfaceTint;
                float  _RippleSlope, _RippleHighlight, _RippleShade;
                float4 _CausticsTex_ST;
                float  _GlintScale, _GlintSpeed, _GlintThreshold, _GlintStrength;
                half4  _FoamColor;
                float  _FoamWidth, _FoamBreakup;
                float  _EdgeBandSpacing, _EdgeBandSpeed, _EdgeBandWidth, _EdgeFalloff, _EdgeStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xy;
                float t = _Time.y;

                float sdf = PoolSDF(p);
                clip(-sdf);
                float di = -sdf;

                // Light the ripple relief from the sun.
                float3 rip = SampleRipple(p);
                float slope = dot(rip.xy * _RippleSlope, -_PoolSun.xy);
                half lit = saturate(slope) * _RippleHighlight;
                half shade = saturate(-slope) * _RippleShade;

                #if defined(_POOL_LOW)
                    // Low tier: no glints, and a single sine instead of value noise for the foam.
                    half glint = 0.0;
                    float n = 0.5 + 0.5 * sin(dot(p, float2(2.3, 1.7)) + t * 0.6);
                #else
                    // Glints.
                    float2 g1 = p / _GlintScale + float2(t, -t * 0.4) * _GlintSpeed;
                    float2 g2 = float2(-p.y, p.x) / (_GlintScale * 0.77) + float2(-t * 0.6, t) * _GlintSpeed;
                    // Sampled a few mips down so the overlaps are soft spots rather than hairline scratches.
                    half ga = SAMPLE_TEXTURE2D_BIAS(_CausticsTex, sampler_CausticsTex, g1 + rip.xy, 2.5).r;
                    half gb = SAMPLE_TEXTURE2D_BIAS(_CausticsTex, sampler_CausticsTex, g2, 2.5).r;
                    half glint = smoothstep(_GlintThreshold, 1.0, ga * gb) * _GlintStrength;

                    float n = PoolNoise(p * 2.3 + float2(t * 0.4, -t * 0.3));
                #endif

                // Edge: foam line on the wall, broken up by noise, and lapping bands moving inwards.
                float foamW = _FoamWidth * (1.0 + (n - 0.5) * 2.0 * _FoamBreakup);
                half foam = 1.0 - smoothstep(foamW * 0.4, foamW, di);
                float band = sin((di / _EdgeBandSpacing - t * _EdgeBandSpeed + n * 0.8) * 6.28318);
                half lap = smoothstep(_EdgeBandWidth, 1.0, band) * exp(-di / _EdgeFalloff) * _EdgeStrength;
                half edge = saturate(foam + lap);

                half3 add = (lit + glint).xxx;
                half alpha = saturate(_SurfaceTint.a + shade + edge * _FoamColor.a);
                half3 col = _SurfaceTint.rgb * _SurfaceTint.a + add;
                col = lerp(col, _FoamColor.rgb, edge * _FoamColor.a);
                col *= 1.0 - shade;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
