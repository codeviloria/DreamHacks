using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DreamHacks.World;

namespace DreamHacks.PlayerNS
{
    /// <summary>
    /// Ataque a distancia para el Player del metroidvania. Al pulsar la acción
    /// "Attack" del Input System se reproduce la animación de ataque (5 frames)
    /// y en el frame en que Iker suelta el proyectil, instancia el prefab
    /// Projectile2D hacia la dirección en que mira el player (según su velocidad
    /// horizontal reciente o la escala del transform del Visual).
    /// Mientras ataca ignora nuevas pulsaciones (cooldown implícito = duración
    /// de la animación + attackCooldown extra).
    /// </summary>
    public class PlayerAttack2D : MonoBehaviour
    {
        [Header("Animación (orden secuencial)")]
        [Tooltip("Frames de la animación de ataque. Se reproducen en orden a frameSeconds por frame; en el frame releaseIndex se dispara el proyectil.")]
        [SerializeField] private Sprite[] attackFrames;
        [Tooltip("Sprite al que vuelve el SpriteRenderer una vez termina el ataque (p. ej. idle o post-ataque).")]
        [SerializeField] private Sprite postFrame;
        [SerializeField, Min(0.01f)] private float frameSeconds = 0.07f;
        [Tooltip("Índice (0-based) del frame en el que se instancia el proyectil.")]
        [SerializeField, Min(0)] private int releaseIndex = 4;
        [SerializeField, Min(0f)] private float attackCooldown = 0.1f;

        [Header("Proyectil")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Transform (hijo del player) desde donde sale el proyectil. Si está vacío, se usa la posición del Visual.")]
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0f)] private float projectileSpeed = 10f;

        [Header("Referencias")]
        [Tooltip("SpriteRenderer que se intercambia durante el ataque. Si está vacío se busca en el mismo GameObject.")]
        [SerializeField] private SpriteRenderer visualRenderer;

        private Rigidbody2D _rb;
        private InputAction _attackAction;
        private bool _attacking;
        private int _facing = 1; // 1 = derecha, -1 = izquierda

        // Otros componentes (PlayerSpriteLateral) leen esto para no pisar
        // los sprites de ataque mientras dura la animación.
        public bool IsAttacking => _attacking;
        public int Facing => _facing;

        private void Awake()
        {
            if (visualRenderer == null) visualRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponentInParent<Rigidbody2D>();
            var asset = InputSystem.actions;
            if (asset != null) _attackAction = asset.FindAction("Player/Attack");
        }

        private void OnEnable()  { _attackAction?.Enable(); if (_attackAction != null) _attackAction.started += OnAttack; }
        private void OnDisable() { if (_attackAction != null) _attackAction.started -= OnAttack; }

        private void Update()
        {
            // Guardamos la última dirección horizontal no nula; en reposo
            // seguimos apuntando a la última dirección movida (como un shooter).
            if (_rb != null)
            {
                float vx = _rb.linearVelocity.x;
                if (Mathf.Abs(vx) > 0.05f) _facing = vx > 0f ? 1 : -1;
            }
        }

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            if (_attacking) return;
            if (attackFrames == null || attackFrames.Length == 0 || projectilePrefab == null) return;
            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            _attacking = true;
            bool fired = false;
            // El spritesheet está dibujado solo hacia la derecha. Si el player
            // mira a la izquierda, reflejamos el SpriteRenderer durante la
            // animación y lo restauramos al terminar para no romper el flipX
            // que esté usando el controlador de sprites lateral.
            bool prevFlipX = visualRenderer != null && visualRenderer.flipX;
            if (visualRenderer != null) visualRenderer.flipX = _facing < 0;

            // Durante el ataque el PlayerSpriteLateral también podría cambiar
            // el sprite; mientras dura el ataque le ganamos cada frame.
            for (int i = 0; i < attackFrames.Length; i++)
            {
                if (visualRenderer != null && attackFrames[i] != null)
                    visualRenderer.sprite = attackFrames[i];
                if (!fired && i >= releaseIndex)
                {
                    FireProjectile();
                    fired = true;
                }
                yield return WaitUnscaled(frameSeconds);
            }
            if (!fired) FireProjectile(); // por si releaseIndex > length
            if (visualRenderer != null && postFrame != null) visualRenderer.sprite = postFrame;
            if (attackCooldown > 0f) yield return WaitUnscaled(attackCooldown);
            if (visualRenderer != null) visualRenderer.flipX = prevFlipX;
            _attacking = false;
        }

        private void FireProjectile()
        {
            Vector3 origin = muzzle != null ? muzzle.position : (visualRenderer != null ? visualRenderer.transform.position : transform.position);
            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var proj = go.GetComponent<Projectile2D>();
            if (proj != null) proj.Launch(new Vector2(_facing, 0f), projectileSpeed);
            // Si no tiene script, aplicamos velocidad al Rigidbody2D directamente.
            else
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = new Vector2(_facing * projectileSpeed, 0f);
            }
            // Voltea el proyectil si va a la izquierda.
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = _facing < 0;
        }

        private static WaitForSecondsRealtime WaitUnscaled(float s) => new WaitForSecondsRealtime(s);
    }
}
