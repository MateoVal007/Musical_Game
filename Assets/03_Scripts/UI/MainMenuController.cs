using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainCanvas;
    [SerializeField] private GameObject songSelectCanvas;

    public void OnPlayPressed()
    {
        mainCanvas.SetActive(false);
        songSelectCanvas.SetActive(true);
    }

    public void OnBackFromSongSelect()
    {
        songSelectCanvas.SetActive(false);
        mainCanvas.SetActive(true);
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