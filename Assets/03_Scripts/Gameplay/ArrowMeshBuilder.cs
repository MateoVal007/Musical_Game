using UnityEngine;

// Genera a mano la malla 2D de una flecha (sin depender de un modelo
// externo) y la asigna al MeshFilter en Awake. Queda apuntando hacia +Y
// (arriba) por defecto — Note.cs la rota en Z según la dirección asignada.
// Al ser un polígono plano centrado, funciona directo con el shader
// Custom/NoteDissolve (que solo necesita posición de vértices, sin UVs).
[RequireComponent(typeof(MeshFilter))]
public class ArrowMeshBuilder : MonoBehaviour
{
    [SerializeField] private float shaftHalfWidth = 0.15f;
    [SerializeField] private float shaftBottom = -0.5f;
    [SerializeField] private float shaftTop = 0.1f;
    [SerializeField] private float headHalfWidth = 0.4f;
    [SerializeField] private float tipY = 0.5f;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = Build();
    }

    private Mesh Build()
    {
        // Contorno de la flecha en sentido horario (vista desde +Z): asta
        // rectangular abajo + cabeza triangular arriba.
        Vector3[] ring =
        {
            new Vector3(-shaftHalfWidth, shaftBottom, 0f),
            new Vector3(shaftHalfWidth, shaftBottom, 0f),
            new Vector3(shaftHalfWidth, shaftTop, 0f),
            new Vector3(headHalfWidth, shaftTop, 0f),
            new Vector3(0f, tipY, 0f),
            new Vector3(-headHalfWidth, shaftTop, 0f),
            new Vector3(-shaftHalfWidth, shaftTop, 0f),
        };

        // Fan triangulation desde un punto central — funciona porque la
        // forma de flecha es "estrellada" respecto a ese centro.
        Vector3 center = new Vector3(0f, (shaftBottom + shaftTop) * 0.5f, 0f);

        var vertices = new Vector3[ring.Length + 1];
        ring.CopyTo(vertices, 0);
        int centerIndex = ring.Length;
        vertices[centerIndex] = center;

        var triangles = new int[ring.Length * 3];
        for (int i = 0; i < ring.Length; i++)
        {
            int next = (i + 1) % ring.Length;
            triangles[i * 3] = centerIndex;
            triangles[i * 3 + 1] = i;
            triangles[i * 3 + 2] = next;
        }

        var mesh = new Mesh { name = "Arrow" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }
}
