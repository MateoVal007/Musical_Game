using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Va calentando/intensificando sutilmente el color de toda la escena hacia
// el clímax de la canción, siguiendo ClimaxIntensity.Value (0 a 1, la curva
// ya dibujada a mano sobre toda la duración). Usa un Volume con Color
// Adjustments — mismo patrón que la viñeta de CloudBoundary (Volume +
// profile.TryGet).
public class ClimaxColorGrading : MonoBehaviour
{
    [SerializeField] private Volume volume;

    [Tooltip("Saturación en el punto más calmo de la canción (intensidad 0).")]
    [SerializeField] private float minSaturation = -35f;
    [Tooltip("Saturación en el clímax (intensidad 1).")]
    [SerializeField] private float maxSaturation = 45f;

    [Tooltip("Contraste en el punto más calmo — más chato/apagado.")]
    [SerializeField] private float minContrast = -20f;
    [Tooltip("Contraste en el clímax — más punch.")]
    [SerializeField] private float maxContrast = 30f;

    [Tooltip("Filtro de color en el punto más calmo — neutro, casi desaturado hacia el azul-violeta base.")]
    [SerializeField] private Color minColorFilter = new Color(0.9f, 0.92f, 1f);
    [Tooltip("Filtro de color en el clímax — tinte cálido bien marcado, para contrastar fuerte con el azul-violeta base de la escena.")]
    [SerializeField] private Color maxColorFilter = new Color(1f, 0.65f, 0.45f);

    [Tooltip("Post Exposure en el punto más calmo.")]
    [SerializeField] private float minPostExposure = -0.3f;
    [Tooltip("Post Exposure en el clímax (bastante más brillo general).")]
    [SerializeField] private float maxPostExposure = 0.6f;

    [Tooltip("Activalo para ver en la consola la intensidad y los valores resultantes cuadro a cuadro — útil para confirmar que el efecto está vivo aunque no se note mucho a simple vista. Apagalo después, tira un log por frame.")]
    [SerializeField] private bool debugLog = false;

    private ColorAdjustments colorAdjustments;

    void Start()
    {
        if (volume != null) volume.profile.TryGet(out colorAdjustments);
    }

    void Update()
    {
        if (colorAdjustments == null || ClimaxIntensity.Instance == null) return;

        float t = ClimaxIntensity.Instance.Value;
        colorAdjustments.saturation.value = Mathf.Lerp(minSaturation, maxSaturation, t);
        colorAdjustments.contrast.value = Mathf.Lerp(minContrast, maxContrast, t);
        colorAdjustments.colorFilter.value = Color.Lerp(minColorFilter, maxColorFilter, t);
        colorAdjustments.postExposure.value = Mathf.Lerp(minPostExposure, maxPostExposure, t);

        if (debugLog)
        {
            Debug.Log($"ClimaxIntensity: {t:F2} | Saturation: {colorAdjustments.saturation.value:F1} | PostExposure: {colorAdjustments.postExposure.value:F2}");
        }
    }
}
