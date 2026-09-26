using UnityEngine;

[System.Serializable]
public class SongEntry
{
    public string title;
    public string artist;
    public string meta;              // "3:24 · fácil" o "PRÓXIMAMENTE"
    public Sprite coverSprite;       // opcional, puede quedar vacío por ahora
    public Color coverTint = Color.white;
    public bool isUnlocked;
    public string sceneToLoad;       // solo importa si isUnlocked
}