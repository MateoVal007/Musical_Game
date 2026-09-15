#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Herramienta para mapear "Dream Ivory" a mano: reproducís la canción, y cada
// vez que apretás la tecla de grabar, guarda el segundo exacto. Mucho más
// rápido que scrubear el waveform buscando picos a ojo.
//
// Uso: 1) Play. 2) Apretá la tecla en el ritmo que sientan. 3) Click derecho
// en el componente (en el Inspector) → "Guardar en BeatMapData".
//
// Usa el Input System nuevo (no UnityEngine.Input) porque este proyecto tiene
// el Active Input Handling en "Input System Package" únicamente (lo necesita
// el VR Template) — a diferencia de otros proyectos, acá el Input viejo
// directamente tira excepción en vez de funcionar.
public class BeatMapRecorder : MonoBehaviour
{
    public AudioSource audioSource;
    public BeatMapData targetBeatMap;
    public Key recordKey = Key.Space;

    [Tooltip("Espacio mínimo entre notas consecutivas, en segundos. Evita que queden varias amontonadas al mismo tiempo (rompería la regla de 'máximo 2 notas a la vez').")]
    public float minSpacing = 0.8f;

    private readonly List<float> recorded = new List<float>();

    void Update()
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current[recordKey].wasPressedThisFrame)
        {
            recorded.Add(audioSource.time);
            Debug.Log($"Beat marcado en {audioSource.time:F2}s (total: {recorded.Count})");
        }
    }

    [ContextMenu("Guardar en BeatMapData")]
    public void SaveToBeatMap()
    {
        if (targetBeatMap == null)
        {
            Debug.LogWarning("Asignale un BeatMapData en el Inspector antes de guardar.");
            return;
        }

        targetBeatMap.beatTimestamps = new List<float>(recorded);

#if UNITY_EDITOR
        EditorUtility.SetDirty(targetBeatMap);
        AssetDatabase.SaveAssets();
        Debug.Log($"Guardados {recorded.Count} beats en '{targetBeatMap.name}'.");
#endif
    }

    // Opera sobre lo que YA está guardado en el asset (no sobre la grabación en
    // memoria) — no hace falta volver a escuchar la canción. Se queda con el
    // primer beat de cada grupo pegado y descarta los que le siguen demasiado cerca.
    [ContextMenu("Filtrar espaciado mínimo (sobre lo ya guardado)")]
    public void FilterMinSpacing()
    {
        if (targetBeatMap == null)
        {
            Debug.LogWarning("Asignale un BeatMapData en el Inspector antes de filtrar.");
            return;
        }

        List<float> source = targetBeatMap.beatTimestamps;
        List<float> filtered = new List<float>();
        float lastKept = float.NegativeInfinity;

        foreach (float t in source)
        {
            if (t - lastKept >= minSpacing)
            {
                filtered.Add(t);
                lastKept = t;
            }
        }

        Debug.Log($"Filtrado: {source.Count} -> {filtered.Count} beats (espaciado mínimo {minSpacing}s).");
        targetBeatMap.beatTimestamps = filtered;

#if UNITY_EDITOR
        EditorUtility.SetDirty(targetBeatMap);
        AssetDatabase.SaveAssets();
#endif
    }

    [ContextMenu("Borrar grabación actual")]
    public void ClearRecording()
    {
        recorded.Clear();
        Debug.Log("Grabación en memoria borrada (el BeatMapData guardado no se toca hasta que grabes y guardes de nuevo).");
    }
}
