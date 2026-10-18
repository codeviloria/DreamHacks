# DreamHacks — Modelo técnico y plan para Claude Code

**Deadline:** domingo 18 oct, 12:00 (≈18 h desde sáb 18:00). **Regla de oro:** primero un juego jugable de principio a fin con caja blanca; arte y pulido después.
**Unity 2D**, mundo 320×240 px, Unity MCP (CoplayDev). Datos en `Assets/Data/` (`stats.json`, `dialogues.json`).

---

## 1. Flujo de juego (escenas)

| # | Escena | Gameplay | Termina cuando |
|---|---|---|---|
| S0 | `Apartment` (top-down) | Despertador → teléfono (msg mamá) → 3 tareas: cama, desayuno, planta (gotas 0/1 + "+1 HP") → tarea "Go to the laundry shop" | Iker cruza la puerta → glitch negro 1010 → absorción |
| S1 | `Forest_Intro` | Slime ataca primero (batalla tutorial, sin espada hasta el narrador) → "You received Sword" → Hannah se une | Diálogo de Hannah termina |
| S2 | `Forest_Rock` | Empujar roca (Sokoban simple) + recoger Healing Fruit + 1–2 batallas normales (Spider, Trunk) | Roca movida → glitch + voz `???` |
| S3 | `Forest_Corrupted` | Orden 1: soltar pociones · Orden 2: seguir camino del hacker (salirse = spawn de enemigo hacker) · Orden 3: soltar comida · Batallas hacker. Música baja de pitch y desatura el bosque por etapas | Llegar al pasillo |
| S4 | `RedStatic` | Pasillo con puerta roja: daño por segundo mientras la cruzan (no letal: mínimo 1 HP) → puerta verde bloqueada → diálogo "DO IT" | Caja de elección **YES / NO** |
| S5a | `Boss_Cave` (metroidvania) | **NO** → boss Hacker 2 fases (reusar controlador con coyote time/dash) | Boss derrotado → `Ending_Good` (hospital) |
| S5b | `Battle_Hannah` | **YES** → batalla por turnos, Hannah no ataca | Hannah derrotada → `Ending_Bad` |

> Las cuevas son el puente visual entre top-down y metroidvania (como acordó el equipo).

---

## 2. Sistemas (arquitectura mínima)

| Sistema | Clase(s) | Responsabilidad |
|---|---|---|
| Estado global | `GameState` (singleton, `DontDestroyOnLoad`) | Nivel, HP/ATK actuales de Iker y Hannah, inventario, flags (`hasSword`, `hannahInParty`, `choice`), escena actual |
| Datos | `StatsDB`, `DialogueDB` | Cargan `stats.json` / `dialogues.json` desde `Resources` |
| Diálogo | `DialogueManager`, `DialogueUI` | Caja de texto con typewriter, nombre, retrato, **blip de voz por personaje** (pitch distinto), `Space/E` avanza, eventos al terminar (`onComplete`) |
| Elección | `ChoiceUI` | YES/NO → setea `GameState.choice` y carga escena |
| Tareas | `TaskManager`, `TaskListUI`, `Interactable` | Lista visible arriba-izq; `Interactable` (tecla E) completa tarea + dispara diálogo |
| Inventario | `Inventory` | Potion, HealingFruit, Food. Método `DropAll(type)` para órdenes del hacker |
| Batalla por turnos | `BattleManager`, `BattleUnit`, `BattleUI` | Estilo Omori: menú Attack / Item; Hannah acción automática (ataque o curar si Iker < 40% HP) |
| Progresión | `LevelSystem` | +1 nivel por batalla ganada: **+15 HP max, +5 ATK** (Iker y Hannah) |
| Trigger de escena | `SceneTrigger`, `SceneFader` | Fade/glitch y carga de escena |
| Corrupción | `CorruptionController` | 0–3 niveles: desatura (post-process/Volume o tint), baja pitch de música (0.9→0.75), glitch overlay |
| Camino forzado | `PathZone` | Fuera del camino → diálogo de advertencia + spawn de batalla hacker |
| Zona de daño | `HazardZone` | −X HP/seg, piso en 1 HP, diálogo de Hannah al entrar |
| Boss | `BossHacker`, `BossPattern`, `HackNode` | 2 fases, ataques como proyectiles/lasers, frases en pantalla |

