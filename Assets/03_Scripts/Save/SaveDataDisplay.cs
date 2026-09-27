using UnityEngine;
using TMPro;

// Muestra en pantalla lo que hay guardado. Es la PRUEBA de que el sistema
// funciona: se juega, se cierra el juego, se vuelve a abrir, y el récord
// sigue ahí.
//
// Ponelo en el menú principal, al lado de la tarjeta de la canción.
public class SaveDataDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Tooltip("Tiene que coincidir con el songId del RunRecorder.")]
    [SerializeField] private string songId = "Moon_DreamIvory";

    [Tooltip("Mostrar también dónde está el archivo. Útil para la defensa; sacalo para la entrega final.")]
    [SerializeField] private bool showFilePath;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) return;

        if (!SaveSystem.SaveExists)
        {
            label.text = "Sin partidas guardadas";
            return;
        }

        SongProgress progress = SaveSystem.GetOrCreateSong(songId);

        string text = progress.timesPlayed == 0
            ? "Sin partidas guardadas"
            : $"Récord: {progress.bestPerfects}/{progress.bestTotalNotes} ({progress.BestPercentage:P0})\n" +
              $"Partidas jugadas: {progress.timesPlayed}\n" +
              $"Luna: {(progress.moonUnlocked ? "conseguida" : "bloqueada")}";

        if (showFilePath) text += $"\n\n<size=60%>{SaveSystem.SavePath}</size>";

        label.text = text;
    }
}
