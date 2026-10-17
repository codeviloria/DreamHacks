# Tarea: transición top-down ↔ cueva metroidvania (Bioma1)

Proyecto Unity DreamHacks. Trabaja en una rama nueva: `feature/transicion-cuevas`.
Haz un commit al terminar cada paso. No edites escenas que no se nombran aquí.

## Objetivo

Un cuadro rojo en el mapa top-down lleva a la escena de cueva **Bioma1** (metroidvania).
Otro cuadro rojo al final de Bioma1 regresa al mundo top-down, pero a otro mapa
o a otro punto de llegada (la siguiente sala), no al mismo lugar de entrada.

Mapa top-down de entrada: `<ESCENA_TOPDOWN_ENTRADA>`
Mapa top-down de regreso: `<ESCENA_TOPDOWN_REGRESO>`
(Si siguen con estos marcadores, dedúcelos en el paso 1 y anótalos en TRANSICIONES.md.)

## Paso 1 – Revisar antes de tocar
- Lista las escenas existentes, los prefabs del jugador (top-down y metroidvania)
  y la configuración de cámara de cada uno. Escríbelo en `TRANSICIONES.md`.
- No cambies nada todavía.

## Paso 2 – Scripts base (`Assets/Scripts/Transitions/`)
- `SceneTransition.cs`: trigger 2D que, al entrar el jugador, carga `targetScene`
  y guarda `targetSpawnId`. Campos: `targetScene` (string), `targetSpawnId` (string).
- `SpawnPoint.cs`: marca un punto de aparición con un `spawnId`.
- `TransitionState.cs`: clase estática con `NextSpawnId`.
- `PlayerSpawner.cs`: al cargar la escena, coloca al jugador en el SpawnPoint
  cuyo id sea `NextSpawnId`. Si no hay coincidencia, usa el spawn `default`.
- `ScreenFader.cs`: fundido a negro de 0.3 s antes y después del cambio de escena.
- Verifica que todo compile sin errores en la consola.

## Paso 3 – Entrada a la cueva (mapa top-down)
- En `<ESCENA_TOPDOWN_ENTRADA>`, crea un cuadro rojo (sprite blanco tintado de rojo,
  `BoxCollider2D` con isTrigger) llamado `Entrada_Bioma1`.
- SceneTransition: `targetScene = "Bioma1"`, `targetSpawnId = "bioma1_inicio"`.

## Paso 4 – Escena Bioma1 (metroidvania)
- Si no existe, crea `Bioma1` duplicando la escena de la maqueta metroidvania
  (coyote time, dash, cámara). No modifiques la escena original.
- Añade un SpawnPoint `bioma1_inicio` a la izquierda y un PlayerSpawner.
- Al final del nivel, a la derecha, crea un cuadro rojo `Salida_Bioma1` con
  SceneTransition: `targetScene = "<ESCENA_TOPDOWN_REGRESO>"`, `targetSpawnId = "salida_cueva"`.

## Paso 5 – Regreso al top-down
- En `<ESCENA_TOPDOWN_REGRESO>`, crea un SpawnPoint `salida_cueva` y un PlayerSpawner.
- Comprueba que el jugador aparece con el controlador top-down y su cámara,
  no con los del metroidvania.

## Paso 6 – Build Settings y prueba
- Agrega a Build Settings las escenas involucradas.
- Prueba en Play Mode el recorrido completo:
  top-down → cuadro rojo → Bioma1 → cuadro rojo → otro mapa top-down.
- Revisa la consola. Corrige los errores y repite la prueba.

## Paso 7 – Reporte
- Actualiza `TRANSICIONES.md` con: escenas tocadas, ids de spawn usados,
  cómo añadir una nueva transición en 3 pasos, y lo que quedó pendiente.

## No hacer
- No borres ni renombres escenas existentes.
- No cambies el controlador metroidvania (coyote, dash, cámara).
- No hagas merge a `main`.
