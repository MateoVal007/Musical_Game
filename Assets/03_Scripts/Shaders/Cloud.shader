Shader "Custom/Cloud"
{
    // Nube "de mentira": color base + fresnel (más brillante en el borde,
    // como si tuviera volumen/niebla en vez de ser una esfera sólida) +
    // ruido suave para romper la uniformidad. Pensado para una esfera
    // achatada (scale Y chico) — la plataforma donde para el jugador.
    //
    // Onda expansiva (shockwave): cuando llega un cometa, CloudShockwave.cs
    // fija _RippleOrigin (posición mundo del impacto) y anima _RippleTime
    // desde 0 — un anillo brillante se expande desde ese punto sobre la
    // superficie de la nube y se apaga solo. _RippleTime < 0 = sin onda activa.
    Properties
    {
        _BaseColor ("Color base", Color) = (0.75, 0.76, 0.95, 1)
        _RimColor ("Color del borde (fresnel)", Color) = (0.88, 0.85, 1, 1)
        _RimPower ("Fuerza del fresnel", Range(0.5, 8)) = 3
        _GlowIntensity ("Brillo general", Range(0, 5)) = 1.2
        _NoiseScale ("Escala del ruido", Range(0.5, 20)) = 4
        _NoiseStrength ("Fuerza del ruido", Range(0, 1)) = 0.2

        _RippleOrigin ("Origen de la onda (mundo)", Vector) = (0, 0, 0, 0)
        _RippleTime ("Tiempo desde el golpe (seg, -1 = sin onda)", Float) = -1
        _RippleSpeed ("Velocidad de expansión (unidades de mundo / seg)", Range(0.1, 20)) = 1.5
        _RippleWidth ("Ancho del anillo", Range(0.05, 2)) = 0.6
        _RippleMaxRadius ("Radio máximo", Range(0.5, 20)) = 3
        _RippleColor ("Color de la onda", Color) = (1, 1, 1, 1)
        _RippleIntensity ("Brillo de la onda", Range(0, 10)) = 4
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _GlowIntensity;
                float _NoiseScale;
                float _NoiseStrength;

                float4 _RippleOrigin;
                float _RippleTime;
                float _RippleSpeed;
                float _RippleWidth;
                float _RippleMaxRadius;
                float4 _RippleColor;
                float _RippleIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(OUT.positionWS);
                return OUT;
            }

            // Ruido simple basado en hash, no necesita textura (mismo truco que NoteDissolve).
            float hashNoise(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, IN.viewDirWS)), _RimPower);

                float n = hashNoise(IN.positionWS * _NoiseScale);
                float noiseTint = lerp(1.0, n, _NoiseStrength);

                half3 color = lerp(_BaseColor.rgb, _RimColor.rgb, fresnel) * _GlowIntensity * noiseTint;

                if (_RippleTime >= 0)
                {
                    float dist = distance(IN.positionWS, _RippleOrigin.xyz);
                    float radius = _RippleTime * _RippleSpeed;
                    float ring = 1.0 - smoothstep(0.0, _RippleWidth, abs(dist - radius));
                    float fade = saturate(1.0 - radius / _RippleMaxRadius);
                    color += _RippleColor.rgb * _RippleIntensity * ring * fade;
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
