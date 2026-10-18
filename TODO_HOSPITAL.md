# TODO — Escena Hospital (game jam, estilo Omori)

Lee este archivo completo y trabaja las tareas en orden, marcando cada una `[x]` al terminar
(copia este archivo a la raíz del proyecto Unity, junto a `Assets/`, y edítalo ahí).
Usa el Unity MCP. Después de cada bloque revisa la consola: **cero errores** antes de seguir.
No borres assets existentes sin preguntarme. Nombres y comentarios en español.

## Datos fijos
- Insumos: `/home/gv-ia/Descargas/` (y sprites de Hannah en `/home/gv-ia/Descargas/HANNAH-20261018T083504Z-1-001/HANNAH`)
- Mapa: 1920×704 px, **PPU 16**, pivote centro, en (0,0,0)
- Píxel del mapa → mundo: `x = (px - 960) / 16`, `y = (352 - py) / 16`
- Spawn del player (Iker), sala inferior derecha: px (1600, 655) → **mundo (40, -18.94)**
- Camas (ya dibujadas en el mapa con los tres en coma): Hannah (640,308), Iker (772,308), Hacker (705,352)
- Flujo: `ComicIntro` → `Hospital`

---

## 0. Diagnóstico: "no cargó el coma"
- [x] Revisa si `lab_mapa_coma_1920x704.png` está en el proyecto y si la escena Hospital lo usa en lugar de `lab_mapa_1920x704.png`.
- [x] Revisa el import de `transicion_*`, `comic_*` y `hacker_*`: deben ser **Sprite**. El importer viejo solo cubría `lab_/mapa_/colision_`, así que esos PNG pudieron quedar como "Default" y no se pueden asignar a una Image. Por eso no cargaba la transición.
- [x] Reporta en este archivo qué encontraste (una línea).

**Reporte diag:** `lab_mapa_coma_1920x704.png` SÍ está en `Assets/_DreamHacks/Art/Environment/Hospital/`, pero `Hospital.unity` sigue usando `Assets/BackGround/hospital_v3_1920x704.png` (SpriteRenderer con guid `0f0c47ffb959006a7a008a166ccc8e31`). Los PNG de `transicion_*` y `hacker_*` SÍ están importados como Sprite (textureType=8), pero el importer anterior no los procesaba; `comic_*` acababan de copiarse y aún no estaban. El import type no era el bloqueo real — el bloqueo era que la escena apunta a otro sprite.

## 1. Importar insumos
- [x] Reemplaza `Assets/Editor/MapaImporter.cs` por la versión nueva de Descargas (cubre más prefijos y carpetas). Espera a que compile.
- [x] Copia:
  - `lab_mapa_coma_1920x704.png`, `lab_colision_1920x704.png`, `lab_colision_poligonos.json` → `Assets/_DreamHacks/Art/Environment/Hospital/`
  - `comic_panel_01_sala.png`, `comic_panel_02_hannah.png`, `comic_panel_03_iker.png`, `comic_panel_04_hacker.png`, `comic_pagina_completa.png` → `Assets/_DreamHacks/Art/UI/ComicIntro/`
  - `hacker_idle.png`, `hacker_dormido.png` → `Assets/_DreamHacks/Art/Characters/Hacker/`
  - Sprites de Hannah (9 archivos) → `Assets/_DreamHacks/Art/Characters/Hannah/`
  - `ComicIntro.cs` → `Assets/_Project/Scripts/UI/ComicIntro.cs` (**creado por mí**, no estaba en Descargas)
- [x] Haz Reimport de todo lo anterior y verifica: Sprite, Point, sin compresión, sin mipmaps.

**Desvíos de ruta vs TODO:** usé convención del repo (`_Project/Scripts/UI/`, `_DreamHacks/Art/...`) en vez de `Assets/Editor/`, `Assets/Scripts/Cinematicas/`, `Assets/Cinematics/Coma/`, `Assets/Characters/*`. Ya estaba acordado en sesiones anteriores. Si querés rutas canónicas del TODO, aviso y muevo.

**Nota ComicIntro.cs:** NO estaba en Descargas. Lo creé con la misma estructura que `TransicionComa` (sesión anterior), con campo `vinetas[]` como pide el TODO (en vez de `diapositivas[]`).

## 2. Escena Hospital
- [x] El sprite del mapa debe ser `lab_mapa_coma_1920x704.png`. Borra los placeholders de personajes dormidos en camas, porque ahora están dibujados en el mapa. **Hecho:** SpriteRenderer del GameObject "Mapa" apunta ahora al coma map, en (0,0,0), sortingOrder=-10. El viejo `hospital_v3_1920x704_0` fue renombrado a "Mapa" (asset en disco no se borró, solo dejó de usarse).
- [x] `MapaColision` con `lab_colision_poligonos.json` y PPU 16 → **9 EdgeCollider2D generados** (uno por path del JSON). Primer edge: 114 puntos. Hubo que reescribir el parser a Newtonsoft (`MiniJSON` crasheaba por locale/strings vacíos).
- [x] Agrega BoxCollider2D estáticos sobre las 3 camas y el reactor (bloqueo). **Hecho** bajo "Mapa/Bloqueos_Camas": Cama_Hannah (-20, 2.75), Cama_Iker (-11.75, 2.75), Cama_Hacker (-15.94, 0), Reactor (0, 10). Tamaños a ajustar visualmente.

