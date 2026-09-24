using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

// Un golpecito de vibración en el control cuando el jugador acierta una nota.
// Se engancha a OnNoteHitByPlayer (no a OnNoteResolved) a propósito: si usara
// el otro, vibraría también cuando una nota llega sola sin que la toquen.
//
// Pulsos cortos y suaves: en VR la vibración larga o fuerte cansa la mano
// enseguida, y acá se dispara una vez por nota durante toda la canción.
public class HitHaptics : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [Tooltip("El Haptic Impulse Player del control derecho.")]
    [SerializeField] private HapticImpulsePlayer hapticPlayer;

    [Range(0f, 1f)]
    [Tooltip("Intensidad de un acierto normal.")]
    [SerializeField] private float amplitude = 0.12f;

    [Range(0f, 1f)]
    [Tooltip("Intensidad de un perfecto — apenas más, para que se note la diferencia sin molestar.")]
    [SerializeField] private float perfectAmplitude = 0.22f;

    [Tooltip("Duración del pulso en segundos. Corto: es un golpe, no un zumbido.")]
    [SerializeField] private float duration = 0.06f;

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer += HandleHit;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteHitByPlayer -= HandleHit;
    }

    private void HandleHit(float quality, bool isPerfect)
    {
        if (hapticPlayer == null) return;

        hapticPlayer.SendHapticImpulse(isPerfect ? perfectAmplitude : amplitude, duration);
    }
}
