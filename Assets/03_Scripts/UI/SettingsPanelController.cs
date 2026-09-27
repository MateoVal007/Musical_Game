using UnityEngine;
using UnityEngine.UI;

// Panel de opciones. Además de aplicar los cambios, los GUARDA: antes el
// volumen y la viñeta se reseteaban cada vez que se abría el juego, porque
// solo vivían en el componente que los usaba.
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CloudBoundary cloudBoundary;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle vignetteToggle;

    void Start()
    {
        // Aplicar lo guardado al arrancar, aunque el panel esté cerrado: si
        // solo se aplicara al abrirlo, el jugador que nunca entra a opciones
        // jugaría siempre con los valores por defecto.
        GameSettings settings = SaveSystem.Settings;

        if (audioSource != null) audioSource.volume = settings.musicVolume;
        if (cloudBoundary != null) cloudBoundary.VignetteEnabled = settings.vignetteEnabled;
    }

    void OnEnable()
    {
        // Reflejar el estado actual al abrir el panel.
        GameSettings settings = SaveSystem.Settings;

        if (volumeSlider != null) volumeSlider.value = settings.musicVolume;
        if (vignetteToggle != null) vignetteToggle.isOn = settings.vignetteEnabled;
    }

    public void OnVolumeChanged(float value)
    {
        if (audioSource != null) audioSource.volume = value;
        Persist();
    }

    public void OnVignetteToggled(bool enabled)
    {
        if (cloudBoundary != null) cloudBoundary.VignetteEnabled = enabled;
        Persist();
    }

    private void Persist()
    {
        SaveSystem.SaveSettings(
            volumeSlider != null ? volumeSlider.value : SaveSystem.Settings.musicVolume,
            vignetteToggle != null ? vignetteToggle.isOn : SaveSystem.Settings.vignetteEnabled);
    }
}
