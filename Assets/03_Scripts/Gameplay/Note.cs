using UnityEngine;
using System;

// Una nota/luz individual: viaja desde el punto de aparición hasta el punto
// de toque. Se "resuelve" de una de dos formas: el jugador la agarra apretando
// el botón en el momento justo, o llega sola si no apretó nada. Nunca hay
// fallo — solo varía la "calidad" del resultado (0 a 1).
public class Note : MonoBehaviour
{
    [Tooltip("Ventana de tiempo (segundos) para calcular qué tan cerca estuvo el botón del beat ideal. Fuera de esta ventana, la calidad llega a 0 (pero nunca peor que no presionar nada).")]
    [SerializeField] private float hitWindow = 0.5f;

    [Tooltip("Si la diferencia con el beat ideal es MENOR a esto (en segundos), cuenta como 'perfecto'. Subilo si sienten que hay que ser demasiado exactos.")]
    [SerializeField] private float perfectWindow = 0.2f;

    [Tooltip("Calidad que se le da si la nota llega sola, sin que el jugador presione nada.")]
    [SerializeField] private float autoArriveQuality = 0.1f;

    // quality (0 a 1), isPerfect, posición donde se resolvió (para efectos como el cometa)
    public event Action<float, bool, Vector3> OnResolved;
    public float TargetBeatTime { get; private set; }

    private Vector3 startPos, endPos;
    private float travelTime, spawnTime;
    private bool resolved;

    public void Init(Vector3 start, Vector3 end, float beatTime, float travel)
    {
        startPos = start;
        endPos = end;
        TargetBeatTime = beatTime;
        travelTime = travel;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (resolved) return;

        float t = (Time.time - spawnTime) / travelTime;
        transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(t));

        if (t >= 1f)
        {
            Resolve(autoArriveQuality, false);
        }
    }

    // Llamado por el NoteSpawner cuando el jugador aprieta el botón y esta
    // es la nota más cercana a su momento ideal.
    public void ResolveByButtonPress(float currentSongTime)
    {
        if (resolved) return;

        float diff = Mathf.Abs(currentSongTime - TargetBeatTime);
        float quality = Mathf.Clamp01(1f - diff / hitWindow);
        bool isPerfect = diff <= perfectWindow;

        if (isPerfect)
        {
            Debug.Log($"¡Perfecto! Diferencia: {diff:F3}s (calidad: {quality:F2})");
        }

        Resolve(quality, isPerfect);
    }

    private void Resolve(float quality, bool isPerfect)
    {
        resolved = true;
        OnResolved?.Invoke(quality, isPerfect, transform.position);
        Destroy(gameObject);
    }
}
