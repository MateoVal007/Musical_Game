# Roadmap del proyecto VR Audio-Reactivo — "Dream, Ivory"

Repo: `MateoVal007/Musical_Game` (GitHub). Trabajo en equipo de 2 — el compañero
armó el menú (`MainMenu.unity`, `MainMenuController`, `PauseController`,
`EndScreenController`, `MoonIconController`); yo (con ayuda de Claude) armé
todo el sistema de nivel/gameplay/audio-reactividad.

## Concepto (no cambió desde el pitch original)

Experiencia VR contemplativa, sin metas ni fallo, sobre la canción **"Dream,
Ivory" de Dream, Ivory**. El jugador está parado en una nube fija, en un
espacio cerrado tipo "niebla urbana nocturna" (inspirado directo en la
portada del álbum: azul-violeta, dos postes de luz, edificios borrosos de
fondo). Le llegan "notas" de luz al ritmo de la canción, las agarra con el
gatillo del control — nunca hay penalización, solo varía la calidad del
resultado. Coleccionable secreto: una luna que se desbloquea si hace
suficientes "perfectos".

## Decisiones de diseño clave (por qué las cosas son como son)

- **Sin recorrido/spline.** Al principio la idea era que la nube viajara por
  un camino. Se descartó: ahora es un espacio cerrado fijo, sin locomoción.
  Elimina riesgo de mareo VR y simplifica todo.
- **Se agarra con el botón del control, no tocando con la mano.** Las notas
  viajan de un punto fijo a otro; al apretar el gatillo (`XRI Right/Activate`)
  se resuelve la nota activa más cercana a su beat ideal. Sin colliders en
  las notas.
- **Las notas viajan de izquierda a derecha** (no de frente hacia el
  jugador) — mucho más fácil de leer el timing, se cruza un punto fijo
  (`NoteTargetPoint`) en vez de "venir hacia la cámara".
- **Nunca hay fallo.** Toda resolución da una "calidad" continua (0 a 1).
  Perfecto = dentro de una ventana de tiempo chica. Auto-llegada (no
  tocada) = calidad mínima pero nunca cero.
- **Niebla + 2 postes + edificio de fondo**, no cielo estrellado abstracto —
  pivotamos cuando vimos la portada real del álbum. Las estrellas de los
  perfectos siguen existiendo, pero ahora la metáfora es "tu logro corta la
  niebla", no "aparece una estrella en el cosmos".
- **Cada estrella reacciona a UNA pista separada de la canción (stem), al
  azar**, no a la mezcla completa — idea del usuario para que el cielo sea
  literalmente una visualización de los instrumentos.

## Estado de los sistemas

### Audio (base)
- [x] `AudioAnalyzer.cs` — FFT de la canción principal, bandas Bass/Mid/Treble suavizadas. Singleton.
- [x] `AudioReactiveTransform.cs` — banda de audio → Transform (escala/posición/rotación). Reusable.
- [x] `AudioReactiveMaterial.cs` — banda de audio → propiedad de material (ej. brillo de un elemento decorativo). Reusable.
- [x] `ClimaxIntensity.cs` — `AnimationCurve` dibujada a mano sobre toda la duración de la canción (0→1), da la intensidad ambiental momento a momento. Reemplaza la vieja idea de "punto de clímax en el camino".
- [x] `SongPlaybackManager.cs` — arranca la canción principal Y las 4 pistas (stems) exactamente en el mismo `Start()`, en vez de depender de varios "Play On Awake" sueltos.

### Audio (stems — pistas separadas)
- [x] Separación con **Ultimate Vocal Remover (UVR)**, modelo Demucs, 4 stems: Voz, Batería, Bajo, Otros (la guitarra cayó en "Otros").
- [x] `StemAnalyzer.cs` — analiza las 4 pistas en paralelo. Por pista: `Source`, `Color`, `Contrast Power` (por pista, no global). Config global: `Sensitivity Multiplier`, `Analyzed Fraction` (qué % del espectro, desde el grave, se promedia — evita diluir con agudos casi silenciosos), `Smooth Speed`.
- [x] **Ruteo por Audio Mixer** (`StemsMixer.mixer`, grupo `StemsSilenciados` a -80dB) — CLAVE: los 4 `AudioSource` de los stems están en `Volume: 1` (¡no bajarlo, no mutear!) y su `Output` apunta al grupo silenciado del Mixer. Así el análisis de espectro recibe la señal real, y el volumen real que escucha el jugador se corta después, en el Mixer.
- [x] `Star.cs` — recibe un índice de stem al azar al nacer (`AssignStem`), fija su color según esa pista, y cada frame lee `StemAnalyzer.GetIntensity(index)` para su brillo (`_GlowIntensity` del shader `StarGlow`).

