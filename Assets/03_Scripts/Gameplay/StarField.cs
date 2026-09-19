using UnityEngine;

// Agrega una estrella PERMANENTE al cielo por cada toque perfecto — un
// registro visual acumulado de lo bien que fue la partida, sin puntaje en
// pantalla. Se suscribe a NoteSpawner.OnNoteResolved.
public class StarField : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private GameObject starPrefab;

    [Tooltip("Referencia del centro del cielo (ej. la nube). Si lo dejás vacío, usa el origen del mundo.")]
    [SerializeField] private Transform skyCenter;

    [SerializeField] private float minRadius = 8f;
    [SerializeField] private float maxRadius = 15f;
    [SerializeField] private float minHeight = 3f;
    [SerializeField] private float maxHeight = 10f;

    public int StarCount { get; private set; }

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved += HandleNoteResolved;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved -= HandleNoteResolved;
    }

    private void HandleNoteResolved(float quality, bool isPerfect, Vector3 position)
    {
        if (!isPerfect || starPrefab == null) return;
        SpawnStar();
    }

    private void SpawnStar()
    {
        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
        float height = Random.Range(minHeight, maxHeight);
        Vector3 center = skyCenter != null ? skyCenter.position : Vector3.zero;
        Vector3 starPos = center + new Vector3(circle.x, height, circle.y);

        GameObject starObj = Instantiate(starPrefab, starPos, Quaternion.identity, transform);

        Star star = starObj.GetComponent<Star>();
        if (star != null && StemAnalyzer.Instance != null)
        {
            star.AssignStem(StemAnalyzer.Instance.GetRandomStemIndex());
        }

        StarCount++;
    }
}
