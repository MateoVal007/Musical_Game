using UnityEngine;
using System;
using System.Collections.Generic;

// Lee el BeatMapData y va instanciando notas en el momento justo para que
// cada una llegue exactamente en su beat. El jugador la "agarra" con un
// golpe de mano (movimiento de batería con el control derecho, ver
// DrumHitDetector) — cuando golpea, se resuelve la nota activa que esté
// más cerca de su momento ideal.
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

    [Tooltip("Detector del golpe de tambor con la mano derecha (arrastralo del mismo objeto donde lo hayas puesto).")]
    public DrumHitDetector drumHitDetector;

    [Tooltip("Cuánto pesa la música al elegir la dirección de cada nota, además de la base pareja (25% cada una). 0 = puro azar. Subilo con cuidado: los graves suelen estar altos casi todo el tiempo en casi cualquier canción, así que un valor muy alto vuelve a hacer que casi todo salga 'abajo'.")]
    [Range(0f, 3f)]
    public float audioDirectionBias = 1f;

    // Un solo lugar donde escuchar TODAS las notas resueltas, sin que Note
    // necesite conocer a CloudGrowth/CometEffect/StarField/PerfectTracker.
    // (quality, isPerfect, posición)
    public event Action<float, bool, Vector3> OnNoteResolved;

    // Solo cuando el jugador ACERTÓ con el gesto. OnNoteResolved también se
    // dispara cuando una nota llega sola sin que la toquen, y para cosas como
    // la vibración eso estaría mal: vibraría sin que hicieras nada.
    // (quality, isPerfect)
    public event Action<float, bool> OnNoteHitByPlayer;

    private int nextBeatIndex;
    private readonly List<Note> activeNotes = new List<Note>();

    void OnEnable()
    {
        if (drumHitDetector != null) drumHitDetector.OnDirectionalHit += TryResolveClosestNote;
    }

    void OnDisable()
    {
        if (drumHitDetector != null) drumHitDetector.OnDirectionalHit -= TryResolveClosestNote;
    }

    void Update()
    {
        HandleSpawning();
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

    // Solo mira las notas activas que coincidan con la dirección del gesto —
    // si ninguna coincide, no pasa nada (nunca hay fallo).
    private void TryResolveClosestNote(NoteDirection direction)
    {
        Note closest = null;
        float closestDiff = float.MaxValue;

        foreach (Note note in activeNotes)
        {
            if (note.Direction != direction) continue;

            float diff = Mathf.Abs(audioSource.time - note.TargetBeatTime);
            if (diff < closestDiff)
            {
                closest = note;
                closestDiff = diff;
            }
        }

        closest?.TryResolveByDirection(direction, audioSource.time);
    }

    private void SpawnNote(float beatTime)
    {
        GameObject obj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        Note note = obj.GetComponent<Note>();

        note.Init(spawnPoint, targetPoint, beatTime, travelTime, PickDirection());
        activeNotes.Add(note);

        note.OnResolved += (quality, isPerfect, position) =>
        {
            activeNotes.Remove(note);
            OnNoteResolved?.Invoke(quality, isPerfect, position);

            if (note.WasHitByPlayer)
            {
                OnNoteHitByPlayer?.Invoke(quality, isPerfect);
            }
        };
    }

    // Base pareja (25% cada dirección) + un empujón acotado según la banda
    // de audio dominante en ese momento. Empuje ADITIVO, no proporcional al
    // nivel crudo — si fuera proporcional, la banda que esté MÁS ALTA en
    // promedio (casi siempre los graves, en casi cualquier canción) se
    // comería casi todas las notas. Con este esquema, aunque bass esté
    // siempre en 1 y el resto en 0, "abajo" nunca pasa de la mitad del total.
    private NoteDirection PickDirection()
    {
        float bass = 0f, mid = 0f, treble = 0f;
        AudioAnalyzer analyzer = AudioAnalyzer.Instance;
        if (analyzer != null)
        {
            bass = analyzer.Bass;
            mid = analyzer.Mid;
            treble = analyzer.Treble;
        }

        float weightDown = 1f + bass * audioDirectionBias;
        float weightUp = 1f + treble * audioDirectionBias;
        float weightLeft = 1f + mid * audioDirectionBias * 0.5f;
        float weightRight = 1f + mid * audioDirectionBias * 0.5f;

        float total = weightDown + weightUp + weightLeft + weightRight;
        float r = UnityEngine.Random.value * total;

        if (r < weightDown) return NoteDirection.Down;
        r -= weightDown;
        if (r < weightUp) return NoteDirection.Up;
        r -= weightUp;
        if (r < weightLeft) return NoteDirection.Left;
        return NoteDirection.Right;
    }
}
