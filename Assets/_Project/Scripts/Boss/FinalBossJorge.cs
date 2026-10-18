using System.Collections;
using UnityEngine;

namespace DreamHacks.Boss
{
    /// <summary>
    /// Controlador del Final Boss "Jorge El Curioso":
    /// - Camina siguiendo al jugador con su ciclo de animación de caminata.
    /// - Salta periódicamente con sprites dedicados de subida, caída y aterrizaje.
    /// - Ataque de proyectiles de fuego estilo Bowser (Super Mario Bros 1) con bola de fuego en la mano.
    /// - Hitbox de contacto para hacer daño al personaje al chocar con él.
    /// - Especialidad del boss: Embestida veloz que, si el jugador la esquiva, estrella al boss contra la pared
    ///   y lo deja aturdido y vulnerable durante unos segundos ("falla con la pared").
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FinalBossJorge : MonoBehaviour
    {
        public enum BossState
        {
            Idle,
            Walking,
            Jumping,
            FireAttack,
            ChargeWindup,
            WallCharging,
            Stunned,
            Defeated
        }

        [Header("Sprites de Animación")]
        [Tooltip("Ciclo de respiración / Idle (5 frames)")]
        [SerializeField] private Sprite[] idleSprites;
        [Tooltip("Ciclo de caminata (3 frames)")]
        [SerializeField] private Sprite[] walkSprites;
        [Tooltip("Sprite preparando la bola de fuego en la mano")]
        [SerializeField] private Sprite firePrepSprite;
        [Tooltip("Sprite lanzando / extendiendo el fuego")]
        [SerializeField] private Sprite fireCastSprite;
        [Tooltip("Sprite final de lanzamiento de fuego")]
        [SerializeField] private Sprite fireReleaseSprite;
        [Tooltip("Sprite de subida en el salto")]
        [SerializeField] private Sprite jumpRiseSprite;
        [Tooltip("Sprite en el ápice / caída del salto con capa extendida")]
        [SerializeField] private Sprite jumpApexSprite;
        [Tooltip("Sprite de aterrizaje / pisada fuerte")]
        [SerializeField] private Sprite jumpLandSprite;
        [Tooltip("Sprite de rugido / preparación de embestida")]
        [SerializeField] private Sprite roarSprite;
        [Tooltip("Sprite de aturdimiento / postrado al fallar contra la pared")]
        [SerializeField] private Sprite stunSprite;

        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float jumpForceY = 12f;
        [SerializeField] private float jumpForceX = 3.5f;
        [SerializeField] private float jumpInterval = 4.5f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Ataque de Fuego (Estilo Bowser)")]
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private Vector2 fireSpawnOffset = new Vector2(1.2f, 1.0f);
        [SerializeField] private float fireCooldown = 5.0f;
        [SerializeField] private int fireballsPerBurst = 2;
        [SerializeField] private float burstInterval = 0.35f;

        [Header("Especialidad del Boss (Embestida & Choque con Pared)")]
        [SerializeField] private float chargeCooldown = 8.0f;
        [SerializeField] private float chargeWindupTime = 0.8f;
        [SerializeField] private float chargeSpeed = 9.5f;
        [SerializeField] private float maxChargeDuration = 2.5f;
        [SerializeField] private float stunDuration = 3.0f;
        [SerializeField] private LayerMask wallLayers = ~0;

        [Header("Combate & Hitbox")]
        [SerializeField] private int maxHealth = 300;
        [SerializeField] private int contactDamage = 15;
        [SerializeField] private int chargeContactDamage = 35;
        [SerializeField] private float contactKnockback = 8f;

        // Referencias internas
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Transform _playerTransform;
        private BossState _currentState = BossState.Idle;

        // Timers y estado
        private int _currentHealth;
        private float _nextJumpTime;
        private float _nextFireTime;
        private float _nextChargeTime;
        private float _animTimer;
        private int _animFrame;
        private bool _isGrounded;
        private int _facingDirection = 1; // 1 = derecha, -1 = izquierda
        private Vector2 _chargeDirection;
        private float _chargeStartTime;

        public BossState CurrentState => _currentState;
        public int CurrentHealth => _currentHealth;
        public bool IsStunned => _currentState == BossState.Stunned;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _currentHealth = maxHealth;

            // Restricción de rotación en 2D
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnsureHitboxColliders();
            TryAutoAssignSprites();
        }

        private void Start()
        {
            FindPlayer();
            _nextJumpTime = Time.time + Random.Range(2f, jumpInterval);
            _nextFireTime = Time.time + Random.Range(2.5f, fireCooldown);
            _nextChargeTime = Time.time + Random.Range(5f, chargeCooldown);
            _currentState = BossState.Walking;
        }

