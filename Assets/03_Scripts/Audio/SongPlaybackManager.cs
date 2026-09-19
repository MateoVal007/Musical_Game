using UnityEngine;

// Arranca la canción principal y TODAS las pistas (stems) exactamente en el
// mismo instante, para que el análisis de cada stem quede sincronizado con
// lo que realmente suena. Reemplaza el "Play On Awake" individual de cada
// Audio Source — con varios "Play On Awake" sueltos no hay garantía de que
// arranquen en el mismo frame exacto.
public class SongPlaybackManager : MonoBehaviour
{
    [SerializeField] private AudioSource mainSong;
    [SerializeField] private StemAnalyzer stemAnalyzer;

    void Start()
    {
        if (mainSong != null)
        {
            mainSong.Play();
        }

        if (stemAnalyzer != null)
        {
            foreach (StemTrack stem in stemAnalyzer.stems)
            {
                if (stem.source != null)
                {
                    stem.source.Play();
                }
            }
        }
    }
}
