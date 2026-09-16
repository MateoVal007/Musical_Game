using UnityEngine;
using UnityEngine.UI;

public class MoonIconController : MonoBehaviour
{
    [SerializeField] private Image moonImage;
    [SerializeField] private Sprite moonEmptySprite;
    [SerializeField] private Sprite moonFullSprite;

    [Tooltip("Debe ser EXACTAMENTE la misma clave que usa MoonCollectible en el nivel (saveKey).")]
    [SerializeField] private string saveKey = "Moon_DreamIvory";

    void Start()
    {
        bool unlocked = MoonCollectible.IsUnlockedByKey(saveKey);
        moonImage.sprite = unlocked ? moonFullSprite : moonEmptySprite;
    }
}