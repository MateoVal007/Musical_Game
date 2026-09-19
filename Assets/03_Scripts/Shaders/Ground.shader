Shader "Custom/Ground"
{
    // Piso tipo asfalto urbano nocturno: color base oscuro con ruido sutil
    // (para que no sea un plano perfectamente liso) + niebla por distancia
    // que lo disuelve hacia _FogColor a medida que se aleja de la cámara —
    // así los objetos apoyados en el piso (postes, farol, edificio) tienen
    // una base sólida cerca, pero se pierde en la niebla lejos, sin corte
    // brusco. _FogColor debería matchear _HorizonColor de SkyGradient.
    Properties
    {
        _BaseColor ("Color base (asfalto)", Color) = (0.11, 0.11, 0.16, 1)
        _NoiseScale ("Escala del ruido", Range(1, 50)) = 15
        _NoiseStrength ("Fuerza del ruido", Range(0, 1)) = 0.15

        _FogColor ("Color de niebla (matchear horizonte del cielo)", Color) = (0.33, 0.35, 0.53, 1)
        _FogStart ("Distancia donde empieza la niebla", Float) = 8
        _FogEnd ("Distancia donde ya es 100% niebla", Float) = 30

        [Header(Asfalto mojado)]
        _WetStrength ("Fuerza de los reflejos", Range(0, 4)) = 1.2
        _WetLength ("Largo del reguero", Float) = 14
        _WetWidth ("Ancho del reguero", Float) = 0.7
        _WetSharpness ("Nitidez del reflejo", Range(0.5, 6)) = 2
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _NoiseScale;
                float _NoiseStrength;

                float4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _WetStrength;
                float _WetLength;
                float _WetWidth;
                float _WetSharpness;
            CBUFFER_END

            // Fuentes de reflejo (farol, postes, luna). Las carga
            // WetFloorReflections.cs como variables globales, por eso van
            // FUERA del CBUFFER de material.
            #define MAX_WET_LIGHTS 4
            float4 _WetLightPositions[MAX_WET_LIGHTS];
            float4 _WetLightColors[MAX_WET_LIGHTS];
            float _WetLightCount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            float hashNoise(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // Reflejo de una luz puntual sobre una superficie mojada plana.
            // No es un reflejo real: es la forma que TIENE un reflejo así —
            // un reguero que nace en la base de la luz, apunta hacia el
            // observador y se va abriendo y apagando a lo largo del camino.
            float WetStreak(float3 positionWS, float3 cameraWS, float3 lightWS)
            {
                float2 toCam = cameraWS.xz - lightWS.xz;
                float camDist = length(toCam);
                if (camDist < 1e-4) return 0.0;
                float2 dir = toCam / camDist;

                float2 d = positionWS.xz - lightWS.xz;
                float along = dot(d, dir);
                if (along < 0.0) return 0.0; // el reguero va hacia el observador, no al revés

                float across = length(d - dir * along);

                float head = saturate(along / max(_WetLength, 0.001));
                float fadeAlong = 1.0 - head;

                // Se ensancha a medida que se aleja de la luz.
                float width = _WetWidth * (0.35 + head * 1.6);
                float fadeAcross = saturate(1.0 - across / max(width, 0.001));

                float streak = pow(fadeAlong, _WetSharpness) * pow(fadeAcross, _WetSharpness);

                // Una luz más alta da un reflejo más difuso y tenue.
                return streak / (1.0 + max(lightWS.y, 0.0) * 0.18);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float n = hashNoise(IN.positionWS * _NoiseScale);
                half3 color = _BaseColor.rgb * lerp(1.0, n, _NoiseStrength);

                float3 cameraWS = GetCameraPositionWS();

                int count = (int)_WetLightCount;
                for (int i = 0; i < MAX_WET_LIGHTS; i++)
                {
                    if (i >= count) break;
                    float streak = WetStreak(IN.positionWS, cameraWS, _WetLightPositions[i].xyz);
                    // El ruido del asfalto también rompe el reflejo, como en la vida real.
                    color += _WetLightColors[i].rgb * streak * _WetStrength * lerp(0.6, 1.0, n);
                }

                float dist = distance(IN.positionWS, GetCameraPositionWS());
                float fogAmount = saturate((dist - _FogStart) / max(_FogEnd - _FogStart, 0.001));
                color = lerp(color, _FogColor.rgb, fogAmount);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
