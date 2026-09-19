using UnityEngine;
using System;

// Vuela desde donde se instanció hasta un destino, y se destruye al llegar.
// Avisa con OnArrived justo antes de destruirse (para el efecto de onda de
// choque en la nube, ver CloudShockwave) — aparte de eso, puramente visual
// (partículas/trail), no tiene más lógica de juego.
public class Comet : MonoBehaviour
{
    [SerializeField] private float flightDuration = 1f;

    public event Action<Vector3> OnArrived;

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
            OnArrived?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }
}
