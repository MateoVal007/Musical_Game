using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;
    [SerializeField] private GameObject endScreenCanvas;
    [SerializeField] private GameObject moonUnlockedText;
    [SerializeField] private Transform headCamera;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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
        // Usamos AllPerfect directo de PerfectTracker en vez de releer PlayerPrefs,
        // porque MoonCollectible está suscripto al mismo evento y el orden entre
        // ambos listeners no está garantizado (podríamos leer el PlayerPref ANTES
        // de que MoonCollectible lo haya escrito recién).
        bool unlockedThisRun = perfectTracker.AllPerfect;

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