Shader "PoolPatrol/WaterFlood"
{
    // Two-phase full-screen water transition for a uGUI RawImage.
    //   Phase 1 (_Flood 0 -> 1): water floods in from the left and right edges behind an opaque white
    //     foam front and fills the screen with a blue gradient. Swap scenes once it reaches 1.
    //   Phase 2 (_Wipe 0 -> 1): a radial wipe centred on _Center opens up and takes the water away.
    // Every shape is procedural (domain-warped value noise), the caustic webbing included, so this
    // needs no art at all and is resolution and aspect independent. Caustics are drawn as a band
    // through a warped noise field, which gives the closed rounded loops a pool surface casts.
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _Flood ("Flood Progress", Range(0,1)) = 0
        _Wipe ("Wipe Progress", Range(0,1)) = 0
        _FoamAmount ("Foam Patch Amount", Range(0,1)) = 1
        _Center ("Wipe Center (uv)", Vector) = (0.5,0.5,0,0)
        _Aspect ("Aspect (w/h)", Float) = 0.5625

        [Header(Colors)]
        _TopColor ("Water Top", Color) = (0.149,0.486,0.820,1)
        _BottomColor ("Water Bottom", Color) = (0.454,0.741,0.796,1)
        _FoamColor ("Foam", Color) = (1,1,1,1)
        _Vignette ("Edge Darkening", Range(0,0.6)) = 0.12

        [Header(Flood Shape)]
        _NoiseScale ("Blob Scale", Float) = 3.0
        _NoiseAmp ("Blob Strength", Range(0,0.4)) = 0.16
        _EdgeSoft ("Edge Softness", Range(0.001,0.08)) = 0.012
        _FoamWidth ("Foam Rim Width", Range(0,0.2)) = 0.06
        _AlphaRamp ("Water Fade Depth", Range(0.01,0.5)) = 0.05
        _Wobble ("Wobble Speed", Float) = 0.35

        [Header(Foam Patches)]
        _PatchScale ("Patch Scale", Float) = 9.0
        _PatchThreshold ("Patch Threshold", Range(0,1)) = 0.72
        _PatchStretch ("Patch Horizontal Stretch", Range(1,6)) = 2.2
        _PatchSoft ("Patch Edge Softness", Range(0.01,0.3)) = 0.10
        _PatchOpacity ("Patch Opacity", Range(0,1)) = 0.85

        [Header(Caustics)]
        _CausticsStrength ("Caustics Strength", Range(0,1)) = 0.16
        _CausticsScale ("Caustics Cell Scale", Float) = 5.5
        _CausticsWidth ("Caustics Line Width", Range(0.01,0.4)) = 0.07
        _CausticsSpeed ("Caustics Drift Speed", Float) = 0.06
        _CausticsCut ("Caustics Band Center", Range(0,1)) = 0.50
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            float  _Flood, _Wipe, _FoamAmount, _Aspect;
            float4 _Center;
            fixed4 _TopColor, _BottomColor, _FoamColor;
            float  _Vignette;
            float  _NoiseScale, _NoiseAmp, _EdgeSoft, _FoamWidth, _AlphaRamp, _Wobble;
            float  _PatchScale, _PatchThreshold, _PatchStretch, _PatchSoft, _PatchOpacity;
            float  _CausticsStrength, _CausticsScale, _CausticsWidth, _CausticsSpeed, _CausticsCut;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.texcoord;
                o.color  = v.color;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Two octaves, renormalised to roughly 0..1.
            float Fbm(float2 p)
            {
                float v = VNoise(p) * 0.5 + VNoise(p * 2.03 + 11.7) * 0.25;
                return v * 1.3333;
            }

            // Fbm pushed around by itself. The warp is what turns a merely noisy boundary into the
            // rounded overlapping lobes that read as liquid instead of as a torn edge.
            float WarpFbm(float2 p)
            {
                float2 q = float2(Fbm(p), Fbm(p + float2(5.2, 1.3)));
                return Fbm(p + 2.0 * q);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ap = float2(uv.x * _Aspect, uv.y);   // aspect-corrected, so blobs stay round
                float2 p  = ap * _NoiseScale;
                float  t  = _Time.y * _Wobble;

                // Flood front.
                // Fully covered is the common case during the wipe, so skip the noise entirely then.
                // This is a uniform branch: every pixel takes the same side, so it costs nothing.
                float field      = 1.0;
                float waterAlpha = 1.0;
                float rim        = 0.0;

                if (_Flood < 0.999)
                {
                    float nL = WarpFbm(p + float2(t, 0.0));
                    float nR = WarpFbm(p + float2(17.3 - t, 9.1));

                    // The front travels linearly to mid-screen, but the margin that guarantees a
                    // seamless meeting with the fully-covered branch at _Flood = 1 is folded in
                    // only at the tail. Spread linearly it closes the screen a third of the way
                    // through the tween and the rest of the cover reads as dead time.
                    float margin = _NoiseAmp + _FoamWidth + _AlphaRamp + 0.06;
                    float f4     = _Flood * _Flood * _Flood * _Flood;
                    float reach  = _Flood * 0.5 + margin * f4;
                    float fieldL = reach - uv.x         + (nL - 0.5) * 2.0 * _NoiseAmp;
                    float fieldR = reach - (1.0 - uv.x) + (nR - 0.5) * 2.0 * _NoiseAmp;
                    field = max(fieldL, fieldR);

                    waterAlpha = smoothstep(0.0, _AlphaRamp, field);
                    rim = saturate(smoothstep(0.0, _EdgeSoft, field)
                                 - smoothstep(_FoamWidth, _FoamWidth + _EdgeSoft, field));
                }

                // Foam patches drifting on the surface.
                float patch = 0.0;
                if (_FoamAmount > 0.001)
                {
                    // Squashed on x so the patches come out as long ovals rather than round blobs.
                    float2 pp = float2(ap.x / _PatchStretch, ap.y) * _PatchScale
                              + float2(t * 0.6, -t * 0.25);
                    float pn = Fbm(pp);
                    // Raising the threshold past the noise ceiling erodes the patches to nothing,
                    // which is how they dissipate once the screen is covered.
                    float thr = lerp(1.05, _PatchThreshold, _FoamAmount);
                    patch = smoothstep(thr, thr + _PatchSoft, pn) * step(_FoamWidth, field) * _PatchOpacity;
                }

                // Radial wipe.
                float wipe    = 0.0;
                float wipeRim = 0.0;
                if (_Wipe > 0.001)
                {
                    float2 c    = float2(_Center.x * _Aspect, _Center.y);
                    float  dist = length(ap - c);
                    float  maxR = length(float2(max(c.x, _Aspect - c.x), max(c.y, 1.0 - c.y)));
                    float  wn   = WarpFbm(p * 0.7 + float2(31.7, 4.3));

                    float wf = _Wipe * (maxR + _FoamWidth + 0.08) - dist
                             + (wn - 0.5) * 2.0 * _NoiseAmp * 0.6;

                    wipe = smoothstep(0.0, _EdgeSoft, wf);
                    // Foam rim sitting just outside the opening, so the wipe edge matches the flood.
                    wipeRim = saturate(smoothstep(-_FoamWidth, -_FoamWidth + _EdgeSoft, wf) - wipe);
                }

                float foam  = saturate(max(max(rim, patch), wipeRim));
                float alpha = max(waterAlpha, foam) * (1.0 - wipe);
                clip(alpha - 0.002);

                // Colour.
                float3 col = lerp(_BottomColor.rgb, _TopColor.rgb, uv.y);

                if (_CausticsStrength > 0.001)
                {
                    // A narrow band through a warped noise field traces that field's contour lines,
                    // which come out as the soft closed loops a pool surface focuses onto the floor.
                    float s  = _Time.y * _CausticsSpeed;
                    float cn = WarpFbm(ap * _CausticsScale + float2(s, -s * 0.4));
                    float web = saturate(1.0 - abs(cn - _CausticsCut) / _CausticsWidth);
                    col += web * web * _CausticsStrength * waterAlpha;
                }

                float2 v = (uv - 0.5) * 2.0;
                col *= 1.0 - saturate(dot(v, v) * 0.5) * _Vignette;

                col = lerp(col, _FoamColor.rgb, foam);
                return fixed4(col, alpha * i.color.a);
            }
            ENDCG
        }
    }
    FallBack Off
}
