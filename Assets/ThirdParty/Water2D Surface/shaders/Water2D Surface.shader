Shader "URP/ShowOpaqueBackground"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.screenUV = OUT.positionHCS.xy / OUT.positionHCS.w * 0.5 + 0.5;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 bg = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, IN.screenUV);
                return bg * _Color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
