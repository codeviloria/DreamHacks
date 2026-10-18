# TODO — Barra de vida con corazones (preparación del sistema de pelea)

Trabaja en orden y marca `[x]`. Usa el Unity MCP. Cero errores de consola al terminar cada bloque.
Insumos en `/home/gv-ia/Descargas/`.

## Datos
- 10 corazones = **40 cuartos**. El daño y la curación se miden en cuartos (1 = ¼ de corazón, 4 = 1 corazón).
- Sprites de 9×9 px: `corazon_lleno`, `corazon_tres_cuartos`, `corazon_medio`, `corazon_un_cuarto`, `corazon_vacio` (también vienen juntos en `corazones_sheet_45x9.png`).

## 1. Importar
- [ ] En `Assets/Editor/MapaImporter.cs`, agrega `"corazon"` al arreglo `Prefijos` y `"Assets/UI/"` al arreglo `Carpetas`.
- [ ] Copia los 5 `corazon_*.png` a `Assets/UI/Corazones/` y verifica que queden como Sprite, Point y sin compresión.
- [ ] Copia `VidaJugador.cs` a `Assets/Scripts/Combate/` y `HUDCorazones.cs` a `Assets/Scripts/UI/`.

## 2. Player
- [ ] Agrega `VidaJugador` al prefab `Player_Iker` con `corazonesMax = 10`.

## 3. HUD
- [ ] En la escena Hospital, crea un Canvas `HUD` (Screen Space Overlay, CanvasScaler Scale With Screen Size, referencia 1280×720).
- [ ] Dentro del Canvas crea `BarraCorazones`, anclado arriba a la izquierda, en la posición (24, -24), con `HUDCorazones`:
  - `vida` = la VidaJugador del player
  - asigna los 5 sprites
  - `tamano = 36`, `separacion = 4`, `porFila = 10`
- [ ] Haz el HUD prefab (`Assets/Prefabs/UI/HUD.prefab`) para reutilizarlo en otras escenas.

## 4. Prueba (Play en el Editor)
- [ ] La barra arranca con 10 corazones llenos.
- [ ] K quita ¼ y el corazón pasa por ¾ → ½ → ¼ → vacío.
- [ ] J quita un corazón completo y H cura uno.
- [ ] Al recibir daño la barra tiembla y parpadea.
- [ ] Con 2 corazones o menos, el último late.
- [ ] Al llegar a 0 se dispara `AlMorir`. Por ahora conéctalo a un `Debug.Log("Game Over")`.

## 5. Gancho para la pelea (sin implementarla todavía)
- [ ] Crea `Assets/Scripts/Combate/ZonaDanio.cs`: un trigger que, al tocar al player, llama `RecibirDanio(cuartos)` (campo configurable, default 1).
- [ ] Coloca una `ZonaDanio` de prueba desactivada en el Hospital.

### Notas de Claude Code
(escribe aquí)
