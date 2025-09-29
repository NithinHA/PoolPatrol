Shader "Custom/ReflectionDistortionURP"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _DistortionMap("Distortion Noise", 2D) = "gray" {}
        _DistortionStrength("Distortion Strength", Float) = 0.03
        _DistortionSpeed("Distortion Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            TEXTURE2D(_DistortionMap);
            SAMPLER(sampler_DistortionMap);
            float4 _DistortionMap_ST;

            float _DistortionStrength;
            float _DistortionSpeed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 noiseUV = IN.uv;
                noiseUV.y += _Time * _DistortionSpeed;

                float2 distortion = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, noiseUV).rg;
                distortion = (distortion - 0.5) * 2.0; // Normalize to -1..1

                float2 distortedUV = IN.uv + distortion * _DistortionStrength;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                return col * IN.color;
            }
            ENDHLSL
        }
    }
}
