using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Animación simple en bucle: recorre una lista de Sprites y los asigna
    /// al SpriteRenderer del mismo GameObject a un intervalo fijo. Pensado
    /// para enemigos y props donde no hace falta un Animator completo.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameSeconds = 0.18f;

        private SpriteRenderer _sr;
        private float _timer;
        private int _index;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0 && frames[0] != null) _sr.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            _timer += Time.deltaTime;
            if (_timer < frameSeconds) return;
            _timer = 0f;
            _index = (_index + 1) % frames.Length;
            if (frames[_index] != null) _sr.sprite = frames[_index];
        }
    }
}
