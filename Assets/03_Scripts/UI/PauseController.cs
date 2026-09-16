using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Transform headCamera;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;
    void Start()
    {
        pauseCanvas.SetActive(false);
    }
    void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
       // Debug.Log($"[PAUSE] Disparado por: {ctx.control?.path} en t={Time.time}");
        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        audioSource.Pause();
        Time.timeScale = 0f;

        if (headCamera != null)
        {
            pauseCanvas.transform.position = headCamera.position + headCamera.forward * 1.5f;
            pauseCanvas.transform.rotation = Quaternion.LookRotation(pauseCanvas.transform.position - headCamera.position);
        }

        pauseCanvas.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        audioSource.UnPause();
        Time.timeScale = 1f;
        pauseCanvas.SetActive(false);
    }

    public void RestartSong()
    {
        Time.timeScale = 1f; // por si estaba en 0 al reiniciar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}