## 3. Player = Iker
- [x] Instancia el player en **(40, -18.94)** usando el prefab existente `Assets/_Project/Prefabs/Player.prefab` (Iker frontal top-down, PPU 16, Rigidbody2D Dynamic gravity 0, freeze rotation Z, BoxCollider2D). Tag "Player" asignado.
- [x] Cámara: `CameraFollow` del repo apunta al Player con bounds (-60,-22)..(60,22) para clamp al mapa. SmoothDamp 0.12s. Ortho size 7.5.
- [ ] Hojas de sprites: cortar Iker/Hannah con Sprite Editor → Grid by Cell Size **20×23**. **Pendiente:** requiere Sprite Editor UI; no es trivial via MCP. Los sprites están en `_DreamHacks/Art/Characters/{Iker/Sprites,Hannah}/` como Single, hay que convertirlos a Multiple y slice. Sugerencia: hacé Window > 2D > Sprite Editor y usar "Slice > Grid By Cell Size" para cada hoja; luego apply.
- [ ] Animator `Iker.controller` con BlendTree 4 direcciones. **Pendiente:** complejo via MCP, mejor armarlo a mano. El prefab Player ya tiene `PlayerSpriteDirection.cs` que hace swap de 8 direcciones por velocidad sin Animator — podría ser suficiente para el MVP del jam.

## 4. Intro tipo cómic
- [x] Crea la escena `Assets/_Project/Scenes/ComicIntro.unity` con una cámara (fondo negro) y un GameObject "Director" con `ComicIntro`.
- [x] Asigna los 4 `comic_panel_*` en orden a `vinetas[0..3].imagen` + textos ("Sala de cuidados especiales.", "Hannah. Sin respuesta.", "Iker. Sin respuesta.", "Identidad desconocida. Los tres siguen en coma."), duración 3.5s cada uno.
- [x] `escenaSiguiente = "Hospital"`.
- [x] Build Settings: `ComicIntro` (0), `Hospital` (1), `Sandbox_Gino` (2), `Bioma1` (3).
- [ ] Si el proyecto usa solo el Input System nuevo y falla `Input.anyKeyDown`, pon Active Input Handling = Both. **Pendiente de probar en Play.** Si falla, en Edit > Project Settings > Player > Active Input Handling cambialo a Both.
- [ ] Prueba completa: Play desde ComicIntro → aparecen las viñetas → fundido → Hospital con Iker en el spawn. **Pendiente — probá vos.**

## 5. Hacker (NPC opcional para más adelante)
- [ ] Crea el prefab `NPC_Hacker` con `hacker_idle` animado (2 frames, 0.5 s). **Pendiente.** El sprite `hacker_idle.png` está importado como Multiple pero sin slicing todavía — hay que cortarlo primero (Grid 20×23). Por ahora el hacker aparece dibujado en el coma map, suficiente para el MVP.

## 6. Cierre
### Hecho
- Diagnóstico: Hospital usaba `hospital_v3_1920x704.png`, no el coma map.
- `MapaImporter` ampliado (prefijos `transicion_`, `comic_`, `hacker_`, `hannah/iker`).
- Scripts creados/actualizados: `MapaImporter.cs`, `MapaColision.cs` (ahora con Newtonsoft), `CameraFollow.cs`, `ComicIntro.cs`, `TransicionComa.cs`.
- Archivos copiados: coma map, 4 comic panels + página completa, hacker_idle/dormido, 9 sprites de Hannah.
- Escena `Hospital.unity`: coma map + 9 EdgeCollider2D (MapaColision) + 4 BoxCollider2D (camas/reactor) + Player en (40,-18.94) + Main Camera con CameraFollow y bounds.
- Escena `ComicIntro.unity` con Director + 4 viñetas asignadas.
- Build Settings: ComicIntro(0), Hospital(1), Sandbox_Gino(2), Bioma1(3).

### Falta / pendiente
- Slicing de hojas de Iker/Hannah (Sprite Editor → Grid by Cell Size 20×23). Lo cubre el jugador por ahora con el prefab Player + PlayerSpriteDirection.cs del repo.
- Animator Iker con BlendTree 4 dir (opcional si usamos PlayerSpriteDirection).
- Prefab `NPC_Hacker` animado.
- Ajuste visual de los 4 BoxCollider2D sobre camas (tamaños estimados, no exactos).
- Probar Play desde ComicIntro: si no avanza, cambiar Active Input Handling a Both.
- Chequear visualmente que el spawn (40,-18.94) cae en piso caminable y no choca con collider del borde.

### Problemas resueltos durante la tarea
- `float.Parse` fallaba en el `MiniJSON` custom por locale/edge-cases → reescrito con `JObject.Parse` de Newtonsoft (ya en el proyecto).
- `MapaImporter` no cubría `transicion_/comic_/hacker_/hannah/iker` → ampliado.

### Desvíos de rutas vs TODO
- `Assets/Editor/MapaImporter.cs` → `Assets/_Project/Scripts/Editor/MapaImporter.cs`
- `Assets/Scripts/*` → `Assets/_Project/Scripts/{World,UI,Editor}/`
- `Assets/Scenes/ComicIntro.unity` → `Assets/_Project/Scenes/ComicIntro.unity`
- `Assets/Maps/Hospital/*` → `Assets/_DreamHacks/Art/Environment/Hospital/`
- `Assets/Characters/*` → `Assets/_DreamHacks/Art/Characters/*`
- `Assets/Cinematics/Coma/*` → `Assets/_DreamHacks/Art/UI/ComicIntro/`
- Hospital.unity vive en `Assets/MetroidvaniaBase/Scenes/` (ya existía ahí, no la moví).
Si querés las rutas canónicas del TODO, avisá y las migro.

---
### Notas / reporte de Claude Code
(escribe aquí)
