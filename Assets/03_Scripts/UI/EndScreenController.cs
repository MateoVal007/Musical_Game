using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;
    [SerializeField] private GameObject endScreenCanvas;
    [SerializeField] private GameObject moonUnlockedText;
    [SerializeField] private Transform headCamera;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Tooltip("Segundos de silencio entre que termina la canción y aparece la pantalla final, para dejar apreciar el paisaje en vez de cortar de golpe.")]
    [SerializeField] private float delayBeforeShowing = 12f;

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
        StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeShowing);

        // Usamos AllPerfect directo de PerfectTracker en vez de releer PlayerPrefs,
        // porque MoonCollectible está suscripto al mismo evento y el orden entre
        // ambos listeners no está garantizado (podríamos leer el PlayerPref ANTES
        // de que MoonCollectible lo haya escrito recién).
        bool unlockedThisRun = perfectTracker.AllPerfect;

        // La posición se calcula DESPUÉS de la espera, no antes: así el cartel
        // aparece frente a donde el jugador está mirando en ese momento, y no
        // donde miraba 12 segundos atrás.
        if (headCamera != null)
        {
            endScreenCanvas.transform.position = headCamera.position + headCamera.forward * 1.5f;
            endScreenCanvas.transform.rotation = Quaternion.LookRotation(endScreenCanvas.transform.position - headCamera.position);
        }

        moonUnlockedText.SetActive(unlockedThisRun);
        endScreenCanvas.SetActive(true);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}