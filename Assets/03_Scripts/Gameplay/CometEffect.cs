using UnityEngine;

// En un toque perfecto, instancia un "cometa" (partículas/trail) que vuela
// desde donde se tocó la nota hasta la nube. Se suscribe a
// NoteSpawner.OnNoteResolved y solo reacciona cuando isPerfect es true.
public class CometEffect : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [Tooltip("Prefab con un Particle System / Trail Renderer + el script Comet.")]
    [SerializeField] private GameObject cometPrefab;

    [Tooltip("Hacia dónde vuela el cometa (normalmente la nube).")]
    [SerializeField] private Transform destination;

    void OnEnable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved += HandleNoteResolved;
    }

    void OnDisable()
    {
        if (noteSpawner != null) noteSpawner.OnNoteResolved -= HandleNoteResolved;
    }

    private void HandleNoteResolved(float quality, bool isPerfect, Vector3 position)
    {
        if (!isPerfect || cometPrefab == null || destination == null) return;

        GameObject cometObj = Instantiate(cometPrefab, position, Quaternion.identity);
        Comet comet = cometObj.GetComponent<Comet>();
        if (comet != null)
        {
            comet.FlyTo(destination.position);
        }
    }
}