        private void Update()
        {
            if (_currentState == BossState.Defeated) return;

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            CheckGrounded();
            HandleFacingDirection();

            // Máquina de estados
            switch (_currentState)
            {
                case BossState.Idle:
                case BossState.Walking:
                    HandleNormalBehavior();
                    UpdateWalkingAnimation();
                    break;

                case BossState.Jumping:
                    UpdateJumpingAnimation();
                    break;

                case BossState.WallCharging:
                    // Timeout de seguridad por si la embestida no toca pared
                    if (Time.time - _chargeStartTime >= maxChargeDuration)
                    {
                        EndCharge(false);
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (_currentState == BossState.Defeated)
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                return;
            }

            switch (_currentState)
            {
                case BossState.Walking:
                    if (_playerTransform != null)
                    {
                        float dirX = Mathf.Sign(_playerTransform.position.x - transform.position.x);
                        _rb.linearVelocity = new Vector2(dirX * walkSpeed, _rb.linearVelocity.y);
                    }
                    break;

                case BossState.WallCharging:
                    _rb.linearVelocity = new Vector2(_chargeDirection.x * chargeSpeed, _rb.linearVelocity.y);
                    break;

                case BossState.Idle:
                case BossState.FireAttack:
                case BossState.ChargeWindup:
                case BossState.Stunned:
                    // En estos estados el boss no se desplaza horizontalmente por input
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    break;
            }
        }

        #region Comportamiento y Decisión de Acciones

        private void HandleNormalBehavior()
        {
            if (!_isGrounded) return;

            // 1. Especialidad del Boss: Embestida contra pared (si está lista)
            if (Time.time >= _nextChargeTime)
            {
                StartCoroutine(PerformChargeRoutine());
                return;
            }

            // 2. Ataque de bolas de fuego estilo Bowser
            if (Time.time >= _nextFireTime)
            {
                StartCoroutine(PerformFireAttackRoutine());
                return;
            }

            // 3. Salto periódico
            if (Time.time >= _nextJumpTime)
            {
                PerformJump();
                return;
            }

            _currentState = BossState.Walking;
        }

        private void PerformJump()
        {
            _currentState = BossState.Jumping;
            _nextJumpTime = Time.time + jumpInterval + Random.Range(-1f, 1.5f);

            float dirX = _playerTransform != null ? Mathf.Sign(_playerTransform.position.x - transform.position.x) : _facingDirection;
            _rb.linearVelocity = new Vector2(dirX * jumpForceX, jumpForceY);

            if (jumpRiseSprite != null) _sr.sprite = jumpRiseSprite;
        }

        private void UpdateJumpingAnimation()
        {
            if (_rb.linearVelocity.y > 1f)
            {
                if (jumpRiseSprite != null) _sr.sprite = jumpRiseSprite;
            }
            else if (_rb.linearVelocity.y < -1f)
            {
                if (jumpApexSprite != null) _sr.sprite = jumpApexSprite;
            }

            // Al tocar suelo nuevamente
            if (_isGrounded && _rb.linearVelocity.y <= 0.1f)
            {
                StartCoroutine(LandRoutine());
            }
        }

        private IEnumerator LandRoutine()
        {
            if (jumpLandSprite != null) _sr.sprite = jumpLandSprite;
            yield return new WaitForSeconds(0.2f);
            if (_currentState == BossState.Jumping)
            {
                _currentState = BossState.Walking;
            }
        }

        #endregion

        #region Ataque de Fuego (Estilo Bowser)

        private IEnumerator PerformFireAttackRoutine()
        {
            _currentState = BossState.FireAttack;
            _nextFireTime = Time.time + fireCooldown + Random.Range(0f, 2f);

            // 1. Preparar bola de fuego en la mano
            if (firePrepSprite != null) _sr.sprite = firePrepSprite;
            yield return new WaitForSeconds(0.45f);

            // 2. Ráfaga de disparos de fuego
            for (int i = 0; i < fireballsPerBurst; i++)
            {
                if (fireCastSprite != null) _sr.sprite = fireCastSprite;
                SpawnFireball();

                yield return new WaitForSeconds(burstInterval);

                if (fireReleaseSprite != null) _sr.sprite = fireReleaseSprite;
                yield return new WaitForSeconds(0.15f);
            }

            yield return new WaitForSeconds(0.2f);
            _currentState = BossState.Walking;
        }

        private void SpawnFireball()
        {
            Vector3 spawnPos = transform.position + new Vector3(fireSpawnOffset.x * _facingDirection, fireSpawnOffset.y, 0f);
            Vector2 shootDir = new Vector2(_facingDirection, 0f);

            if (_playerTransform != null)
            {
                // Ligero apunte hacia el player si está en diferente altura
                Vector2 targetDir = (_playerTransform.position - spawnPos).normalized;
                if (Mathf.Abs(targetDir.y) < 0.6f)
                {
                    shootDir = targetDir;
                }
            }

            if (fireballPrefab != null)
            {
                GameObject fbObj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
                var fb = fbObj.GetComponent<BossFireball>();
                if (fb != null) fb.Initialize(shootDir);
            }
            else
            {
                // Fallback procedural si no hay prefab asignado
                CreateProceduralFireball(spawnPos, shootDir);
            }
        }

        private void CreateProceduralFireball(Vector3 position, Vector2 direction)
        {
            GameObject fb = new GameObject("BossFireball_Instance");
            fb.transform.position = position;

            var sr = fb.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.45f, 0.1f, 1f);
            sr.sortingOrder = 15;

            // Crear un sprite circular de fuego
            Texture2D tex = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    Color c = dist <= 7f ? (dist < 4f ? Color.yellow : new Color(1f, 0.3f, 0f, 1f)) : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);

