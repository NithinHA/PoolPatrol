Shader "Custom/PlasmaWorldPos"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Effect scale", float) = 1
        _PlasmaEffectMultiplier ("Plasma effect multiplier", Range(0,1)) = .01
        _Timescale ("Timescale", float) = 1
        _Color1 ("Plasma color 1", Color) = (1,0,0)
        _Color2 ("Plasma color 2", Color) = (0,1,0)
        _WorldUVScale ("World uv scale", float) = .1
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

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PlasmaEffectMultiplier;
            float _Scale;
            float _Timescale;
            fixed4 _Color1;
            fixed4 _Color2;
            float _WorldUVScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed3 Plasma(fixed2 uv)
            {
                uv = uv * _Scale - _Scale / 2;
                float time = _Time.y * _Timescale;
                // horizontal wave
                float w1 = sin(uv.x + time);
                // vertical wave
                float w2 = sin(uv.y + time) * .5;
                // diagonal wave
                float w3 = sin(uv.x + uv.y + time);
                // radial wave
                float r = sin(sqrt(pow(uv.x,2) + pow(uv.y,2)) + time);  // r^2 = x^2 + y^2

                float finalVal = r + w1 + w2 + w3;
                float3 finalWave = float3(sin(finalVal * UNITY_PI), cos(finalVal * UNITY_PI), 0);
                return finalWave * .5 + .5;    // converting [-0.5,0.5] to [0,1]
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 worldUV = i.worldPos.xy * _WorldUVScale;
                fixed3 plasma = Plasma(worldUV);
                fixed4 col = tex2D(_MainTex, worldUV + plasma.rg * _PlasmaEffectMultiplier);
                fixed4 plasmaColor = _Color1 * plasma.r + _Color2 * plasma.g;
                col *= plasmaColor;
                return col;
            }

            ENDCG
        }
    }
}
