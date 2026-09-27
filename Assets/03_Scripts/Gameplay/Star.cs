using UnityEngine;

// Una estrella individual: al aparecer recibe una pista (stem) al azar —
// su color queda fijado al color de esa pista, y su brillo sube/baja según
// la energía de ESA pista específica (no de la canción entera).
//
// Como las estrellas son PERMANENTES (se van acumulando toda la partida),
// cualquier costo por-estrella se multiplica con el tiempo. Por eso:
// - Usa MaterialPropertyBlock en vez de "renderer.material" — esto último
//   crea una instancia de material POR ESTRELLA, lo que rompe el SRP Batcher
//   de Unity y obliga a un draw call separado por cada una. Con el property
//   block, todas comparten el mismo material y Unity las puede batchear.
// - No actualiza el brillo todos los frames — alterna cuál de cada 3 frames
//   le toca a cada estrella (offset aleatorio para no sincronizarlas todas),
//   ya que StemAnalyzer ya suaviza la señal y no se nota el salto de 1 frame.
public class Star : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private string intensityProperty = "_GlowIntensity";
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 6f;
    [SerializeField] private int updateEveryNFrames = 3;

    [Header("Twinkle")]
    [Tooltip("Qué tan rápido parpadea. Cada estrella tiene su propia fase, así no titilan todas juntas.")]
    [SerializeField] private float twinkleSpeed = 2f;
    [Tooltip("Cuánto varía el brillo por el parpadeo (0 = sin twinkle, 0.2 = ±20%).")]
    [SerializeField] private float twinkleStrength = 0.15f;

    [Header("Destello de lente")]
    [Range(0f, 1f)]
    [Tooltip("Qué proporción de estrellas tiene puntas. Bajo a propósito: en un cielo real solo las más brillantes las muestran, y dárselo a todas las aplana a una sola jerarquía. Además cada una cuesta relleno de pantalla.")]
    [SerializeField] private float flareChance = 0.15f;

    [Tooltip("Las estrellas con destello necesitan un quad más grande para que las puntas tengan dónde desvanecerse. El shader compensa el núcleo para que no se vean más gordas.")]
    [SerializeField] private float flareSizeMultiplier = 3.5f;

    [Range(0f, 1f)]
    [Tooltip("Cuánto destellan cuando su pista está callada. Lo que queda hasta 1 lo aporta la música.")]
    [SerializeField] private float flareIdle = 0.25f;

    [Header("Respuesta a la música")]
    [Tooltip("Valor del stem que ya se considera silencio. Todo lo que esté por debajo deja la estrella en su mínimo.")]
    [SerializeField] private float stemFloor = 0.2f;

    [Tooltip("Valor del stem que se considera el máximo. Los stems casi nunca llegan a 1 — suelen moverse entre 0.3 y 0.6 — así que sin este techo las estrellas nunca alcanzaban su brillo máximo. Mirá los 'Smoothed Intensity' en el StemAnalyzer mientras suena y poné acá el pico real.")]
    [SerializeField] private float stemCeiling = 0.6f;

    [Header("Cierre")]
    [Tooltip("Segundos que tarda un ciclo completo de apagarse y encenderse cuando termina la canción. Lento: es un momento para mirar, no para que pase algo.")]
    [SerializeField] private float breathPeriod = 4f;

    [Range(0f, 1f)]
    [Tooltip("Hasta dónde llega el latido del cierre. Un poco por debajo de 1 para que los picos de la canción sigan siendo lo más brillante que se ve en toda la partida.")]
    [SerializeField] private float breathPeak = 0.85f;

    [Header("Color: de apagado a puro (sutil)")]
    [Tooltip("Qué tan desaturado se ve en el momento más flojo de su pista (0 = sin cambio, 1 = gris total).")]
    [Range(0f, 1f)]
    [SerializeField] private float mutedSaturationDrop = 0.5f;

    private MaterialPropertyBlock propertyBlock;
    private Color pureColor;
    private Color mutedColor;
    private int stemIndex = -1;
    private int frameOffset;
    private float twinklePhase;
    private bool hasFlare;
    private bool breathing;

    private static readonly int FlareAmountId = Shader.PropertyToID("_FlareAmount");
    private static readonly int CoreShrinkId = Shader.PropertyToID("_CoreShrink");

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // Blanco por defecto y no negro: si una estrella se queda sin pista
        // asignada (StemAnalyzer todavía sin inicializar, por ejemplo), al
        // latir en el cierre se pintaría de negro y desaparecería.
        pureColor = Color.white;
        mutedColor = Color.white;

        frameOffset = Random.Range(0, Mathf.Max(1, updateEveryNFrames));
        twinklePhase = Random.Range(0f, Mathf.PI * 2f);

        hasFlare = Random.value < flareChance;

        if (hasFlare)
        {
            // El quad crece para darle lugar a las puntas, y el shader achica
            // el núcleo en la misma proporción: la estrella se ve del mismo
            // tamaño que las demás, solo que con puntas saliéndole.
            transform.localScale *= flareSizeMultiplier;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(CoreShrinkId, flareSizeMultiplier);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void AssignStem(int index)
    {
        stemIndex = index;

        if (StemAnalyzer.Instance != null)
        {
            pureColor = StemAnalyzer.Instance.GetColor(index);

            // Versión apagada del mismo color: mismo tono/brillo, menos saturación.
            Color.RGBToHSV(pureColor, out float h, out float s, out float v);
            mutedColor = Color.HSVToRGB(h, s * (1f - mutedSaturationDrop), v);

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty, mutedColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    // Al terminar la canción los stems se van a cero y las estrellas se
    // quedaban clavadas en el mínimo, justo en los segundos que están
    // pensados para mirar el paisaje. Esto las hace latir solas.
    public void StartBreathing()
    {
        breathing = true;
    }

    void Update()
    {
        if ((Time.frameCount + frameOffset) % updateEveryNFrames != 0) return;

        float intensity;

        if (breathing)
        {
            // Cada estrella arranca en un punto distinto del ciclo (reusa la
            // fase del titileo): si respiraran todas juntas el cielo entero
            // parpadearía de golpe y se vería como un error.
            float t = Time.time * (2f * Mathf.PI / Mathf.Max(0.1f, breathPeriod)) + twinklePhase;
            intensity = (Mathf.Sin(t) * 0.5f + 0.5f) * breathPeak;
        }
        else
        {
            if (stemIndex < 0 || StemAnalyzer.Instance == null) return;

            // Estirar la banda REAL del stem al rango completo. Sin esto las
            // estrellas nunca se acercaban a su brillo máximo durante la
            // canción, porque un stem que se mueve entre 0.3 y 0.6 solo usaba
            // la mitad de abajo del Lerp — y encima achataba el contraste,
            // porque todo el movimiento pasaba en un tramo angosto.
            float raw = StemAnalyzer.Instance.GetIntensity(stemIndex);
            intensity = Mathf.InverseLerp(stemFloor, Mathf.Max(stemCeiling, stemFloor + 0.01f), raw);
        }

        float twinkle = 1f + Mathf.Sin(Time.time * twinkleSpeed + twinklePhase) * twinkleStrength;
        float mapped = Mathf.Lerp(minIntensity, maxIntensity, intensity) * twinkle;

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorProperty, Color.Lerp(mutedColor, pureColor, intensity));
        propertyBlock.SetFloat(intensityProperty, mapped);

        if (hasFlare)
        {
            // Las PUNTAS crecen con la música, no solo el brillo. Un cambio de
            // brillo contra un cielo oscuro casi no se lee; que a la estrella
            // le salgan y se le achiquen las puntas se ve desde cualquier lado.
            propertyBlock.SetFloat(FlareAmountId, Mathf.Lerp(flareIdle, 1f, intensity) * twinkle);
            propertyBlock.SetFloat(CoreShrinkId, flareSizeMultiplier);
        }

        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
