using UnityEngine;

// Enciende un grupo de flores de golpe y las deja apagarse solas — como una
// luciérnaga, pero disparada por el juego.
//
// Hay dos grupos y cada uno responde a su propio evento, por eso el canal:
//   - Canal A: las flores azules, con los perfectos del jugador.
//   - Canal B: las flores blancas del anillo de la nube, cuando llega el cometa.
// El material de cada grupo elige qué canal escucha con _FlashChannel.
//
// La curva de apagado se calcula acá y se manda como un solo número al
// shader Custom/Flower. Hacerlo en C# deja el control del tiempo en el
// Inspector, en segundos entendibles.
public class FlowerWave : MonoBehaviour
{
    public enum Trigger { PerfectoDelJugador, LlegadaDelCometa }
    public enum Channel { A, B }

    [Header("Qué lo dispara")]
    [SerializeField] private Trigger trigger = Trigger.PerfectoDelJugador;

    [Tooltip("Tiene que coincidir con el _FlashChannel del material de estas flores.")]
    [SerializeField] private Channel channel = Channel.A;

    [Tooltip("Solo para el disparador por perfectos.")]
    [SerializeField] private NoteSpawner noteSpawner;

    [Tooltip("Solo para el disparador por cometa.")]
    [SerializeField] private CometEffect cometEffect;

    [Tooltip("Con los perfectos: si está apagado, también dispara con aciertos que no fueron perfectos.")]
    [SerializeField] private bool onlyOnPerfect = true;

    [Header("Tiempos")]
    [Tooltip("Segundos que se queda al máximo antes de empezar a bajar.")]
    [SerializeField] private float holdDuration = 0.05f;

    [Tooltip("Cuánto tarda en apagarse del todo después del destello, en segundos.")]
    [SerializeField] private float fadeDuration = 0.6f;

    private static readonly int FlashAId = Shader.PropertyToID("_FlowerFlashA");
    private static readonly int FlashBId = Shader.PropertyToID("_FlowerFlashB");

    private float lastFlashTime = float.NegativeInfinity;

    private int PropertyId => channel == Channel.A ? FlashAId : FlashBId;

    void OnEnable()
    {
        if (trigger == Trigger.PerfectoDelJugador)
        {
            if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer += HandleHit;
        }
        else
        {
            if (cometEffect != null) cometEffect.OnCometArrived += HandleCometArrived;
        }
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer -= HandleHit;
        if (cometEffect != null) cometEffect.OnCometArrived -= HandleCometArrived;

        Shader.SetGlobalFloat(PropertyId, 0f);
    }

    private void HandleHit(float quality, bool isPerfect)
    {
        if (onlyOnPerfect && !isPerfect) return;
        Flash();
    }

    private void HandleCometArrived(Vector3 position)
    {
        Flash();
    }

    private void Flash()
    {
        // Un disparo nuevo reinicia el destello desde el máximo, aunque el
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

        Shader.SetGlobalFloat(PropertyId, flash);
    }
}
