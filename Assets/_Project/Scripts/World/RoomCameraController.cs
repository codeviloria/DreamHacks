using System.Collections;
using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Cámara estilo Zelda clásico: cada sala es una pantalla.
    /// Cuando el player cruza el borde horizontal de la sala, la cámara se
    /// desliza suavemente a la nueva y el input del player queda bloqueado
    /// durante la transición.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RoomCameraController : MonoBehaviour
    {
        // Ancho/alto de una sala en tiles (= en unidades mundo con PPU=16 y cellSize=1).
        [SerializeField] private int roomWidth = 20;
        [SerializeField] private int roomHeight = 15;

        // Número de salas en horizontal; con 3 salas, índices 0, 1, 2.
        [SerializeField] private int roomCount = 3;

        // Transform del jugador para saber en qué sala está.
        [SerializeField] private Transform player;

        // Duración del slide entre salas (segundos).
        [SerializeField] private float slideDuration = 0.45f;

        private int _currentRoom = -1;
        private bool _sliding;

        private void Start()
        {
            // Autobuscar al player si no se asignó en el inspector.
            // Usamos tag para no acoplar la cámara al namespace del player.
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

            int room = RoomIndexOf(player.position.x);
            if (room != _currentRoom)
            {
                StartCoroutine(SlideTo(room));
            }
        }

        private int RoomIndexOf(float worldX)
        {
            int idx = Mathf.FloorToInt(worldX / roomWidth);
            return Mathf.Clamp(idx, 0, roomCount - 1);
        }

        private Vector3 CenterOfRoom(int roomIndex)
        {
            // Centro en coordenadas de mundo: (roomWidth/2, roomHeight/2) + offset de sala.
            // Importante mantener la Z en -10 para que la cámara ortográfica vea el plano XY.
            float cx = roomIndex * roomWidth + roomWidth * 0.5f;
            float cy = roomHeight * 0.5f;
            return new Vector3(cx, cy, -10f);
        }

        private void SnapToRoomOfPlayer()
        {
            if (player == null) return;
            _currentRoom = RoomIndexOf(player.position.x);
            transform.position = CenterOfRoom(_currentRoom);
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
