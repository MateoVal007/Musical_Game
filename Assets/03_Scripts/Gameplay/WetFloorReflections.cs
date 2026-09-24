using UnityEngine;

// Le pasa al shader del piso qué luces tiene que reflejar. El reflejo lo
// dibuja Custom/Ground; esto solo mantiene actualizadas las posiciones y
// los colores.
//
// Si una fuente tiene Renderer, por defecto le lee el color y el brillo a su
// material — así el reflejo del farol late junto con el farol sin tener que
// duplicar la lógica de audio.
public class WetFloorReflections : MonoBehaviour
{
    private const int MaxLights = 4;

    [System.Serializable]
    public class ReflectionSource
    {
        [Tooltip("El objeto que emite la luz (el foco del farol, la luna, etc.).")]
        public Transform source;

        [Tooltip("Color del reflejo. Se ignora si 'Read Color From Renderer' está activo y el objeto tiene material con _Color.")]
        public Color color = Color.white;

        [Range(0f, 5f)]
        public float intensity = 1f;

        [Tooltip("Leer color y brillo del material de la fuente, para que el reflejo acompañe sus cambios.")]
        public bool readColorFromRenderer = true;
    }

    [Tooltip("Hasta 4 fuentes. Las que estén desactivadas en la escena se saltean solas (la luna, antes de desbloquearse).")]
    [SerializeField] private ReflectionSource[] sources = new ReflectionSource[0];

    private static readonly int PositionsId = Shader.PropertyToID("_WetLightPositions");
    private static readonly int ColorsId = Shader.PropertyToID("_WetLightColors");
    private static readonly int CountId = Shader.PropertyToID("_WetLightCount");

    private readonly Vector4[] positions = new Vector4[MaxLights];
    private readonly Vector4[] colors = new Vector4[MaxLights];

    // Cacheado: buscar el Renderer y su material en cada frame, para cada
    // fuente, es trabajo repetido que nunca cambia de respuesta.
    private Renderer[] cachedRenderers;
    private Material[] cachedMaterials;

    void Awake()
    {
        cachedRenderers = new Renderer[sources.Length];
        cachedMaterials = new Material[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            ReflectionSource s = sources[i];
            if (s == null || s.source == null || !s.readColorFromRenderer) continue;

            cachedRenderers[i] = s.source.GetComponent<Renderer>();
            if (cachedRenderers[i] != null) cachedMaterials[i] = cachedRenderers[i].material;
        }
    }

    void Update()
    {
        int count = 0;

        for (int i = 0; i < sources.Length && count < MaxLights; i++)
        {
            ReflectionSource s = sources[i];
            if (s == null || s.source == null || !s.source.gameObject.activeInHierarchy) continue;

            positions[count] = s.source.position;
            colors[count] = ResolveColor(s, i) * s.intensity;
            count++;
        }

        Shader.SetGlobalVectorArray(PositionsId, positions);
        Shader.SetGlobalVectorArray(ColorsId, colors);
        Shader.SetGlobalFloat(CountId, count);
    }

    private Color ResolveColor(ReflectionSource s, int index)
    {
        if (!s.readColorFromRenderer) return s.color;

        Material material = index < cachedMaterials.Length ? cachedMaterials[index] : null;
        if (material == null || !material.HasProperty("_Color")) return s.color;

        Color c = material.GetColor("_Color");
        if (material.HasProperty("_GlowIntensity"))
        {
            // El brillo del emisor manda: si el foco está apagado, el reflejo también.
            c *= Mathf.Clamp01(material.GetFloat("_GlowIntensity") / 6f);
        }
        return c;
    }
}
