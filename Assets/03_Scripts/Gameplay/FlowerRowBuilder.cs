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
    public enum Layout { Fila, Circulo }

    [Header("Disposición")]
    [Tooltip("Fila: en línea sobre el eje X local. Círculo: en anillo alrededor del centro del objeto (para rodear la nube).")]
    [SerializeField] private Layout layout = Layout.Fila;

    [SerializeField] private int flowerCount = 40;

    [Tooltip("Largo de la fila sobre el eje X local. Solo se usa en modo Fila.")]
    [SerializeField] private float rowLength = 12f;

    [Tooltip("Radio del anillo. Solo se usa en modo Círculo.")]
    [SerializeField] private float ringRadius = 3f;

    [Tooltip("Desvío aleatorio: a los costados en modo Fila, hacia adentro y afuera en modo Círculo.")]
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

    [Header("Estallido de polen")]
    [Tooltip("Motas de luz que salen disparadas al encenderse. 0 las desactiva.")]
    [Range(0, 12)]
    [SerializeField] private int motesPerFlower = 4;

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
        int motes = Mathf.Max(0, motesPerFlower);

        // 4 por pétalo + 4 del tallo + 4 por mota de polen
        int vertsPerFlower = petals * 4 + 4 + motes * 4;
        int trisPerFlower = petals * 2 + 2 + motes * 2;

        var vertices = new Vector3[flowerCount * vertsPerFlower];
        var colors = new Color[flowerCount * vertsPerFlower];
        var uv0 = new Vector4[flowerCount * vertsPerFlower];
        var uv1 = new Vector3[flowerCount * vertsPerFlower];
        var triangles = new int[flowerCount * trisPerFlower * 3];

        int vi = 0, ti = 0;
        float tallest = 0f;

        for (int f = 0; f < flowerCount; f++)
        {
            // Posición EN LA FILA: esto es lo que hace posible la ola.
            float rowPos = flowerCount > 1 ? f / (float)(flowerCount - 1) : 0f;

            Vector3 basePos;
            if (layout == Layout.Circulo)
            {
                // El jitter va sobre el radio: algunas más adentro, otras más
                // afuera, para que el anillo no se vea trazado con compás.
                float angle = rowPos * Mathf.PI * 2f;
                float radius = ringRadius + Random.Range(-rowJitter, rowJitter);
                basePos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            else
            {
                basePos = new Vector3(
                    (rowPos - 0.5f) * rowLength,
                    0f,
                    Random.Range(-rowJitter, rowJitter));
            }

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

            // --- Motas de polen: quads diminutos apilados en la cabeza de la
            // flor. En reposo tienen tamano cero (no se ven ni cuestan pixeles);
            // al encenderse salen disparadas en la direccion que lleva cada una
            // guardada. El shader las anima, aca solo se siembran.
            for (int m = 0; m < motes; m++)
            {
                // Direccion sesgada hacia arriba: el polen sube y se abre, no
                // se dispara para abajo contra el tallo.
                Vector3 dir = (Random.onUnitSphere + Vector3.up * 1.1f).normalized;
                dir *= Random.Range(0.5f, 1f);   // velocidad distinta por mota

                float moteRandom = Random.value;

                // Arranca repartida por la corona, no todas del mismo punto.
                Vector3 origin = head + Random.insideUnitSphere * (minPetalLength * 0.35f);

                int moteStart = vi;
                AddMoteVertex(vertices, colors, uv0, uv1, ref vi, origin, new Vector2(-1f, -1f), moteRandom, dir, rowPos, flowerRandom);
                AddMoteVertex(vertices, colors, uv0, uv1, ref vi, origin, new Vector2(1f, -1f), moteRandom, dir, rowPos, flowerRandom);
                AddMoteVertex(vertices, colors, uv0, uv1, ref vi, origin, new Vector2(-1f, 1f), moteRandom, dir, rowPos, flowerRandom);
                AddMoteVertex(vertices, colors, uv0, uv1, ref vi, origin, new Vector2(1f, 1f), moteRandom, dir, rowPos, flowerRandom);
                AddQuad(triangles, ref ti, moteStart, moteStart + 1, moteStart + 2, moteStart + 3);
            }
        }

        var mesh = new Mesh { name = "FlowerRow" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetTriangles(triangles, 0);
        float spanX = layout == Layout.Circulo ? (ringRadius + rowJitter) * 2f : rowLength;
        float spanZ = layout == Layout.Circulo ? (ringRadius + rowJitter) * 2f : rowJitter * 2f;
        mesh.bounds = new Bounds(
            new Vector3(0f, tallest * 0.5f, 0f),
            new Vector3(spanX + 2f, tallest + 1f, spanZ + 2f));

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

    // La mota se guarda con los 4 vertices en el MISMO punto: la esquina va
    // aparte, en la UV, y el shader la expande mirando a la camara. Asi la
    // mota siempre queda de frente a los dos ojos.
    // .a del color = 0.5 es la marca de "esto es una mota" (0 tallo, 1 petalo).
    private static void AddMoteVertex(Vector3[] vertices, Color[] colors, Vector4[] uv0, Vector3[] uv1,
        ref int vi, Vector3 position, Vector2 corner, float moteRandom, Vector3 direction,
        float rowPos, float flowerRandom)
    {
        vertices[vi] = position;
        colors[vi] = new Color(1f, rowPos, flowerRandom, 0.5f);
        uv0[vi] = new Vector4(corner.x, corner.y, moteRandom, 0f);
        uv1[vi] = direction;
        vi++;
    }

    private static void AddQuad(int[] triangles, ref int ti, int a, int b, int c, int d)
    {
        triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = b;
        triangles[ti++] = b; triangles[ti++] = c; triangles[ti++] = d;
    }
}
