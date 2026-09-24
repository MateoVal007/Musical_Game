Shader "Custom/Firefly"
{
    // Luciérnagas: puntos de luz que flotan despacio y se prenden/apagan por
    // su cuenta, cada una con su propio ritmo (NO con la música — como los
    // bichos de verdad).
    //
    // El parpadeo es pow(sin, exponente alto): eso da destellos cortos y
    // brillantes con pausas largas de oscuridad, que es el patrón real de una
    // luciérnaga. Un sin pelado daría un latido suave y constante, que se ve
    // a lámpara, no a bicho.
    //
    // Para que se sienta LUZ y no un círculo de color, tres cosas:
    //   - Dos capas: un núcleo chiquito y durísimo + un halo ancho y tenue.
    //     Una luz real tiene las dos; un solo degradado se ve a sticker.
    //   - El núcleo se va a blanco al destellar. Una luz fuerte satura el ojo
    //     (y la cámara) hacia el blanco, con el color quedando en los bordes.
    //   - El quad crece con el destello: la luz intensa "ocupa" más espacio.
    // El resto lo tiene que poner el Bloom del Volume — sin eso no derrama.
    //
    // Espera: POSITION = centro de la luciérnaga (los 4 vértices comparten el
    // mismo centro), UV0 = esquina del quad (-1..1), COLOR = .r fase de
    // parpadeo, .g velocidad, .b fase de deriva, .a tamaño. Los genera
    // FireflyFieldMeshBuilder.cs.
    Properties
    {
        _Color ("Color", Color) = (0.05, 0.12, 1, 1)
        _GlowIntensity ("Brillo del destello", Range(0, 40)) = 18
        _MinGlow ("Brillo mínimo (nunca se apagan del todo)", Range(0, 1)) = 0.04

        _BlinkSpeed ("Velocidad base del parpadeo", Float) = 1.1
        _BlinkSharpness ("Qué tan corto es el destello", Range(1, 40)) = 14

        _CoreSharpness ("Qué tan chico es el núcleo", Range(1, 30)) = 10
        _CoreWhiteness ("Cuánto se va a blanco el núcleo", Range(0, 1)) = 0.75
        _HaloFalloff ("Suavidad del halo", Range(0.3, 4)) = 1.2
        _HaloStrength ("Fuerza del halo", Range(0, 2)) = 0.55

        _Size ("Tamaño", Float) = 0.06
        _GlowSizeBoost ("Cuánto crece al destellar", Range(1, 4)) = 2.2
        _DriftSpeed ("Velocidad de la deriva", Float) = 0.35
        _DriftRadius ("Cuánto se alejan flotando", Float) = 0.5

        // Lo maneja FireflyGathering.cs en runtime, no hace falta tocarlo a mano.
        _GatherTarget ("Punto de reunión (mundo)", Vector) = (0, 0, 0, 0)
        _GatherAmount ("Cuánto se juntaron (0-1)", Range(0, 1)) = 0
        _GatherRadius ("Radio de la nube alrededor del punto", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float glow : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _GlowIntensity;
                float _MinGlow;
                float _BlinkSpeed;
                float _BlinkSharpness;
                float _CoreSharpness;
                float _CoreWhiteness;
                float _HaloFalloff;
                float _HaloStrength;
                float _Size;
                float _GlowSizeBoost;
                float _DriftSpeed;
                float _DriftRadius;
                float4 _GatherTarget;
                float _GatherAmount;
                float _GatherRadius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float blinkPhase = IN.color.r * 6.28318;
                float speedVar = 0.6 + IN.color.g * 0.8; // cada una parpadea a su ritmo
                float driftPhase = IN.color.b * 6.28318;
                float sizeVar = 0.6 + IN.color.a * 0.8;

                float3 centerWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Reunión alrededor de un punto (la luna). Cada luciérnaga va
                // a un lugar distinto de una esfera alrededor del objetivo —
                // usando sus propios números aleatorios como dirección — para
                // que formen una nube y no se apilen todas en el mismo punto.
                float3 gatherDir = normalize(float3(
                    IN.color.r - 0.5,
                    IN.color.b - 0.5,
                    IN.color.g - 0.5) + 1e-5);
                float3 gatherPos = _GatherTarget.xyz + gatherDir * _GatherRadius;
                centerWS = lerp(centerWS, gatherPos, _GatherAmount);

                // Deriva: tres senos de distinta frecuencia por eje — nunca
                // repite el mismo recorrido, se siente errático como un bicho.
                // Se suma DESPUÉS de la reunión, así siguen revoloteando
                // alrededor de la luna en vez de quedarse clavadas.
                float t = _Time.y * _DriftSpeed;
                centerWS += float3(
                    sin(t + driftPhase),
                    sin(t * 1.37 + driftPhase * 2.1) * 0.6,
                    sin(t * 0.83 + driftPhase * 3.3)) * _DriftRadius;

                float blink = sin(_Time.y * _BlinkSpeed * speedVar + blinkPhase) * 0.5 + 0.5;
                blink = pow(blink, _BlinkSharpness);
                float glow = max(blink, _MinGlow);

                // Billboard: las filas de la matriz de vista dan los ejes de
                // la cámara en mundo, así el quad siempre mira al jugador.
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float size = _Size * sizeVar * lerp(1.0, _GlowSizeBoost, glow);
                float3 positionWS = centerWS + (camRight * IN.uv.x + camUp * IN.uv.y) * size;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv;
                OUT.glow = glow;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float d = saturate(1.0 - length(IN.uv));

                float core = pow(d, _CoreSharpness);
                float halo = pow(d, _HaloFalloff) * _HaloStrength;
                float shape = core + halo;

                // El núcleo se blanquea al destellar; el color queda en el halo.
                half3 tint = lerp(_Color.rgb, half3(1, 1, 1), core * _CoreWhiteness * IN.glow);

                half3 color = tint * _GlowIntensity * IN.glow * shape;
                return half4(color, saturate(shape * IN.glow));
            }
            ENDHLSL
        }
    }
}
