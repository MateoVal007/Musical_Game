using UnityEngine;

// Mantiene la altura (eje Y) de este objeto siempre igual a la del jugador,
// sin importar qué le pase a la nube (crecimiento, etc.) ni si el jugador se
// agacha/estira un poco. La posición X/Z no se toca — solo la altura.
public class FollowPlayerHeight : MonoBehaviour
{
    [Tooltip("La cámara real del jugador (la cabeza).")]
    [SerializeField] private Transform headCamera;

    [Tooltip("Offset respecto a la altura de los ojos. Negativo = más abajo (ej. altura del pecho/manos).")]
    [SerializeField] private float heightOffset = -0.3f;

    void LateUpdate()
    {
        if (headCamera == null) return;

        Vector3 pos = transform.position;
        pos.y = headCamera.position.y + heightOffset;
        transform.position = pos;
    }
}
