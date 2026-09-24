using UnityEngine;

// Genera todas las luciérnagas como UN SOLO mesh de quads — un draw call
// para todas, sin un GameObject ni un script por bicho. El parpadeo, la
// deriva y la orientación a cámara los hace el shader Custom/Firefly; acá
// solo se reparten las posiciones y se les da a cada una sus números
// aleatorios (fase, velocidad, tamaño).
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class FireflyFieldMeshBuilder : MonoBehaviour
{
    [SerializeField] private int fireflyCount = 120;

    [Tooltip("Radio del área donde se reparten.")]
    [SerializeField] private float areaRadius = 8f;

    [Tooltip("Altura mínima sobre este objeto — dejala por encima del pasto para que se vean flotando.")]
    [SerializeField] private float minHeight = 0.4f;
    [SerializeField] private float maxHeight = 2.5f;

    [SerializeField] private int randomSeed = 4321;

    private static readonly Vector2[] Corners =
    {
        new Vector2(-1f, -1f),
        new Vector2(1f, -1f),
        new Vector2(1f, 1f),
        new Vector2(-1f, 1f),
    };

    void OnEnable()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Diferido: Unity no deja tocar componentes en medio de OnValidate.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Rebuild();
        };
    }
#endif

    private void Rebuild()
    {
        Mesh mesh = Build();
        // DontSave: la malla se regenera sola, no tiene que quedar guardada
        // adentro del archivo de escena inflándolo.
        mesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private Mesh Build()
    {
        Random.State previousState = Random.state;
        Random.InitState(randomSeed);

        var vertices = new Vector3[fireflyCount * 4];
        var uvs = new Vector2[fireflyCount * 4];
        var colors = new Color[fireflyCount * 4];
        var triangles = new int[fireflyCount * 6];

        for (int i = 0; i < fireflyCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * areaRadius;
            var center = new Vector3(circle.x, Random.Range(minHeight, maxHeight), circle.y);

            // Estos 4 números definen la "personalidad" de cada luciérnaga:
            // cuándo destella, qué tan seguido, por dónde flota y qué tamaño.
            var traits = new Color(Random.value, Random.value, Random.value, Random.value);

            int v = i * 4;
            for (int c = 0; c < 4; c++)
            {
                // Los 4 vértices comparten el centro: el shader los separa
                // hacia las esquinas orientándolos a cámara.
                vertices[v + c] = center;
                uvs[v + c] = Corners[c];
                colors[v + c] = traits;
            }

            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        var mesh = new Mesh { name = "FireflyField" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);

        // Los vértices están todos en el centro de cada quad y el shader los
        // expande y los mueve, así que los bounds calculados quedarían chicos
        // y Unity culearía el mesh de más. Se los damos con margen.
        mesh.bounds = new Bounds(
            new Vector3(0f, (minHeight + maxHeight) * 0.5f, 0f),
            new Vector3(areaRadius * 2f + 4f, maxHeight + 4f, areaRadius * 2f + 4f));

        Random.state = previousState;
        return mesh;
    }
}
