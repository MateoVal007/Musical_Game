using UnityEngine;

// Curva de intensidad ambiental a lo largo de TODA la canción, dibujada a mano
// en el Inspector (Unity trae un editor visual de curvas para esto). Reemplaza
// la idea de "el camino pasa por el punto de mayor intensidad en el clímax" —
// ahora, como el entorno es fijo (no hay recorrido), es una curva 0→1 que
// puede subir, bajar, tener un pico en el clímax, lo que quieran, calcada a
// cómo se siente realmente "Dream Ivory".
public class ClimaxIntensity : MonoBehaviour
{
    public static ClimaxIntensity Instance { get; private set; }

    public AudioSource audioSource;

    [Tooltip("Duración total de la canción, en segundos (fijate el largo real del AudioClip en su Inspector).")]
    public float songDuration = 180f;

    [Tooltip("Eje X: progreso de la canción (0 a 1). Eje Y: intensidad ambiental (0 a 1). Dibujen la curva para que coincida con cómo se siente la canción.")]
    public AnimationCurve intensityOverTime = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public float Value { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Update()
    {
        if (audioSource == null || songDuration <= 0f) return;

        float t = Mathf.Clamp01(audioSource.time / songDuration);
        Value = intensityOverTime.Evaluate(t);
    }
}