### Acceso a datos (diccionarios)
`dialogues.json` = diccionarios planos por id + índice `biomes` (4: `apartment`, `forest`, `corrupted_forest`, `cave`).
```csharp
DialogueDB.Play("forest_hannah");                 // cualquier diálogo, cuando quieras
DialogueDB.Line("hacked", "idle");               // línea random de un set de batalla
DialogueDB.Biome("forest").dialogues;            // ids que usa ese bioma
DialogueDB.Biome(GameState.CurrentBiome).music;  // música / corrupción por bioma
StatsDB.Enemy("SlimeHacked"); StatsDB.Encounter("S3_a");
```
Regla: un id puede estar en varios biomas; el bioma activo lo define cada escena (`BiomeTag` en la escena).

---

## 3. Fórmulas de combate

- **Daño:** `dmg = round(ATK × Random(0.85, 1.15))`
- **Fallo:** 10 % (enemigos hacker 5 %). Fallo → línea "Whoa, too close!"
- **Curación:** Potion +30 HP · Healing Fruit +20 HP · Food +15 HP · Hannah "Here, use this!" cura 25 HP (1 vez por batalla)
- **Orden de turno:** Iker → Hannah → enemigos.
- **Game over** en batalla: reintentar desde inicio de la batalla con HP completo (jam: sin frustración).
- **Plataformas (boss):** cada golpe de espada = `ATK de Iker`; i-frames 1 s al recibir daño; checkpoint al inicio de cada fase.
- HP del boss `300.14` / `500.14`: guardar como `float`, mostrar con 2 decimales (decimal = detalle "glitch" intencional).

### Curva estimada (1 nivel por victoria)
| Momento | Batallas ganadas | Iker HP/ATK | Hannah HP/ATK |
|---|---|---|---|
| S1 tras slime | 1 | 115 / 40 | 85 / 25 (se une aquí) |
| Fin S2 | 3 | 145 / 50 | 115 / 35 |
| Fin S3 | 6 | 190 / 65 | 160 / 50 |

Con esto: boss fase 1 ≈ 5 golpes, fase 2 ≈ 8 golpes; Iker aguanta ~6 golpes en fase 1 y ~4 en fase 2. Batalla vs Hannah (200 HP) ≈ 3–4 golpes. **Si se siente fácil/difícil, solo se cambia `stats.json`.**

---

## 4. Backlog priorizado (P0 = sin esto no hay juego)

### P0 — Jugable de inicio a fin (meta: sáb 23:59)
| ID | Tarea | Dueño | Prompt para Claude Code |
|---|---|---|---|
| T1 | `GameState` + `StatsDB` + `DialogueDB` cargando los JSON | Dev A (Gino) | *"Crea GameState singleton persistente y loaders para Assets/Resources/Data/stats.json y dialogues.json según PLAN_CLAUDE_CODE.md §2. Incluye tests de carga."* |
| T2 | `DialogueManager` + UI con typewriter, retrato placeholder y blip por personaje | Dev B | *"Implementa DialogueManager que reproduzca un dialogue id de dialogues.json, typewriter 40 cps, blip AudioClip con pitch por speaker (Iker 1.0, Hannah 1.3, ??? 0.6, Narrator 0.9), evento onComplete."* |
| T3 | `TaskManager` + `Interactable` + escena `Apartment` caja blanca | Dev B | *"Crea escena Apartment 320×240 con cama, cocina, planta, teléfono y puerta como cubos/sprites blancos; TaskManager con tareas de §1 S0 y diálogos `apt_*`."* |
| T4 | `BattleManager` por turnos con datos de `stats.json`, líneas de batalla aleatorias por evento | Dev A | *"Implementa batalla por turnos estilo Omori según §3: party Iker+Hannah opcional, enemigo desde stats.json, menú Attack/Item, Hannah IA, líneas por evento (start/attack/hurt/miss/win) desde dialogues.json `battle_lines`. Escena aditiva Battle."* |
| T5 | `LevelSystem` (+15 HP/+5 ATK por victoria) | Dev A | *"Agrega LevelSystem a GameState: al ganar batalla sube nivel a Iker y Hannah (si está en party) usando hp_per_level/atk_per_level."* |
| T6 | Escenas `Forest_Intro`, `Forest_Rock`, `Forest_Corrupted`, `RedStatic` en caja blanca con triggers | Dev C | *"Crea las escenas S1–S4 en caja blanca con SceneTrigger, triggers de batalla y de diálogo según §1. Roca empujable con Rigidbody2D kinematic por tiles."* |
| T7 | `ChoiceUI` YES/NO y ramas S5a/S5b | Dev C | *"ChoiceUI con dos botones; NO → Boss_Cave, YES → Battle_Hannah."* |
| T8 | `Battle_Hannah` (Hannah sin ataques, líneas por umbral de HP 100%/75%/30%/0%) | Dev A | *"Reusa BattleManager con flag enemyCannotAttack; líneas de dialogues.json `battle_hannah_enemy` por umbral de HP."* |
| T9 | Endings: `Ending_Good` (hospital) y `Ending_Bad` como diálogo + imagen fija | Dev B | *"Escenas de final con imagen fullscreen y diálogo; botón volver al menú."* |

