using UnityEngine;

// Guarda el resultado de la partida cuando termina la canción: puntuación,
// veces jugadas y si se consiguió la luna.
//
// Va en el nivel, junto al PerfectTracker. Es un componente aparte y no
// código metido adentro del PerfectTracker porque el tracker se ocupa de
// CONTAR durante la partida; persistir es otra responsabilidad y otro
// momento. Separados, se puede cambiar cómo se guarda sin tocar cómo se
// cuenta.
public class RunRecorder : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;

    [Tooltip("Quién sabe si la luna se consiguió en esta partida.")]
    [SerializeField] private MoonCollectible moonCollectible;

    [Tooltip("Identificador de esta canción. Tiene que coincidir con el saveKey del MoonCollectible.")]
    [SerializeField] private string songId = "Moon_DreamIvory";

    void OnEnable()
    {
        if (perfectTracker != null) perfectTracker.OnSongEnded += HandleSongEnded;
    }

    void OnDisable()
    {
        if (perfectTracker != null) perfectTracker.OnSongEnded -= HandleSongEnded;
    }

    private void HandleSongEnded()
    {
        if (perfectTracker == null) return;

        bool moon = moonCollectible != null && moonCollectible.UnlockedThisRun;

        SaveSystem.RecordRun(songId, perfectTracker.PerfectCount, perfectTracker.TotalNotes, moon);

        SongProgress progress = SaveSystem.GetOrCreateSong(songId);
        Debug.Log($"[RunRecorder] Partida guardada. Esta vez: {perfectTracker.PerfectCount}/{perfectTracker.TotalNotes}. " +
                  $"Récord: {progress.bestPerfects}/{progress.bestTotalNotes} ({progress.BestPercentage:P0}). " +
                  $"Jugadas: {progress.timesPlayed}. Luna: {progress.moonUnlocked}.");
    }
}
