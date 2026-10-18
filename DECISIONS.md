# Decisiones de integración (claude-integration)

Registro de decisiones tomadas durante la integración de las fases F1–F8 del
brief, cuando el pedido original tenía ambigüedades o chocaba con convenciones
del proyecto. Cada entrada incluye el porqué.

## 2026-10-17 — F1 (Núcleo)

### Rutas y convenciones (brief vs. CLAUDE.md)
El brief pide `Assets/Scripts/<Sistema>/` y `Assets/Prefabs/`. El proyecto ya
usa `Assets/_Project/Scripts/<Sistema>/` y `Assets/_Project/Prefabs/`
(CLAUDE.md). **Se sigue la convención del proyecto** para no fragmentar la
estructura del repo: todo lo nuevo cae bajo `Assets/_Project/...`.

- Scripts nuevos: `Assets/_Project/Scripts/Core/`
- Tests EditMode: `Assets/_Project/Tests/EditMode/`
- JSON de datos: `Assets/_Project/Resources/Data/` (movidos desde la raíz)

### Rama
Rama nueva `claude-integration` a partir de `feature/transicion-cuevas`
(contiene Iker + araña). No se mergea a `main` desde esta sesión.

### Serialización JSON
`UnityEngine.JsonUtility` no soporta `Dictionary<string, T>`, y los dos JSON
son diccionarios. Se añade el paquete `com.unity.nuget.newtonsoft-json@3.2.1`
al `manifest.json` y se usa `Newtonsoft.Json` en los loaders.

### Enum de biomas
`Biome.CorruptedForest` ↔ string `"corrupted_forest"`, mapeado por
`BiomeExtensions.ToKey()`. Se evita exponer strings sueltos para no romper por
typos; el enum solo cubre los 4 biomas listados en `dialogues.json`.

### GameState: bootstrap sin escena de arranque
`GameState` se crea con `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`,
compatible con `DisableDomainReload`. El bootstrap también resetea los
`event` estáticos para que no queden suscriptores de una sesión anterior.

### Tests EditMode: asmdef propio
Para no obligar a los scripts de producción a vivir en un asmdef (lo que
rompería la visibilidad desde `Assembly-CSharp`), los tests tienen su propio
asmdef `DreamHacks.Core.EditModeTests` con `references: ["Assembly-CSharp"]`.

### Pendientes conocidos
- F2–F8: aún sin empezar (esperan luz verde del equipo tras validar F1).
- `ending_good` y `ending_bad` en `dialogues.json` traen `TODO_LORE`.
- `HannahEnemy.atk` está en 20 aunque `canAttack: false` (como indica §7 del
  plan: se deja por si cambian de idea).
