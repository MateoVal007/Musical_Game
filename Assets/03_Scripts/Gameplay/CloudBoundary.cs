using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Evita que el jugador se aleje caminando (room-scale) más allá del radio de
// la nube. Mide la posición de la CÁMARA (la cabeza real, que es la que se
// mueve al caminar), y si se pasa del límite, empuja el XR Origin (la raíz)
// en la dirección contraria para compensar — nunca se toca la cámara
// directamente, porque el sistema de tracking le pisa la posición cada frame.
public class CloudBoundary : MonoBehaviour
{
    [Tooltip("El XR Origin (la raíz del rig) — este es el que se mueve/empuja.")]
    [SerializeField] private Transform xrOrigin;

    [Tooltip("La cámara real (la cabeza del jugador) — este es el que se MIDE, adentro del XR Origin.")]
    [SerializeField] private Transform headCamera;

    [Tooltip("El centro de la nube.")]
    [SerializeField] private Transform cloudCenter;

    [Tooltip("Qué tan lejos del centro puede alejarse antes de tocar el límite.")]
    [SerializeField] private float radius = 2f;

    [Header("Límite vertical (no 'volar' fuera de la nube)")]
    [Tooltip("Altura mínima permitida sobre el centro de la nube.")]
    [SerializeField] private float minHeight = 0.5f;
    [Tooltip("Altura máxima permitida sobre el centro de la nube.")]
    [SerializeField] private float maxHeight = 3f;

    [Header("Viñeta de advertencia (opcional)")]
    [SerializeField] private Volume boundaryVolume;
    [SerializeField] private float maxVignetteIntensity = 0.4f;

    private Vignette vignette;

    void Start()
    {
        if (boundaryVolume != null)
        {
            boundaryVolume.profile.TryGet(out vignette);
        }
    }

    void LateUpdate()
    {
        if (xrOrigin == null || headCamera == null || cloudCenter == null) return;

        Vector3 headPos = headCamera.position;
        Vector3 center = cloudCenter.position;
        Vector3 flatOffset = new Vector3(headPos.x - center.x, 0f, headPos.z - center.z);

        if (vignette != null)
        {
            float t = Mathf.Clamp01(flatOffset.magnitude / radius);
            vignette.intensity.value = t * maxVignetteIntensity;
        }

        if (flatOffset.magnitude > radius)
        {
            Vector3 clampedOffset = flatOffset.normalized * radius;
            Vector3 excess = flatOffset - clampedOffset;
            xrOrigin.position -= new Vector3(excess.x, 0f, excess.z);
        }

        // Límite vertical: no dejar "volar" muy alto ni hundirse debajo de la nube.
        float relativeHeight = headPos.y - center.y;
        float clampedHeight = Mathf.Clamp(relativeHeight, minHeight, maxHeight);
        if (!Mathf.Approximately(relativeHeight, clampedHeight))
        {
            xrOrigin.position -= new Vector3(0f, relativeHeight - clampedHeight, 0f);
        }
    }
}