### P1 — Lo que hace que se sienta el juego (meta: dom 08:00)
| ID | Tarea | Dueño |
|---|---|---|
| T10 | `Boss_Cave` metroidvania: importar controlador existente, `BossHacker` fase 1 (3 ataques) y fase 2 (2 ataques), barra de HP con decimales, frases del hacker | Dev A |
| T11 | `CorruptionController` (desaturar + pitch de música + glitch overlay) en 3 niveles | Dev C |
| T12 | Órdenes del hacker: `Inventory.DropAll`, `PathZone` con castigo, `HazardZone` | Dev C |
| T13 | Transición puerta → negro con 1010 + "absorción" (escala + shake) | Dev B |
| T14 | Planta: partículas de 0/1 + texto flotante "+1 HP" | Dev B |
| T15 | Integrar sprites de artistas y música (reemplazo de placeholders por prefab) | Todos |

### P2 — Si sobra tiempo (cortar sin culpa)
- `HackNode` en el boss (hackear nodos para dañarlo) · menú de inicio con opciones · animaciones de ataque · guardado.

---

## 5. Cronograma

| Hora | Dev A (Gino) | Dev B | Dev C | Arte / Música / Lore |
|---|---|---|---|---|
| Sáb 18–20 | T1, T4 | T2 | T6 (S1, S2) | Sprites Iker/Hannah/slime · música bosque normal · revisar `dialogues.json` |
| Sáb 20–24 | T5, T8 | T3, T9 | T6 (S3, S4), T7 | Enemigos + versiones hacker · música corrupta · blips de voz |
| Sáb 24 | **Build de integración P0** | | | |
| Dom 00–08 | T10 | T13, T14 | T11, T12 | Fondos, boss, endings |
| Dom 08–11 | T15, balance en `stats.json` | T15 | T15 | Pulido |
| Dom 11–12 | **Build final + entrega** (congelar a las 11:00) | | | |

---

## 6. Reglas para trabajar en paralelo con Claude Code (equipo de 14–19 años)

1. **Una escena = un dueño.** Nadie edita la escena de otro; se comparten **prefabs** (Player, DialogueUI, BattleUI).
2. Rama por dev: `dev-a`, `dev-b`, `dev-c`; merge a `main` en los checkpoints (24:00, 08:00, 11:00).
3. Lore/diálogos **solo se editan en `dialogues.json`**; balance **solo en `stats.json`**. Así los no-programadores cambian contenido sin tocar código.
4. Cada prompt a Claude Code empieza con: *"Lee PLAN_CLAUDE_CODE.md. Tarea Tn."*
5. Antes de cada merge: abrir Unity, revisar consola sin errores, jugar la escena 1 vez.

---

## 7. Ajustes detectados en el guion (para el equipo de lore)

- La **Orden 1** y la **Orden 3** tenían el mismo texto del hacker. En `dialogues.json` la Orden 3 ya dice *food* en vez de *healing items*.
- La línea del teléfono venía cortada ("ame thing again…"). Quedó como: *"Same thing again. I can't deal with this right now. Better get the day started."* — confirmar.
- `Hannah` enemiga tiene ATK 20 pero el guion dice que no ataca: se deja en datos por si cambian de idea, pero `canAttack: false`.
- Falta el texto del **final bueno (hospital)** y **final malo**: hay placeholders `TODO_LORE` en `dialogues.json`.
- Pequeñas correcciones de inglés aplicadas (p. ej. "Where am I?", "if it wasn't me who attacked you, you would have attacked me first").
