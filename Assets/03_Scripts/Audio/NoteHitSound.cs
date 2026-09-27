using UnityEngine;

// El sonido de pegarle a una nota. Sintetizado al arrancar, sin archivos.
//
// Son dos capas: un cuerpo percusivo (el golpe en sí) y un destello agudo
// que SOLO suena en los perfectos. Esa segunda capa es la que convierte el
// sonido en información: el jugador escucha si clavó el tiempo sin tener que
// mirar ningún número.
//
// El tono cambia según la dirección de la flecha — arriba agudo, abajo
// grave — así una secuencia de notas bien tocada arma una melodía en vez de
// repetir el mismo click. Los intervalos son de escala, no cualquier número.
//
// VA EN CUALQUIER OBJETO de la escena. Necesita que le asignes el NoteSpawner.
public class NoteHitSound : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [Header("Mezcla")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.55f;

    [Range(0f, 1f)]
    [Tooltip("Volumen de un acierto flojo. Los buenos suenan más fuerte, así el volumen también es información.")]
    [SerializeField] private float minVolume = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("Cuánto suma el destello agudo de los perfectos.")]
    [SerializeField] private float sparkleVolume = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("0 = suena igual en los dos oídos. Subilo y el golpe viene de donde estaba la nota. En el Quest cada fuente espacializada cuesta CPU, así que no lo subas de más.")]
    [SerializeField] private float spatialBlend = 0.6f;

    [Header("El cuerpo del golpe")]
    [Tooltip("Frecuencia base, en Hz. Más bajo = más tambor grande.")]
    [SerializeField] private float bodyFrequency = 190f;

    [Tooltip("Qué tan rápido se apaga. Alto = seco y corto.")]
    [SerializeField] private float bodyDecay = 26f;

    [Tooltip("Cuánto cae el tono en el ataque. Esto es lo que hace que suene a golpe y no a pitido: un parche real baja de tono mientras se destensa.")]
    [SerializeField] private float pitchDrop = 1.6f;

    [Range(0f, 1f)]
    [Tooltip("Ruido en el ataque: el 'chas' del contacto. Sin esto el golpe no tiene filo.")]
    [SerializeField] private float clickAmount = 0.35f;

    [Header("Voces simultáneas")]
    [Tooltip("Cuántos golpes pueden sonar encima. Si las notas vienen muy rápido y escuchás que se cortan entre sí, subilo.")]
    [SerializeField] private int voices = 8;

    private AudioClip bodyClip;
    private AudioClip sparkleClip;
    private AudioSource[] pool;
    private int nextVoice;

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitDetailed += HandleHit;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitDetailed -= HandleHit;
    }

    void Start()
    {
        int sampleRate = AudioSettings.outputSampleRate;

        bodyClip = BuildBody(sampleRate);
        sparkleClip = BuildSparkle(sampleRate);

        // Una fuente sola no alcanza: al volver a llamar Play() cortaría el
        // golpe anterior a la mitad. Con varias, los golpes se superponen
        // como lo harían de verdad.
        pool = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("HitVoice " + i);
            go.transform.SetParent(transform, false);

            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.clip = bodyClip;
            s.spatialBlend = spatialBlend;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = 4f;
            s.maxDistance = 60f;
            pool[i] = s;
        }
    }

    private void HandleHit(float quality, bool isPerfect, NoteDirection direction, Vector3 position)
    {
        if (pool == null || pool.Length == 0) return;

        AudioSource s = pool[nextVoice];
        nextVoice = (nextVoice + 1) % pool.Length;

        s.transform.position = position;
        s.pitch = PitchFor(direction);
        s.volume = Mathf.Lerp(minVolume, 1f, Mathf.Clamp01(quality)) * volume;

        s.Play();

        if (isPerfect) s.PlayOneShot(sparkleClip, sparkleVolume * volume);
    }

    // Intervalos de una escala, no números al azar: 1 / 1.189 / 1.414 / 1.682
    // son cuarta, tritono y sexta respecto de la base. Cualquier combinación
    // de direcciones suena afinada entre sí.
    private static float PitchFor(NoteDirection direction)
    {
        switch (direction)
        {
            case NoteDirection.Down: return 1f;
            case NoteDirection.Left: return 1.189f;
            case NoteDirection.Right: return 1.414f;
            default: return 1.682f;   // Up
        }
    }

    private AudioClip BuildBody(int sampleRate)
    {
        int samples = Mathf.RoundToInt(sampleRate * 0.35f);
        var data = new float[samples];

        var rng = new System.Random(12345);
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;

            // El tono arranca alto y cae rápido a la frecuencia base.
            float freq = bodyFrequency * (1f + pitchDrop * Mathf.Exp(-t * 35f));
            phase += 2f * Mathf.PI * freq / sampleRate;

            float envelope = Mathf.Exp(-t * bodyDecay);
            float tone = Mathf.Sin(phase) * envelope;

            // Ruido muy corto encima: el contacto de la baqueta.
            float click = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 160f) * clickAmount;

            data[i] = Mathf.Clamp(tone + click, -1f, 1f) * 0.85f;
        }

        var clip = AudioClip.Create("NoteHitBody", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildSparkle(int sampleRate)
    {
        int samples = Mathf.RoundToInt(sampleRate * 0.5f);
        var data = new float[samples];

        // Tres parciales en proporciones no enteras: así suena a campana y no
        // a nota de órgano. Los armónicos enteros dan tono musical; los
        // inarmónicos dan metal.
        const float f1 = 1480f, f2 = 2260f, f3 = 3570f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * 9f);

            float s = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.3f
                    + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.2f;

            data[i] = s * envelope * 0.6f;
        }

        var clip = AudioClip.Create("NoteHitSparkle", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
