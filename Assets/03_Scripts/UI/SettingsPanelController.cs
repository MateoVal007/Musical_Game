using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CloudBoundary cloudBoundary;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle vignetteToggle;

    void OnEnable()
    {
        // Reflejar el estado actual al abrir el panel.
        if (audioSource != null) volumeSlider.value = audioSource.volume;
        if (cloudBoundary != null) vignetteToggle.isOn = cloudBoundary.VignetteEnabled;
    }

    public void OnVolumeChanged(float value)
    {
        if (audioSource != null) audioSource.volume = value;
    }

    public void OnVignetteToggled(bool enabled)
    {
        if (cloudBoundary != null) cloudBoundary.VignetteEnabled = enabled;
    }
}