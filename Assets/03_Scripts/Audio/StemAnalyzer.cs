using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StemTrack
{
    public string trackName;
    public AudioSource source; // tiene que estar en Mute (no Volume 0) y arrancar sincronizado con la canción principal
    public Color color = Color.white;

    [Range(1f, 4f)]
    [Tooltip("Curva de contraste PROPIA de esta pista: más alto = más diferencia entre silencio y fuerte. Subilo en pistas que están 'siempre altas' (ej. 'Otros', donde suele caer la guitarra) para que igual se note el silencio.")]
    public float contrastPower = 2f;

    public float smoothedIntensity; // visible temporalmente en el Inspector para depurar
}

// Analiza VARIAS pistas de audio a la vez (los stems separados de la canción:
// voz, batería, bajo, otros), cada una con su propia intensidad suavizada y
// su color asignado. Las estrellas le preguntan a este singleton, por índice
// de pista, cuánto "brillo" les corresponde en cada momento.
public class StemAnalyzer : MonoBehaviour
{
    public static StemAnalyzer Instance { get; private set; }

    public List<StemTrack> stems = new List<StemTrack>();

    [SerializeField] private int sampleSize = 1024;
    [SerializeField] private float smoothSpeed = 8f;

    [Tooltip("Multiplicador de la señal detectada. Subilo si el Glow Intensity nunca llega a valores altos.")]
    [SerializeField] private float sensitivityMultiplier = 300f;

    [Range(0.1f, 1f)]
    [Tooltip("Qué fracción del espectro (desde el grave) se promedia. Menos que 1 evita diluir el promedio con las frecuencias agudas casi silenciosas.")]
    [SerializeField] private float analyzedFraction = 0.5f;

    private float[] spectrum;

    void Awake()
    {
        if (Instance == null) Instance = this;
        spectrum = new float[sampleSize];
    }

    void Update()
    {
        foreach (StemTrack stem in stems)
        {
            if (stem.source == null || !stem.source.isPlaying) continue;

            stem.source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

            int limit = Mathf.Max(1, Mathf.RoundToInt(spectrum.Length * analyzedFraction));
            float sum = 0f;
            for (int i = 0; i < limit; i++)
            {
                sum += spectrum[i];
            }

            float raw = Mathf.Clamp01((sum / limit) * sensitivityMultiplier);
            raw = Mathf.Pow(raw, stem.contrastPower); // aplasta los valores bajos mucho más que los altos, distinto por pista
            stem.smoothedIntensity = Mathf.Lerp(stem.smoothedIntensity, raw, smoothSpeed * Time.deltaTime);
        }
    }

    public int StemCount => stems.Count;

    public float GetIntensity(int index)
    {
        if (index < 0 || index >= stems.Count) return 0f;
        return stems[index].smoothedIntensity;
    }

    public Color GetColor(int index)
    {
        if (index < 0 || index >= stems.Count) return Color.white;
        return stems[index].color;
    }

    public int GetRandomStemIndex()
    {
        return stems.Count > 0 ? Random.Range(0, stems.Count) : -1;
    }
}
