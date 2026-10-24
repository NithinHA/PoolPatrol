Shader "PoolPatrol/PoolDeck"
{
    // The pool surround: a rounded stone coping hugging the water, then the deck. Drawn on one big
    // quad with the pool cut out by the SDF, above the water and below gameplay.
    // The coping is lit as a bullnose (its normal tilts down towards the water) from the same sun that
    // casts the rim shadow in PoolFloor, which is most of what makes the edge feel raised.
    Properties
    {
        [Header(Coping)]
        _CopingWidth ("Coping Width", Float) = 0.7
        _CopingColor ("Coping Color", Color) = (0.98, 0.95, 0.88, 1)
        _CopingTileLength ("Coping Stone Length", Float) = 1.1
        _CopingBevel ("Bevel Portion", Range(0.05, 1)) = 0.55
        _CopingGroutColor ("Coping Grout", Color) = (0.74, 0.69, 0.62, 1)

        [Header(Deck)]
        _DeckTex ("Deck Texture (optional, tileable)", 2D) = "white" {}
        _DeckTexScale ("Deck Texture Size (world units)", Float) = 4
        _DeckTexWeight ("Deck Texture Weight", Range(0, 1)) = 0
        _DeckTileSize ("Deck Tile Size", Float) = 1.6
        _DeckColorA ("Deck Color A", Color) = (0.93, 0.80, 0.64, 1)
        _DeckColorB ("Deck Color B", Color) = (0.88, 0.74, 0.58, 1)
        _DeckGroutColor ("Deck Grout", Color) = (0.78, 0.66, 0.52, 1)
        _DeckGroutWidth ("Deck Grout Width", Range(0, 0.2)) = 0.02

        [Header(Lighting)]
        _Ambient ("Ambient", Range(0, 1)) = 0.62
        _SunStrength ("Sun Strength", Range(0, 1.5)) = 0.5
        _CopingShadowWidth ("Shadow Below Coping On Deck", Float) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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

            TEXTURE2D(_DeckTex); SAMPLER(sampler_DeckTex);

            CBUFFER_START(UnityPerMaterial)
                float  _CopingWidth;
                half4  _CopingColor;
                float  _CopingTileLength, _CopingBevel;
                half4  _CopingGroutColor;
                float4 _DeckTex_ST;
                float  _DeckTexScale, _DeckTexWeight, _DeckTileSize;
                half4  _DeckColorA, _DeckColorB, _DeckGroutColor;
                float  _DeckGroutWidth;
                float  _Ambient, _SunStrength, _CopingShadowWidth;
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

            float GroutMask(float2 g, float width)
            {
                float2 f = frac(g);
                float edge = min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y));
                float aa = max(fwidth(g.x), 1e-4) * 1.5;
                return 1.0 - smoothstep(width, width + aa, edge);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xy;
                float sdf = PoolSDF(p);
                float aa = max(fwidth(sdf), 1e-4);
                half alpha = smoothstep(-aa, aa, sdf);
                clip(alpha - 0.001);

                float2 n = PoolNormal(p);
                float3 L = normalize(float3(-_PoolSun.xy, 1.1));

                // Coping: bullnose profile. Near the water the surface curls down towards the pool.
                float ct = saturate(sdf / _CopingWidth);
                float curl = 1.0 - saturate(ct / _CopingBevel);
                float3 N = normalize(float3(-n * curl * curl * 2.2, 1.0));
                float2 tangent = float2(-n.y, n.x);
                float along = dot(p, tangent) / _CopingTileLength;
                float seamAA = max(fwidth(along), 1e-4) * 1.5;
                float seam = 1.0 - smoothstep(0.02, 0.02 + seamAA, min(frac(along), 1.0 - frac(along)));
                seam *= 1.0 - PoolInCorner(p);   // each rounded corner is a single curved stone
                half3 coping = lerp(_CopingColor.rgb * (0.97 + 0.06 * PoolHash21(floor(along).xx)), _CopingGroutColor.rgb, seam);
                // Thin grout line where the coping meets the deck, and the shadowed lip over the water.
                coping = lerp(coping, _CopingGroutColor.rgb, 1.0 - smoothstep(0.0, 0.06, 1.0 - ct));
                coping *= lerp(0.55, 1.0, smoothstep(0.0, 0.07, sdf));

                // Deck.
                float2 dg = p / _DeckTileSize;
                half3 deck = lerp(_DeckColorA.rgb, _DeckColorB.rgb, PoolHash21(floor(dg)));
                #if !defined(_POOL_LOW)
                    deck *= 0.94 + 0.12 * PoolNoise(p * 3.1);
                #endif
                // Pavers are slightly domed: a touch of light on their sun side, shade on the other.
                float2 df = frac(dg) - 0.5;
                deck *= 1.0 + dot(df, -_PoolSun.xy) * 0.1;
                float2 duv = p / _DeckTexScale;
                half3 deckTex = SAMPLE_TEXTURE2D_GRAD(_DeckTex, sampler_DeckTex, frac(duv), ddx(duv), ddy(duv)).rgb;
                deck = lerp(deck, deckTex, _DeckTexWeight);
                deck = lerp(deck, _DeckGroutColor.rgb, GroutMask(dg, _DeckGroutWidth) * (1.0 - _DeckTexWeight));
                // The raised coping shades the deck right next to it on its shadow side.
                float behind = saturate(dot(n, _PoolSun.xy));
                deck *= 1.0 - 0.25 * behind * (1.0 - smoothstep(0.0, _CopingShadowWidth, sdf - _CopingWidth));

                bool onCoping = sdf < _CopingWidth;
                half3 albedo = onCoping ? coping : deck;
                float3 normal = onCoping ? N : float3(0, 0, 1);
                half light = _Ambient + _SunStrength * saturate(dot(normal, L));
                return half4(albedo * light, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
