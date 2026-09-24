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

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.data = float2(IN.color.r, IN.color.a);
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
