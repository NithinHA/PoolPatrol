Shader "Unlit/RippleShader"
{
    Properties
    {
        _Damping ("Damping", Float) = 0.99
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _PrevRT;
            sampler2D _CurrentRT;
            half4 _CurrentRT_TexelSize;
            half _Damping;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half3 e = half3(_CurrentRT_TexelSize.xy, 0);
                half2 uv = i.uv;
                half speed = 1.0;

                half p10 = tex2D(_CurrentRT, uv - e.zy * speed).x;
                half p01 = tex2D(_CurrentRT, uv - e.xz * speed).x;
                half p21 = tex2D(_CurrentRT, uv + e.xz * speed).x;
                half p12 = tex2D(_CurrentRT, uv + e.zy * speed).x;

                half p11 = tex2D(_PrevRT, uv).x;

                half d = (p10 + p01 + p21 + p12) * 0.5 - p11;
                d *= _Damping;
                return half4(d, d, d, d);
            }
            ENDCG
        }
    }
}
