using UnityEngine;

// Herramientas para inspeccionar y probar el guardado sin tener que jugar
// una canción entera. Click derecho en el componente, en el Inspector.
//
// El método de "Borrar partida" también sirve conectado a un botón de la UI
// si querés poder reiniciar el progreso desde adentro del casco durante la
// demostración.
public class SaveSystemDebug : MonoBehaviour
{
    [ContextMenu("1. Mostrar dónde se guarda")]
    public void ShowPath()
    {
        Debug.Log($"[SaveSystem] Archivo: {SaveSystem.SavePath}\nExiste: {SaveSystem.SaveExists}");
    }

    [ContextMenu("2. Mostrar contenido guardado")]
    public void ShowContents()
    {
        if (!SaveSystem.SaveExists)
        {
            Debug.Log("[SaveSystem] Todavía no hay archivo de guardado.");
            return;
        }

        Debug.Log("[SaveSystem] Contenido:\n" + JsonUtility.ToJson(SaveSystem.Data, true));
    }

    [ContextMenu("3. Guardar una partida de prueba")]
    public void WriteFakeRun()
    {
        SaveSystem.RecordRun("Moon_DreamIvory", 110, 132, true);
        Debug.Log("[SaveSystem] Partida de prueba guardada. Cerrá el juego, volvé a abrirlo y mirá el contenido.");
    }

    [ContextMenu("4. Borrar partida")]
    public void DeleteSave()
    {
        SaveSystem.DeleteSave();
    }
}
