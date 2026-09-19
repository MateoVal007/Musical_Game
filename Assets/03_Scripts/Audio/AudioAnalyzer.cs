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

    [Header("Calibración por banda")]
    [Tooltip("Multiplicador de graves. Bajalo si Bass se queda siempre pegado cerca de 1.")]
    public float bassScale = 50f;
    [Tooltip("Multiplicador de medios. Subilo si Mid se queda siempre pegado cerca de 0.")]
    public float midScale = 50f;
    [Tooltip("Multiplicador de agudos.")]
    public float trebleScale = 50f;

    [Tooltip("Curva de contraste por banda (gamma) DESPUÉS de aplicar la escala — 1 = sin cambio, menor a 1 empuja valores bajos hacia arriba (más sensible), mayor a 1 los empuja hacia abajo (menos sensible). Mismo truco que Contrast Power en los stems.")]
    public float bassContrastPower = 1f;
    public float midContrastPower = 1f;
    public float trebleContrastPower = 1f;

    [Header("Rango real observado (estira el piso/techo real a 0-1)")]
    [Tooltip("El valor de Bass más BAJO que viste en la consola durante partes flojas. Todo lo que esté en o por debajo de esto pasa a ser 0 (apagado de verdad).")]
    public float bassFloor = 0f;
    [Tooltip("El valor de Bass más ALTO que viste en la consola durante partes fuertes. Todo lo que esté en o por encima de esto pasa a ser 1 (máximo de verdad).")]
    public float bassCeiling = 1f;
    public float midFloor = 0f;
    public float midCeiling = 1f;
    public float trebleFloor = 0f;
    public float trebleCeiling = 1f;

    [Tooltip("Activalo para ver Bass/Mid/Treble en la consola cuadro a cuadro mientras suena la canción — son propiedades de C#, no aparecen solas en el Inspector. Apagalo después, tira un log por frame.")]
    [SerializeField] private bool debugLog = false;

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

        float bassScaled = Mathf.Clamp01(rawBass * bassScale);
        float midScaled = Mathf.Clamp01(rawMid * midScale);
        float trebleScaled = Mathf.Clamp01(rawTreble * trebleScale);

        // Estira el rango real (piso-techo observados) a 0-1 ANTES del
        // contraste — si no, un piso que nunca baja de 0.27 hace que todo
        // se sienta "siempre prendido", por más contraste que le pongas
        // después (pow(x, cualquier cosa) no puede arreglar un rango que ya
        // viene comprimido en el medio).
        float bassNorm = Mathf.InverseLerp(bassFloor, bassCeiling, bassScaled);
        float midNorm = Mathf.InverseLerp(midFloor, midCeiling, midScaled);
        float trebleNorm = Mathf.InverseLerp(trebleFloor, trebleCeiling, trebleScaled);

        float bass = Mathf.Pow(Mathf.Clamp01(bassNorm), bassContrastPower);
        float mid = Mathf.Pow(Mathf.Clamp01(midNorm), midContrastPower);
        float treble = Mathf.Pow(Mathf.Clamp01(trebleNorm), trebleContrastPower);

        Bass = Smooth(Bass, bass);
        Mid = Smooth(Mid, mid);
        Treble = Smooth(Treble, treble);

        if (debugLog)
        {
            Debug.Log($"Bass: {Bass:F3} | Mid: {Mid:F3} | Treble: {Treble:F3}");
        }
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
