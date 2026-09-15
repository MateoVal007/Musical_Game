# Roadmap del proyecto VR Audio-Reactivo

## Aviso técnico importante para todo el proyecto

Este proyecto tiene `Active Input Handling` en **"Input System Package" únicamente**
(no "Both", como el otro proyecto de la tienda) — lo requiere el VR Template. Esto
significa que **`UnityEngine.Input` (el viejo) tira excepción en vez de funcionar**.
Cualquier script que necesite leer teclado/mouse tiene que usar
`UnityEngine.InputSystem` (`Keyboard.current`, `Mouse.current`, etc.), no `Input.*`.

## Decisión de diseño (10/09): entorno cerrado, sin recorrido

Ya NO hay spline ni entorno que se desplaza — el jugador/nube se queda fijo en un solo
espacio cerrado (tipo "concierto"), y todo el audio-reactive pasa ahí. Se elimina el
`WorldScroller`. El "momento de clímax" que antes dependía de llegar a un punto del
camino, ahora se logra con una variable de intensidad acumulada:
`t = audioSource.time / climaxTimestamp`, alimentando niebla/brillo/partículas.

## Sistema general (orden de construcción)

- [x] `AudioAnalyzer` — análisis FFT, bandas graves/medios/agudos suavizadas.
- [x] `AudioReactiveTransform` — componente reusable para enganchar cualquier objeto a una banda.
- [x] `ClimaxIntensity` — curva 0→1 a lo largo de toda la canción, alimenta el ambiente (reemplaza al `WorldScroller`).
- [x] `BeatMapData` + herramienta de mapeo por tecla (grabar timestamps de "Dream Ivory" escuchándola). Beatmap grabado y filtrado (espaciado mínimo 0.8s).
- [x] `NoteSpawner` + `Note` — las luces aparecen y viajan a un punto fijo. Se agarran apretando el botón `Activate` del control (XRI Right/Activate), NO tocándolas con la mano — decisión de diseño posterior a la propuesta original.
- [x] `CloudGrowth` — la nube crece con cada nota resuelta (sin penalización nunca).
- [x] `CloudBoundary` — límite horizontal Y vertical (no se puede alejar ni "volar" fuera de la nube), con viñeta opcional.
- [x] Notas reposicionadas: viajan de izquierda a derecha cruzando un punto fijo, en vez de venir de frente (más fácil de leer el timing).
- [x] `CometEffect` + `Comet` — estela de partículas en un toque perfecto.
- [x] `StarField` — estrella permanente en el cielo por cada toque perfecto.
- [x] `PerfectTracker` — cuenta perfectos vs. total de notas del `BeatMapData`, avisa al terminar la canción.
- [x] `MoonCollectible` — si `perfectos == total`, desbloquea la luna (aparece en el cielo + se guarda con `PlayerPrefs`).
- [ ] Menús / UI (al final, incluye el ícono vacío/lleno de la luna). **← Único sistema de juego que falta.**
- [ ] Shaders (ver tabla de shaders más abajo) — pendiente de pulido visual.

## Shaders pendientes (Shader Graph, URP)

Cuando lleguemos a cada uno, seguimos el tutorial correspondiente para la técnica visual, y al final le agregamos una propiedad expuesta (ej. `_AudioIntensity`) para que reciba datos desde `AudioAnalyzer` por C# (`material.SetFloat(...)` cada frame).

| Elemento | Técnica | Referencia |
|---|---|---|
| Nube | Fresnel (brillo en bordes) + ruido/vertex displacement (se "mueve" sutilmente sola) | [Cyanilux Shader Tutorials](https://www.cyanilux.com/contents/) · [Vertex Displacement (Unity Learn)](https://learn.unity.com/course/make-a-flag-move-with-shadergraph/tutorial/shader-graph-vertex-displacement) |
| Notas (al tocarlas) | Dissolve (disolución con borde brillante) en vez de `Destroy` directo | [Dissolve Effect in Shader Graph and URP](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/) · [video](https://www.youtube.com/watch?v=0NuesGD0msI) |
| Entorno | Niebla baja (paneo de ruido + gradiente de altura) | [Low-lying fog effect (Paulina VFX)](https://paulinavfx.com/how-to-create-a-low-lying-fog-effect-with-unitys-shadergraph/) |
| Panorama general | Repaso rápido de varios efectos útiles | [5 useful ShaderGraphs (Paulina VFX)](https://paulinavfx.com/5-useful-shadergraphs-for-visual-effects-and-game-development-unity-shadergraph/) |
| Estrellas / Cometa | Glow/emisivo real (sin esto se ven planos, no brillan) | [Make ANYTHING GLOW in Unity](https://www.youtube.com/watch?v=Q4MW3T4VpxM) |
| Cielo | Gradiente + estrellas de fondo en el skybox (complementa a `StarField`) | [Gradient skybox with distant stars](https://mikeyoung.ghost.io/creating-a-gradient-skybox-with-distant-stars-in-unitys-shader-graph/) · [video alternativo](https://www.youtube.com/watch?v=AHd5Bh5myVY) |
| Pulso al tocar una nota (opcional) | Onda de choque radial desde la nube | [Shockwave Shader Graph](https://gamedevbill.com/shockwave-shader-graph/) |

**Regla de oro:** solo Shader Graph / explícitamente URP. Nada de shaders para Built-in Render Pipeline (se ven magenta/rosa en este proyecto).

## Referencias de tono/estilo (para el docente)

- Juego: [TRIPP](https://www.youtube.com/watch?v=r6Eap_mqQIg) — mundo abstracto, sin metas, audio-reactivo.
- Animación (paleta/mood, no interactiva): [Soul — "The Great Before"](https://www.youtube.com/watch?v=UuWB_1h6gCM).
- Interacción con manos al ritmo: [Synth Riders](https://www.youtube.com/watch?v=zXkF_bI7rsc).
- Entorno reactivo técnico: [Rez Infinite — Area X](https://www.youtube.com/watch?v=ycB1TIwgD7s).
- Filosofía "sin fallar": [Journey](https://www.youtube.com/watch?v=7ptuOWjYwSA).
