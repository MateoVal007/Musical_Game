Shader "Custom/MoonFlare"
{
    // Destello de lente para la luna: núcleo quemado, halo y las estelas
    // cruzadas que se estiran, como cuando el sol le pega de lleno a una
    // cámara. Pensado para apilarse ENCIMA de Custom/Aura — el aura pone el
    // resplandor redondo y esto le agrega las líneas.
    //
    // Va sobre un Quad normal de Unity, como el aura. Se orienta a cámara en
    // el vertex shader, así que la luna puede girar sobre su eje sin que el
    // destello gire con ella.
    //
    // Las estelas quedan alineadas a la PANTALLA, no al mundo, y eso es a
    // propósito: un destello de lente pasa adentro del lente, no allá lejos.
    // Como el billboard se arma con los ejes de la cámara, sale gratis.
    Properties
    {
        _Color ("Color", Color) = (0.15, 0.45, 1, 1)
        _GlowIntensity ("Brillo", Range(0, 40)) = 8

        _Size ("Tamaño (unidades de mundo)", Float) = 8

        [Header(Nucleo)]
        _CoreSharpness ("Qué tan chico es el núcleo", Range(1, 40)) = 14
        _CoreWhiteness ("Cuánto se va a blanco el núcleo", Range(0, 1)) = 0.8

        [Header(Halo)]
        _HaloStrength ("Fuerza del halo", Range(0, 2)) = 0.35
        _HaloFalloff ("Suavidad del halo", Range(0.3, 6)) = 2.5

        [Header(Estela horizontal)]
        _StreakHStrength ("Fuerza", Range(0, 3)) = 1
        _StreakHLength ("Largo", Range(0.2, 6)) = 3
        _StreakHThickness ("Finura", Range(10, 300)) = 90

        [Header(Estela vertical)]
        // En la referencia la columna vertical es mucho más larga que la
        // horizontal: eso es lo que la hace leer como haz de luz y no como
        // una simple cruz.
        _StreakVStrength ("Fuerza", Range(0, 3)) = 0.8
        _StreakVLength ("Largo", Range(0.2, 6)) = 5
        _StreakVThickness ("Finura", Range(10, 300)) = 160

        [Header(Aberracion cromatica)]
        // Cada canal se estira un poco distinto. Es lo que produce el filo
        // rosado y celeste en las puntas: sin esto la estela se ve como una
        // barra de color plano y pierde todo el realismo.
        _Chroma ("Separación de color", Range(0, 0.15)) = 0.04

        [Header(Titileo)]
        _TwinkleSpeed ("Velocidad", Float) = 1.3
        _TwinkleAmount ("Cuánto titila", Range(0, 0.6)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 offset : TEXCOORD0;
                float twinkle : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _GlowIntensity;
                float _Size;
                float _CoreSharpness;
                float _CoreWhiteness;
                float _HaloStrength;
                float _HaloFalloff;
                float _StreakHStrength;
                float _StreakHLength;
                float _StreakHThickness;
                float _StreakVStrength;
                float _StreakVLength;
                float _StreakVThickness;
                float _Chroma;
                float _TwinkleSpeed;
                float _TwinkleAmount;
            CBUFFER_END

            // Una estela es una exponencial en cada eje: muy cerrada a lo
            // ancho (la hoja fina) y muy abierta a lo largo (el alcance). La
            // exponencial no llega nunca a cero de golpe, así que la punta se
            // desvanece sola en vez de cortarse en el borde del quad.
            float Streak(float across, float along, float thickness, float length)
            {
                float reach = 8.0 / max(0.05, length);
                return exp(-abs(across) * thickness) * exp(-abs(along) * reach);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // El Quad de Unity va de -0.5 a 0.5: lo llevamos a -1..1.
                float2 corner = IN.positionOS.xy * 2.0;

                // Titileo sobre dos senos de periodo distinto, para que no se
                // sienta un pulso regular de máquina.
                float t = _Time.y * _TwinkleSpeed;
                float twinkle = 1.0 + (sin(t) * 0.6 + sin(t * 2.37) * 0.4) * _TwinkleAmount;

                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));

                // Hereda la escala del objeto para que el destello crezca
                // junto con la luna cuando se desbloquea y hace su animación
                // de aparición. Con escala 1 no cambia nada.
                float parentScale = length(GetObjectToWorldMatrix()._m00_m10_m20);

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = centerWS
                    + (camRight * corner.x + camUp * corner.y) * _Size * 0.5 * parentScale;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.offset = corner;
                OUT.twinkle = twinkle;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.offset;
                float d = saturate(1.0 - length(p));

                float core = pow(d, _CoreSharpness);
                float halo = pow(d, _HaloFalloff) * _HaloStrength;

                // Cada canal estira la estela un poquito distinto. Rojo se
                // queda corto y azul se pasa, así que las puntas se abren en
                // rosado de un lado y celeste del otro.
                half3 streaks = 0;
                [unroll]
                for (int c = 0; c < 3; c++)
                {
                    float shift = 1.0 + (c - 1) * _Chroma;

                    float h = Streak(p.y, p.x * shift, _StreakHThickness, _StreakHLength) * _StreakHStrength;
                    float v = Streak(p.x, p.y * shift, _StreakVThickness, _StreakVLength) * _StreakVStrength;

                    streaks[c] = h + v;
                }

                float shape = core + halo;

                half3 tint = lerp(_Color.rgb, half3(1, 1, 1), core * _CoreWhiteness);

                // Las estelas heredan el color pero con el blanco del núcleo
                // ya aplicado por canal, así el centro del cruce queda
                // quemado igual que en una foto real.
                half3 color = (tint * shape + _Color.rgb * streaks) * _GlowIntensity * IN.twinkle;

                float alpha = saturate(shape + (streaks.r + streaks.g + streaks.b) * 0.333);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
