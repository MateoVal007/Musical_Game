Shader "Custom/Moon"
{
    // Luna. Como toda la escena es unlit (no hay luces reales), este shader
    // se fabrica la iluminación solo — que es justo lo que le falta a un
    // modelo con material plano para no verse como una calcomanía redonda:
    //
    //   - Terminador: una dirección de luz inventada (_LightDirection) hace
    //     que un lado esté más iluminado que el otro. Esto es lo que hace que
    //     el ojo lea "esfera" y no "círculo".
    //   - Oscurecimiento del limbo: el borde del disco se apaga un poco, como
    //     en las fotos reales de la luna.
    //   - Borde con luz (_RimColor): un halo finito en el contorno que la
    //     amarra visualmente con el aura azul de alrededor.
    //
    // _Brightness puede pasar de 1 para que el Bloom la agarre y derrame.
    // Funciona con o sin textura: sin una asignada, queda blanca lisa.
    Properties
    {
        _BaseMap ("Textura de la luna", 2D) = "white" {}
        _Color ("Tinte", Color) = (0.85, 0.88, 1, 1)
        _Brightness ("Brillo", Range(0, 8)) = 1.6

        _LightDirection ("Dirección de la luz inventada", Vector) = (-0.5, 0.35, -1, 0)
        _ShadowStrength ("Fuerza del terminador", Range(0, 1)) = 0.4
        _ShadowSoftness ("Suavidad del terminador", Range(0.01, 1)) = 0.6

        _LimbDarkening ("Oscurecimiento del borde", Range(0, 1)) = 0.35

        _RimColor ("Color del borde iluminado", Color) = (0.25, 0.45, 1, 1)
        _RimStrength ("Fuerza del borde", Range(0, 5)) = 0.8
        _RimPower ("Finura del borde", Range(0.5, 8)) = 3
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Color;
                float _Brightness;
                float4 _LightDirection;
                float _ShadowStrength;
                float _ShadowSoftness;
                float _LimbDarkening;
                float4 _RimColor;
                float _RimStrength;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(_LightDirection.xyz + float3(1e-5, 1e-5, 1e-5));
                float facing = saturate(dot(normalWS, IN.viewDirWS));

                // Terminador suave: medio lado más iluminado que el otro.
                float ndotl = dot(normalWS, lightDir) * 0.5 + 0.5;
                float lit = smoothstep(0.5 - _ShadowSoftness * 0.5, 0.5 + _ShadowSoftness * 0.5, ndotl);
                float lightFactor = lerp(1.0, lit, _ShadowStrength);

                // El borde del disco se apaga — sin esto se ve chata.
                float limb = lerp(1.0, pow(facing, 0.45), _LimbDarkening);

                half3 color = tex.rgb * _Color.rgb * _Brightness * lightFactor * limb;

                // Contorno iluminado, para engancharla con el aura.
                float rim = pow(1.0 - facing, _RimPower);
                color += _RimColor.rgb * rim * _RimStrength;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
