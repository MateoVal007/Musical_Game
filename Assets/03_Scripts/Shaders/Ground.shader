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

        [Header(Ondas de lluvia)]
        _RainRippleStrength ("Fuerza de las ondas", Range(0, 3)) = 0.8
        _RainRippleColor ("Color de las ondas", Color) = (0.7, 0.78, 1, 1)
        _RainRippleCellSize ("Separación entre gotas", Float) = 1.8
        _RainRippleMaxRadius ("Radio máximo de la onda", Float) = 0.5
        _RainRippleWidth ("Grosor del anillo", Float) = 0.05
        _RainRipplePeriod ("Segundos entre gota y gota", Float) = 1.6
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
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
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
                float _RainRippleStrength;
                float4 _RainRippleColor;
                float _RainRippleCellSize;
                float _RainRippleMaxRadius;
                float _RainRippleWidth;
                float _RainRipplePeriod;
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
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
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

            // Una onda de lluvia por celda de una grilla invisible. La celda
            // decide, con su propia coordenada, dónde cae la gota y en qué
            // momento del ciclo está — así cada una va por su lado sin que
            // nadie tenga que llevar una lista de gotas.
            //
            // La gota se mantiene a _RainRippleMaxRadius de los bordes de su
            // celda, así la onda nunca se sale: con eso alcanza con mirar UNA
            // celda por fragmento en vez de las 9 vecinas.
            float RainRipple(float2 p, float2 gridOffset)
            {
                float cellSize = max(_RainRippleCellSize, 0.001);
                float2 uv = (p + gridOffset) / cellSize;
                float2 cell = floor(uv);
                float2 local = frac(uv) * cellSize;

                float h1 = hashNoise(float3(cell, 0.0));
                float h2 = hashNoise(float3(cell, 7.3));
                float h3 = hashNoise(float3(cell, 19.1));

                // Margen para que la onda entre entera en la celda.
                float margin = min(_RainRippleMaxRadius, cellSize * 0.45);
                float2 center = float2(lerp(margin, cellSize - margin, h1),
                                       lerp(margin, cellSize - margin, h2));

                float period = max(_RainRipplePeriod, 0.01) * (0.7 + h3 * 0.6);
                float t = frac((_Time.y + h3 * period * 10.0) / period);

                float radius = t * margin;
                float d = distance(local, center);

                float ring = 1.0 - smoothstep(0.0, max(_RainRippleWidth, 0.001), abs(d - radius));
                float fade = 1.0 - t; // se apaga a medida que se abre
                return ring * fade * fade;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float n = hashNoise(IN.positionWS * _NoiseScale);
                half3 color = _BaseColor.rgb * lerp(1.0, n, _NoiseStrength);

                // Dos grillas desfasadas media celda: duplica la densidad y
                // rompe el patrón regular, por el costo de una sola celda más.
                float ripples = RainRipple(IN.positionWS.xz, 0.0)
                              + RainRipple(IN.positionWS.xz, _RainRippleCellSize * 0.5);
                color += _RainRippleColor.rgb * ripples * _RainRippleStrength;

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
