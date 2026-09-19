using UnityEngine;

// Como AudioReactiveMaterial, pero anima un COLOR completo en vez de un
// solo float — de un color apagado/grisáceo a un color puro/saturado según
// una banda de audio (además de la intensidad de brillo). Pensado para el
// foco del farol: de azul apagado a azul puro con el bajo de la canción.
public class AudioReactiveColor : MonoBehaviour
{
    public enum Band { Bass, Mid, Treble, ClimaxCurve }

    [Header("Qué banda escuchar")]
    public Band band = Band.Bass;

    [Header("Material a afectar")]
    [Tooltip("Si lo dejás vacío, usa el Renderer de este mismo objeto.")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private string intensityProperty = "_GlowIntensity";

    [Header("Color: de apagado a puro")]
    public Color minColor = new Color(0.3f, 0.32f, 0.42f);
    public Color maxColor = new Color(0.05f, 0.12f, 1f);

    [Header("Brillo")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 9f;

    private Material material;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        material = targetRenderer.material;
    }

    void Update()
    {
        float t = GetBandValue();
        if (t < 0f) return;

        material.SetColor(colorProperty, Color.Lerp(minColor, maxColor, t));
        material.SetFloat(intensityProperty, Mathf.Lerp(minIntensity, maxIntensity, t));
    }

    private float GetBandValue()
    {
        switch (band)
        {
            case Band.Bass:
                return AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.Bass : -1f;
            case Band.Mid:
                return AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.Mid : -1f;
            case Band.Treble:
                return AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.Treble : -1f;
            default:
                return ClimaxIntensity.Instance != null ? ClimaxIntensity.Instance.Value : -1f;
        }
    }
}
