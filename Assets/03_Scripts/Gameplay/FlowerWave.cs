using UnityEngine;

// Enciende TODAS las flores de golpe cuando el jugador clava un perfecto, y
// las deja apagarse solas — como una luciérnaga, pero disparada por el juego.
//
// La curva de apagado se calcula acá y se manda como un solo número
// (_FlowerFlash, de 0 a 1) al shader Custom/Flower. Hacerlo en C# en vez de
// en el shader deja el control del tiempo en el Inspector, en segundos
// entendibles, en vez de una constante invertida adentro del shader.
public class FlowerWave : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [Tooltip("Dispara también con aciertos que no fueron perfectos. Apagado, las flores son exclusivas de los perfectos.")]
    [SerializeField] private bool onlyOnPerfect = true;

    [Tooltip("Cuánto tarda en apagarse del todo después del destello, en segundos.")]
    [SerializeField] private float fadeDuration = 2.5f;

    [Tooltip("Segundos que se queda al máximo antes de empezar a bajar.")]
    [SerializeField] private float holdDuration = 0.15f;

    private static readonly int FlashId = Shader.PropertyToID("_FlowerFlash");

    private float lastFlashTime = float.NegativeInfinity;

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer += HandleHit;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer -= HandleHit;
        Shader.SetGlobalFloat(FlashId, 0f);
    }

    private void HandleHit(float quality, bool isPerfect)
    {
        if (onlyOnPerfect && !isPerfect) return;

        // Un perfecto nuevo reinicia el destello desde el máximo, aunque el
        // anterior siguiera apagándose.
        lastFlashTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - lastFlashTime;
        float flash;

        if (elapsed < 0f || float.IsInfinity(elapsed))
        {
            flash = 0f;
        }
        else if (elapsed <= holdDuration)
        {
            flash = 1f;
        }
        else
        {
            float t = (elapsed - holdDuration) / Mathf.Max(fadeDuration, 0.01f);
            flash = Mathf.Clamp01(1f - t);
            flash = flash * flash; // se apaga más rápido al principio, con cola suave
        }

        Shader.SetGlobalFloat(FlashId, flash);
    }
}
