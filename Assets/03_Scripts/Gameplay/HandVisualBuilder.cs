using UnityEngine;

// Arma la mano del jugador con primitivas, como hijo del controlador. Todo
// lo que cuelga del controlador sigue la pose trackeada solo, así que esto
// es puramente visual: no toca el input, ni el gatillo, ni DrumHitDetector.
//
// Es un puño cerrado a propósito: además de ser la forma correcta para
// agarrar una baqueta, un puño se lee bien con pocas primitivas, mientras
// que una mano abierta con dedos sueltos necesitaría un modelo de verdad.
//
// La baqueta se extiende sobre el eje FORWARD del controlador — que es
// justo el eje que mide DrumHitDetector. Así el jugador ve la punta
// trazando el gesto en vez de tener que adivinar hacia dónde apunta.
public class HandVisualBuilder : MonoBehaviour
{
    [Header("Materiales")]
    [SerializeField] private Material skinMaterial;
    [SerializeField] private Material stickMaterial;

    [Tooltip("Material de la punta de la baqueta. Si lo dejás vacío usa el de la baqueta. Poné uno emisivo (StarGlow) para que se vea de noche.")]
    [SerializeField] private Material stickTipMaterial;

    [Header("Mano")]
    [Tooltip("Destildalo para quedarte solo con la baqueta, sin mano.")]
    [SerializeField] private bool includeHand = false;

    [SerializeField] private bool isLeftHand;

    [Tooltip("Escala general de la mano. Subilo o bajalo hasta que se sienta del tamaño de tu mano en el visor.")]
    [SerializeField] private float handScale = 1f;

    [Tooltip("Corrimiento de toda la mano respecto al controlador. Sirve para bajarla/atrasarla hasta que quede donde realmente está tu puño, sin tocar el código.")]
    [SerializeField] private Vector3 handOffset = new Vector3(0f, -0.02f, -0.01f);

    [Header("Baqueta (solo la derecha normalmente)")]
    [SerializeField] private bool includeDrumstick = true;
    [SerializeField] private float stickLength = 0.3f;
    [SerializeField] private float stickThickness = 0.009f;

    [Tooltip("Dónde arranca la baqueta respecto al controlador. Bajá la Z para tenerla más cerca de la mano, subila para alejarla.")]
    [SerializeField] private Vector3 stickOffset = new Vector3(0f, -0.015f, -0.02f);

    [Header("Estela de la punta")]
    [SerializeField] private bool includeTrail = true;

    [Tooltip("Material de la estela. Usá el shader Custom/Trail: necesita leer el color del vértice para que la cola se desvanezca.")]
    [SerializeField] private Material trailMaterial;

    [Tooltip("Cuántos segundos queda dibujado el rastro. Corto: es un gesto, no una serpentina.")]
    [SerializeField] private float trailTime = 0.3f;

    [Tooltip("Ancho de la estela donde nace, en la punta de la baqueta.")]
    [SerializeField] private float trailWidth = 0.022f;

    void Awake()
    {
        if (includeHand) BuildHand();
        if (includeDrumstick) BuildDrumstick();
    }

    private void BuildHand()
    {
        float mirror = isLeftHand ? -1f : 1f;

        // Un puño real mide ~9 cm de largo por ~7 de ancho y es CHATO, no una
        // bola: la palma va aplastada en Z. Menos piezas y más planas se leen
        // mucho mejor que muchas esferas superpuestas.
        CreatePart(PrimitiveType.Sphere, "Palm", skinMaterial,
            handOffset,
            Vector3.zero,
            new Vector3(0.070f, 0.058f, 0.042f) * handScale);

        // Dedos enrollados: una masa única cruzada al frente, no dedos sueltos.
        CreatePart(PrimitiveType.Capsule, "Fingers", skinMaterial,
            handOffset + new Vector3(0f, -0.008f, 0.024f) * handScale,
            new Vector3(0f, 0f, 90f),
            new Vector3(0.026f, 0.032f, 0.024f) * handScale);

        // Pulgar: cruzado por encima, del lado interno. Es lo que hace que se
        // lea como mano y no como una piedra.
        CreatePart(PrimitiveType.Capsule, "Thumb", skinMaterial,
            handOffset + new Vector3(0.024f * mirror, 0.008f, 0.020f) * handScale,
            new Vector3(25f, 0f, -40f * mirror),
            new Vector3(0.016f, 0.022f, 0.016f) * handScale);

        // Muñeca: cierra el puño hacia atrás para que no quede cortado.
        CreatePart(PrimitiveType.Capsule, "Wrist", skinMaterial,
            handOffset + new Vector3(0f, 0f, -0.038f) * handScale,
            new Vector3(90f, 0f, 0f),
            new Vector3(0.044f, 0.028f, 0.040f) * handScale);
    }

    private void BuildDrumstick()
    {
        // El Cylinder de Unity mide 2 de alto sobre su eje Y: se rota 90° en X
        // para que quede acostado sobre el forward del controlador.
        CreatePart(PrimitiveType.Cylinder, "StickShaft", stickMaterial,
            stickOffset + new Vector3(0f, 0f, stickLength * 0.5f),
            new Vector3(90f, 0f, 0f),
            new Vector3(stickThickness, stickLength * 0.5f, stickThickness));

        GameObject tip = CreatePart(PrimitiveType.Sphere, "StickTip",
            stickTipMaterial != null ? stickTipMaterial : stickMaterial,
            stickOffset + new Vector3(0f, 0f, stickLength),
            Vector3.zero,
            Vector3.one * (stickThickness * 2.4f));

        if (includeTrail) AddTrail(tip);
    }

    private void AddTrail(GameObject tip)
    {
        var trail = tip.AddComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;

        // Corto: si la distancia mínima es grande, un gesto rápido genera
        // pocos puntos y la estela sale poligonal en vez de curva.
        trail.minVertexDistance = 0.004f;
        trail.numCapVertices = 4;

        // La estela siempre de frente a la cámara. En VR, sin esto se ve de
        // canto y desaparece según desde dónde la mires.
        trail.alignment = LineAlignment.View;

        trail.autodestruct = false;
        if (trailMaterial != null) trail.sharedMaterial = trailMaterial;

        // Degradado de opaco a transparente: el shader Custom/Trail lo lee
        // del color del vértice y con eso la cola se desvanece.
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;

        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        trail.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private GameObject CreatePart(PrimitiveType type, string name, Material material,
        Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;

        // CreatePrimitive trae collider de fábrica. Hay que sacarlo: el rig
        // usa un CharacterController y no queremos colliders sueltos pegados
        // al jugador interfiriendo con él.
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localEulerAngles = localEuler;
        part.transform.localScale = localScale;

        var renderer = part.GetComponent<MeshRenderer>();
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        return part;
    }
}
