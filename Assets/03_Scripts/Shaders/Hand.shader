Shader "Custom/Hand"
{
    // Piel para las manos del jugador. Como toda la escena es unlit, este
    // shader se fabrica su propia iluminación (misma idea que Custom/Moon):
    // sin eso, una mano de un solo color plano se ve como una silueta de
    // cartón y no se le entiende la forma.
    //
    //   - Terminador: una dirección de luz inventada da un lado más
    //     iluminado que el otro, para que se lea el volumen de los dedos.
    //   - Borde con luz azul (_RimColor): el contorno agarra el tono del
    //     ambiente nocturno, que es lo que hace que la mano pertenezca a
    //     esta escena y no parezca pegada encima.
    Properties
    {
        _BaseColor ("Color de piel", Color) = (0.42, 0.26, 0.18, 1)
        _Brightness ("Brillo", Range(0, 4)) = 1

        _LightDirection ("Dirección de la luz inventada", Vector) = (-0.4, 0.6, -0.7, 0)
        _ShadowStrength ("Fuerza del sombreado", Range(0, 1)) = 0.55
        _ShadowSoftness ("Suavidad del sombreado", Range(0.01, 1)) = 0.75
        _AmbientFloor ("Piso de luz ambiente", Range(0, 1)) = 0.25

        // Rim sutil a propósito: un borde fuerte hincha la silueta y hace que
        // la mano parezca de goma. Es apenas un reflejo del ambiente, no un aura.
        _RimColor ("Color del borde", Color) = (0.30, 0.45, 1, 1)
        _RimStrength ("Fuerza del borde", Range(0, 5)) = 0.2
        _RimPower ("Finura del borde", Range(0.5, 8)) = 5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Brightness;
                float4 _LightDirection;
                float _ShadowStrength;
                float _ShadowSoftness;
                float _AmbientFloor;
                float4 _RimColor;
                float _RimStrength;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(_LightDirection.xyz + float3(1e-5, 1e-5, 1e-5));
                float facing = saturate(dot(normalWS, IN.viewDirWS));

                float ndotl = dot(normalWS, lightDir) * 0.5 + 0.5;
                float lit = smoothstep(0.5 - _ShadowSoftness * 0.5, 0.5 + _ShadowSoftness * 0.5, ndotl);

                // El piso de ambiente evita que la parte en sombra quede negra
                // — la piel oscura sin esto pierde toda la forma en la sombra.
                float lightFactor = lerp(1.0, max(lit, _AmbientFloor), _ShadowStrength);

                half3 color = _BaseColor.rgb * _Brightness * lightFactor;

                float rim = pow(1.0 - facing, _RimPower);
                color += _RimColor.rgb * rim * _RimStrength;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
