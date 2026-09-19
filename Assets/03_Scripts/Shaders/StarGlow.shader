Shader "Custom/StarGlow"
{
    // Shader simple y emisivo para las estrellas. Expone Color (el tinte,
    // que va a variar según la pista asignada) e Intensity (el brillo, que
    // sube y baja con la energía de esa pista específica). Escrito a mano
    // en vez de Shader Graph, pero 100% compatible con URP.
    Properties
    {
        _BaseMap ("Base Map (opcional, textura suave)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Color;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 glow = tex.rgb * _Color.rgb * _GlowIntensity;
                // Alfa fijo en 1: con blend Additive, no necesitamos transparencia
                // variable — si el alfa llegaba a 0 por cualquier motivo (textura,
                // color, etc.), la estrella se volvía invisible. Así no puede pasar.
                return half4(glow, 1);
            }
            ENDHLSL
        }
    }
}
