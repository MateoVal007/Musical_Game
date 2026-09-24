Shader "Custom/Flower"
{
    // Flores bioluminiscentes. No parpadean solas: TODAS se encienden juntas
    // cuando se dispara su canal, y después se apagan de a poco — como una
    // luciérnaga, pero disparada por el juego.
    //
    // Lo que se ilumina es el PÉTALO ENTERO, no solo unos puntitos: se va de
    // un azul casi negro a un azul intenso, más fuerte hacia la punta. Los
    // puntitos tipo fibra óptica son un adorno encima, no el efecto principal.
    //
    // Además cada flor escupe un puñado de MOTAS DE POLEN luminoso al
    // encenderse. Van en el mismo mesh, en un segundo pass aditivo, y se
    // animan solas con el mismo valor de destello: como el destello va de 1 a
    // 0, (1 - destello) sirve de reloj del estallido. El CPU no manda nada
    // más que el disparo.
    //
    // El destello lo maneja FlowerWave.cs: salta a 1 con cada disparo y decae.
    //
    // Vértices con COLOR: .r = gradiente del pétalo (0 base, 1 punta),
    // .g = posición en la fila (sin usar por ahora), .b = aleatorio por flor,
    // .a = 1 pétalo / 0.5 mota de polen / 0 tallo.
    // UV0 de la mota: .xy esquina del quad, .z aleatorio por mota.
    // UV1 de la mota: dirección de disparo (su largo es la velocidad).
    // Todo lo genera FlowerRowBuilder.cs.
    Properties
    {
        _BaseColor ("Color apagada", Color) = (0.02, 0.04, 0.16, 1)
        _TipColor ("Color encendida", Color) = (0.18, 0.48, 1, 1)
        _StemColor ("Color del tallo", Color) = (0.06, 0.12, 0.10, 1)

        _GlowIntensity ("Brillo al encenderse", Range(0, 30)) = 8
        _IdleGlow ("Brillo en reposo", Range(0, 2)) = 0.25

        _BaseLit ("Cuánto se ilumina también la base del pétalo", Range(0, 1)) = 0.35

        [Header(Disparador)]
        // 0 = canal A (las azules, con los perfectos)
        // 1 = canal B (las blancas del anillo, con la llegada del cometa)
        [Enum(A, 0, B, 1)] _FlashChannel ("Canal de destello", Float) = 0

        _SpeckleScale ("Escala de los puntitos", Float) = 120
        _SpeckleDensity ("Densidad de los puntitos", Range(0, 1)) = 0.1
        _SpeckleBoost ("Brillo extra de los puntitos", Range(0, 6)) = 1.2

        [Header(Estallido de polen)]
        // El color lo hereda de _TipColor: así el polen combina solo tanto con
        // las flores azules como con las blancas, sin configurar nada aparte.
        _MoteSize ("Tamaño de la mota", Range(0, 0.1)) = 0.018
        _MoteDistance ("Cuánto se alejan", Range(0, 3)) = 0.45
        _MoteFall ("Cuánto caen al final", Range(0, 1)) = 0.12
        _MoteGlow ("Brillo del polen", Range(0, 20)) = 6
        _MoteFalloff ("Dureza del núcleo", Range(0.5, 8)) = 2.5
        _MoteWhiteCore ("Núcleo blanco", Range(0, 1)) = 0.55

        [Header(Viento)]
        // Conviene dejar Direction, Speed y Scale iguales a los del pasto:
        // así la misma ráfaga recorre el pasto y las flores a la vez, en vez
        // de parecer dos vientos distintos soplando en el mismo lugar.
        _WindDirection ("Dirección del viento (XZ)", Vector) = (1, 0, 0.35, 0)
        _BaseLean ("Inclinación constante", Range(0, 1)) = 0.06
        _WindSpeed ("Velocidad de la ráfaga", Float) = 1.2
        _WindScale ("Escala espacial de la ráfaga", Float) = 0.25
        _WindStrength ("Fuerza de la ráfaga", Range(0, 2)) = 0.14
        _FlutterSpeed ("Velocidad del temblor", Float) = 5
        _FlutterStrength ("Fuerza del temblor", Range(0, 0.5)) = 0.02
    }

    SubShader
    {
        // Transparent-1: justo después de todo lo opaco y antes de la lluvia y
        // la niebla. Las motas se mezclan sin escribir profundidad, así que
        // necesitan que el escenario opaco YA esté dibujado; si quedaran en la
        // cola opaca, cualquier objeto lejano dibujado después las taparía.
        // Los pétalos siguen escribiendo profundidad, así que se ven igual.
        Tags { "RenderType"="Transparent" "Queue"="Transparent-1" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
            float4 uv0 : TEXCOORD0;   // mota: .xy esquina, .z aleatorio
            float3 uv1 : TEXCOORD1;   // mota: dirección de disparo
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float4 data : TEXCOORD1; // x gradiente, y tipo, zw esquina de la mota
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _TipColor;
            float4 _StemColor;
            float _GlowIntensity;
            float _IdleGlow;
            float _BaseLit;
            float _SpeckleScale;
            float _SpeckleDensity;
            float _SpeckleBoost;
            float4 _WindDirection;
            float _BaseLean;
            float _WindSpeed;
            float _WindScale;
            float _WindStrength;
            float _FlutterSpeed;
            float _FlutterStrength;
            float _FlashChannel;
            float _MoteSize;
            float _MoteDistance;
            float _MoteFall;
            float _MoteGlow;
            float _MoteFalloff;
            float _MoteWhiteCore;
        CBUFFER_END

        // Dos canales para que grupos distintos de flores respondan a eventos
        // distintos: A las azules con los perfectos, B las blancas con la
        // llegada del cometa. Los carga FlowerWave.cs.
        float _FlowerFlashA;
        float _FlowerFlashB;

        float FlowerFlash()
        {
            return saturate(lerp(_FlowerFlashA, _FlowerFlashB, _FlashChannel));
        }

        // 0 tallo, 0.5 mota de polen, 1 pétalo.
        bool IsMote(float a) { return abs(a - 0.5) < 0.2; }

        // Detrás del plano cercano Y con los tres vértices en el mismo punto:
        // el triángulo se recorta antes de llegar a los píxeles. Es como cada
        // pass ignora lo que no le toca.
        // Ojo con devolver w = 0: en las GPU del Quest la división por w da
        // NaN y el triángulo puede terminar dibujándose en cualquier lado.
        float4 Discarded() { return float4(0, 0, -10, 1); }

        float hashNoise(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }
        ENDHLSL

        // ---------------------------------------------------------------
        // Pass 1: la flor (tallo + pétalos). Opaca, escribe profundidad.
        // ---------------------------------------------------------------
        Pass
        {
            Name "Flower"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float gradient = IN.color.r;
                float isPetal = IN.color.a;

                OUT.data = float4(gradient, isPetal, 0, 0);

                // Las motas las dibuja el otro pass.
                if (IsMote(isPetal))
                {
                    OUT.positionHCS = Discarded();
                    OUT.positionWS = 0;
                    return OUT;
                }

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Los puntitos se calculan con la posición SIN viento, para
                // que queden pegados al pétalo en vez de nadar por encima
                // mientras la flor se mueve.
                OUT.positionWS = positionWS;

                // Altura a efectos del viento. El tallo se dobla a lo largo
                // (0 en la raíz, 1 en la cabeza), pero la cabeza de la flor es
                // RÍGIDA: todos los vértices del pétalo se desplazan lo mismo
                // que la punta del tallo. Si se doblaran según su propio
                // gradiente, los pétalos se separarían del tallo.
                float windHeight = isPetal > 0.5 ? (1.0 + gradient * 0.15) : gradient;
                float bend = windHeight * windHeight;

                float phase = IN.color.b * 6.28318;
                float spatial = positionWS.x + positionWS.z;

                float gust = sin(_Time.y * _WindSpeed + spatial * _WindScale + phase);
                float flutter = sin(_Time.y * _FlutterSpeed + phase * 3.7) * _FlutterStrength;
                float sway = (_BaseLean + gust * _WindStrength + flutter) * bend;

                float2 windDir = normalize(_WindDirection.xz + float2(1e-5, 1e-5));
                positionWS.xz += windDir * sway;
                positionWS.y -= sway * sway * 0.35;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float gradient = IN.data.x;

                // El tallo no se ilumina: si brillara se perdería la silueta
                // de flor y quedaría un palito luminoso.
                if (IN.data.y < 0.25) return half4(_StemColor.rgb, 1);

                float glow = FlowerFlash();

                // El pétalo entero cambia de color, no solo la punta.
                half3 petal = lerp(_BaseColor.rgb, _TipColor.rgb, gradient);

                // La punta brilla más que la base, pero la base también sube.
                float spread = lerp(_BaseLit, 1.0, gradient);
                float brightness = _IdleGlow + glow * _GlowIntensity * spread;

                half3 color = petal * brightness;

                // Puntitos tipo fibra óptica: adorno encima, atados al mismo
                // brillo para que nunca dominen como antes.
                float speckle = hashNoise(IN.positionWS * _SpeckleScale);
                float dots = step(1.0 - _SpeckleDensity, speckle) * gradient;
                color += _TipColor.rgb * dots * brightness * _SpeckleBoost;

                return half4(color, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // Pass 2: el estallido de polen. Aditivo, sin escribir profundidad.
        // ---------------------------------------------------------------
        Pass
        {
            Name "Pollen"
            // UniversalForwardOnly y no SRPDefaultUnlit: URP recorre los tags
            // en orden y este va último, así el polen se dibuja DESPUÉS de
            // los pétalos. Con el otro tag salía antes y los pétalos opacos
            // lo tapaban justo en el pico del destello, que es cuando las
            // motas todavía están pegadas a la flor.
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertMote
            #pragma fragment fragMote
            #pragma multi_compile_instancing

            Varyings vertMote(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.data = float4(0, IN.color.a, IN.uv0.xy);
                OUT.positionWS = 0;

                // Este pass dibuja SOLO las motas.
                if (!IsMote(IN.color.a))
                {
                    OUT.positionHCS = Discarded();
                    return OUT;
                }

                float flash = FlowerFlash();

                // El destello es el reloj del estallido: va de 1 a 0, así que
                // (1 - destello) va de 0 a 1. Como la curva de apagado cae
                // rápido al principio, el polen sale disparado y después frena
                // solo — justo lo que hace una nube de polvo de verdad.
                float burst = 1.0 - flash;

                float3 centerWS = TransformObjectToWorld(IN.positionOS.xyz);
                centerWS += IN.uv1 * _MoteDistance * burst;
                centerWS.y -= _MoteFall * burst * burst;

                // Tamaño proporcional al destello: en reposo el quad queda en
                // cero y el triángulo se descarta, así que cuando no hay
                // estallido el polen no cuesta un solo píxel.
                float moteRandom = IN.uv0.z;
                float size = _MoteSize * (0.6 + moteRandom * 0.8) * flash;

                // Billboard contra la cámara, en espacio de mundo: en VR tiene
                // que quedar de frente a los dos ojos por igual.
                float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
                float3 camUp = UNITY_MATRIX_V._m10_m11_m12;
                float3 positionWS = centerWS + (camRight * IN.uv0.x + camUp * IN.uv0.y) * size;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            half4 fragMote(Varyings IN) : SV_Target
            {
                // Disco suave: núcleo fuerte y borde que se desvanece, para
                // que no se note que en realidad es un cuadradito.
                float d = length(IN.data.zw);
                float core = pow(saturate(1.0 - d), _MoteFalloff);

                float flash = FlowerFlash();

                // El centro quema hacia el blanco y el borde se queda con el
                // color del grupo: azul en las flores azules, cálido en las
                // del anillo. Así el polen combina solo, sin configurar nada.
                half3 color = lerp(_TipColor.rgb, half3(1, 1, 1), _MoteWhiteCore * core);

                return half4(color * core * _MoteGlow * flash, core);
            }
            ENDHLSL
        }
    }
}
