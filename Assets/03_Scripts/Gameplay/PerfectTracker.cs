using UnityEngine;
using System;

// Cuenta cuántas notas se resolvieron y cuántas de ellas fueron perfectas,
// comparando contra el total del BeatMapData. Avisa una sola vez cuando
// termina la canción, con el resultado ya calculado (AllPerfect).
public class PerfectTracker : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private BeatMapData beatMap;
    [SerializeField] private AudioSource audioSource;

    [Tooltip("El final 'real' de la música (sin el silencio de cola), en segundos.")]
    [SerializeField] private float songEndTime = 205f;

    public int ResolvedCount { get; private set; }
    public int PerfectCount { get; private set; }
    public int TotalNotes => beatMap != null ? beatMap.beatTimestamps.Count : 0;

    public bool AllPerfect =>
        beatMap != null &&
        beatMap.beatTimestamps.Count > 0 &&
        ResolvedCount == beatMap.beatTimestamps.Count &&
        PerfectCount == beatMap.beatTimestamps.Count;

    public event Action OnSongEnded;

    // Avisa cada vez que cambian los contadores, para lo que necesite
    // reaccionar DURANTE la canción y no al final (ej. la luna, que aparece
    // en el momento exacto en que se alcanza el porcentaje).
    public event Action OnProgressChanged;

    private bool songEndedFired;

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved += HandleNoteResolved;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved -= HandleNoteResolved;
    }

    void Update()
    {
        if (songEndedFired || audioSource == null) return;

        if (audioSource.time >= songEndTime)
        {
            songEndedFired = true;
            OnSongEnded?.Invoke();
        }
    }

    private void HandleNoteResolved(float quality, bool isPerfect, Vector3 position)
    {
        ResolvedCount++;
        if (isPerfect) PerfectCount++;

        OnProgressChanged?.Invoke();
    }
}
