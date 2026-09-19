using UnityEngine;

// Gira el objeto sobre su propio eje, constante. Para la luna.
public class SelfRotate : MonoBehaviour
{
    [Tooltip("Grados por segundo en cada eje. Para la luna alcanza con Y.")]
    [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 6f, 0f);

    [Tooltip("Si está activo gira en espacio local (acompaña la inclinación del objeto); si no, en ejes del mundo.")]
    [SerializeField] private bool useLocalSpace = true;

    void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime,
            useLocalSpace ? Space.Self : Space.World);
    }
}
