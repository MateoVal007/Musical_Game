using UnityEngine;

// Componente reusable: enganchá esto en CUALQUIER objeto de la escena y
// configurá desde el Inspector a qué banda de frecuencia escucha y qué
// propiedad afecta. No hace falta escribir código nuevo por cada objeto
// que quieran que reaccione a la música.
public class AudioReactiveTransform : MonoBehaviour
{
    public enum Band { Bass, Mid, Treble, ClimaxCurve }
    public enum TargetProperty { UniformScale, PositionY, RotationY }

    [Header("Qué banda escuchar")]
    public Band band = Band.Bass;

    [Header("Qué afecta")]
    public TargetProperty targetProperty = TargetProperty.UniformScale;

    [Header("Rango de reacción")]
    [Tooltip("Valor del objeto cuando la banda está en 0.")]
    public float minValue = 1f;
    [Tooltip("Valor del objeto cuando la banda está en 1 (máxima intensidad).")]
    public float maxValue = 1.5f;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Vector3 baseEuler;

    void Start()
    {
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        baseEuler = transform.localEulerAngles;
    }

    void Update()
    {
        if (band == Band.ClimaxCurve)
        {
            if (ClimaxIntensity.Instance == null) return;
        }
        else if (AudioAnalyzer.Instance == null)
        {
            return;
        }

        float bandValue = GetBandValue();
        float mapped = Mathf.Lerp(minValue, maxValue, bandValue);

        switch (targetProperty)
        {
            case TargetProperty.UniformScale:
                transform.localScale = baseScale * mapped;
                break;

            case TargetProperty.PositionY:
                transform.localPosition = basePosition + Vector3.up * (mapped - minValue);
                break;

            case TargetProperty.RotationY:
                transform.localEulerAngles = baseEuler + Vector3.up * mapped;
                break;
        }
    }

    private float GetBandValue()
    {
        switch (band)
        {
            case Band.Bass: return AudioAnalyzer.Instance.Bass;
            case Band.Mid: return AudioAnalyzer.Instance.Mid;
            case Band.Treble: return AudioAnalyzer.Instance.Treble;
            default: return ClimaxIntensity.Instance.Value;
        }
    }
}
