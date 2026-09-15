using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

// Lee el BeatMapData y va instanciando notas en el momento justo para que
// cada una llegue exactamente en su beat. El jugador la "agarra" apretando
// el botón del control (no tocándola con la mano) — cuando lo aprieta, se
// resuelve la nota activa que esté más cerca de su momento ideal.
public class NoteSpawner : MonoBehaviour
{
    public AudioSource audioSource;
    public BeatMapData beatMap;
    public GameObject notePrefab;

    [Tooltip("De dónde aparece la nota (adelante del jugador, dentro de su campo de visión cómodo).")]
    public Transform spawnPoint;

    [Tooltip("Adónde llega la nota si nadie la agarra.")]
    public Transform targetPoint;

    [Tooltip("Cuánto tarda en viajar del spawn al target. Con el espaciado del beatmap (mínimo 0.8s), esto da a lo sumo 1-2 notas en vuelo a la vez.")]
    public float travelTime = 1.5f;

    [Tooltip("La acción 'Activate' del control (ej. XRI Right/Activate), que es el gatillo. Arrastrala desde el asset de Input Actions del proyecto.")]
    public InputActionReference activateAction;

    // Un solo lugar donde escuchar TODAS las notas resueltas, sin que Note
    // necesite conocer a CloudGrowth/CometEffect/StarField/PerfectTracker.
    // (quality, isPerfect, posición)
    public event Action<float, bool, Vector3> OnNoteResolved;

    private int nextBeatIndex;
    private readonly List<Note> activeNotes = new List<Note>();

    void OnEnable()
    {
        if (activateAction != null) activateAction.action.Enable();
    }

    void OnDisable()
    {
        if (activateAction != null) activateAction.action.Disable();
    }

    void Update()
    {
        HandleSpawning();
        HandleButtonPress();
    }

    private void HandleSpawning()
    {
        if (beatMap == null || nextBeatIndex >= beatMap.beatTimestamps.Count) return;

        float nextBeat = beatMap.beatTimestamps[nextBeatIndex];

        if (audioSource.time >= nextBeat - travelTime)
        {
            SpawnNote(nextBeat);
            nextBeatIndex++;
        }
    }

    private void HandleButtonPress()
    {
        if (activateAction == null) return;

        // WasPressedThisFrame ya distingue el instante del apriete de mantenerlo sostenido.
        if (activateAction.action.WasPressedThisFrame())
        {
            TryResolveClosestNote();
        }
    }

    private void TryResolveClosestNote()
    {
        if (activeNotes.Count == 0) return;

        Note closest = activeNotes[0];
        float closestDiff = Mathf.Abs(audioSource.time - closest.TargetBeatTime);

        for (int i = 1; i < activeNotes.Count; i++)
        {
            float diff = Mathf.Abs(audioSource.time - activeNotes[i].TargetBeatTime);
            if (diff < closestDiff)
            {
                closest = activeNotes[i];
                closestDiff = diff;
            }
        }

        closest.ResolveByButtonPress(audioSource.time);
    }

    private void SpawnNote(float beatTime)
    {
        GameObject obj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        Note note = obj.GetComponent<Note>();

        note.Init(spawnPoint.position, targetPoint.position, beatTime, travelTime);
        activeNotes.Add(note);

        note.OnResolved += (quality, isPerfect, position) =>
        {
            activeNotes.Remove(note);
            OnNoteResolved?.Invoke(quality, isPerfect, position);
        };
    }
}
