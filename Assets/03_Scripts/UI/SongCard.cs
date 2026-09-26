using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongCard : MonoBehaviour
{
    [SerializeField] private Image coverImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject lockedLabel;

    public void Setup(SongEntry data, System.Action onPlayClicked)
    {
        titleText.text = data.title;
        artistText.text = data.artist;
        coverImage.color = data.coverTint;
        if (data.coverSprite != null)
        {
            coverImage.sprite = data.coverSprite;
            coverImage.color = Color.white;   // sin tinte, se ve el sprite real
        }
        else
        {
            coverImage.color = data.coverTint; // sin sprite, usa el color plano (red love / la tercera)
        }
        playButton.gameObject.SetActive(data.isUnlocked);
        lockedLabel.SetActive(!data.isUnlocked);
        metaText.text = data.isUnlocked ? data.meta : "PRÓXIMAMENTE";

        if (data.isUnlocked)
            playButton.onClick.AddListener(() => onPlayClicked?.Invoke());
    }
}