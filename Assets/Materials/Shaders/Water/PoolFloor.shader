Shader "PoolPatrol/PoolFloor"
{
    // Everything you see *through* the water: the tiled floor, the inner wall face, caustics, the
    // shadow the pool rim casts, and the colour the water adds with depth. Opaque, drawn first.
    //
    // The 3D read comes from four cheap tricks, all driven by the pool SDF in PoolCommon.hlsl:
    //   1. Wall face  - a band just inside the outline is drawn as the vertical pool wall, deepest
    //                   on the far (top) side, as if the camera were tilted a few degrees.
    //   2. Rim shadow - a pixel is shadowed when stepping towards the sun leaves the pool.
    //   3. Refraction - floor and caustics are sampled through the ripple sim's slope.
    //   4. Edge AO    - the floor darkens where it meets the wall.
    //
    // Everything static (1, 2, 4, tiles, depth tint) lives in StaticFloor(). At runtime
    // PoolEnvironment renders it once into a texture with the BakeStatic pass and turns on
    // _POOL_BAKED, so each frame only pays for one texture read plus the moving parts. In edit mode
    // (or if the bake is off) the same function runs live, so both paths look identical.
    Properties
    {
        [Header(Floor Tiles)]
        _TileSize ("Tile Size (world units)", Float) = 0.5
        _TileColorA ("Tile Color A", Color) = (0.72, 0.93, 0.97, 1)
        _TileColorB ("Tile Color B", Color) = (0.60, 0.86, 0.94, 1)
        _GroutColor ("Grout Color", Color) = (0.86, 0.97, 1.0, 1)
        _GroutWidth ("Grout Width", Range(0, 0.2)) = 0.045
        _FloorTex ("Floor Texture (optional, tileable)", 2D) = "white" {}
        _FloorTexScale ("Floor Texture Size (world units)", Float) = 4
        _FloorTexWeight ("Floor Texture Weight", Range(0, 1)) = 0

        [Header(Wall Face)]
        _WallFace ("Face Depth (top, sides, bottom)", Vector) = (1.3, 0.55, 0.0, 0)
        _WallHeight ("Wall Height In Tiles", Float) = 4
        _WallShade ("Wall Shade (top, bottom)", Vector) = (0.8, 0.52, 0, 0)

        [Header(Water)]
        _WaterTint ("Water Tint (multiplies floor)", Color) = (0.45, 0.85, 0.95, 1)
        _DeepColor ("Deep Color (alpha = amount)", Color) = (0.05, 0.45, 0.75, 0.45)
        _DepthDistance ("Distance From Edge To Full Depth", Float) = 5
        _AOStrength ("Edge AO Strength", Range(0, 1)) = 0.5
        _AOWidth ("Edge AO Width", Float) = 0.45
        _RimShadowStrength ("Rim Shadow Strength", Range(0, 1)) = 0.45

        [Header(Caustics)]
        _CausticsTex ("Caustics (tileable)", 2D) = "black" {}
        _CausticsColor ("Caustics Color (alpha = strength)", Color) = (0.9, 1, 1, 0.42)
        _CausticsScale ("Caustics Tile Size (world units)", Float) = 8
        _CausticsSpeed ("Caustics Speed", Float) = 0.08
        _CausticsRange ("Caustics Contrast (lo, hi)", Vector) = (0.12, 0.9, 0, 0)
        [Toggle(_CAUSTICS_CHROMA)] _CausticsChromaOn ("Chromatic Split", Float) = 1
        _CausticsChroma ("Chromatic Split Amount", Range(0, 0.05)) = 0.005
        _WallCaustics ("Caustics On Wall Face", Range(0, 1)) = 0.2

        [Header(Refraction)]
        _RefractionStrength ("Ripple Refraction", Float) = 5
        _RippleLight ("Ripple Light Focus", Float) = 3
        _WobbleStrength ("Idle Wobble", Range(0, 0.2)) = 0.04
        _WobbleSpeed ("Idle Wobble Speed", Float) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        // Nothing in this 2D scene depth-tests against the floor, and writing depth here would hide
        // anything placed behind it, such as a Screen Space - Camera canvas at a large plane distance.
        ZWrite Off

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PoolCommon.hlsl"

            TEXTURE2D(_FloorTex);    SAMPLER(sampler_FloorTex);
            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);

            // Written once at load by PoolEnvironment through the BakeStatic pass.
            TEXTURE2D(_PoolBakedFloor); SAMPLER(sampler_linear_clamp);
            float4 _PoolBakedRect;     // xy = world min corner, zw = 1 / world size

            CBUFFER_START(UnityPerMaterial)
                float  _TileSize;
                half4  _TileColorA, _TileColorB, _GroutColor;
                float  _GroutWidth;
                float4 _FloorTex_ST;
                float  _FloorTexScale, _FloorTexWeight;
                float4 _WallFace;
                float  _WallHeight;
                float4 _WallShade;
                half4  _WaterTint, _DeepColor;
                float  _DepthDistance, _AOStrength, _AOWidth, _RimShadowStrength;
                float4 _CausticsTex_ST;
                half4  _CausticsColor;
                float  _CausticsScale, _CausticsSpeed;
                float4 _CausticsRange;
                float  _CausticsChromaOn, _CausticsChroma, _WallCaustics;
                float  _RefractionStrength, _RippleLight, _WobbleStrength, _WobbleSpeed;
            CBUFFER_END

            // Square tiles in tile space (1 unit = 1 tile), with grout and a little colour jitter.
            half3 Tiles(float2 g)
            {
                float2 id = floor(g);
                float2 f = frac(g);
                float edge = min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y));
                float aa = max(fwidth(g.x), 1e-4);
                float grout = 1.0 - smoothstep(_GroutWidth, _GroutWidth + aa * 1.5, edge);

                half3 col = lerp(_TileColorA.rgb, _TileColorB.rgb, PoolHash21(id));
                // Soft bevel: each tile is a touch lighter towards its upper-left.
                col *= 1.0 + (f.y - f.x) * 0.06;
                return lerp(col, _GroutColor.rgb, grout);
            }

            half3 FloorTexture(float2 p)
            {
                // Sampled with gradients from the unwrapped uv so a Clamp texture tiles without mip seams.
                float2 uv = p / _FloorTexScale;
                return SAMPLE_TEXTURE2D_GRAD(_FloorTex, sampler_FloorTex, frac(uv), ddx(uv), ddy(uv)).rgb;
            }

            // Everything about the floor that never moves: tiles, wall face, depth tint, edge AO and
            // rim shadow. rgb = colour with AO and shadow already applied; a = how much caustic light
            // reaches this point, darkened the same way. The final floor is always
            //     rgb + caustics * a
            // whether this runs live or comes out of the baked texture.
            //   p     : where the pixel is (pool geometry)
            //   fp    : where the floor is seen through the water (p + refraction)
            //   refrY : vertical refraction, lets the wall face wobble a little
            half4 StaticFloor(float2 p, float2 fp, float refrY)
            {
                float sdf = PoolSDF(p);
                // Outside the water the deck covers this; keep it dark so any gap reads as a seam.
                if (sdf > 0.0) return half4(_DeepColor.rgb * 0.5, 0.0);

                float di = -sdf;                      // distance inward from the outline
                float2 n = PoolNormal(p);

                // Fake perspective: how deep the visible wall face is on this side of the pool.
                float faceH = _WallFace.x * max(n.y, 0.0) + _WallFace.y * abs(n.x) + _WallFace.z * max(-n.y, 0.0);
                float faceV = saturate((di + refrY * 0.3) / max(faceH, 1e-3));   // 0 at the rim, 1 at the floor
                float isFace = step(di, faceH) * step(1e-3, faceH);

                // Floor.
                half3 floorCol = Tiles(fp / _TileSize);
                floorCol = lerp(floorCol, floorCol * FloorTexture(fp) * 1.6, _FloorTexWeight);

                // Wall face: tiles run along the edge and up the wall, getting darker towards the floor.
                float2 tangent = float2(-n.y, n.x);
                float2 wallG = float2(dot(fp, tangent) / _TileSize, faceV * _WallHeight);
                half3 wallCol = Tiles(wallG) * lerp(_WallShade.x, _WallShade.y, faceV);

                half3 col = lerp(floorCol, wallCol, isFace);

                // Water absorbs, more so away from the walls.
                float floorDist = max(di - faceH, 0.0);
                col *= _WaterTint.rgb;
                col = lerp(col, _DeepColor.rgb, _DeepColor.a * saturate(floorDist / _DepthDistance));

                // Shadow from the rim and deck, cast in the sun's direction.
                float shadowSdf = PoolSDF(p - _PoolSun.xy * _PoolSun.z);
                float shadow = smoothstep(-_PoolSun.w, _PoolSun.w, shadowSdf);
                half causMask = (1.0 - shadow * 0.85) * lerp(1.0, _WallCaustics, isFace);

                // Contact darkening where floor meets wall, then the rim shadow on top.
                float ao = exp(-floorDist / max(_AOWidth, 1e-3)) * (1.0 - isFace);
                half darken = (1.0 - _AOStrength * ao) * (1.0 - _RimShadowStrength * shadow);

                return half4(col * darken, causMask * darken);
            }
        ENDHLSL

        Pass
        {
            Name "Floor"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _CAUSTICS_CHROMA
            // Global keywords set by PoolEnvironment: _POOL_BAKED once the static layer is baked,
            // _POOL_LOW on the low quality tier.
            #pragma multi_compile _ _POOL_BAKED
            #pragma multi_compile _ _POOL_LOW

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half Caustics(float2 p, float t)
            {
                float2 uv1 = p / _CausticsScale + float2(t, t * 0.6);
                float2 uv2 = float2(p.y, -p.x) / (_CausticsScale * 1.31) + float2(-t * 0.7, t * 0.45);
                // The first web bends with a slow wobble; the second, drifting the other way, modulates
                // it and flares where they cross, so the pattern keeps re-forming instead of sliding.
                #if !defined(_POOL_LOW)
                    uv1 += PoolWobble(p * 0.6, t * 6.0) * 0.03;
                #endif
                half a = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv1).r;
                half b = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv2).r;
                return smoothstep(_CausticsRange.x, _CausticsRange.y, a * (0.55 + 0.45 * b) + a * b * 0.6 + b * 0.12);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xy;
                float t = _Time.y;

                // Refraction: ripple slope + an always-on slow wobble.
                float3 rip = SampleRipple(p);
                float2 refr = rip.xy * _RefractionStrength + PoolWobble(p, t * _WobbleSpeed) * _WobbleStrength;

                #if defined(_POOL_BAKED)
                    // The bake has a hard dark edge at the waterline, so fade refraction out right at
                    // the wall rather than let it drag that edge into the water.
                    refr *= saturate(-PoolSDF(p) * 4.0);
                    float2 fp = p + refr;
                    half4 st = SAMPLE_TEXTURE2D(_PoolBakedFloor, sampler_linear_clamp, (fp - _PoolBakedRect.xy) * _PoolBakedRect.zw);
                #else
                    float2 fp = p + refr;
                    half4 st = StaticFloor(p, fp, refr.y);
                #endif

                // Caustics.
                float ct = t * _CausticsSpeed;
                #if defined(_CAUSTICS_CHROMA) && !defined(_POOL_LOW)
                    float2 co = _PoolSun.xy * _CausticsChroma * _CausticsScale;
                    half3 caus = half3(Caustics(fp + co, ct), Caustics(fp, ct), Caustics(fp - co, ct));
                #else
                    half3 caus = Caustics(fp, ct).xxx;
                #endif
                // Ripple crests focus light into bright rings on the floor.
                caus += saturate(rip.z * _RippleLight);
                #if defined(_POOL_LOW)
                    half causVar = 0.85;
                #else
                    // Slow, large patches of stronger and weaker light keep the web from looking stamped.
                    half causVar = 0.55 + 0.6 * PoolNoise(p * 0.18 + t * 0.03);
                #endif

                half3 col = st.rgb + caus * _CausticsColor.rgb * _CausticsColor.a * st.a * causVar;
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // Renders StaticFloor across _PoolBakedRect into whatever target is bound. Drawn by
        // PoolEnvironment as a full-screen triangle, never by a renderer.
        Pass
        {
            Name "BakeStatic"
            Cull Off ZTest Always ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertBake
            #pragma fragment fragBake

            struct BakeVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            BakeVaryings vertBake(uint vertexID : SV_VertexID)
            {
                BakeVaryings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            half4 fragBake(BakeVaryings i) : SV_Target
            {
                float2 q = _PoolBakedRect.xy + i.uv / _PoolBakedRect.zw;
                return StaticFloor(q, q, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
