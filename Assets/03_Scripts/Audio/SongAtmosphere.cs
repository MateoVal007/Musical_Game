using UnityEngine;

// Le da lugar al sonido. La canción entraba plana y seca, directo a los oídos:
// sonaba a auriculares, no a una noche lluviosa de ciudad. Esto agrega reverb
// y un filtro que va abriendo con la curva de ClimaxIntensity, así la canción
// arranca lejana y apagada y "llega" junto con el cometa.
//
// VA EN EL MISMO OBJETO QUE EL AudioListener (la Main Camera), no en la
// fuente de la canción. Si los filtros se cuelgan de la fuente, el
// GetSpectrumData del AudioAnalyzer lee la señal YA FILTRADA: al principio
// los agudos estarían cortados, el análisis de treble daría casi cero y toda
// la calibración de bandas se iría al demonio. En el listener el filtro es lo
// último de la cadena: se escucha, pero nadie lo analiza.
[RequireComponent(typeof(AudioListener))]
public class SongAtmosphere : MonoBehaviour
{
    [Header("Filtro que abre con el clímax")]
    [Tooltip("Corte con la intensidad al mínimo: la canción sonando 'desde afuera'. Bajalo para un arranque más ahogado.")]
    [SerializeField] private float closedCutoff = 1400f;

    [Tooltip("Corte con la intensidad al máximo. 22000 = sin filtrar, la canción entera en la cara.")]
    [SerializeField] private float openCutoff = 22000f;

    [Range(0f, 10f)]
    [Tooltip("Suavizado. La curva de intensidad puede tener saltos y un filtro que salta se escucha como un click.")]
    [SerializeField] private float smoothSpeed = 1.5f;

    [Header("Reverb")]
    [SerializeField] private bool useReverb = true;

    [Tooltip("Cuánto tarda en apagarse la cola, en segundos. Más alto = espacio más grande y vacío.")]
    [SerializeField] private float decayTime = 2.6f;

    [Tooltip("Nivel del reverb con la intensidad al MÍNIMO, en dB. Arranca más mojado: suena lejos.")]
    [SerializeField] private float wetAtStart = -600f;

    [Tooltip("Nivel del reverb con la intensidad al MÁXIMO, en dB. Se seca al llegar el clímax, y eso es lo que da la sensación de que la canción se te acercó.")]
    [SerializeField] private float wetAtClimax = -1800f;

    private AudioLowPassFilter lowPass;
    private AudioReverbFilter reverb;
    private float smoothedIntensity;

    void Awake()
    {
        lowPass = GetComponent<AudioLowPassFilter>();
        if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.lowpassResonanceQ = 1f;

        if (!useReverb) return;

        reverb = GetComponent<AudioReverbFilter>();
        if (reverb == null) reverb = gameObject.AddComponent<AudioReverbFilter>();

        // User es el único preset que deja tocar los parámetros a mano.
        reverb.reverbPreset = AudioReverbPreset.User;
        reverb.decayTime = decayTime;
        reverb.dryLevel = 0f;
        // El preset User arranca con room en el piso (-10000 = sin sala). Sin
        // esta línea no se escucharía absolutamente nada de reverb.
        reverb.room = 0f;
        reverb.roomHF = -1200f;   // la cola pierde agudos: cuerpo, no siseo
        reverb.reflectionsLevel = -1500f;
        reverb.reverbDelay = 0.035f;
    }

    void Start()
    {
        // Sin esto el primer frame arrancaría en 0 y el filtro haría un barrido
        // de abierto a cerrado que se escucha como un "whoosh" al empezar.
        smoothedIntensity = CurrentIntensity();
    }

    private static float CurrentIntensity()
    {
        // Sin ClimaxIntensity no hay curva que seguir: dejar todo abierto es
        // lo mismo que no tener el efecto, que es el fallback correcto.
        return ClimaxIntensity.Instance != null ? ClimaxIntensity.Instance.Value : 1f;
    }

    void Update()
    {
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, CurrentIntensity(), smoothSpeed * Time.deltaTime);

        float t = Mathf.Clamp01(smoothedIntensity);

        // Interpolación LOGARÍTMICA, no lineal: el oído escucha en octavas.
        // Lineal de 1400 a 22000 se pasaría casi todo el recorrido arriba de
        // 10 kHz, donde ya no se nota nada, y el cambio se sentiría de golpe
        // al principio en vez de ir abriendo parejo.
        float low = Mathf.Max(10f, closedCutoff);
        lowPass.cutoffFrequency = low * Mathf.Pow(Mathf.Max(low, openCutoff) / low, t);

        if (reverb != null) reverb.reverbLevel = Mathf.Lerp(wetAtStart, wetAtClimax, t);
    }
}
