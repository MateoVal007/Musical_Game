using UnityEngine;

// Lluvia sintetizada en tiempo real. No usa ningún archivo de audio: la
// lluvia es ruido con una forma espectral determinada, así que sale más
// barato generarla que guardarla.
//
// Ventaja sobre un sample en loop: no hay loop. Un archivo de lluvia de unos
// segundos repitiéndose se delata enseguida — el oído encuentra el patrón y
// ya no lo puede ignorar. Esto no se repite nunca.
//
// Son dos capas: un rumor grave de fondo (la lluvia lejana, todo el campo) y
// un siseo agudo encima (las gotas cerca tuyo). La proporción entre las dos
// es lo que decide si suena a tormenta lejana o a estar abajo del agua.
//
// VA EN SU PROPIO GameObject con AudioSource. Poné el volumen por debajo de
// la música: la lluvia tiene que notarse cuando prestás atención, no competir.
[RequireComponent(typeof(AudioSource))]
public class RainAudio : MonoBehaviour
{
    [Header("Mezcla")]
    [Range(0f, 1f)]
    [Tooltip("Volumen general. Arrancá bajo (0.15 a 0.3): la lluvia suma clima, no protagonismo.")]
    [SerializeField] private float volume = 0.22f;

    [Range(0f, 1f)]
    [Tooltip("El rumor grave de fondo: la lluvia cayendo en todo el campo, lejos.")]
    [SerializeField] private float bodyLevel = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("El siseo agudo: las gotas cerca tuyo. Subilo y la lluvia se te viene encima; bajalo y queda de fondo.")]
    [SerializeField] private float hissLevel = 0.3f;

    [Header("Tono")]
    [Tooltip("Corte de la capa grave, en Hz. Más bajo = más lejana y sorda.")]
    [SerializeField] private float bodyCutoff = 700f;

    [Tooltip("Corte de la capa aguda, en Hz. Más alto = gotas más finas y cercanas.")]
    [SerializeField] private float hissCutoff = 2500f;

    [Header("Ráfagas")]
    [Tooltip("Qué tan rápido va y viene la intensidad. Bien lento: una lluvia real respira en decenas de segundos, no en dos.")]
    [SerializeField] private float gustSpeed = 0.13f;

    [Range(0f, 1f)]
    [Tooltip("Cuánto sube y baja con las ráfagas. Sin esto la lluvia queda como un ventilador: plana y evidente.")]
    [SerializeField] private float gustDepth = 0.35f;

    [Header("Clímax")]
    [Range(0f, 1f)]
    [Tooltip("Cuánto arrecia la lluvia siguiendo la curva de ClimaxIntensity. En 0 la lluvia ignora la canción.")]
    [SerializeField] private float climaxBoost = 0.35f;

    private AudioSource source;

    // Coeficientes de los filtros de un polo. Se calculan una vez: adentro
    // del callback de audio hay que hacer lo mínimo posible.
    private float bodyCoef;
    private float hissCoef;

    // Estado de los filtros, uno por canal. Separados a propósito: dos ruidos
    // independientes en L y R es lo que le da el ancho estéreo. Con el MISMO
    // ruido en los dos canales la lluvia colapsa a un punto en el centro de
    // la cabeza y deja de envolver.
    private float bodyStateL, bodyStateR;
    private float hissStateL, hissStateR;

    private uint rngL = 2463534242;
    private uint rngR = 1013904223;

    // Lo escribe el hilo principal en Update y lo lee el hilo de audio.
    private float currentAmplitude;
    private float gustPhase;

    private float fadeMultiplier = 1f;
    private float fadeSpeed;

    // Para el cierre de la canción: la lluvia se va apagando de a poco en vez
    // de cortarse. Una lluvia que para de golpe se nota muchísimo.
    public void FadeOut(float seconds)
    {
        fadeSpeed = 1f / Mathf.Max(0.01f, seconds);
    }

    void Start()
    {
        source = GetComponent<AudioSource>();

        int sampleRate = AudioSettings.outputSampleRate;
        bodyCoef = OnePoleCoefficient(bodyCutoff, sampleRate);
        hissCoef = OnePoleCoefficient(hissCutoff, sampleRate);

        // stream: true hace que Unity llame a GeneratePcm cada vez que
        // necesita más audio, en vez de pedir el buffer entero de una. Es lo
        // que permite generar sonido infinito sin ocupar memoria.
        AudioClip clip = AudioClip.Create("RainNoise", sampleRate, 2, sampleRate, true, GeneratePcm);

        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;   // la lluvia está en todos lados, no en un punto
        source.volume = volume;
        source.Play();
    }

    private static float OnePoleCoefficient(float cutoffHz, int sampleRate)
    {
        return 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Max(1f, cutoffHz) / sampleRate);
    }

    void Update()
    {
        gustPhase += gustSpeed * Time.deltaTime;

        // Dos senos de periodo distinto en vez de uno: uno solo se escucha
        // como un vaivén regular, y la lluvia de verdad no tiene compás.
        float gust = Mathf.Sin(gustPhase * 6.28318f) * 0.6f
                   + Mathf.Sin(gustPhase * 2.37f) * 0.4f;

        float climax = ClimaxIntensity.Instance != null ? ClimaxIntensity.Instance.Value : 0f;

        currentAmplitude = Mathf.Clamp01(1f + gust * gustDepth) * (1f + climax * climaxBoost);

        if (fadeSpeed > 0f)
        {
            fadeMultiplier = Mathf.MoveTowards(fadeMultiplier, 0f, fadeSpeed * Time.deltaTime);
        }

        if (source != null) source.volume = volume * fadeMultiplier;
    }

    // ¡Hilo de audio! Acá no se puede tocar nada de la API de Unity ni
    // reservar memoria: se corre cientos de veces por segundo y cualquier
    // pausa se escucha como un chasquido.
    private void GeneratePcm(float[] data)
    {
        float amp = currentAmplitude;
        float body = bodyLevel;
        float hiss = hissLevel;

        for (int i = 0; i < data.Length; i += 2)
        {
            data[i] = Sample(NextNoise(ref rngL), ref bodyStateL, ref hissStateL, body, hiss) * amp;
            data[i + 1] = Sample(NextNoise(ref rngR), ref bodyStateR, ref hissStateR, body, hiss) * amp;
        }
    }

    private float Sample(float noise, ref float bodyState, ref float hissState, float body, float hiss)
    {
        // Paso bajo: el estado persigue al ruido, y lo que no llega a seguir
        // son justamente las frecuencias altas. Queda el rumor.
        bodyState += (noise - bodyState) * bodyCoef;

        // Paso alto: lo mismo, pero nos quedamos con lo que el filtro NO pudo
        // seguir. Ahí están las gotas.
        hissState += (noise - hissState) * hissCoef;
        float high = noise - hissState;

        return bodyState * body + high * hiss;
    }

    // PRNG propio en vez de UnityEngine.Random: Random solo se puede llamar
    // desde el hilo principal, y esto corre en el de audio.
    private static float NextNoise(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state * (2f / uint.MaxValue) - 1f;
    }
}
