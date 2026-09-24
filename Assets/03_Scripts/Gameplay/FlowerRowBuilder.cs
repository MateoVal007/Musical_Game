using UnityEngine;

// Genera una FILA de flores como un solo mesh. El orden importa: cada flor
// guarda su posición a lo largo de la fila (0 al principio, 1 al final) en
// el color del vértice, y eso es lo que le permite al shader hacer que la
// luz recorra la fila en vez de encender todo junto.
//
// La fila va sobre el eje X local del objeto: rotalo en la escena para
// orientarla como quieras.
//
// Cada flor: pétalos radiales que se abren hacia arriba + un tallo fino.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class FlowerRowBuilder : MonoBehaviour
{
    [Header("La fila")]
    [SerializeField] private int flowerCount = 40;

    [Tooltip("Largo de la fila sobre el eje X local.")]
    [SerializeField] private float rowLength = 12f;

    [Tooltip("Desvío aleatorio a los costados, para que no sea una línea de regla.")]
    [SerializeField] private float rowJitter = 0.35f;

    [Header("La flor")]
    [SerializeField] private int petalsPerFlower = 5;
    [SerializeField] private float minPetalLength = 0.10f;
    [SerializeField] private float maxPetalLength = 0.17f;
    [SerializeField] private float petalWidth = 0.055f;

    [Tooltip("Cuánto se abren los pétalos: 0 = apuntan al cielo, 1 = totalmente abiertos.")]
    [Range(0f, 1f)]
    [SerializeField] private float petalOpenness = 0.55f;

    [Header("El tallo")]
    [Tooltip("Altura a la que queda la flor. Conviene que asome por encima del pasto.")]
    [SerializeField] private float minStemHeight = 0.5f;
    [SerializeField] private float maxStemHeight = 0.95f;
    [SerializeField] private float stemWidth = 0.012f;

    [SerializeField] private int randomSeed = 777;

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
        Random.State previous = Random.state;
        Random.InitState(randomSeed);

        int petals = Mathf.Max(3, petalsPerFlower);
        int vertsPerFlower = petals * 4 + 4;  // 4 por pétalo + 4 del tallo
        int trisPerFlower = petals * 2 + 2;

        var vertices = new Vector3[flowerCount * vertsPerFlower];
        var colors = new Color[flowerCount * vertsPerFlower];
        var triangles = new int[flowerCount * trisPerFlower * 3];

        int vi = 0, ti = 0;
        float tallest = 0f;

        for (int f = 0; f < flowerCount; f++)
        {
            // Posición EN LA FILA: esto es lo que hace posible la ola.
            float rowPos = flowerCount > 1 ? f / (float)(flowerCount - 1) : 0f;

            var basePos = new Vector3(
                (rowPos - 0.5f) * rowLength,
                0f,
                Random.Range(-rowJitter, rowJitter));

            float stemHeight = Random.Range(minStemHeight, maxStemHeight);
            float flowerRandom = Random.value;
            float yaw = Random.Range(0f, Mathf.PI * 2f);

            tallest = Mathf.Max(tallest, stemHeight + maxPetalLength);

            Vector3 head = basePos + Vector3.up * stemHeight;

            // --- Tallo (una cinta fina, a=0 para que el shader no lo ilumine)
            Vector3 stemSide = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * (stemWidth * 0.5f);
            int stemStart = vi;
            AddVertex(vertices, colors, ref vi, basePos - stemSide, 0f, rowPos, flowerRandom, 0f);
            AddVertex(vertices, colors, ref vi, basePos + stemSide, 0f, rowPos, flowerRandom, 0f);
            AddVertex(vertices, colors, ref vi, head - stemSide * 0.6f, 1f, rowPos, flowerRandom, 0f);
            AddVertex(vertices, colors, ref vi, head + stemSide * 0.6f, 1f, rowPos, flowerRandom, 0f);
            AddQuad(triangles, ref ti, stemStart, stemStart + 1, stemStart + 2, stemStart + 3);

            // --- Pétalos, repartidos en círculo alrededor del centro
            for (int p = 0; p < petals; p++)
            {
                float angle = yaw + (p / (float)petals) * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = new Vector3(-outward.z, 0f, outward.x);

                float length = Random.Range(minPetalLength, maxPetalLength);

                // Se abre entre apuntar al cielo y salir horizontal.
                Vector3 dir = Vector3.Slerp(Vector3.up, outward, petalOpenness).normalized;
                Vector3 tip = head + dir * length;

                int petalStart = vi;
                AddVertex(vertices, colors, ref vi, head - side * (petalWidth * 0.25f), 0f, rowPos, flowerRandom, 1f);
                AddVertex(vertices, colors, ref vi, head + side * (petalWidth * 0.25f), 0f, rowPos, flowerRandom, 1f);
                // La punta se ensancha primero y después cierra: forma de pétalo.
                // El gradiente va a 1 en la punta — con menos, el shader
                // apagaba justo la parte que más tiene que brillar.
                AddVertex(vertices, colors, ref vi, tip - side * (petalWidth * 0.5f) - dir * (length * 0.25f), 1f, rowPos, flowerRandom, 1f);
                AddVertex(vertices, colors, ref vi, tip + side * (petalWidth * 0.5f) - dir * (length * 0.25f), 1f, rowPos, flowerRandom, 1f);
                AddQuad(triangles, ref ti, petalStart, petalStart + 1, petalStart + 2, petalStart + 3);
            }
        }

        var mesh = new Mesh { name = "FlowerRow" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(
            new Vector3(0f, tallest * 0.5f, 0f),
            new Vector3(rowLength + 2f, tallest + 1f, rowJitter * 2f + 2f));

        Random.state = previous;
        return mesh;
    }

    private static void AddVertex(Vector3[] vertices, Color[] colors, ref int vi,
        Vector3 position, float gradient, float rowPos, float flowerRandom, float isPetal)
    {
        vertices[vi] = position;
        colors[vi] = new Color(gradient, rowPos, flowerRandom, isPetal);
        vi++;
    }

    private static void AddQuad(int[] triangles, ref int ti, int a, int b, int c, int d)
    {
        triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = b;
        triangles[ti++] = b; triangles[ti++] = c; triangles[ti++] = d;
    }
}
