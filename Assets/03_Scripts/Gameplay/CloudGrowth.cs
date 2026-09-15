using UnityEngine;

// La nube va subiendo despacio (en el eje Y) con cada nota resuelta, sin
// importar qué tan bien (nunca baja, nunca hay penalización — solo varía
// cuánto sube). Se suscribe a NoteSpawner.OnNoteResolved.
//
// Como CloudBoundary lee la posición de la nube en vivo cada frame, la zona
// donde puede moverse el jugador sube automáticamente junto con la nube,
// sin que haya que tocar nada ahí.
public class CloudGrowth : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [Tooltip("El objeto que efectivamente sube. Si lo dejás vacío, usa este mismo Transform.")]
    [SerializeField] private Transform cloudVisual;

    [Tooltip("Cuánto sube (en metros) por cada punto de calidad (0 a 1). Con 0.05, un perfecto (calidad 1) sube 5cm.")]
    [SerializeField] private float risePerQuality = 0.05f;

    [Tooltip("Tope de cuánto puede subir en total, en metros, para canciones largas.")]
    [SerializeField] private float maxRiseHeight = 3f;

    [Tooltip("Qué tan rápido se anima hacia la nueva altura (más alto = más inmediato).")]
    [SerializeField] private float riseSpeed = 3f;

    private Vector3 basePosition;
    private float accumulatedRise;
    private float targetRise;

    void Awake()
    {
        if (cloudVisual == null) cloudVisual = transform;
        basePosition = cloudVisual.localPosition;
    }

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved += HandleNoteResolved;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved -= HandleNoteResolved;
    }

    void Update()
    {
        // Anima suavemente hacia la altura objetivo, en vez de saltar de golpe.
        float currentRise = cloudVisual.localPosition.y - basePosition.y;
        float newRise = Mathf.Lerp(currentRise, targetRise, riseSpeed * Time.deltaTime);

        Vector3 pos = cloudVisual.localPosition;
        pos.y = basePosition.y + newRise;
        cloudVisual.localPosition = pos;
    }

    private void HandleNoteResolved(float quality, bool isPerfect, Vector3 position)
    {
        accumulatedRise += quality * risePerQuality;
        targetRise = Mathf.Min(accumulatedRise, maxRiseHeight);
    }
}
