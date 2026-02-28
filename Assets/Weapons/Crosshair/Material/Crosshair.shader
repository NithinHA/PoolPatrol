Shader "Unlit/Crosshair"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Header(Dot)]
        _DotSize ("Dot size", Range(0,.2)) = .1
        [Space(6)]
        [Header(Ring)]
        _RingWidth ("Ring Width", Range(0, .5)) = .1
        _RingDistance ("Ring Distance", Range(0, .5)) = .4
        _RingCrossCutout ("Ring Cross Cutout", Range(0, .4)) = .1
        [Space(6)]
        [Header(Lines)]
        _LineWidth ("Line Width", Range(0, .1)) = .1
        _LineLengthMin ("Line Length Min", Range(0, .5)) = .1
        _LineLengthMax ("Line Length Max", Range(0, .5)) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _DotSize;
            float _RingDistance;
            float _RingWidth;
            float _RingCrossCutout;
            float _LineWidth;
            float _LineLengthMax;
            float _LineLengthMin;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float createCircles(v2f i)
            {
                float centralDot = step(distance(i.uv, fixed2(.5,.5)), _DotSize);
                float ringOuter = step(distance(i.uv, fixed2(.5,.5)), _RingDistance);
                float ringInner = step(distance(i.uv, fixed2(.5,.5)), _RingDistance - _RingWidth);

                float crossCutoutHorizontal = 1 - step(distance(i.uv.x, fixed2(.5,.5)), _RingCrossCutout);
                float crossCutoutVertical = 1 - step(distance(i.uv.y, fixed2(.5,.5)), _RingCrossCutout);

                return centralDot + (ringOuter - ringInner) * (crossCutoutHorizontal && crossCutoutVertical);
            }

            float createLines(v2f i)
            {
                float radialDist = distance(i.uv, float2(.5, .5));
                float lineLengthMask = step(radialDist, _LineLengthMax) - step(radialDist, _LineLengthMin); // Band mask

                // abs(uv.x - 0.5) is the correct 1D distance from the vertical center axis
                float lineX = step(abs(i.uv.x - 0.5), _LineWidth);
                float lineY = step(abs(i.uv.y - 0.5), _LineWidth);

                return saturate(lineX + lineY) * lineLengthMask;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a = createCircles(i) + createLines(i);
                return col;
            }
            ENDCG
        }
    }
}
