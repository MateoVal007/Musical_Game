using UnityEngine;
using System;
using System.Collections;

// La luna secreta: aparece EN EL MOMENTO en que el jugador alcanza el
// porcentaje de perfectos necesario, no al final de la canción — así la
// recompensa se ve mientras todavía está jugando.
//
// Se guarda con PlayerPrefs para que el ícono del menú (vacío/lleno) la
// recuerde entre sesiones, pero eso NO la muestra al arrancar: hay que
// ganársela de nuevo en cada partida.
public class MoonCollectible : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;

    [Tooltip("El objeto de la luna en el cielo. Se activa solo al desbloquearla.")]
    [SerializeField] private GameObject moonVisual;

    [Tooltip("Clave única de guardado para ESTA canción/nivel. Si agregan más canciones, cada una necesita su propia clave.")]
    [SerializeField] private string saveKey = "Moon_DreamIvory";

    [Range(0f, 1f)]
    [Tooltip("Porcentaje de notas perfectas (sobre el TOTAL de la canción) necesario para desbloquearla. Ojo: al ser sobre el total, lo más temprano que puede aparecer es cuando ya pasó ese porcentaje de la canción.")]
    [SerializeField] private float requiredPerfectPercentage = 0.8f;

    [Tooltip("Cuánto tarda en aparecer creciendo, para que no salga de golpe.")]
    [SerializeField] private float appearDuration = 2.5f;

    // Avisa dónde apareció la luna, para lo que quiera reaccionar
    // (las luciérnagas se agrupan alrededor, ver FireflyGathering).
    public event Action<Vector3> OnMoonUnlocked;

    private bool unlocked;

    void Start()
    {
        if (moonVisual != null) moonVisual.SetActive(false);
    }

    void OnEnable()
    {
        if (perfectTracker != null) perfectTracker.OnProgressChanged += HandleProgress;
    }

    void OnDisable()
    {
        if (perfectTracker != null) perfectTracker.OnProgressChanged -= HandleProgress;
    }

    private void HandleProgress()
    {
        if (unlocked || perfectTracker == null || perfectTracker.TotalNotes <= 0) return;

        float achieved = (float)perfectTracker.PerfectCount / perfectTracker.TotalNotes;
        if (achieved >= requiredPerfectPercentage)
        {
            Unlock();
        }
    }

    private void Unlock()
    {
        unlocked = true;

        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        Vector3 position = transform.position;

        if (moonVisual != null)
        {
            position = moonVisual.transform.position;
            moonVisual.SetActive(true);
            StartCoroutine(AppearAnimation(moonVisual.transform));
        }

        Debug.Log($"¡Luna desbloqueada! {perfectTracker.PerfectCount}/{perfectTracker.TotalNotes} perfectos " +
                  $"({(float)perfectTracker.PerfectCount / perfectTracker.TotalNotes:P0}).");

        OnMoonUnlocked?.Invoke(position);
    }

    private IEnumerator AppearAnimation(Transform moon)
    {
        Vector3 finalScale = moon.localScale;
        float duration = Mathf.Max(appearDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            moon.localScale = finalScale * Mathf.SmoothStep(0f, 1f, elapsed / duration);
            yield return null;
        }

        moon.localScale = finalScale;
    }

    // Para que el menú pueda preguntar el estado sin necesitar una instancia
    // de este script en escena.
    public static bool IsUnlockedByKey(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
}