            var script = fb.AddComponent<BossFireball>();
            script.Initialize(direction);
        }

        #endregion

        #region Especialidad del Boss (Embestida & Fallo contra Pared)

        private IEnumerator PerformChargeRoutine()
        {
            _currentState = BossState.ChargeWindup;
            _nextChargeTime = Time.time + chargeCooldown + Random.Range(0f, 3f);

            // Rugido / Advertencia visual
            if (roarSprite != null) _sr.sprite = roarSprite;

            // Parpadeo de color para telegrafiar el peligro
            Color originalColor = _sr.color;
            for (int i = 0; i < 4; i++)
            {
                _sr.color = Color.red;
                yield return new WaitForSeconds(chargeWindupTime / 8f);
                _sr.color = originalColor;
                yield return new WaitForSeconds(chargeWindupTime / 8f);
            }
            _sr.color = originalColor;

            // Iniciar carrera veloz
            _currentState = BossState.WallCharging;
            _chargeStartTime = Time.time;
            float dirX = _playerTransform != null ? Mathf.Sign(_playerTransform.position.x - transform.position.x) : _facingDirection;
            _chargeDirection = new Vector2(dirX, 0f);

            // Sprite de carrera mientras embiste
            if (walkSprites != null && walkSprites.Length > 0 && walkSprites[0] != null)
            {
                _sr.sprite = walkSprites[walkSprites.Length - 1];
            }
        }

        /// <summary>
        /// Se llama cuando choca con una pared durante la embestida: el boss falla y queda aturdido.
        /// </summary>
        private void TriggerWallCrash()
        {
            if (_currentState != BossState.WallCharging) return;
            StartCoroutine(StunRoutine());
        }

        private IEnumerator StunRoutine()
        {
            _currentState = BossState.Stunned;
            _rb.linearVelocity = Vector2.zero;

            Debug.Log("[FinalBossJorge] ¡El Boss falló la embestida y se estrelló contra la pared! Queda aturdido.");

            // Sprite aturdido / arrodillado
            if (stunSprite != null) _sr.sprite = stunSprite;

            // Efecto de aturdimiento durante stunDuration segundos
            float elapsed = 0f;
            while (elapsed < stunDuration)
            {
                elapsed += Time.deltaTime;
                _sr.color = (Mathf.PingPong(elapsed * 5f, 1f) > 0.5f) ? new Color(0.8f, 0.8f, 1f) : Color.white;
                yield return null;
            }
            _sr.color = Color.white;

            // Recuperarse
            if (roarSprite != null)
            {
                _sr.sprite = roarSprite;
                yield return new WaitForSeconds(0.4f);
            }

            _currentState = BossState.Walking;
        }

        private void EndCharge(bool hitWall)
        {
            if (hitWall)
            {
                TriggerWallCrash();
            }
            else
            {
                _currentState = BossState.Walking;
            }
        }

        #endregion

        #region Animaciones y Orientación

        private void HandleFacingDirection()
        {
            if (_currentState == BossState.WallCharging || _currentState == BossState.Stunned) return;

            if (_playerTransform != null)
            {
                float diffX = _playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(diffX) > 0.2f)
                {
                    _facingDirection = diffX > 0 ? 1 : -1;
                    // Invertir visualmente el sprite
                    _sr.flipX = (_facingDirection < 0);
                }
            }
        }

        private void UpdateWalkingAnimation()
        {
            if (walkSprites == null || walkSprites.Length == 0) return;

            _animTimer += Time.deltaTime;
            if (_animTimer >= 0.18f)
            {
                _animTimer = 0f;
                _animFrame = (_animFrame + 1) % walkSprites.Length;
                if (walkSprites[_animFrame] != null)
                {
                    _sr.sprite = walkSprites[_animFrame];
                }
            }
        }

        #endregion

        #region Detección de Suelo y Colisiones

        private void CheckGrounded()
        {
            // Raycast desde la base de la hitbox hacia abajo
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.15f;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.4f, groundLayer);
            _isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            GameObject other = collision.gameObject;

            // 1. Detección de choque con el personaje (Hitbox física)
            if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
            {
                DamagePlayer(other, _currentState == BossState.WallCharging ? chargeContactDamage : contactDamage);
            }

            // 2. Detección de choque con pared durante la embestida ("falla con la pared")
            if (_currentState == BossState.WallCharging)
            {
                // Verificar si el contacto es lateral (pared o obstáculo vertical)
                foreach (var contact in collision.contacts)
                {
                    // Si la normal es horizontal (indica impacto frontal contra pared)
                    if (Mathf.Abs(contact.normal.x) > 0.5f)
                    {
                        TriggerWallCrash();
                        return;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
            {
                DamagePlayer(other.gameObject, _currentState == BossState.WallCharging ? chargeContactDamage : contactDamage);
            }
        }

        private void DamagePlayer(GameObject player, int damage)
        {
            Debug.Log($"[FinalBossJorge] ¡Golpeó al jugador con {damage} de daño!");

            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 pushDir = (player.transform.position - transform.position).normalized;
                if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.up + Vector2.right * _facingDirection;
                playerRb.linearVelocity = pushDir * contactKnockback;
            }
        }

        #endregion

        #region Daño al Boss y Utilidades

        public void TakeDamage(int amount)
        {
            if (_currentState == BossState.Defeated) return;

            // Mayor daño si está aturdido tras estrellarse contra la pared
            int finalDmg = IsStunned ? Mathf.RoundToInt(amount * 1.5f) : amount;
            _currentHealth -= finalDmg;
            Debug.Log($"[FinalBossJorge] Recibió {finalDmg} de daño. Vida restante: {_currentHealth}/{maxHealth}");

            StartCoroutine(DamageFlashRoutine());

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            _sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (_currentState != BossState.Stunned) _sr.color = Color.white;
        }

        private void Die()
        {
            _currentState = BossState.Defeated;
            _rb.linearVelocity = Vector2.zero;
            if (stunSprite != null) _sr.sprite = stunSprite;
            Debug.Log("[FinalBossJorge] ¡El Boss Jorge El Curioso ha sido derrotado!");
        }

        private void FindPlayer()
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                _playerTransform = pObj.transform;
            }
            else
            {
                var pCtrl = FindFirstObjectByType<PlayerNS.PlayerController>();
                if (pCtrl != null) _playerTransform = pCtrl.transform;
            }
        }

        private void EnsureHitboxColliders()
        {
            // BoxCollider2D principal para colisiones con suelo, paredes y player
            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(2.4f, 3.2f);
            boxCol.offset = new Vector2(0f, 1.6f);

            // Trigger secundario para área de daño certera
            var childTrig = transform.Find("DamageHitbox");
            if (childTrig == null)
            {
                var trigObj = new GameObject("DamageHitbox");
                trigObj.transform.SetParent(transform, false);
                var tCol = trigObj.AddComponent<BoxCollider2D>();
                tCol.isTrigger = true;
                tCol.size = new Vector2(2.6f, 3.4f);
                tCol.offset = new Vector2(0f, 1.6f);
            }
        }

        private void TryAutoAssignSprites()
        {
#if UNITY_EDITOR
            if (walkSprites != null && walkSprites.Length > 0 && walkSprites[0] != null) return;

            string spritePath = "Assets/_Project/Prefabs/FINAL_BOSS_JORGE_EL_CURIOSO.png";
            var loadedSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spritePath);
            if (loadedSprites == null || loadedSprites.Length == 0) return;

            var idleList = new System.Collections.Generic.List<Sprite>();
            var walkList = new System.Collections.Generic.List<Sprite>();

            foreach (var asset in loadedSprites)
            {
                if (asset is Sprite s)
                {
                    if (s.name.Contains("Idle")) idleList.Add(s);
                    else if (s.name.Contains("Walk")) walkList.Add(s);
                    else if (s.name.Contains("Fire_Prep")) firePrepSprite = s;
                    else if (s.name.Contains("Fire_Cast")) fireCastSprite = s;
                    else if (s.name.Contains("Fire_Release")) fireReleaseSprite = s;
                    else if (s.name.Contains("Jump_Rise")) jumpRiseSprite = s;
                    else if (s.name.Contains("Jump_Apex")) jumpApexSprite = s;
                    else if (s.name.Contains("Jump_Land")) jumpLandSprite = s;
                    else if (s.name.Contains("Roar")) roarSprite = s;
                    else if (s.name.Contains("Stun")) stunSprite = s;
                }
            }

            if (idleList.Count > 0) idleSprites = idleList.ToArray();
            if (walkList.Count > 0) walkSprites = walkList.ToArray();

            if (idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
            {
                _sr.sprite = idleSprites[0];
            }
#endif
        }

        #endregion
    }
}
