# Escenas del proyecto

Rutas relativas a la raíz del repo (`/mnt/seagate/Unity_Projects/DreamHacks`).
Las "activas" son las que están en Build Settings (`ProjectSettings/EditorBuildSettings.asset`).

## Build Settings (orden de carga)

| # | Escena | Ruta | GUID |
|---|---|---|---|
| 0 | **Sandbox_Gino** | `Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity` | `7e255cbfe1cd0a52194e3524274e65cc` |
| 1 | **Bioma1** | `Assets/_Project/Scenes/Bioma1.unity` | `fd67938c096c42dca5482b7f3ff06a4b` |

## Escenas del juego (en `Assets/_Project/Scenes/`)

### Sandbox_Gino — top-down RPG (ground map)
- Ruta: `Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity`
- Cámara: Orthographic, size **8.4375**
- Pixel Perfect Camera: **320 × 240**, Assets PPU **16**
- Propósito: escena de pruebas de Gino para el modo RPG top-down (Iker visto desde arriba, 8 direcciones, araña, diálogos).

### Bioma1 — metroidvania lateral
- Ruta: `Assets/_Project/Scenes/Bioma1.unity`
- Cámara: Orthographic, size **5** (sin Pixel Perfect Camera; usa el tileset Kenney a PPU 18)
- Propósito: Iker en vista lateral, movimiento + salto + ataque a distancia.

## Escenas de la base del metroidvania (no tocar sin acordarlo)

Vienen del template Kenney 2D Platformer. Mantener como referencia; integrarlas al flujo del juego mediante prefabs en `Assets/_Project/Prefabs/`.

| Escena | Ruta | Cámara |
|---|---|---|
| Bioma_WhatsApp_Sala1 | `Assets/MetroidvaniaBase/Scenes/Bioma_WhatsApp_Sala1.unity` | ortho size 5 |
| Test_Movement | `Assets/MetroidvaniaBase/Scenes/Test_Movement.unity` | ortho size 7 |
| Test_Movimiento | `Assets/MetroidvaniaBase/Scenes/Test_Movimiento.unity` | ortho size 5 |

## Escenas del template Unity (no borrar sin avisar)

| Escena | Ruta | Para qué |
|---|---|---|
| SampleScene | `Assets/Scenes/SampleScene.unity` | Escena por defecto de Unity (puede quedar vacía) |
| URP2DSceneTemplate | `Assets/Settings/Scenes/URP2DSceneTemplate.unity` | Template de URP 2D (no editar) |

## Recuperación automática de Unity

- `Assets/_Recovery/0.unity` — backup de `Sandbox_Gino` que Unity creó tras un cierre forzado. Si la escena en disco está bien, se puede borrar la carpeta `_Recovery/` con `rm -rf Assets/_Recovery/`.

## Convención del equipo

- Una escena por persona en `Assets/_Project/Scenes/_Sandbox/` (archivo `Sandbox_<Nombre>.unity`). Nadie edita la escena de otro sin avisar.
- Las escenas "canónicas" del juego viven directamente en `Assets/_Project/Scenes/`.
- Lo compartido entre escenas vive como prefab en `Assets/_Project/Prefabs/`.
- Resolución interna estándar: **320 × 240** (PPU 16) para pixel art, salvo el metroidvania que usa el tileset Kenney.
