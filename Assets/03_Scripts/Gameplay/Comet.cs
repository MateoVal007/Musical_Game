using UnityEngine;

// Vuela desde donde se instanció hasta un destino, y se destruye al llegar.
// Puramente visual (partículas/trail), no tiene lógica de juego.
public class Comet : MonoBehaviour
{
    [SerializeField] private float flightDuration = 1f;

    private Vector3 start, destination;
    private float elapsed;
    private bool flying;

    public void FlyTo(Vector3 target)
    {
        start = transform.position;
        destination = target;
        flying = true;
    }

    void Update()
    {
        if (!flying) return;

        elapsed += Time.deltaTime;
        float t = elapsed / flightDuration;
        transform.position = Vector3.Lerp(start, destination, t);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
