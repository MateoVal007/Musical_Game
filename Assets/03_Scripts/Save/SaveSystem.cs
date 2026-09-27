using System.IO;
using UnityEngine;

// Sistema de guardado y carga. Escribe un archivo JSON en el disco del
// dispositivo, así el progreso sobrevive a cerrar el juego.
//
// POR QUÉ UN ARCHIVO Y NO PlayerPrefs:
// PlayerPrefs solo guarda pares clave-valor de tipos primitivos. Para guardar
// el progreso de varias canciones habría que inventar claves como
// "DreamIvory_bestPerfects", armadas concatenando strings — sin estructura,
// sin forma de recorrerlas, y con el riesgo de que dos sistemas pisen la
// misma clave sin que nadie se entere (nos pasó: saveKey estaba escrito a
// mano en dos scripts distintos).
// Un JSON da datos estructurados, un número de versión para migrar formatos
// viejos, y un archivo que se puede abrir y mostrar durante la defensa.
//
// Es estático porque el guardado no pertenece a ninguna escena: el menú y el
// nivel tienen que ver exactamente los mismos datos, y una escena que se
// descarga no puede llevarse el progreso con ella.
public static class SaveSystem
{
    private const string FileName = "savegame.json";

    // Clave del sistema viejo, para no perder la luna de quien ya la tenía.
    private const string LegacyMoonKey = "Moon_DreamIvory";

    private static SaveData data;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool SaveExists => File.Exists(SavePath);

    // Carga perezosa: cualquiera puede pedir Data sin preocuparse por el
    // orden de arranque de las escenas.
    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(json);
            }
        }
        catch (System.Exception e)
        {
            // Un archivo corrupto no puede impedir que el juego arranque:
            // se descarta y se empieza de cero.
            Debug.LogWarning($"[SaveSystem] No se pudo leer la partida ({e.Message}). Se empieza de cero.");
            data = null;
        }

        if (data == null)
        {
            data = new SaveData();
            MigrateFromPlayerPrefs();
        }
    }

    // Traer lo que había guardado el sistema anterior. Solo corre cuando no
    // existe archivo, o sea una única vez por dispositivo.
    private static void MigrateFromPlayerPrefs()
    {
        if (PlayerPrefs.GetInt(LegacyMoonKey, 0) != 1) return;

        GetOrCreateSong(LegacyMoonKey).moonUnlocked = true;
        Debug.Log("[SaveSystem] Luna recuperada del guardado viejo (PlayerPrefs).");
        Save();
    }

    public static void Save()
    {
        Data.lastSavedUtc = System.DateTime.UtcNow.ToString("o");

        string json = JsonUtility.ToJson(Data, true);
        string temp = SavePath + ".tmp";

        try
        {
            // Escritura atómica: primero a un archivo temporal y recién
            // después se reemplaza el bueno. Si el juego se cierra o se
            // queda sin batería a mitad de la escritura, el archivo original
            // sigue intacto. Escribir directo sobre el definitivo puede
            // dejarlo truncado y perder TODO el progreso.
            File.WriteAllText(temp, json);

            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temp, SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] No se pudo guardar: {e.Message}");
        }
    }

    public static SongProgress GetOrCreateSong(string songId)
    {
        foreach (SongProgress song in Data.songs)
        {
            if (song.songId == songId) return song;
        }

        var created = new SongProgress { songId = songId };
        Data.songs.Add(created);
        return created;
    }

    // Registra el resultado de una partida. Solo pisa el récord si mejoró:
    // una partida mala no puede borrar una buena.
    public static void RecordRun(string songId, int perfects, int totalNotes, bool moonUnlocked)
    {
        SongProgress song = GetOrCreateSong(songId);

        song.timesPlayed++;

        if (perfects > song.bestPerfects)
        {
            song.bestPerfects = perfects;
            song.bestTotalNotes = totalNotes;
        }

        // La luna, una vez conseguida, no se pierde.
        if (moonUnlocked) song.moonUnlocked = true;

        Save();
    }

    public static bool IsMoonUnlocked(string songId)
    {
        return GetOrCreateSong(songId).moonUnlocked;
    }

    public static GameSettings Settings => Data.settings;

    public static void SaveSettings(float musicVolume, bool vignetteEnabled)
    {
        Data.settings.musicVolume = musicVolume;
        Data.settings.vignetteEnabled = vignetteEnabled;
        Save();
    }

    // Para la demostración: borra la partida y deja todo como recién instalado.
    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] No se pudo borrar: {e.Message}");
        }

        PlayerPrefs.DeleteKey(LegacyMoonKey);
        data = new SaveData();
        Debug.Log("[SaveSystem] Partida borrada.");
    }
}
