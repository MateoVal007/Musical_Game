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

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        frameOffset = Random.Range(0, Mathf.Max(1, updateEveryNFrames));
        twinklePhase = Random.Range(0f, Mathf.PI * 2f);
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

    void Update()
    {
        if (stemIndex < 0 || StemAnalyzer.Instance == null) return;
        if ((Time.frameCount + frameOffset) % updateEveryNFrames != 0) return;

        float intensity = StemAnalyzer.Instance.GetIntensity(stemIndex);
        float twinkle = 1f + Mathf.Sin(Time.time * twinkleSpeed + twinklePhase) * twinkleStrength;
        float mapped = Mathf.Lerp(minIntensity, maxIntensity, intensity) * twinkle;

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorProperty, Color.Lerp(mutedColor, pureColor, intensity));
        propertyBlock.SetFloat(intensityProperty, mapped);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
