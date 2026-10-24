Shader "PoolPatrol/WaterlineRing"
{
    // Procedural rings where a floating object meets the water: a bright, slightly broken contact
    // line hugging the object, plus a few rings that keep spreading out and fading. Fully procedural
    // from the quad's uv, so it is drawn on a full-rect SpriteRenderer and batches with its siblings.
    // SpriteRenderer.color: r = phase offset (so neighbours don't pulse in sync), a = intensity.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (unused, needs Full Rect mesh)", 2D) = "white" {}
        _RingColor ("Ring Color", Color) = (1, 1, 1, 1)
        _ContactRadius ("Contact Radius (0-1 of quad)", Range(0.1, 0.9)) = 0.5
        _ContactWidth ("Contact Width", Range(0.005, 0.2)) = 0.045
        _ContactAlpha ("Contact Alpha", Range(0, 1)) = 0.75
        _ContactBreakup ("Contact Breakup", Range(0, 1)) = 0.5
        _RingCount ("Spreading Rings", Range(0, 4)) = 2
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.035
        _RingAlpha ("Ring Alpha", Range(0, 1)) = 0.55
        _RingSpeed ("Ring Speed (cycles/s)", Float) = 0.45
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RingColor;
                float _ContactRadius, _ContactWidth, _ContactAlpha, _ContactBreakup;
                float _RingCount, _RingWidth, _RingAlpha, _RingSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.uv * 2.0 - 1.0;
                float r = length(c);
                float ang = atan2(c.y, c.x);
                float phase0 = i.color.r;
                float t = _Time.y;

                // Contact line, brightened and thinned in a few lobes that slowly turn.
                float lobes = 0.5 + 0.5 * sin(ang * 5.0 + t * 1.7 + phase0 * 6.28) * sin(ang * 3.0 - t * 1.1);
                float cw = _ContactWidth * (1.0 - _ContactBreakup * 0.6 * lobes);
                float contact = exp(-pow((r - _ContactRadius) / cw, 2.0)) * lerp(1.0, lobes, _ContactBreakup);
                contact *= _ContactAlpha;

                // Rings spreading from the contact line to the quad edge, fading as they go.
                float rings = 0.0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    if (k >= _RingCount) break;
                    float ph = frac(t * _RingSpeed + phase0 + k / max(_RingCount, 1.0));
                    float rr = lerp(_ContactRadius, 1.0, ph);
                    float fade = (1.0 - ph) * (1.0 - ph) * smoothstep(0.0, 0.15, ph);
                    rings += exp(-pow((r - rr) / _RingWidth, 2.0)) * fade;
                }
                rings *= _RingAlpha;

                half a = saturate(contact + rings) * i.color.a * _RingColor.a;
                a *= 1.0 - smoothstep(0.92, 1.0, r);
                return half4(_RingColor.rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
