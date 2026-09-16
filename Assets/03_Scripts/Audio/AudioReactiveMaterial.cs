using UnityEngine;

// Hermano de AudioReactiveTransform, pero para materiales en vez de Transform:
// prende/apaga (o sube/baja) el brillo de un elemento del ambiente según una
// banda de audio. Poner en cualquier objeto con un material emisivo (armado
// con un Shader Graph que tenga una propiedad float expuesta, ej. siguiendo
// el tutorial de "glow") para que titile/pulse con la música — sin código nuevo.
public class AudioReactiveMaterial : MonoBehaviour
{
    public enum Band { Bass, Mid, Treble, ClimaxCurve }

    [Header("Qué banda escuchar")]
    public Band band = Band.Treble;

    [Header("Material a afectar")]
    [Tooltip("Si lo dejás vacío, usa el Renderer de este mismo objeto.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("El nombre EXACTO de la propiedad expuesta en el Shader Graph (ej. '_EmissionIntensity'). Se ve en el Blackboard del Shader Graph, con el prefijo '_' adelante.")]
    [SerializeField] private string propertyName = "_EmissionIntensity";

    [Header("Rango de reacción")]
    [Tooltip("Valor de la propiedad cuando la banda está en 0 (ej. 'apagado').")]
    public float minValue = 0f;
    [Tooltip("Valor de la propiedad cuando la banda está en 1 (ej. brillo máximo).")]
    public float maxValue = 3f;

    private Material material;
    private int propertyId;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        // .material (no .sharedMaterial) crea una instancia propia, para no
        // modificar el asset compartido si hay varios objetos iguales en la escena.
        material = targetRenderer.material;
        propertyId = Shader.PropertyToID(propertyName);
    }

    void Update()
    {
        float bandValue = GetBandValue();
        if (bandValue < 0f) return; // todavía no hay dato disponible (analizador no listo)

        float mapped = Mathf.Lerp(minValue, maxValue, bandValue);
        material.SetFloat(propertyId, mapped);
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
