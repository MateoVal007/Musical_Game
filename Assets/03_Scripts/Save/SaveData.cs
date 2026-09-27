using System;
using System.Collections.Generic;

// Estructura de lo que se guarda en disco. Todo esto termina serializado a
// JSON por SaveSystem.
//
// Son clases [Serializable] simples y no MonoBehaviours a propósito: los
// datos guardados no deben depender de ningún objeto de la escena. Si el
// progreso viviera dentro de un componente, cerrar la escena lo perdería.

[Serializable]
public class SongProgress
{
    // Identificador de la canción. Es la misma clave que usa MoonCollectible.
    public string songId;

    [NonSerialized] public const int Unset = -1;

    // Mejor partida: cuántos perfectos sobre cuántas notas.
    public int bestPerfects;
    public int bestTotalNotes;

    public bool moonUnlocked;
    public int timesPlayed;

    // Se calcula, no se guarda: guardar un dato derivado es pedir que algún
    // día quede inconsistente con los números de los que sale.
    public float BestPercentage => bestTotalNotes > 0 ? (float)bestPerfects / bestTotalNotes : 0f;
}

[Serializable]
public class GameSettings
{
    public float musicVolume = 1f;
    public bool vignetteEnabled = true;
}

[Serializable]
public class SaveData
{
    // Número de versión del FORMATO. Si algún día se agrega o renombra un
    // campo, esto permite leer partidas viejas y convertirlas en vez de
    // tener que descartarlas.
    public int version = 1;

    public string lastSavedUtc;

    public GameSettings settings = new GameSettings();
    public List<SongProgress> songs = new List<SongProgress>();
}
