# DreamHacks — Convenciones del proyecto

RPG 2D top-down en pixel art (estilo Omori / Zelda clásico). Proyecto de game jam: priorizar que funcione y sea jugable sobre que sea perfecto.

## Motor
- Unity **6000.6.0f1**, plantilla **Universal 2D** (URP 2D). Todos en el equipo usan esta misma versión.
- Ubuntu: abrir el editor con `-force-vulkan`.
- Control de cambios: Visible Meta Files, Asset Serialization = Force Text, Git LFS para png/aseprite/audio/fuentes.

## Resolución y grilla (no cambiar sin acordarlo con el equipo)
- Resolución interna: **320×240** (4:3).
- Tile: **16×16 px**, **PPU = 16** en todos los sprites.
- Una sala = una pantalla = **20×15 tiles**.
- Cámara: Orthographic, size **7.5**, con **Pixel Perfect Camera** (Reference Resolution 320×240, Assets PPU 16, Crop Frame X/Y activado).
- Escalado de ventana: múltiplos enteros (×3 = 960×720, ×4 = 1280×960).

## Import de sprites
- Filter Mode: **Point (no filter)**.
- Compression: **None**.
- Pixels Per Unit: **16**.
- Pivot de personajes: **Bottom Center**. Tiles: Center.
- Spritesheets exportados desde Aseprite y cortados en Grid de 16×16 (o el tamaño del personaje).

## Física
- Gravity 2D = **(0, 0)** (top-down).
- Player: Rigidbody2D Dynamic, Gravity Scale 0, Freeze Rotation Z, Interpolate.
- Movimiento en 8 direcciones con el Input System (nuevo).
- Paredes: Tilemap con TilemapCollider2D + CompositeCollider2D.

## Caja blanca (fase actual)
Placeholders de 16×16 en color plano:
- Gris = suelo
- Negro = pared (colisión)
- Azul = agua (bloquea)
- Amarillo = puerta / transición de sala
- Verde = player
- Rojo = enemigo
- Morado = NPC / interactuable

Cuando llegue el arte final se reemplaza el sprite; la lógica y los prefabs no cambian.

## Estructura de carpetas
```
Assets/
  _Project/
    Art/          sprites, tilesets, animaciones
    Audio/
    Prefabs/
    Scenes/
      _Sandbox/   una escena de pruebas por persona (Sandbox_Nombre.unity)
    Scripts/
      Player/
      World/      salas, puertas, transiciones
      UI/
      Dialogue/
    Tiles/        Tile Palettes y Tile assets
```

## Reglas de equipo (evitan conflictos)
- **Una escena por persona.** Nadie edita la escena de otro sin avisar.
- Lo compartido va en **prefabs**.
- Una rama por tarea: `feat/<tarea>`, PR hacia `main`. `main` siempre debe abrir y correr.
- Commits pequeños y frecuentes.
- Tareas como issues de GitHub (`gh issue create`).

## Código
- C#, un componente por responsabilidad.
- Nombres en inglés para clases y métodos; comentarios en español.
- Campos configurables con `[SerializeField] private`, no públicos.
- Sin `Find()` en `Update()`.

## Uso de Claude Code + MCP de Unity
- Antes de terminar una tarea: revisar `read_console` y dejar cero errores.
- No modificar `ProjectSettings/` ni los valores de resolución/PPU de este archivo sin que se pida.
- Crear sprites placeholder siguiendo la paleta de caja blanca de arriba.
- Trabajar en la escena Sandbox de quien pide la tarea, salvo que se indique otra.
