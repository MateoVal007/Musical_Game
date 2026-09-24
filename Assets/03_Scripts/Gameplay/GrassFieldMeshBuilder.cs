using UnityEngine;

// Genera un campo entero de pasto como UN SOLO mesh (todos los blades
// combinados) — un draw call para todo el campo, sin importar cuántos sean.
//
// Cada blade es una tira curva de varios segmentos, no un triángulo recto:
// eso es lo que hace que se vea como pasto y no como púas. Cada uno tiene
// su propio arco, altura, ancho, orientación y sombreado.
//
// En el color de cada vértice se guarda lo que el shader Custom/Grass
// necesita: .r = altura normalizada (0 raíz, 1 punta), .g = fase aleatoria
// (para que no ventee todo sincronizado), .b = sombreado según hacia dónde
// mira el blade, .a = variación de brillo.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class GrassFieldMeshBuilder : MonoBehaviour
{
    [Header("Cantidad y área")]
    [Tooltip("Cuántos blades genera. Al ser un solo mesh, miles no son problema.")]
    [SerializeField] private int bladeCount = 6000;

    [Tooltip("Radio del área donde se reparten.")]
    [SerializeField] private float areaRadius = 10f;

    [Tooltip("Cuántas matas. El pasto real crece en grupos, no perfectamente parejo — esto evita el look de 'repartido al azar uniforme'.")]
    [SerializeField] private int clumpCount = 250;

    [Tooltip("Qué tan desparramados están los blades dentro de su mata.")]
    [SerializeField] private float clumpRadius = 0.6f;

    [Header("Forma del blade")]
    [Tooltip("Segmentos por blade. Más = curva más suave (y más triángulos). 4 alcanza de sobra.")]
    [SerializeField] private int segments = 4;

    [SerializeField] private float minHeight = 0.7f;
    [SerializeField] private float maxHeight = 1.5f;
    [SerializeField] private float minWidth = 0.035f;
    [SerializeField] private float maxWidth = 0.07f;

    [Tooltip("Cuánto se arquea cada blade por su propio peso, antes del viento.")]
    [SerializeField] private float minBend = 0.1f;
    [SerializeField] private float maxBend = 0.45f;

    [Header("Sombreado horneado")]
    [Tooltip("Dirección de luz imaginaria: los blades que 'miran' hacia acá quedan más claros. Da profundidad sin usar luces reales (todos los shaders del proyecto son unlit).")]
    [SerializeField] private Vector3 lightDirection = new Vector3(0.4f, 0f, -1f);

    [SerializeField] private int randomSeed = 12345;

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

        int levels = Mathf.Max(2, segments) + 1;
        int vertsPerBlade = (levels - 1) * 2 + 1;
        int trisPerBlade = (levels - 2) * 2 + 1;

        var vertices = new Vector3[bladeCount * vertsPerBlade];
        var colors = new Color[bladeCount * vertsPerBlade];
        var triangles = new int[bladeCount * trisPerBlade * 3];

        Vector3 lightDir = lightDirection.normalized;
        Vector3[] clumps = BuildClumpCenters();

        int vi = 0;
        int ti = 0;
        float tallest = 0f;

        for (int i = 0; i < bladeCount; i++)
        {
            Vector3 clumpCenter = clumps[Random.Range(0, clumps.Length)];
            Vector2 offset = Random.insideUnitCircle * clumpRadius;
            Vector3 basePos = clumpCenter + new Vector3(offset.x, 0f, offset.y);

            float height = Random.Range(minHeight, maxHeight);
            float width = Random.Range(minWidth, maxWidth);
            float bend = Random.Range(minBend, maxBend);
            float yaw = Random.Range(0f, Mathf.PI * 2f);
            float phase = Random.value;
            float variation = Random.value;

            tallest = Mathf.Max(tallest, height);

            Vector3 widthDir = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 bendDir = new Vector3(-widthDir.z, 0f, widthDir.x);

            // El blade es una cinta plana: su "cara" apunta perpendicular al
            // ancho. Cuanto más de frente mire a la luz imaginaria, más claro.
            float facing = Mathf.Abs(Vector3.Dot(bendDir, lightDir));
            float shade = Mathf.Clamp01(facing);

            int bladeStart = vi;

            for (int level = 0; level < levels; level++)
            {
                float t = level / (float)(levels - 1);

                // Arco propio del blade: nada en la raíz, máximo en la punta.
                Vector3 center = basePos
                    + Vector3.up * (height * t)
                    + bendDir * (bend * t * t);

                // La punta se cierra, el resto mantiene ancho (forma de cinta,
                // no de triángulo) y recién afina cerca del final.
                float halfWidth = width * 0.5f * (1f - Mathf.Pow(t, 2.5f));
                var color = new Color(t, phase, shade, variation);

                if (level == levels - 1)
                {
                    vertices[vi] = center;
                    colors[vi] = color;
                    vi++;
                }
                else
                {
                    vertices[vi] = center - widthDir * halfWidth;
                    colors[vi] = color;
                    vertices[vi + 1] = center + widthDir * halfWidth;
                    colors[vi + 1] = color;
                    vi += 2;
                }
            }

            for (int level = 0; level < levels - 2; level++)
            {
                int a = bladeStart + level * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                triangles[ti++] = a;
                triangles[ti++] = c;
                triangles[ti++] = b;

                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = d;
            }

            int lastPairStart = bladeStart + (levels - 2) * 2;
            triangles[ti++] = lastPairStart;
            triangles[ti++] = bladeStart + vertsPerBlade - 1;
            triangles[ti++] = lastPairStart + 1;
        }

        var mesh = new Mesh { name = "GrassField" };
        // Miles de blades superan de largo el límite de índices de 16 bits.
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);

        // Margen extra: el viento mueve los vértices en el shader, y si los
        // bounds quedan justos Unity culea el mesh antes de tiempo al mirarlo
        // de costado.
        mesh.bounds = new Bounds(
            new Vector3(0f, tallest * 0.5f, 0f),
            new Vector3(areaRadius * 2f + 4f, tallest + 4f, areaRadius * 2f + 4f));

        Random.state = previousState;
        return mesh;
    }

    private Vector3[] BuildClumpCenters()
    {
        var clumps = new Vector3[Mathf.Max(1, clumpCount)];
        for (int i = 0; i < clumps.Length; i++)
        {
            Vector2 circle = Random.insideUnitCircle * areaRadius;
            clumps[i] = new Vector3(circle.x, 0f, circle.y);
        }
        return clumps;
    }
}
