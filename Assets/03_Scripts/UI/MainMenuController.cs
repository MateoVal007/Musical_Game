using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del nivel, tal cual aparece en Build Settings.")]
    [SerializeField] private string levelSceneName = "BasicScene";

    public void OnPlayPressed()
    {
        SceneManager.LoadScene(levelSceneName);
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}