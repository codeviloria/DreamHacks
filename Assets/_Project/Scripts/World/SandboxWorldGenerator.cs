using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DreamHacks.Battle;

namespace DreamHacks.World
{
    /// <summary>
    /// Generador procedural de salas estilo Saw Game / Sandbox para explorar.
    /// Replica el estilo visual del Sandbox_Gino (suelo gris, paredes negras con colisión, aperturas con marco amarillo).
    /// Genera salas a los laterales y alrededores en una cuadrícula de 20x15 unidades mundo por sala,
    /// con puzzles interactivos (placas de presión, barreras láser, interruptores), y enemigos patrulleros
    /// que inician combate RPG convencional al contacto (EncounterTrigger -> Battle scene).
    /// </summary>
    public class SandboxWorldGenerator : MonoBehaviour
    {
        [Header("Configuración de Salas")]
        [Tooltip("Dimensiones de cada habitación (debe coincidir con RoomCameraController: 20x15).")]
        [SerializeField] private int roomWidth = 20;
        [SerializeField] private int roomHeight = 15;

        [Header("Generación")]
        [Tooltip("Generar automáticamente al iniciar la escena.")]
        [SerializeField] private bool generateOnStart = true;
        [Tooltip("Semilla aleatoria (0 para aleatorio puro).")]
        [SerializeField] private int seed = 12345;

        [Header("Prefabs y Configuración")]
        [SerializeField] private GameObject enemyPrefab;

        private static Sprite _squareSprite;
        private readonly HashSet<Vector2Int> _allRooms = new HashSet<Vector2Int>();

        private static readonly string[] PossibleEnemies = new string[]
        {
            "Slime",
            "GiantSpider",
            "EvilTrunk",
            "SlimeHacked"
        };

        private void Start()
        {
            // Asegurar componente BattleReturn en la escena para restauración tras combates RPG
            if (FindFirstObjectByType<BattleReturn>() == null)
            {
                gameObject.AddComponent<BattleReturn>();
            }

            // Asegurar que enemigos preexistentes en la escena tengan EncounterTrigger
            EquipExistingEnemies();

            if (generateOnStart)
            {
                GenerateWorld();
            }
        }

        private void EquipExistingEnemies()
        {
            var existingEnemies = GameObject.FindObjectsByType<EnemyWanderer>(FindObjectsSortMode.None);
            foreach (var wanderer in existingEnemies)
            {
                if (wanderer.GetComponentInChildren<EncounterTrigger>() == null)
                {
                    var triggerGO = new GameObject("EncounterTrigger");
                    triggerGO.transform.SetParent(wanderer.transform, false);

                    var col = triggerGO.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(1.5f, 1.5f);
                    col.isTrigger = true;

                    var enc = triggerGO.AddComponent<EncounterTrigger>();
                    SetPrivateField(enc, "enemyId", "Slime");
                    SetPrivateField(enc, "battleScene", "Battle");
                }
            }
        }

        [ContextMenu("Generar Mundo Saw Game")]
        public void GenerateWorld()
        {
            if (seed != 0) Random.InitState(seed);
            EnsureSprite();

            // Registrar las 3 salas originales de Sandbox_Gino en (0,0), (1,0) y (2,0)
            _allRooms.Clear();
            _allRooms.Add(new Vector2Int(0, 0));
            _allRooms.Add(new Vector2Int(1, 0));
            _allRooms.Add(new Vector2Int(2, 0));

            // Salas a generar en laterales (este/oeste) y pisos superior/inferior
            Vector2Int[] newRoomCoords = new Vector2Int[]
            {
                // Extensión lateral Este (a la derecha de Sala 3)
                new Vector2Int(3, 0),
                new Vector2Int(4, 0),

                // Extensión lateral Oeste (a la izquierda de Sala 1)
                new Vector2Int(-1, 0),
                new Vector2Int(-2, 0),

                // Fila Superior (Norte de las salas)
                new Vector2Int(-1, 1),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(3, 1),

                // Fila Inferior (Sur de las salas)
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
                new Vector2Int(2, -1),
                new Vector2Int(3, -1)
            };

            foreach (var c in newRoomCoords)
            {
                _allRooms.Add(c);
            }

            // Desbloquear pasos en el Tilemap de paredes original para conectar con las nuevas salas
            OpenOriginalPerimeterDoors();

            var parent = new GameObject("Salas_Generadas_SawGame");
            parent.transform.SetParent(transform, false);

            for (int i = 0; i < newRoomCoords.Length; i++)
            {
                BuildRoom(parent.transform, newRoomCoords[i], i);
            }

            Debug.Log($"[SandboxWorldGenerator] Se generaron exitosamente {newRoomCoords.Length} nuevas salas Saw Game conectadas.");
        }

