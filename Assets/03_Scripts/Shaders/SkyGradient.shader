Shader "Custom/SkyGradient"
{
    // Degradado simple de cielo (horizonte -> arriba), aplicado a una esfera
    // grande que envuelve toda la escena desde adentro. No reemplaza el
    // Skybox de Unity ni afecta a las estrellas (son objetos aparte, se
    // dibujan encima de esto sin problema).
    Properties
    {
        _HorizonColor ("Color del horizonte", Color) = (0.33, 0.35, 0.53, 1)
        _ZenithColor ("Color de arriba", Color) = (0.24, 0.25, 0.4, 1)
        _GradientExponent ("Curva del degradado", Range(0.2, 4)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOSInterp : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HorizonColor;
                float4 _ZenithColor;
                float _GradientExponent;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOSInterp = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Y normalizado de -1 (horizonte/abajo) a 1 (arriba), mapeado a 0-1.
                float t = saturate(normalize(IN.positionOSInterp).y * 0.5 + 0.5);
                t = pow(t, _GradientExponent);

                half3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, t);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
