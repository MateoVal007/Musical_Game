Shader "Custom/StarFlare"
{
    // SOLO para las estrellas del cielo. Color e Intensity los maneja Star.cs por MaterialPropertyBlock:
    // el tinte sale de la pista (stem) asignada y el brillo sube y baja con la
    // energía de ESA pista.
    //
    // Algunas estrellas además tienen destello de lente: las puntas cruzadas
    // que se estiran, como en el destello de la luna. NO lo tienen todas, y no
    // es por ahorrar: en un cielo real solo las más brillantes muestran puntas.
    // Dárselo a todas las aplana a una sola jerarquía y se nota falso.
    //
    // Lo importante es que las puntas van DENTRO de este mismo quad, sin
    // geometría extra. Las estrellas se acumulan toda la partida (una por
    // perfecto), así que cualquier costo por estrella se multiplica sin techo.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 1

        [Header(Nucleo)]
        _CoreSharpness ("Qué tan chico es el núcleo", Range(1, 40)) = 9
        _CoreWhiteness ("Cuánto se va a blanco el núcleo", Range(0, 1)) = 0.6
        _HaloStrength ("Fuerza del halo", Range(0, 2)) = 0.5
        _HaloFalloff ("Suavidad del halo", Range(0.3, 6)) = 2.2

        [Header(Destello)]
        // Lo setea Star.cs por estrella: 0 en las normales, y en las elegidas
        // sube y baja con la música.
        _FlareAmount ("Cuánto destella (lo pone Star.cs)", Range(0, 1)) = 0

        // También lo pone Star.cs. Las estrellas con destello usan un quad más
        // grande para que las puntas tengan lugar; esto achica el núcleo en la
        // misma proporción para que no se vean más gordas que las demás.
        _CoreShrink ("Compensación de tamaño (lo pone Star.cs)", Range(1, 8)) = 1

        _StreakStrength ("Fuerza de las puntas", Range(0, 3)) = 1.2
        _StreakLength ("Largo de las puntas", Range(0.2, 6)) = 2.5
        _StreakThickness ("Finura de las puntas", Range(10, 300)) = 110
        _Chroma ("Separación de color", Range(0, 0.15)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
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
                float2 offset : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _GlowIntensity;
                float _CoreSharpness;
                float _CoreWhiteness;
                float _HaloStrength;
                float _HaloFalloff;
                float _FlareAmount;
                float _CoreShrink;
                float _StreakStrength;
                float _StreakLength;
                float _StreakThickness;
                float _Chroma;
            CBUFFER_END

            float Streak(float across, float along, float thickness, float len)
            {
                float reach = 8.0 / max(0.05, len);
                return exp(-abs(across) * thickness) * exp(-abs(along) * reach);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // El Quad de Unity va de -0.5 a 0.5: lo llevamos a -1..1.
                float2 corner = IN.positionOS.xy * 2.0;

                // Ahora mira a cámara. Antes se dibujaba con la rotación del
                // objeto, y como StarField las instancia con Quaternion.identity
                // TODAS apuntaban al mismo lado: las que quedaban de costado se
                // veían aplastadas. Además, un destello de lente tiene que estar
                // alineado a la pantalla para leerse como tal.
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float scale = length(GetObjectToWorldMatrix()._m00_m10_m20);

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = centerWS + (camRight * corner.x + camUp * corner.y) * scale * 0.5;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.offset = corner;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.offset;

                // El núcleo se mide sobre el quad ACHICADO, así una estrella con
                // destello (quad grande) tiene el mismo punto que una normal.
                float d = saturate(1.0 - length(p) * _CoreShrink);

                float core = pow(d, _CoreSharpness);
                float halo = pow(d, _HaloFalloff) * _HaloStrength;
                float shape = core + halo;

                half3 tint = lerp(_Color.rgb, half3(1, 1, 1), core * _CoreWhiteness);
                half3 color = tint * shape;

                float streakSum = 0;

                if (_FlareAmount > 0.001)
                {
                    // Cada canal se estira distinto: eso abre las puntas en
                    // rosado y celeste en vez de dejarlas de un color plano.
                    half3 streaks = 0;
                    [unroll]
                    for (int c = 0; c < 3; c++)
                    {
                        float shift = 1.0 + (c - 1) * _Chroma;

                        float h = Streak(p.y, p.x * shift, _StreakThickness, _StreakLength);
                        float v = Streak(p.x, p.y * shift, _StreakThickness, _StreakLength);

                        streaks[c] = (h + v) * _StreakStrength * _FlareAmount;
                    }

                    color += _Color.rgb * streaks;
                    streakSum = (streaks.r + streaks.g + streaks.b) * 0.333;
                }

                // Alfa fijo en 1 donde hay forma: con blend aditivo no hace
                // falta transparencia variable, y si el alfa llegaba a 0 por
                // cualquier motivo la estrella desaparecía.
                return half4(color * _GlowIntensity, saturate(shape + streakSum));
            }
            ENDHLSL
        }
    }
}
