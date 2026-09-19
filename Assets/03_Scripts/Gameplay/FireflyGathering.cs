using UnityEngine;

// Cuando se desbloquea la luna, las luciérnagas se van juntando de a poco a
// su alrededor. El movimiento lo hace el shader Custom/Firefly; este script
// solo le va subiendo _GatherAmount de 0 a 1 y le dice dónde está la luna.
//
// Como todas las luciérnagas son un solo mesh/renderer, alcanza con un
// MaterialPropertyBlock — no se toca el material compartido ni se crean
// instancias.
public class FireflyGathering : MonoBehaviour
{
    [SerializeField] private MoonCollectible moonCollectible;
    [SerializeField] private Renderer fireflyRenderer;

    [Tooltip("Cuánto tardan en terminar de juntarse, en segundos. Largo a propósito: la gracia es que se note que van llegando de a poco.")]
    [SerializeField] private float gatherDuration = 20f;

    [Tooltip("Qué tan lejos de la luna se quedan revoloteando.")]
    [SerializeField] private float gatherRadius = 2.5f;

    private MaterialPropertyBlock propertyBlock;
    private float elapsed = -1f;

    void Awake()
    {
        if (fireflyRenderer == null) fireflyRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (moonCollectible != null) moonCollectible.OnMoonUnlocked += HandleMoonUnlocked;
    }

    void OnDisable()
    {
        if (moonCollectible != null) moonCollectible.OnMoonUnlocked -= HandleMoonUnlocked;
    }

    private void HandleMoonUnlocked(Vector3 moonPosition)
    {
        elapsed = 0f;

        fireflyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector("_GatherTarget", moonPosition);
        propertyBlock.SetFloat("_GatherRadius", gatherRadius);
        fireflyRenderer.SetPropertyBlock(propertyBlock);
    }

    void Update()
    {
        if (elapsed < 0f) return;

        elapsed += Time.deltaTime;
        float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(gatherDuration, 0.01f)));

        fireflyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_GatherAmount", amount);
        fireflyRenderer.SetPropertyBlock(propertyBlock);

        if (amount >= 1f) elapsed = -1f;
    }
}
