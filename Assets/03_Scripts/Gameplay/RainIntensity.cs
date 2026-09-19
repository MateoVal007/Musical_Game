using UnityEngine;

// Controla qué tan fuerte llueve según la curva de ClimaxIntensity (0 a 1,
// dibujada a mano sobre toda la canción) — llovizna suave al principio,
// más intensa hacia el clímax. Sin ClimaxIntensity en la escena, usa un
// valor fijo (constantIntensity) para que la lluvia no dependa de nada.
public class RainIntensity : MonoBehaviour
{
    [SerializeField] private ParticleSystem rainParticles;

    [Tooltip("Emisión (partículas/seg) cuando la intensidad es 0.")]
    [SerializeField] private float minEmissionRate = 40f;

    [Tooltip("Emisión (partículas/seg) cuando la intensidad es 1 (clímax).")]
    [SerializeField] private float maxEmissionRate = 200f;

    [Tooltip("Velocidad de caída (m/s, negativa) cuando la intensidad es 0.")]
    [SerializeField] private float minFallSpeed = -6f;

    [Tooltip("Velocidad de caída (m/s, negativa) cuando la intensidad es 1.")]
    [SerializeField] private float maxFallSpeed = -12f;

    [Tooltip("Si no hay ClimaxIntensity en la escena, usa este valor fijo (0 a 1) en su lugar.")]
    [Range(0f, 1f)]
    [SerializeField] private float constantIntensity = 0.3f;

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.VelocityOverLifetimeModule velocity;

    void Awake()
    {
        if (rainParticles == null) rainParticles = GetComponent<ParticleSystem>();
        emission = rainParticles.emission;
        velocity = rainParticles.velocityOverLifetime;
        velocity.enabled = true;
    }

    void Update()
    {
        float intensity = ClimaxIntensity.Instance != null ? ClimaxIntensity.Instance.Value : constantIntensity;

        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, intensity);
        velocity.y = new ParticleSystem.MinMaxCurve(Mathf.Lerp(minFallSpeed, maxFallSpeed, intensity));
    }
}
