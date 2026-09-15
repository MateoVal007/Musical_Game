using UnityEngine;

// El corazón del sistema audio-reactivo. Analiza el espectro de la canción
// cada frame con la FFT que trae Unity, lo agrupa en 3 bandas (graves/medios/
// agudos) y las suaviza para que el resto del juego pueda reaccionar sin
// que todo tiemble por el ruido crudo del análisis.
//
// Cualquier otro script le pregunta a AudioAnalyzer.Instance.Bass / Mid / Treble
// (todos entre 0 y 1). No hace falta que nada más toque la FFT directamente.
public class AudioAnalyzer : MonoBehaviour
{
    public static AudioAnalyzer Instance { get; private set; }

    [Tooltip("El AudioSource que reproduce la canción. Tiene que estar sonando para que haya datos.")]
    public AudioSource audioSource;

    [Tooltip("Tamaño del análisis FFT. Tiene que ser potencia de 2 (512, 1024, 2048...).")]
    public int sampleSize = 1024;

    [Tooltip("Qué tan rápido reacciona cada banda a los cambios. Más alto = más nervioso, más bajo = más suave/lento.")]
    public float smoothSpeed = 8f;

    public float Bass { get; private set; }
    public float Mid { get; private set; }
    public float Treble { get; private set; }

    private float[] spectrum;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        spectrum = new float[sampleSize];
    }

    void Update()
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // El array va de graves (índice 0) a agudos (índice sampleSize-1).
        // Dividimos en 3 tercios simples; se puede afinar más adelante si hace falta.
        int third = spectrum.Length / 3;

        float rawBass = AverageRange(0, third);
        float rawMid = AverageRange(third, third * 2);
        float rawTreble = AverageRange(third * 2, spectrum.Length);

        // La FFT devuelve valores muy chicos (por eso el * 50, para llevarlos
        // a un rango 0-1 más manejable). Este multiplicador es el primero que
        // conviene ajustar a ojo si todo sale siempre muy apagado o saturado.
        float scale = 50f;

        Bass = Smooth(Bass, Mathf.Clamp01(rawBass * scale));
        Mid = Smooth(Mid, Mathf.Clamp01(rawMid * scale));
        Treble = Smooth(Treble, Mathf.Clamp01(rawTreble * scale));
    }

    private float AverageRange(int start, int end)
    {
        float sum = 0f;
        for (int i = start; i < end; i++)
        {
            sum += spectrum[i];
        }
        return sum / (end - start);
    }

    private float Smooth(float current, float target)
    {
        return Mathf.Lerp(current, target, smoothSpeed * Time.deltaTime);
    }
}
