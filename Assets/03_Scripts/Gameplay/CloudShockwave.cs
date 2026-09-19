using UnityEngine;

// Cuando un cometa llega a la nube, dispara una onda expansiva que se ve en
// la superficie (propiedades _RippleOrigin/_RippleTime del shader
// Custom/Cloud). Se suscribe a CometEffect.OnCometArrived.
//
// Solo soporta UNA onda activa a la vez — si llega un cometa mientras otra
// onda sigue expandiéndose, la reemplaza desde cero. Para esta escala de
// juego (una nota resuelta a la vez, con su cooldown) no hace falta más.
public class CloudShockwave : MonoBehaviour
{
    [SerializeField] private CometEffect cometEffect;
    [SerializeField] private Renderer cloudRenderer;

    [Tooltip("Cuánto dura la onda en pantalla (segundos) antes de dejar de dibujarse. Tiene que ser al menos Ripple Max Radius / Ripple Speed (del material) para que le dé tiempo a apagarse sola en vez de cortarse de golpe.")]
    [SerializeField] private float rippleDuration = 2.2f;

    private MaterialPropertyBlock propertyBlock;
    private float rippleElapsed = -1f;

    void Awake()
    {
        if (cloudRenderer == null) cloudRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (cometEffect != null) cometEffect.OnCometArrived += HandleCometArrived;
    }

    void OnDisable()
    {
        if (cometEffect != null) cometEffect.OnCometArrived -= HandleCometArrived;
    }

    private void HandleCometArrived(Vector3 position)
    {
        rippleElapsed = 0f;

        cloudRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector("_RippleOrigin", position);
        cloudRenderer.SetPropertyBlock(propertyBlock);
    }

    void Update()
    {
        if (rippleElapsed < 0f) return;

        rippleElapsed += Time.deltaTime;

        cloudRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_RippleTime", rippleElapsed);
        cloudRenderer.SetPropertyBlock(propertyBlock);

        if (rippleElapsed >= rippleDuration)
        {
            rippleElapsed = -1f;

            cloudRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_RippleTime", -1f);
            cloudRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
