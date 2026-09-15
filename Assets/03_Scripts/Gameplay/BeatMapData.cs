using UnityEngine;
using System.Collections.Generic;

// Lista de momentos exactos (en segundos) de la canción donde debería
// "llegar" una nota. Se llena con la herramienta BeatMapRecorder, no a mano.
[CreateAssetMenu(menuName = "Audio/Beat Map", fileName = "BeatMap_")]
public class BeatMapData : ScriptableObject
{
    public List<float> beatTimestamps = new List<float>();
}
