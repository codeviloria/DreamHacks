using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Cámara ortográfica top-down que sigue al Player con lerp suave y recorta
    /// a los límites del mapa (minBounds/maxBounds en coordenadas de mundo).
    /// Si Pixel Perfect Camera está activo, no interpola la posición para
    /// evitar jitter de subpíxel.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Suavizado del seguimiento (segundos). 0 = instantáneo.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [Header("Límites del mapa (mundo)")]
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;
        [SerializeField] private bool useBounds = true;

        private Camera _cam;
        private Vector3 _velocity;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

            if (useBounds)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                desired.x = Mathf.Clamp(desired.x, minBounds.x + halfW, maxBounds.x - halfW);
                desired.y = Mathf.Clamp(desired.y, minBounds.y + halfH, maxBounds.y - halfH);
            }

            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime)
                : desired;
        }
    }
}
