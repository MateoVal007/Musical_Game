Shader "Custom/Grass"
{
    // Pasto con viento. El movimiento se compone de tres cosas, que es lo
    // que hace que se vea real en vez de "todo meneándose igual":
    //   1. Inclinación constante (_BaseLean): el pasto siempre está echado
    //      hacia donde sopla el viento, como en una foto de campo ventoso.
    //   2. Ráfaga (_WindSpeed/_WindScale): una onda grande que VIAJA por el
    //      campo — se ve la ola recorriendo el pasto.
    //   3. Aleteo (_Flutter*): tembleque rápido y chico, distinto por blade.
    //
    // Todo escala con heightFactor² — nada en la raíz, máximo en la punta,
    // que es como se dobla un blade real. Además se baja un poco la altura
    // al doblarse, porque un blade doblado "llega" menos alto.
    //
    // Espera vértices con COLOR: .r = altura normalizada (0 base, 1 punta),
    // .g = fase aleatoria por blade, .b = sombreado por orientación del
    // blade, .a = variación de brillo por blade. Los genera
    // GrassFieldMeshBuilder.cs.
    Properties
    {
        _BaseColor ("Color en la raíz (oscuro)", Color) = (0.07, 0.10, 0.08, 1)
        _TipColor ("Color en la punta", Color) = (0.36, 0.43, 0.54, 1)

        _WindDirection ("Dirección del viento (XZ)", Vector) = (1, 0, 0.35, 0)
        _BaseLean ("Inclinación constante", Range(0, 1)) = 0.25
        _WindSpeed ("Velocidad de la ráfaga", Float) = 1.2
        _WindScale ("Escala espacial de la ráfaga", Float) = 0.25
        _WindStrength ("Fuerza de la ráfaga", Range(0, 2)) = 0.45

        _FlutterSpeed ("Velocidad del aleteo", Float) = 7
        _FlutterStrength ("Fuerza del aleteo", Range(0, 0.5)) = 0.08

        _ShadeStrength ("Contraste de sombreado entre blades", Range(0, 1)) = 0.45
        _ColorVariation ("Variación de brillo por blade", Range(0, 0.6)) = 0.2
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
                float heightFactor : TEXCOORD0;
                float brightness : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _WindDirection;
                float _BaseLean;
                float _WindSpeed;
                float _WindScale;
                float _WindStrength;
                float _FlutterSpeed;
                float _FlutterStrength;
                float _ShadeStrength;
                float _ColorVariation;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float heightFactor = IN.color.r;
                float phase = IN.color.g * 6.28318;
                float shade = IN.color.b;
                float variation = IN.color.a;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float gust = sin(_Time.y * _WindSpeed + (positionWS.x + positionWS.z) * _WindScale + phase);
                float flutter = sin(_Time.y * _FlutterSpeed + phase * 3.7) * _FlutterStrength;

                // La raíz no se mueve; la punta es la que se dobla.
                float bendAmount = heightFactor * heightFactor;
                float sway = (_BaseLean + gust * _WindStrength + flutter) * bendAmount;

                float2 windDir = normalize(_WindDirection.xz + float2(1e-5, 1e-5));
                positionWS.xz += windDir * sway;

                // Al doblarse pierde altura — si no, el blade se "estira" al ventear.
                positionWS.y -= sway * sway * 0.35;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.heightFactor = heightFactor;
                OUT.brightness = lerp(1.0 - _ShadeStrength, 1.0, shade)
                               * lerp(1.0 - _ColorVariation, 1.0 + _ColorVariation, variation);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Gradiente con curva: mantiene la raíz bien oscura y aclara
                // recién cerca de la punta (como el pasto real, que tiene la
                // base en sombra por la densidad de los blades de al lado).
                float t = IN.heightFactor * IN.heightFactor;
                half3 color = lerp(_BaseColor.rgb, _TipColor.rgb, t) * IN.brightness;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
