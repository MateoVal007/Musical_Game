Shader "Custom/DigitalWaterfall"
{
    // Cascada digital: una cortina de hilos de luz cayendo, con un resplandor
    // en la base donde "golpea". Todo procedural, sin texturas ni partículas.
    //
    // Cómo funciona: la cortina se divide en columnas verticales. Cada columna
    // saca de un hash su propia velocidad, fase, grosor y brillo — por eso los
    // hilos caen desparejos en vez de como una persiana bajando. Dentro de la
    // columna, frac() genera los hilos repetidos y una potencia les da cabeza
    // brillante abajo y cola hacia arriba, que es la forma de una gota cayendo.
    //
    // Espera COLOR.r = índice de capa (0 la más cercana, 1 la más lejana),
    // para que cada capa caiga distinto. Lo genera WaterfallBuilder.cs.
    Properties
    {
        _TopColor ("Color arriba", Color) = (0.55, 0.8, 1, 1)
        _BottomColor ("Color abajo", Color) = (0.15, 0.35, 0.9, 1)
        _PoolColor ("Color del resplandor de la base", Color) = (0.8, 0.95, 1, 1)

        _Brightness ("Brillo general", Range(0, 20)) = 3

        _Columns ("Cantidad de hilos", Float) = 90
        _StreakDensity ("Hilos apilados a lo alto", Float) = 4
        _StreakSharpness ("Qué tan corta es la gota", Range(1, 40)) = 9
        _FallSpeed ("Velocidad de caída", Float) = 0.35

        _PoolGlow ("Fuerza del resplandor de la base", Range(0, 10)) = 2.5
        _PoolHeight ("Altura del resplandor", Range(0.01, 0.6)) = 0.12

        _EdgeFade ("Difuminado de los bordes laterales", Range(0, 0.5)) = 0.15
        _TopFade ("Difuminado del borde de arriba", Range(0, 0.5)) = 0.1

        _DepthFade ("Cuánto se apagan las capas del fondo", Range(0, 1)) = 0.5
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
                float layer : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float4 _PoolColor;
                float _Brightness;
                float _Columns;
                float _StreakDensity;
                float _StreakSharpness;
                float _FallSpeed;
                float _PoolGlow;
                float _PoolHeight;
                float _EdgeFade;
                float _TopFade;
                float _DepthFade;
            CBUFFER_END

            float hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.layer = IN.color.r;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;          // x a lo ancho, y: 0 abajo, 1 arriba
                float layerSeed = IN.layer * 37.0 + 1.0;

                // --- Columnas: cada hilo con su propia personalidad
                float colF = uv.x * _Columns;
                float col = floor(colF);
                float inCol = frac(colF);

                float hPhase = hash11(col + layerSeed);
                float hSpeed = hash11(col + layerSeed + 13.7);
                float hBright = hash11(col + layerSeed + 51.3);
                float hWidth = hash11(col + layerSeed + 77.9);

                // Grosor del hilo dentro de su columna.
                float width = lerp(0.08, 0.35, hWidth);
                float across = 1.0 - smoothstep(0.0, width, abs(inCol - 0.5));

                // --- Caída: el patrón baja con el tiempo
                float speed = _FallSpeed * lerp(0.6, 1.5, hSpeed);
                float t = uv.y * _StreakDensity + _Time.y * speed + hPhase * 10.0;

                // Cabeza brillante abajo, cola hacia arriba.
                float streak = pow(saturate(1.0 - frac(t)), _StreakSharpness);

                float threads = across * streak * lerp(0.35, 1.0, hBright);

                // --- Envolventes: sin bordes duros
                float edges = smoothstep(0.0, _EdgeFade, uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - uv.x);
                float topEdge = 1.0 - smoothstep(1.0 - _TopFade, 1.0, uv.y);

                // Las capas del fondo más apagadas: perspectiva atmosférica.
                float depth = lerp(1.0, 1.0 - _DepthFade, IN.layer);

                half3 color = lerp(_BottomColor.rgb, _TopColor.rgb, uv.y) * threads;

                // --- Resplandor donde golpea abajo
                float pool = exp(-uv.y / max(_PoolHeight, 0.001)) * _PoolGlow;
                color += _PoolColor.rgb * pool * edges;

                color *= edges * topEdge * depth * _Brightness;

                float alpha = saturate((threads + pool) * edges * topEdge * depth);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
