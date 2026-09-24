Shader "Custom/Flower"
{
    // Flores bioluminiscentes azules. No parpadean solas: TODAS se encienden
    // juntas cuando el jugador clava un perfecto, y después se apagan de a
    // poco — como una luciérnaga, pero disparada por el juego.
    //
    // Lo que se ilumina es el PÉTALO ENTERO, no solo unos puntitos: se va de
    // un azul casi negro a un azul intenso, más fuerte hacia la punta. Los
    // puntitos tipo fibra óptica son un adorno encima, no el efecto principal.
    //
    // _FlowerFlash (0 a 1) lo maneja FlowerWave.cs: salta a 1 con cada
    // perfecto y decae solo.
    //
    // Vértices con COLOR: .r = gradiente del pétalo (0 base, 1 punta),
    // .g = posición en la fila (sin usar por ahora), .b = aleatorio por flor,
    // .a = 1 pétalo / 0 tallo. Los genera FlowerRowBuilder.cs.
    Properties
    {
        _BaseColor ("Color apagada", Color) = (0.02, 0.04, 0.16, 1)
        _TipColor ("Color encendida", Color) = (0.18, 0.48, 1, 1)
        _StemColor ("Color del tallo", Color) = (0.06, 0.12, 0.10, 1)

        _GlowIntensity ("Brillo al encenderse", Range(0, 30)) = 8
        _IdleGlow ("Brillo en reposo", Range(0, 2)) = 0.25

        _BaseLit ("Cuánto se ilumina también la base del pétalo", Range(0, 1)) = 0.35

        _SpeckleScale ("Escala de los puntitos", Float) = 120
        _SpeckleDensity ("Densidad de los puntitos", Range(0, 1)) = 0.1
        _SpeckleBoost ("Brillo extra de los puntitos", Range(0, 6)) = 1.2

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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 data : TEXCOORD1; // x = gradiente del pétalo, y = 1 pétalo / 0 tallo
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
            CBUFFER_END

            // 0 = apagadas, 1 = destello máximo. Lo carga FlowerWave.cs.
            float _FlowerFlash;

            float hashNoise(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float gradient = IN.color.r;
                float isPetal = IN.color.a;

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
                OUT.data = float2(gradient, isPetal);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float gradient = IN.data.x;

                // El tallo no se ilumina: si brillara se perdería la silueta
                // de flor y quedaría un palito luminoso.
                if (IN.data.y < 0.5) return half4(_StemColor.rgb, 1);

                float glow = max(_FlowerFlash, 0.0);

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
    }
}
