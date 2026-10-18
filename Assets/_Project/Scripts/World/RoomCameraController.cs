using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Cámara estilo Zelda / Saw Game: cada sala es una pantalla fija con su propio centro y tamaño.
    /// Soporta:
    /// 1. Modo Grilla Dinámica 2D (Saw Game / Generación de Mundo: se desliza horizontal y verticalmente al centro de cada sala de W x H).
    /// 2. Modo CustomRooms (lista explícita de rectángulos).
    /// 3. Modo Original de bandas horizontales.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RoomCameraController : MonoBehaviour
    {
        [System.Serializable]
        public struct CustomRoom
        {
            public string name;
            public Rect bounds; // En coordenadas mundo (X min, Y min, Ancho, Alto)
            public Vector2 customCenter; // Si es zero, usa bounds.center
        }

        [Header("Modo Habitaciones Personalizadas")]
        [Tooltip("Si hay habitaciones aquí, se usan sus límites rectangulares.")]
        [SerializeField] private CustomRoom[] customRooms;

        [Header("Modo Grilla de Salas (Ancho x Alto de cada sala en unidades mundo)")]
        [SerializeField] private int roomWidth = 20;
        [SerializeField] private int roomHeight = 15;
        [SerializeField] private int roomCount = 3;
        [Tooltip("Si está activo, detecta cambios de sala tanto en X como en Y.")]
        [SerializeField] private bool grid2D = true;

        [Header("Configuración General")]
        [SerializeField] private Transform player;
        [SerializeField] private float slideDuration = 0.45f;

        private Vector2Int _currentCoord = new Vector2Int(int.MinValue, int.MinValue);
        private int _currentRoom = -1;
        private bool _sliding;

        public void SetPlayer(Transform t) => player = t;

        private void Start()
        {
            if (player == null)
            {
                var pGO = GameObject.FindGameObjectWithTag("Player");
                if (pGO != null) player = pGO.transform;
            }

            SnapToRoomOfPlayer();
        }

        private void LateUpdate()
        {
            if (_sliding || player == null) return;

            if (customRooms != null && customRooms.Length > 0)
            {
                int room = CustomRoomIndexOf(player.position);
                if (room != -1 && room != _currentRoom)
                {
                    StartCoroutine(SlideToCustomRoom(room));
                }
            }
            else if (grid2D)
            {
                Vector2Int coord = GridCoordOf(player.position);
                if (coord != _currentCoord)
                {
                    StartCoroutine(SlideToGrid(coord));
                }
            }
            else
            {
                int room = RoomIndexOf(player.position.x);
                if (room != _currentRoom)
                {
                    StartCoroutine(SlideTo(room));
                }
            }
        }

        private Vector2Int GridCoordOf(Vector3 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x / roomWidth);
            int gy = Mathf.FloorToInt(worldPos.y / roomHeight);
            return new Vector2Int(gx, gy);
        }

        private Vector3 CenterOfGrid(Vector2Int coord)
        {
            float cx = coord.x * roomWidth + roomWidth * 0.5f;
            float cy = coord.y * roomHeight + roomHeight * 0.5f;
            return new Vector3(cx, cy, -10f);
        }

        private int CustomRoomIndexOf(Vector3 worldPos)
        {
            for (int i = 0; i < customRooms.Length; i++)
            {
                if (customRooms[i].bounds.Contains(worldPos))
                    return i;
            }
            return _currentRoom != -1 ? _currentRoom : 0;
        }

        private int RoomIndexOf(float worldX)
        {
            int idx = Mathf.FloorToInt(worldX / roomWidth);
            return Mathf.Clamp(idx, 0, roomCount - 1);
        }

        private Vector3 CenterOfRoom(int roomIndex)
        {
            float cx = roomIndex * roomWidth + roomWidth * 0.5f;
            float cy = roomHeight * 0.5f;
            return new Vector3(cx, cy, -10f);
        }

        private void SnapToRoomOfPlayer()
        {
            if (player == null) return;

            if (customRooms != null && customRooms.Length > 0)
            {
                _currentRoom = CustomRoomIndexOf(player.position);
                var cr = customRooms[_currentRoom];
                Vector2 c = (cr.customCenter != Vector2.zero) ? cr.customCenter : cr.bounds.center;
                transform.position = new Vector3(c.x, c.y, -10f);
            }
            else if (grid2D)
            {
                _currentCoord = GridCoordOf(player.position);
                transform.position = CenterOfGrid(_currentCoord);
            }
            else
            {
                _currentRoom = RoomIndexOf(player.position.x);
                transform.position = CenterOfRoom(_currentRoom);
            }
        }

        private IEnumerator SlideToGrid(Vector2Int targetCoord)
        {
            _sliding = true;
            GameSignals.InputBlocked = true;

            Vector3 from = transform.position;
            Vector3 to = CenterOfGrid(targetCoord);
            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideDuration));
                transform.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            transform.position = to;

            _currentCoord = targetCoord;
            _sliding = false;
            GameSignals.InputBlocked = false;
        }

        private IEnumerator SlideToCustomRoom(int targetRoom)
        {
            _sliding = true;
            GameSignals.InputBlocked = true;

            var cr = customRooms[targetRoom];
            Vector2 c = (cr.customCenter != Vector2.zero) ? cr.customCenter : cr.bounds.center;
            Vector3 from = transform.position;
            Vector3 to = new Vector3(c.x, c.y, -10f);
            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideDuration));
                transform.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            transform.position = to;

            _currentRoom = targetRoom;
            _sliding = false;
            GameSignals.InputBlocked = false;
        }

        private IEnumerator SlideTo(int targetRoom)
        {
            _sliding = true;
            GameSignals.InputBlocked = true;

            Vector3 from = transform.position;
            Vector3 to = CenterOfRoom(targetRoom);
            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideDuration));
                transform.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            transform.position = to;

            _currentRoom = targetRoom;
            _sliding = false;
            GameSignals.InputBlocked = false;
        }
    }
}
