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
