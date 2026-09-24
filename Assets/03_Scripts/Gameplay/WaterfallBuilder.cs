using UnityEngine;

// Arma la cascada digital como varias cortinas paralelas a distinta
// profundidad, todas en un solo mesh (un draw call).
//
// Por qué capas y no un plano solo: en VR, un plano único se delata como
// calcomanía porque los dos ojos ven exactamente lo mismo. Con capas a
// distinta profundidad cayendo a distinta velocidad hay parallax real entre
// ojo y ojo, y el cerebro lo lee como volumen. Son 2 triángulos por capa.
//
// La cortina se arma sobre el plano XY local: X a lo ancho, Y a lo alto,
// mirando hacia -Z. Rotá el objeto en la escena para orientarla.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class WaterfallBuilder : MonoBehaviour
{
    [Tooltip("Cantidad de cortinas superpuestas. Más capas = más sensación de volumen, pero también más superposición de transparencias.")]
    [Range(1, 8)]
    [SerializeField] private int layers = 4;

    [SerializeField] private float width = 30f;
    [SerializeField] private float height = 22f;

    [Tooltip("Separación en profundidad entre capa y capa.")]
    [SerializeField] private float depthSpacing = 1.5f;

    [Tooltip("Cuánto se ensancha cada capa hacia el fondo, para que no se vean los bordes alineados.")]
    [SerializeField] private float widthGrowth = 0.08f;

    void OnEnable()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Rebuild();
        };
    }
#endif

    private void Rebuild()
    {
        Mesh mesh = Build();
        mesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private Mesh Build()
    {
        int count = Mathf.Max(1, layers);

        var vertices = new Vector3[count * 4];
        var uvs = new Vector2[count * 4];
        var colors = new Color[count * 4];
        var triangles = new int[count * 6];

        for (int i = 0; i < count; i++)
        {
            // 0 la capa de adelante, 1 la del fondo.
            float layerT = count > 1 ? i / (float)(count - 1) : 0f;

            float w = width * (1f + layerT * widthGrowth) * 0.5f;
            float z = i * depthSpacing;

            int v = i * 4;
            vertices[v] = new Vector3(-w, 0f, z);
            vertices[v + 1] = new Vector3(w, 0f, z);
            vertices[v + 2] = new Vector3(-w, height, z);
            vertices[v + 3] = new Vector3(w, height, z);

            uvs[v] = new Vector2(0f, 0f);
            uvs[v + 1] = new Vector2(1f, 0f);
            uvs[v + 2] = new Vector2(0f, 1f);
            uvs[v + 3] = new Vector2(1f, 1f);

            var layerColor = new Color(layerT, 0f, 0f, 1f);
            colors[v] = layerColor;
            colors[v + 1] = layerColor;
            colors[v + 2] = layerColor;
            colors[v + 3] = layerColor;

            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        var mesh = new Mesh { name = "DigitalWaterfall" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
