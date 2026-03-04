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
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _DotSize)
                UNITY_DEFINE_INSTANCED_PROP(float, _RingDistance)
                UNITY_DEFINE_INSTANCED_PROP(float, _RingWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _RingCrossCutout)
                UNITY_DEFINE_INSTANCED_PROP(float, _LineWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _LineLengthMax)
                UNITY_DEFINE_INSTANCED_PROP(float, _LineLengthMin)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float createCircles(v2f i)
            {
                float dotSize = UNITY_ACCESS_INSTANCED_PROP(Props, _DotSize);
                float ringDist = UNITY_ACCESS_INSTANCED_PROP(Props, _RingDistance);
                float ringWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _RingWidth);
                float ringCutout = UNITY_ACCESS_INSTANCED_PROP(Props, _RingCrossCutout);

                float centralDot = step(distance(i.uv, float2(0.5, 0.5)), dotSize);
                float ringOuter = step(distance(i.uv, float2(0.5, 0.5)), ringDist);
                float ringInner = step(distance(i.uv, float2(0.5, 0.5)), ringDist - ringWidth);

                float crossCutoutHorizontal = 1.0 - step(abs(i.uv.x - 0.5), ringCutout);
                float crossCutoutVertical = 1.0 - step(abs(i.uv.y - 0.5), ringCutout);

                return saturate(centralDot + (ringOuter - ringInner) * (crossCutoutHorizontal * crossCutoutVertical));
            }

            float createLines(v2f i)
            {
                float lineLenMax = UNITY_ACCESS_INSTANCED_PROP(Props, _LineLengthMax);
                float lineLenMin = UNITY_ACCESS_INSTANCED_PROP(Props, _LineLengthMin);
                float lineWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _LineWidth);

                float radialDist = distance(i.uv, float2(0.5, 0.5));
                float lineLengthMask = step(radialDist, lineLenMax) - step(radialDist, lineLenMin); // Band mask

                float lineX = step(abs(i.uv.x - 0.5), lineWidth);
                float lineY = step(abs(i.uv.y - 0.5), lineWidth);

                return saturate(lineX + lineY) * lineLengthMask;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 col = tex2D(_MainTex, i.uv) * (half4)UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                col.a = saturate(createCircles(i) + createLines(i));
                return col;
            }
            ENDCG
        }
    }
}
