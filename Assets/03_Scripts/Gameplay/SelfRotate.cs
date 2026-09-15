using UnityEngine;

// Rotación continua sobre su propio eje, tipo bola de disco. Genérico —
// se puede poner en cualquier objeto (la luna, alguna estrella, lo que sea).
public class SelfRotate : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 20f;

    void Update()
    {
        transform.Rotate(rotationAxis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
