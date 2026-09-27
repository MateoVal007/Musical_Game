using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] private PerfectTracker perfectTracker;

    [Tooltip("Quién decide si la luna se desbloqueó. Se le pregunta a él para que la pantalla final y la luna no puedan contradecirse.")]
    [SerializeField] private MoonCollectible moonCollectible;

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

        // Se lo preguntamos a MoonCollectible en vez de decidirlo acá. Antes
        // esto usaba perfectTracker.AllPerfect, o sea el 100%, mientras que la
        // luna se desbloquea al 80%: quien sacaba 85% VEÍA la luna aparecer en
        // el cielo a mitad de canción y después la pantalla final le decía que
        // no la había conseguido.
        //
        // Tampoco sirve releer PlayerPrefs: eso queda en true de partidas
        // anteriores, y acá hace falta saber si se la ganó en ESTA.
        bool unlockedThisRun = moonCollectible != null
            ? moonCollectible.UnlockedThisRun
            : perfectTracker.AllPerfect;

        if (moonCollectible == null)
        {
            Debug.LogWarning("[EndScreenController] Falta asignar Moon Collectible: " +
                             "la pantalla final vuelve a exigir el 100% en vez del 80%.", this);
        }

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