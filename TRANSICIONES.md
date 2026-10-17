# Transiciones top-down ↔ cueva metroidvania (Bioma1)

> Documento vivo. Actualizado en cada paso de `TAREA_TRANSICIONES.md`.

## Inventario inicial (paso 1)

### Escenas existentes

| Escena | Rol |
|---|---|
| `Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity` | Mundo top-down (3 salas, player verde, NPCs, enemigos). |
| `Assets/MetroidvaniaBase/Scenes/Bioma_WhatsApp_Sala1.unity` | Base metroidvania (tilemap Kenney, player 2D con dash/salto). **No tocar.** |
| `Assets/MetroidvaniaBase/Scenes/Test_Movement.unity` | Test metroidvania. **No tocar.** |
| `Assets/MetroidvaniaBase/Scenes/Test_Movimiento.unity` | Test metroidvania (duplicada). **No tocar.** |
| `Assets/Scenes/SampleScene.unity` | Residuo del template de Unity. Ignorar. |
| `Assets/Settings/Scenes/URP2DSceneTemplate.unity` | Template URP del template 2D. Ignorar. |

### Prefabs de jugador

| Prefab | Modo | Componentes clave |
|---|---|---|
| `Assets/_Project/Prefabs/Player.prefab` | Top-down | `Rigidbody2D` Dynamic, GravityScale 0, FreezeRotationZ · `DreamHacks.PlayerNS.PlayerController` (8 direcciones, Input System `Player/Move`). |
| `Assets/MetroidvaniaBase/Prefabs/Player.prefab` | Metroidvania | `Rigidbody2D` con gravedad, `CapsuleCollider2D` · `PlayerController2D` (coyote time, jump buffer, dash, `groundLayer`) · `PlayerRespawn` (killY, checkpoint). **No modificar.** |

### Cámaras por escena

| Escena | Cámara |
|---|---|
| `Sandbox_Gino` | `Main Camera` ortho size ~8.44 · `PixelPerfectCamera` (320×240, PPU 16) · `RoomCameraController` (desliza entre salas con InputBlocked). |
| `Bioma_WhatsApp_Sala1` | `Main Camera` ortho size 5 · `UniversalAdditionalCameraData` · `CinemachineBrain` (requiere `com.unity.cinemachine`, ya instalado). |

### Marcadores resueltos

- `<ESCENA_TOPDOWN_ENTRADA>` → **`Sandbox_Gino`** (única escena top-down del proyecto).
- `<ESCENA_TOPDOWN_REGRESO>` → **`Sandbox_Gino`** también, pero spawn distinto (sala 3). La tarea permite "otro mapa **o** a otro punto de llegada"; evitamos crear una segunda escena duplicada por ahora.

### Ids de spawn planificados

- `bioma1_inicio` — SpawnPoint en Bioma1, lado izquierdo.
- `salida_cueva` — SpawnPoint en Sandbox_Gino, dentro de la sala 3 (lejos del inicio original para que no se sienta un ciclo).
- `default` — fallback si `TransitionState.NextSpawnId` no coincide (p.ej. al abrir una escena en modo editor).

---

## Reporte final (paso 7)

### Escenas tocadas

| Escena | Qué se agregó |
|---|---|
| `Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity` | `Entrada_Bioma1` trigger (15, 10), `Spawn_salida_cueva` (50, 7.5), `Spawn_default` (10, 7.5), `PlayerSpawner`. |
| `Assets/_Project/Scenes/Bioma1.unity` **(nueva)** | Duplicada de `Bioma_WhatsApp_Sala1`. `Spawn_bioma1_inicio` (-2, 5), `PlayerSpawner`, `Salida_Bioma1` trigger (46, 5). |

### Ids de spawn usados

| Id | Escena | Posición | Rol |
|---|---|---|---|
| `bioma1_inicio` | Bioma1 | (-2, 5) | Entrada desde el top-down. |
| `salida_cueva` | Sandbox_Gino | (50, 7.5) | Regreso tras completar Bioma1 (sala 3). |
| `default` | Sandbox_Gino | (10, 7.5) | Fallback (equivale al spawn inicial original). |

### Cómo añadir una nueva transición en 3 pasos

1. **Marca el destino** en la escena de llegada: crea un GameObject vacío, añade `SpawnPoint` y escribe un `spawnId` único (p.ej. `bosque_norte`). Si la escena de llegada aún no lo tiene, pégale también un `PlayerSpawner`.
2. **Pon el trigger** en la escena de salida: crea un GameObject con un `SpriteRenderer` (sprite `P_PortalCueva` o el que corresponda), un `BoxCollider2D` con `isTrigger`, y un `SceneTransition` con `targetScene = "<escena destino>"` y `targetSpawnId = "<el id del paso 1>"`.
3. **Registra la escena destino** en `File → Build Settings` si todavía no está (sin eso, `LoadScene` falla en runtime).

### Lo que quedó pendiente

- La escena `Bioma1` arrastra una advertencia visual: el prefab `Kenney_WhatsApp` usa un material URP `Sprite-Lit-Default` cuyo GUID se resuelve desde el PackageCache local — en algunos reimports Unity puede marcarlo como rosa hasta que se refresca. No bloquea.
- El `Player` de Bioma1 es el prefab del metroidvania (`Assets/MetroidvaniaBase/Prefabs/Player.prefab`) con `PlayerController2D` + `PlayerRespawn`. Cuando entras desde Sandbox_Gino, el Player del top-down se destruye con la escena y aparece el 2D — esto es el comportamiento deseado. Al volver pasa lo mismo al revés.
- `Entrada_Bioma1` está en (15, 10) de Sala 1, visualmente cerca del borde superior. Si molesta a la cámara deslizante, moverlo a otra sala es un cambio de posición puro, sin tocar lógica.
- No se añadió trigger de salida en Bioma1 por si el jugador se cae del nivel. `PlayerRespawn` del prefab metroidvania maneja `killY = -10`; con el spawn en (-2, 5) hay margen de sobra.
- Falta pulido: no hay sonido al pasar, ni animación distinta del fade; tampoco un bloqueo del input del jugador durante el fade (el `ScreenFader` cubre visualmente pero el `PlayerController` sigue leyendo input).

### No hacer (recordatorio)

- No borrar ni renombrar escenas existentes.
- No tocar `Assets/MetroidvaniaBase/*` (es la base de referencia). Si hace falta cambiar algo del metroidvania, duplicar como se hizo con `Bioma1`.
- No fusionar `feature/transicion-cuevas` en `main` todavía.