### Gameplay / notas
- [x] `BeatMapData.cs` (ScriptableObject) + `BeatMapRecorder.cs` — mapeo de la canción a mano (tecla `Espacio` mientras suena, grabado con `Keyboard.current`, NO `Input.GetKeyDown` — este proyecto tiene el Input System puro, no "Both"). Tiene filtro de espaciado mínimo (`Filtrar espaciado mínimo`, contextmenu) para evitar notas amontonadas.
- [x] `NoteSpawner.cs` + `Note.cs` — instancia notas que viajan de `spawnPoint` a `targetPoint` sobre `travelTime`, llegando exactamente en su beat. Botón (`InputActionReference` a `XRI Right/Activate`) resuelve la más cercana. `Note.OnResolved` da `(quality, isPerfect, position)`.
- [x] `CloudGrowth.cs` — la nube **sube en Y** (no crece de escala) con cada nota resuelta, proporcional a la calidad, con tope (`maxRiseHeight`).
- [x] `CloudBoundary.cs` — límite horizontal (radio) Y vertical (altura min/max) para que el jugador no se aleje caminando ni "vuele" con el simulador. Empuja el `XR Origin` para compensar, mide la posición de la `Main Camera` (no del Origin). Viñeta opcional vía `Volume`+`Vignette`.
- [x] `CometEffect.cs` + `Comet.cs` — en un toque perfecto, vuela una estela (`Trail Renderer` + `StarGlow` dorado) desde la nota hasta la nube.
- [x] `StarField.cs` — estrella permanente por cada perfecto, con stem al azar asignado (ver arriba).
- [x] `PerfectTracker.cs` — cuenta resueltas/perfectas vs. total del `BeatMapData`. `OnSongEnded` se dispara cuando `audioSource.time >= songEndTime` (205s — el final REAL de la música, sin el silencio de cola del archivo).
- [x] `MoonCollectible.cs` — desbloquea si `%perfectos >= Required Perfect Percentage` (configurable, default pensado en 1.0 = 100%, bajarlo solo para testear). Guarda con `PlayerPrefs`. `IsUnlockedByKey(string)` estático para que el menú (del compañero) pregunte el estado.
- [x] `FollowPlayerHeight.cs` — mantiene la altura de `NoteSpawnPoint`/`NoteTargetPoint` igual a la de la cámara real, independiente de lo que haga la nube (evita que crecer/subir la nube desalinee las notas).

### Menú (armado por el compañero, no yo)
- [x] `MainMenu.unity`, `MainMenuController.cs`, `PauseController.cs`, `EndScreenController.cs`, `MoonIconController.cs`. Integrado sin problemas tras un merge (ver "Lecciones" abajo).

### Modelos y assets visuales
- [x] Luna: geometría = `Sphere` default de Unity (NO usar el modelo pesado de Tripo, salió con ~2M triángulos). Textura: real, de Solar System Scope (gratis, no comercial).
- [x] 2 postes de la portada + farol/baliza de las notas: generados en **Tripo3D** (texto, no imagen — imagen de referencia da resultados deformes en objetos finos como postes). El farol de las notas quedó con el "ojo" mirando al costado, no hacia arriba (la IA no lo resolvió en 2 intentos) — **se aceptó así**: el haz de luz (`LightBeam`, ver shaders) sale de la parte de arriba del objeto igual, sin depender de que el modelo "apunte" bien.
- [ ] Edificio(s) de fondo — prompt ya definido, pendiente generar/traer a Unity.
- [ ] Ventanas que se encienden en el edificio (según perfectos) — pendiente, va a ser `Quad`s emisivos puestos a mano, no parte del modelo.

### Shaders (todos escritos a mano en HLSL/ShaderLab, no Shader Graph — más rápido de iterar sin depender de tutoriales)
- [x] `StarGlow.shader` — color + brillo (`_GlowIntensity`) plano, blend `Additive`, alfa forzado a `1` (evita que quede invisible si `_Color.a` o la textura tienen alfa 0). Usado en: estrellas, cometa (estela).
- [x] `LightBeam.shader` — haz de luz: degradado vertical (brillante en la base, se apaga hacia arriba) + Fresnel (translúcido de frente, más visible de canto). Pensado para un Cylinder default (pivot centrado, altura 2 unidades). Usado en: el haz sobre el farol de las notas. Color actual: amarillo cálido `#FFD98A` (tipo lámpara de sodio).
- [x] `SkyGradient.shader` — degradado de cielo (horizonte→arriba) en una esfera gigante (`SkyDome`) que envuelve la escena desde adentro, dejando el sistema de estrellas intacto (son objetos aparte, no está baked en el shader). **Tuvo que simplificarse a opaco normal** (ver Lecciones) — nada de `Queue=Background`/`ZWrite Off`, es un objeto opaco común con `Cull Off`.
- [ ] Niebla baja — pendiente (tutorial: Paulina VFX).
- [ ] Ventanas del edificio — pendiente, probablemente reusa `AudioReactiveMaterial` + un material emisivo simple, no necesita shader nuevo.
- [ ] Corrimiento de color hacia el clímax — pendiente, es post-procesado (`Volume` + `Color Adjustments`, mismo patrón que la viñeta), no shader.

