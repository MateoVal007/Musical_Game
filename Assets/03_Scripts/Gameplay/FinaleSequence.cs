using UnityEngine;
using System.Collections;

// El cierre. Entre que se termina la canción y aparece la pantalla final hay
// una ventana de silencio (EndScreenController la estira a propósito para que
// se aprecie el paisaje), pero ahí no pasaba nada: la música cortaba y el
// mundo se quedaba igual, lloviendo.
//
// Esto usa lo que ya existe y solo lo dispara en orden: las flores se
// encienden y se quedan, la lluvia se va apagando y las partículas dejan de
// emitir. Casi no hay lógica nueva — es una coreografía de cosas que ya
// funcionaban por separado.
public class FinaleSequence : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;

    [Header("Las flores")]
    [Tooltip("Todos los grupos de flores: las azules y el anillo blanco de la nube.")]
    [SerializeField] private FlowerWave[] flowerGroups;

    [Tooltip("Segundos que las flores se quedan encendidas al máximo.")]
    [SerializeField] private float bloomHold = 7f;

    [Tooltip("Cuánto tardan en apagarse después. Largo a propósito: es lo último que ve el jugador.")]
    [SerializeField] private float bloomFade = 8f;

    [Tooltip("Retraso entre grupo y grupo. Encendidos todos juntos es un flash; escalonados, la luz recorre el campo.")]
    [SerializeField] private float stagger = 0.35f;

    [Header("Las estrellas")]
    [Tooltip("El objeto que contiene las estrellas (el StarField). Sin música los stems se van a cero y las estrellas quedaban congeladas en el mínimo, justo en los segundos pensados para mirar el cielo.")]
    [SerializeField] private Transform starFieldRoot;

    [Header("La lluvia")]
    [SerializeField] private RainAudio rainAudio;

    [Tooltip("Partículas que dejan de emitir al cerrar: la lluvia y la niebla. Las que ya están en el aire terminan su recorrido normal.")]
    [SerializeField] private ParticleSystem[] systemsToStop;

    [SerializeField] private float rainFadeDuration = 6f;

    [Tooltip("Espera antes de empezar a cortar la lluvia. Le da lugar a que primero se vean las flores.")]
    [SerializeField] private float rainFadeDelay = 2f;

    void OnEnable()
    {
        if (perfectTracker != null) perfectTracker.OnSongEnded += HandleSongEnded;
    }

    void OnDisable()
    {
        if (perfectTracker != null) perfectTracker.OnSongEnded -= HandleSongEnded;
    }

    private void HandleSongEnded()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        StartCoroutine(FadeRain());
        StartBreathingStars();

        if (flowerGroups == null) yield break;

        foreach (FlowerWave group in flowerGroups)
        {
            if (group != null) group.FlashCustom(bloomHold, bloomFade);

            // El escalonado corre EN PARALELO al apagado de la lluvia, por eso
            // el fade va en su propia corrutina: si esperáramos acá, la lluvia
            // no empezaría a bajar hasta que se encendiera el último grupo.
            if (stagger > 0f) yield return new WaitForSeconds(stagger);
        }
    }

    private void StartBreathingStars()
    {
        if (starFieldRoot == null) return;

        // GetComponentsInChildren es caro, pero esto corre UNA vez en toda la
        // partida y no hay otra forma de alcanzarlas: las estrellas se van
        // instanciando durante la canción, así que no existe una lista previa.
        foreach (Star star in starFieldRoot.GetComponentsInChildren<Star>())
        {
            if (star != null) star.StartBreathing();
        }
    }

    private IEnumerator FadeRain()
    {
        yield return new WaitForSeconds(rainFadeDelay);

        if (rainAudio != null) rainAudio.FadeOut(rainFadeDuration);

        if (systemsToStop == null) yield break;

        foreach (ParticleSystem ps in systemsToStop)
        {
            // StopEmitting y no Clear: las gotas que ya están cayendo terminan
            // de caer. Borrarlas de golpe se ve como un corte de energía.
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