        private void OpenOriginalPerimeterDoors()
        {
            var wallsGO = GameObject.Find("Walls");
            if (wallsGO == null) return;

            var tilemap = wallsGO.GetComponent<Tilemap>();
            if (tilemap == null) return;

            // Abrir puerta Este de Sala 2 (x=59..60, y=6..8)
            for (int y = 6; y <= 9; y++)
            {
                tilemap.SetTile(new Vector3Int(59, y, 0), null);
                tilemap.SetTile(new Vector3Int(60, y, 0), null);
            }
            CreateSpriteBox("DoorMarker_East_Original", transform, new Vector2(59.5f, 7.5f), new Vector2(1f, 3f),
                new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);

            // Abrir puerta Oeste de Sala 0 (x=-1..0, y=6..8)
            for (int y = 6; y <= 9; y++)
            {
                tilemap.SetTile(new Vector3Int(0, y, 0), null);
                tilemap.SetTile(new Vector3Int(-1, y, 0), null);
            }
            CreateSpriteBox("DoorMarker_West_Original", transform, new Vector2(0.5f, 7.5f), new Vector2(1f, 3f),
                new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);

            // Abrir puertas Norte y Sur en las 3 salas originales (centros en X: 10, 30, 50)
            int[] roomCenterXs = new int[] { 10, 30, 50 };
            foreach (int rcx in roomCenterXs)
            {
                // Puerta Norte
                for (int x = rcx - 1; x <= rcx + 1; x++)
                {
                    for (int y = 14; y <= 15; y++)
                        tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
                CreateSpriteBox($"DoorMarker_N_{rcx}", transform, new Vector2(rcx, 14.5f), new Vector2(3f, 1f),
                    new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);

                // Puerta Sur
                for (int x = rcx - 1; x <= rcx + 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                        tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
                CreateSpriteBox($"DoorMarker_S_{rcx}", transform, new Vector2(rcx, 0.5f), new Vector2(3f, 1f),
                    new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);
            }
        }

        private void BuildRoom(Transform parent, Vector2Int coord, int roomIndex)
        {
            var roomGO = new GameObject($"Sala_SawGame_{coord.x}_{coord.y}");
            roomGO.transform.SetParent(parent, false);

            float cx = coord.x * roomWidth + roomWidth * 0.5f;
            float cy = coord.y * roomHeight + roomHeight * 0.5f;

            // 1. Suelo gris (#808080)
            CreateSpriteBox("Floor", roomGO.transform, new Vector2(cx, cy), new Vector2(roomWidth - 1, roomHeight - 1),
                new Color(0.5f, 0.5f, 0.5f), 0, false);

            // 2. Paredes negras con BoxCollider2D (#000000) respetando puertas hacia salas adyacentes
            float wallThickness = 1f;
            float halfW = roomWidth * 0.5f;
            float halfH = roomHeight * 0.5f;
            float doorSize = 3f;

            bool hasNorth = _allRooms.Contains(coord + Vector2Int.up);
            bool hasSouth = _allRooms.Contains(coord + Vector2Int.down);
            bool hasEast = _allRooms.Contains(coord + Vector2Int.right);
            bool hasWest = _allRooms.Contains(coord + Vector2Int.left);

            // Pared Norte
            float posY_N = cy + halfH - wallThickness * 0.5f;
            if (hasNorth)
            {
                float segmentWidth = (roomWidth - doorSize) * 0.5f;
                CreateSpriteBox("Wall_N_L", roomGO.transform, new Vector2(cx - halfW + segmentWidth * 0.5f, posY_N),
                    new Vector2(segmentWidth, wallThickness), Color.black, 2, true);
                CreateSpriteBox("Wall_N_R", roomGO.transform, new Vector2(cx + halfW - segmentWidth * 0.5f, posY_N),
                    new Vector2(segmentWidth, wallThickness), Color.black, 2, true);
                CreateSpriteBox("Door_N", roomGO.transform, new Vector2(cx, posY_N),
                    new Vector2(doorSize, wallThickness), new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);
            }
            else
            {
                CreateSpriteBox("Wall_N", roomGO.transform, new Vector2(cx, posY_N),
                    new Vector2(roomWidth, wallThickness), Color.black, 2, true);
            }

            // Pared Sur
            float posY_S = cy - halfH + wallThickness * 0.5f;
            if (hasSouth)
            {
                float segmentWidth = (roomWidth - doorSize) * 0.5f;
                CreateSpriteBox("Wall_S_L", roomGO.transform, new Vector2(cx - halfW + segmentWidth * 0.5f, posY_S),
                    new Vector2(segmentWidth, wallThickness), Color.black, 2, true);
                CreateSpriteBox("Wall_S_R", roomGO.transform, new Vector2(cx + halfW - segmentWidth * 0.5f, posY_S),
                    new Vector2(segmentWidth, wallThickness), Color.black, 2, true);
                CreateSpriteBox("Door_S", roomGO.transform, new Vector2(cx, posY_S),
                    new Vector2(doorSize, wallThickness), new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);
            }
            else
            {
                CreateSpriteBox("Wall_S", roomGO.transform, new Vector2(cx, posY_S),
                    new Vector2(roomWidth, wallThickness), Color.black, 2, true);
            }

            // Pared Oeste
            float posX_W = cx - halfW + wallThickness * 0.5f;
            if (hasWest)
            {
                float segmentHeight = (roomHeight - doorSize) * 0.5f;
                CreateSpriteBox("Wall_W_T", roomGO.transform, new Vector2(posX_W, cy + halfH - segmentHeight * 0.5f),
                    new Vector2(wallThickness, segmentHeight), Color.black, 2, true);
                CreateSpriteBox("Wall_W_B", roomGO.transform, new Vector2(posX_W, cy - halfH + segmentHeight * 0.5f),
                    new Vector2(wallThickness, segmentHeight), Color.black, 2, true);
                CreateSpriteBox("Door_W", roomGO.transform, new Vector2(posX_W, cy),
                    new Vector2(wallThickness, doorSize), new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);
            }
            else
            {
                CreateSpriteBox("Wall_W", roomGO.transform, new Vector2(posX_W, cy),
                    new Vector2(wallThickness, roomHeight), Color.black, 2, true);
            }

            // Pared Este
            float posX_E = cx + halfW - wallThickness * 0.5f;
            if (hasEast)
            {
                float segmentHeight = (roomHeight - doorSize) * 0.5f;
                CreateSpriteBox("Wall_E_T", roomGO.transform, new Vector2(posX_E, cy + halfH - segmentHeight * 0.5f),
                    new Vector2(wallThickness, segmentHeight), Color.black, 2, true);
                CreateSpriteBox("Wall_E_B", roomGO.transform, new Vector2(posX_E, cy - halfH + segmentHeight * 0.5f),
                    new Vector2(wallThickness, segmentHeight), Color.black, 2, true);
                CreateSpriteBox("Door_E", roomGO.transform, new Vector2(posX_E, cy),
                    new Vector2(wallThickness, doorSize), new Color(0.95f, 0.8f, 0.1f, 0.6f), 1, false);
            }
            else
            {
                CreateSpriteBox("Wall_E", roomGO.transform, new Vector2(posX_E, cy),
                    new Vector2(wallThickness, roomHeight), Color.black, 2, true);
            }

            // 3. Contenido temático de la sala: Puzzles Saw Game y/o Combates RPG
            int roomType = roomIndex % 4;
            if (roomType == 0)
            {
                // Sala con Puzzle Saw Game puro (Placas de presión + Barrera láser)
                BuildSawPuzzleInRoom(roomGO.transform, cx, cy, 2);
            }
            else if (roomType == 1)
            {
                // Sala con Enemigo patrullero que inicia combate RPG convencional al contacto
                string enemyType = PossibleEnemies[roomIndex % PossibleEnemies.Length];
                BuildRPGEnemyInRoom(roomGO.transform, cx, cy, enemyType);
            }
            else if (roomType == 2)
            {
                // Sala Mixta: Puzzle Saw Game con 3 interruptores custodiado por un enemigo RPG
                BuildSawPuzzleInRoom(roomGO.transform, cx - 2f, cy, 3);
                string enemyType = PossibleEnemies[(roomIndex + 1) % PossibleEnemies.Length];
                BuildRPGEnemyInRoom(roomGO.transform, cx + 4f, cy + 2f, enemyType);
            }
            else
            {
                // Sala Doble Enemigo RPG (desafío)
                BuildRPGEnemyInRoom(roomGO.transform, cx - 4f, cy, PossibleEnemies[0]);
                BuildRPGEnemyInRoom(roomGO.transform, cx + 4f, cy, PossibleEnemies[1]);
            }
        }

        private void BuildSawPuzzleInRoom(Transform roomTransform, float cx, float cy, int platesCount)
        {
            var puzzleGO = new GameObject("SawPuzzle");
            puzzleGO.transform.SetParent(roomTransform, false);
            var puzzle = puzzleGO.AddComponent<SawPuzzleRoom>();

            // Barrera electrificada roja que bloquea el paso
            var barrier = CreateSpriteBox("Saw_Barrier", puzzleGO.transform, new Vector2(cx, cy), new Vector2(1.2f, 3.5f),
                new Color(1f, 0.2f, 0.2f, 0.85f), 10, true);

            // Luz indicadora de estado del puzzle (rojo = bloqueado, verde = resuelto)
            var light = CreateSpriteBox("Status_Light", puzzleGO.transform, new Vector2(cx, cy + 2.2f), new Vector2(0.6f, 0.6f),
                new Color(1f, 0.2f, 0.2f), 12, false);

            // Placas de presión que el jugador debe pisar para desactivar la barrera
            for (int p = 0; p < platesCount; p++)
            {
                float offsetX = (p == 0) ? -4.5f : ((p == 1) ? 4.5f : 0f);
                float offsetY = (p == 0) ? -2.5f : ((p == 1) ? 2.5f : -3f);
                CreateSwitchPlate($"Plate_{p + 1}", puzzleGO.transform, new Vector2(cx + offsetX, cy + offsetY), puzzle);
            }

            SetPrivateField(puzzle, "doorBarrier", barrier);
            SetPrivateField(puzzle, "statusLight", light.GetComponent<SpriteRenderer>());
            SetPrivateField(puzzle, "requiredSteps", platesCount);
            SetPrivateField(puzzle, "puzzleName", $"Mecanismo Saw {platesCount} Placas");
        }

        private GameObject CreateSwitchPlate(string name, Transform parent, Vector2 pos, SawPuzzleRoom puzzle)
        {
            var plate = CreateSpriteBox(name, parent, pos, new Vector2(1.2f, 1.2f), new Color(0.94f, 0.82f, 0.16f), 5, false);
            var col = plate.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
            col.isTrigger = true;

            var sw = plate.AddComponent<SawPuzzleSwitch>();
            SetPrivateField(sw, "targetPuzzle", puzzle);
            SetPrivateField(sw, "triggerOnStep", true);
            SetPrivateField(sw, "visualIndicator", plate.GetComponent<SpriteRenderer>());
            SetPrivateField(sw, "activatedColor", Color.cyan);

            return plate;
        }

        private void BuildRPGEnemyInRoom(Transform roomTransform, float cx, float cy, string enemyId)
        {
            GameObject enemyGO;
            if (enemyPrefab != null)
            {
                enemyGO = Instantiate(enemyPrefab, new Vector3(cx, cy, 0), Quaternion.identity, roomTransform);
                enemyGO.name = $"Enemy_{enemyId}_{roomTransform.name}";
            }
            else
            {
                // Crear enemigo procedural con sprite rojo característico
                enemyGO = CreateSpriteBox($"Enemy_{enemyId}", roomTransform, new Vector2(cx, cy), new Vector2(1.2f, 1.2f),
                    new Color(0.88f, 0.18f, 0.18f), 10, false);

                var rb = enemyGO.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                var solidCol = enemyGO.AddComponent<BoxCollider2D>();
                solidCol.size = new Vector2(1f, 1f);
                solidCol.isTrigger = false;

                enemyGO.AddComponent<EnemyWanderer>();
            }

            // Trigger hijo de batalla RPG convencional
            var triggerGO = new GameObject("EncounterTrigger");
            triggerGO.transform.SetParent(enemyGO.transform, false);

            var trigCol = triggerGO.AddComponent<BoxCollider2D>();
            trigCol.size = new Vector2(1.6f, 1.6f);
            trigCol.isTrigger = true;

            var enc = triggerGO.AddComponent<EncounterTrigger>();
            SetPrivateField(enc, "enemyId", enemyId);
            SetPrivateField(enc, "battleScene", "Battle");
        }

        private GameObject CreateSpriteBox(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int sortingOrder, bool addSolidCollider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _squareSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(size.x, size.y, 1);

            if (addSolidCollider)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                col.isTrigger = false;
            }

            return go;
        }

        private static void EnsureSprite()
        {
            if (_squareSprite != null) return;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(pixels);
            tex.Apply();
            _squareSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