### VR / Input
- Simulador: **XR Interaction Simulator** (3.5.1, el nuevo, con panel en pantalla). Botón de gatillo derecho = tecla `T` (atajo directo) o `` ` `` para ciclar + `Espacio` para apretar. `R` reposiciona la vista si "se vuela".
- El proyecto usa **Input System puro** (`Active Input Handling: Input System Package`, no "Both") — `UnityEngine.Input` clásico tira excepción. Todo nuestro código usa `UnityEngine.InputSystem` (`Keyboard.current`, `InputActionReference`).
- Testeo recomendado con Quest real: **Quest Link/Air Link** para iterar rápido (pero NO valida rendimiento real, la PC hace el trabajo pesado) + al menos una build APK standalone antes de la entrega para confirmar que corre bien en el hardware real del Quest.

## Lecciones aprendidas (para no perder tiempo de nuevo)

1. **`Input.GetKeyDown`/`Input class` clásico no funciona en este proyecto** — tira `InvalidOperationException`. Siempre `UnityEngine.InputSystem`.
2. **`AudioSource.mute` Y `Volume` bajo SÍ afectan `GetSpectrumData`** (a pesar de lo que dice la sabiduría popular de tutoriales de internet) — para tener una pista audible-cero pero analizable de verdad, hay que rutearla por un **Audio Mixer** con el grupo a -80dB, dejando el `Volume` del `AudioSource` en `1`.
3. **Promediar el espectro completo (1024 bins) diluye mucho la señal** — mejor limitar a una fracción (la mitad grave/media) como hace `AudioAnalyzer` con sus bandas.
4. **Un shader `Additive` sin cuidado con el alfa puede quedar invisible** — si `_Color.a` o la textura terminan en alfa 0, con blend `SrcAlpha One` no se ve nada. Forzar alfa `1` en el shader si no necesitás transparencia variable de verdad.
5. **`Cull Front`/`Cull Back` en un shader personalizado no siempre se comporta como uno espera** — si algo se ve invisible sin razón aparente, probar `Cull Off` primero como diagnóstico antes de perder tiempo con la lógica de culling.
6. **Shaders con `Queue: Background` + `ZWrite Off` pueden fallar sin dar error** (nuestro caso del `SkyDome`) — si un objeto de "fondo" no aparece, simplificar a un shader opaco normal (cola default, `ZWrite` normal) antes de asumir que es un problema de culling o escala.
7. **`OpenXRPackageSettings.asset` da conflictos de git "falsos" seguido** — Unity regenera los fileID internos de sus features en cada máquina; si hay conflicto ahí, comparar que ambos lados apunten a la MISMA feature (mismo `m_Script` guid) antes de asumir que hay que elegir un lado.
8. **Archivos `.unity`/`.asset` de Unity no se mergean bien a mano en general** — pero si el conflicto es chico (pocas líneas), sí se puede resolver leyendo el YAML con cuidado, no hace falta UnityYAMLMerge para todo.
9. **Reorganizar carpetas de scripts hay que hacerlo DESDE Unity** (arrastrando en la ventana Project), nunca copiando/moviendo por fuera con el Explorador — si no, quedan carpetas duplicadas con clases repetidas (nos pasó una vez, con `Note`/`NoteSpawner`).
10. **Modelos generados por IA (Tripo) para postes/objetos finos con imagen de referencia salen deformados** — mejor texto, describiendo la forma en palabras. Para orientación específica ("que mire hacia arriba"), la IA tiene sesgos fuertes hacia formas "típicas" (spotlight con soporte horizontal) y cuesta mucho corregir con el prompt — a veces es más rápido aceptar la orientación que salió y resolver visualmente con otra pieza (nuestro caso: el haz de luz sale de arriba del objeto, sin depender del "ojo" del modelo).
11. **Modelos de IA para geometría simple (una esfera, ej. la luna) suelen venir con polycounts absurdos** (~2M triángulos) — para formas simples, mejor usar primitivas de Unity + una textura real, no gastar créditos de IA en geometría.

## Pendiente (por orden sugerido)

1. Terminar de pulir el cometa (Trail Renderer — en progreso ahora).
2. Confirmar que `SkyGradient`/`SkyDome` se vea bien ya simplificado.
3. Edificio(s) de fondo (Tripo, texto) + ventanas emisivas que se prenden con perfectos.
4. Niebla baja (shader pendiente).
5. Corrimiento de color hacia el clímax (post-procesado, reusa patrón de la viñeta).
6. Integrar farol+haz+postes+edificio en la composición final de la escena, apuntando a que se vea como la portada.
7. Pulido general de todos los valores numéricos (sensibilidades, contrastes, radios) jugando la canción completa de punta a punta.
8. Build APK de prueba en un Quest real antes de la entrega.
