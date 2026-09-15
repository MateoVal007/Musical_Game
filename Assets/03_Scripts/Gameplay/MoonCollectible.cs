using UnityEngine;

// Si el jugador hizo TODOS los perfectos de la canción, desbloquea la luna:
// aparece en el cielo de este nivel, y se guarda con PlayerPrefs para que el
// ícono del menú (vacío/lleno) la recuerde entre sesiones.
public class MoonCollectible : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;

    [Tooltip("El objeto de la luna en el cielo. Arranca desactivado en la escena.")]
    [SerializeField] private GameObject moonVisual;

    [Tooltip("Clave única de guardado para ESTA canción/nivel. Si agregan más canciones, cada una necesita su propia clave.")]
    [SerializeField] private string saveKey = "Moon_DreamIvory";

    [Range(0f, 1f)]
    [Tooltip("Porcentaje de notas 'perfectas' (sobre el total) necesario para desbloquear la luna. 1 = 100% (todas perfectas). Bajalo para probar más fácil, subilo a 1 para la versión final.")]
    [SerializeField] private float requiredPerfectPercentage = 1f;

    void Start()
    {
        // Si ya la habían desbloqueado en una partida anterior, se muestra desde el arranque.
        if (IsUnlocked() && moonVisual != null)
        {
            moonVisual.SetActive(true);
        }
    }

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
        if (perfectTracker.TotalNotes <= 0) return;

        float actualPercentage = (float)perfectTracker.PerfectCount / perfectTracker.TotalNotes;

        Debug.Log($"Fin de canción: {perfectTracker.PerfectCount}/{perfectTracker.TotalNotes} perfectos ({actualPercentage:P0}). Se necesita {requiredPerfectPercentage:P0}.");

        if (actualPercentage >= requiredPerfectPercentage)
        {
            Unlock();
        }
    }

    private void Unlock()
    {
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        if (moonVisual != null)
        {
            moonVisual.SetActive(true);
        }

        Debug.Log("¡Luna desbloqueada! Hiciste todos los perfectos de la canción.");
    }

    private bool IsUnlocked()
    {
        return PlayerPrefs.GetInt(saveKey, 0) == 1;
    }

    // Para que el menú (más adelante) pueda preguntar el estado sin necesitar
    // una instancia de este script en escena.
    public static bool IsUnlockedByKey(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
}
