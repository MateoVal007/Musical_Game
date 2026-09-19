using UnityEngine;
using System;
using System.Collections;

// Una nota-flecha individual: viaja desde el punto de aparición hasta el
// punto de toque, apuntando en una de 4 direcciones (Up/Down/Left/Right).
// Se "resuelve" de una de dos formas: el jugador hace el gesto de muñeca en
// la dirección correcta en el momento justo, o llega sola si nadie le pegó.
// Nunca hay fallo — solo varía la "calidad" del resultado (0 a 1).
//
// Visualmente: se materializa al aparecer (disolución inversa) y se disuelve
// al resolverse — usa el shader Custom/NoteDissolve, propiedad _DissolveAmount.
// La geometría de flecha la genera ArrowMeshBuilder en el mismo objeto.
public class Note : MonoBehaviour
{
    [Tooltip("Ventana de tiempo (segundos) para calcular qué tan cerca estuvo el botón del beat ideal. Fuera de esta ventana, la calidad llega a 0 (pero nunca peor que no presionar nada).")]
    [SerializeField] private float hitWindow = 0.5f;

    [Tooltip("Si la diferencia con el beat ideal es MENOR a esto (en segundos), cuenta como 'perfecto'. Subilo si sienten que hay que ser demasiado exactos.")]
    [SerializeField] private float perfectWindow = 0.2f;

    [Tooltip("Calidad que se le da si la nota llega sola, sin que el jugador presione nada.")]
    [SerializeField] private float autoArriveQuality = 0.1f;

    [Header("Disolución")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float materializeDuration = 0.2f;
    [SerializeField] private float dissolveDuration = 0.2f;

    // quality (0 a 1), isPerfect, posición donde se resolvió (para efectos como el cometa)
    public event Action<float, bool, Vector3> OnResolved;
    public float TargetBeatTime { get; private set; }
    public NoteDirection Direction { get; private set; }

    // Se guardan los Transform, no las posiciones: los puntos de aparición y
    // de llegada suben con la cabeza del jugador (FollowPlayerHeight) mientras
    // la nota está en vuelo. Si se guardara la posición del momento del spawn,
    // la nota volaría hacia una altura vieja y se vería bajando en diagonal.
    private Transform startPoint, endPoint;
    private float travelTime, spawnTime;
    private bool resolved;
    private Material material;
    private Coroutine dissolveRoutine;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        material = targetRenderer.material;
    }

    public void Init(Transform start, Transform end, float beatTime, float travel, NoteDirection direction)
    {
        startPoint = start;
        endPoint = end;
        TargetBeatTime = beatTime;
        travelTime = travel;
        spawnTime = Time.time;
        Direction = direction;

        // La flecha (ArrowMeshBuilder) está modelada apuntando hacia +Y;
        // giramos en Z para que apunte en la dirección que le tocó.
        transform.rotation *= Quaternion.Euler(0f, 0f, ZRotationForDirection(direction));

        // Cada dirección tiene su propio color, para que se distinga de un
        // vistazo (además de la forma de la flecha) qué gesto hace falta.
        material.SetColor("_Color", ColorForDirection(direction));

        // Materializar: arranca disuelta del todo y se va formando.
        material.SetFloat("_DissolveAmount", 1f);
        StartDissolveAnimation(1f, 0f, materializeDuration);
    }

    private static float ZRotationForDirection(NoteDirection direction)
    {
        switch (direction)
        {
            case NoteDirection.Up: return 0f;
            case NoteDirection.Right: return -90f;
            case NoteDirection.Down: return 180f;
            case NoteDirection.Left: return 90f;
            default: return 0f;
        }
    }

    private static Color ColorForDirection(NoteDirection direction)
    {
        switch (direction)
        {
            case NoteDirection.Up: return new Color32(0xC4, 0x10, 0x2F, 0xFF); // escarlata
            case NoteDirection.Down: return new Color32(0xE8, 0xA3, 0x3D, 0xFF); // ámbar
            case NoteDirection.Left: return new Color32(0x8B, 0x3F, 0xE0, 0xFF); // violeta eléctrico
            case NoteDirection.Right: return new Color32(0x2D, 0xD4, 0xC4, 0xFF); // turquesa
            default: return Color.white;
        }
    }

    void Update()
    {
        if (resolved) return;

        float t = (Time.time - spawnTime) / travelTime;
        transform.position = Vector3.Lerp(startPoint.position, endPoint.position, Mathf.Clamp01(t));

        if (t >= 1f)
        {
            Resolve(autoArriveQuality, false);
        }
    }

    // Llamado por el NoteSpawner cuando el jugador hace el gesto de muñeca y
    // esta es la nota activa más cercana a su momento ideal QUE ADEMÁS
    // coincide en dirección. Si la dirección no coincide, no hace nada (no
    // hay fallo — la nota sigue su curso y se auto-resuelve si nadie le pega).
    public bool TryResolveByDirection(NoteDirection inputDirection, float currentSongTime)
    {
        if (resolved || inputDirection != Direction) return false;

        float diff = Mathf.Abs(currentSongTime - TargetBeatTime);
        float quality = Mathf.Clamp01(1f - diff / hitWindow);
        bool isPerfect = diff <= perfectWindow;

        Resolve(quality, isPerfect);
        return true;
    }

    private void Resolve(float quality, bool isPerfect)
    {
        resolved = true;

        // El evento dispara YA (crece la nube, cometa, contador de perfectos, etc.)
        // — el objeto visual se queda un instante más para mostrar la disolución.
        OnResolved?.Invoke(quality, isPerfect, transform.position);

        StartDissolveAnimation(0f, 1f, dissolveDuration, destroyAfter: true);
    }

    private void StartDissolveAnimation(float from, float to, float duration, bool destroyAfter = false)
    {
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(AnimateDissolve(from, to, duration, destroyAfter));
    }

    private IEnumerator AnimateDissolve(float from, float to, float duration, bool destroyAfter)
    {
        duration = Mathf.Max(duration, 0.01f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            material.SetFloat("_DissolveAmount", Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        material.SetFloat("_DissolveAmount", to);

        if (destroyAfter)
        {
            Destroy(gameObject);
        }
    }
}
