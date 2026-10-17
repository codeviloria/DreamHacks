using UnityEngine;
using UnityEngine.InputSystem;

// Controla el movimiento del personaje jugable: caminar, saltar (con salto variable,
// coyote time y jump buffer) y dashear. Va en el GameObject "Player" (2D), junto a un
// Rigidbody2D y un Collider2D (cápsula). Lee teclado/mando con el New Input System a
// través del asset Assets/Input/PlayerControls.inputactions (acción Move/Jump/Dash).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Input")]
    // Asset con los controles del jugador (Move, Jump, Dash) y sus bindings de teclado
    // y mando. Si queda vacío, el personaje no va a responder a ningún input.
    [SerializeField, Tooltip("Asset de Input Actions con los controles del jugador. Ver Assets/Input/PlayerControls.inputactions.")]
    private InputActionAsset inputActions;

    [Header("Movimiento horizontal")]
    // Qué tan rápido corre el personaje en línea recta. Subirlo lo hace más veloz;
    // bajarlo lo hace más lento (útil, por ejemplo, para una zona de sigilo).
    [SerializeField, Tooltip("Velocidad horizontal máxima, en unidades por segundo.")]
    private float moveSpeed = 8f;

    // Qué tan rápido llega a la velocidad máxima al empezar a moverse. Subirlo da una
    // arrancada más brusca; bajarlo da una arrancada más suave, como "patinando".
    [SerializeField, Tooltip("Aceleración horizontal aplicada mientras hay input de movimiento (unidades/seg^2).")]
    private float acceleration = 60f;

    // Qué tan rápido frena al soltar las teclas de movimiento. Subirlo lo detiene casi en
    // seco; bajarlo hace que resbale un trecho antes de pararse del todo.
    [SerializeField, Tooltip("Desaceleración horizontal aplicada cuando no hay input de movimiento (unidades/seg^2).")]
    private float deceleration = 60f;

    [Header("Salto")]
    // Velocidad vertical que se aplica en el instante de saltar. Subirlo hace el salto más
    // alto; bajarlo lo hace más bajo (con la misma gravedad).
    [SerializeField, Tooltip("Velocidad vertical aplicada al iniciar un salto (unidades/segundo).")]
    private float jumpForce = 14f;

    // Cuánto se recorta la velocidad de subida si sueltas el botón antes de llegar arriba
    // (salto variable). En 1 soltar no hace nada; en 0 el salto se corta apenas sueltas.
    [SerializeField, Range(0f, 1f), Tooltip("Multiplicador aplicado a la velocidad vertical si se suelta el botón de salto mientras el personaje sigue subiendo (salto de altura variable).")]
    private float lowJumpMultiplier = 0.5f;

    // Ventana de gracia para saltar justo después de salir de una plataforma sin apoyo.
    // Subirlo perdona saltos más "tardíos"; en 0 hay que saltar antes de empezar a caer.
    [SerializeField, Tooltip("Tiempo, en segundos, durante el cual todavía se permite saltar después de dejar el suelo (coyote time).")]
    private float coyoteTime = 0.1f;

    // Ventana de gracia para un salto apretado un poco antes de tocar el piso: se "guarda"
    // y se ejecuta apenas aterriza. Subirlo perdona pulsaciones más tempranas.
    [SerializeField, Tooltip("Tiempo, en segundos, durante el cual se recuerda una pulsación de salto realizada justo antes de tocar el suelo (jump buffer).")]
    private float jumpBufferTime = 0.1f;

    [Header("Chequeo de suelo")]
    // Punto (normalmente a la altura de los pies) desde donde se dibuja la caja que detecta
    // el suelo. Si queda vacío, el personaje nunca detecta suelo y no puede saltar.
    [SerializeField, Tooltip("Transform (normalmente en los pies del personaje) desde el que se comprueba el contacto con el suelo.")]
    private Transform groundCheck;

    // Tamaño de la caja de detección de suelo. Muy grande (sobre todo de ancho) puede tocar
    // el costado de una plataforma y creer que hay suelo estando en el aire; muy chica puede
    // no detectar el suelo estando parado encima.
    [SerializeField, Tooltip("Tamaño (ancho, alto) de la caja usada por OverlapBox para detectar el suelo.")]
    private Vector2 groundCheckSize = new Vector2(0.9f, 0.05f);

    // Capas que cuentan como "suelo". Solo lo que esté en la capa Ground activa isGrounded,
    // aunque otra cosa toque la caja.
    [SerializeField, Tooltip("Capas consideradas como suelo por el chequeo de OverlapBox.")]
    private LayerMask groundLayer;

    [Header("Dash")]
    // Si está apagado, el dash no funciona todavía (pensado para desbloquearse más adelante,
    // ej. detrás de una puerta de habilidad del hito 5). Se puede prender/apagar desde otro
    // script con SetDashUnlocked, o marcar/desmarcar acá mismo para probar.
    [SerializeField, Tooltip("Si está desactivado, el dash no funciona (pensado para desbloquearse más adelante en el juego).")]
    private bool canDash = true;

    // Velocidad horizontal fija durante el dash (reemplaza a moveSpeed mientras dura).
    // Subirlo hace el dash más largo/rápido; bajarlo lo hace más corto y sutil.
    [SerializeField, Tooltip("Velocidad horizontal aplicada durante el dash (unidades/segundo).")]
    private float dashSpeed = 20f;

    // Cuánto dura el dash. Subirlo alarga la distancia recorrida; bajarlo lo hace un
    // empujoncito breve.
    [SerializeField, Tooltip("Duración del dash, en segundos.")]
    private float dashDuration = 0.15f;

    // Tiempo mínimo de espera entre un dash y el siguiente. Subirlo lo hace una habilidad
    // más escasa; bajarlo permite encadenar dashes más seguido.
    [SerializeField, Tooltip("Tiempo mínimo de espera entre un dash y el siguiente, en segundos.")]
    private float dashCooldown = 0.4f;

    private Rigidbody2D rb;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

    private float moveInputX;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;

    // Hacia dónde "mira" el personaje según el último input horizontal (1 = derecha,
    // -1 = izquierda). El dash siempre viaja en esta dirección, aunque sueltes las teclas
    // justo antes de dashear.
    private float facingDirection = 1f;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashDirectionLocked;

    // Único cargador de dash en el aire: se gasta al dashear sin estar en el suelo y se
    // recarga solo en cuanto vuelve a pisar suelo (ver CheckGrounded). En el suelo el dash
    // no gasta esta carga, solo respeta el cooldown.
    private bool airDashAvailable = true;
    private float originalGravityScale;

    // Awake corre una sola vez, al cargar el objeto, antes que Start/Update/OnEnable.
    // Acá se cachean referencias y se buscan las acciones (Move/Jump/Dash) dentro del
    // asset de Input Actions asignado en el Inspector.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        playerActionMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerActionMap.FindAction("Move");
        jumpAction = playerActionMap.FindAction("Jump");
        dashAction = playerActionMap.FindAction("Dash");
    }

    // OnEnable se llama cada vez que el objeto se activa: hay que habilitar el mapa de
    // acciones para que el asset empiece a escuchar el teclado/mando.
    private void OnEnable()
    {
        playerActionMap.Enable();
    }

    // OnDisable es lo opuesto a OnEnable: apaga el mapa de acciones para que no siga
    // escuchando el teclado/mando cuando el objeto está desactivado.
    private void OnDisable()
    {
        playerActionMap.Disable();
    }

    // Update corre una vez por frame (variable, según los FPS). Es el lugar correcto para
    // leer input y actualizar temporizadores "de sensación" (buffers, cooldowns), porque
    // necesitan la máxima resolución posible, no el ritmo fijo de la física.
    private void Update()
    {
        // Move es un Vector2 (para soportar D-pad/stick con dos ejes), pero este juego
        // solo se mueve en horizontal, así que usamos nada más la parte X.
        moveInputX = moveAction.ReadValue<Vector2>().x;
        if (Mathf.Abs(moveInputX) > 0.01f)
        {
            facingDirection = Mathf.Sign(moveInputX);
        }

        jumpBufferCounter -= Time.deltaTime;
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * lowJumpMultiplier);
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
        else if (dashAction.WasPressedThisFrame() && CanStartDash())
        {
            StartDash();
        }
    }

    // FixedUpdate corre a ritmo fijo (Time.fixedDeltaTime), igual que el motor de física:
    // todo lo que mueva al Rigidbody2D (chequeo de suelo, movimiento, salto, dash) va acá.
    private void FixedUpdate()
    {
        CheckGrounded();

        if (isDashing)
        {
            // Mientras dashea, la velocidad es fija y no la tocan ni el input normal
            // ni la gravedad (que ya está en 0, ver StartDash).
            rb.linearVelocity = new Vector2(dashDirectionLocked * dashSpeed, 0f);
        }
        else
        {
            ApplyHorizontalMovement();
            TryConsumeJump();
        }
    }

    // Pregunta con una caja invisible (OverlapBox) si hay suelo debajo de groundCheck.
    // También exige que no esté subiendo (linearVelocity.y <= 0.01) para no contar como
    // "suelo" el instante en que despega pegado a una plataforma. De paso recarga el
    // coyote time y el dash aéreo cuando sí hay suelo.
    private void CheckGrounded()
    {
        isGrounded = groundCheck != null &&
            Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) &&
            rb.linearVelocity.y <= 0.01f;

        coyoteTimeCounter = isGrounded ? coyoteTime : coyoteTimeCounter - Time.fixedDeltaTime;

        if (isGrounded)
        {
            airDashAvailable = true;
        }
    }

    // Acelera o frena la velocidad horizontal hacia la que pide el input, usando
    // acceleration o deceleration según si hay input o no. No se llama mientras dashea.
    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInputX * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float accelerationRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float movement = speedDifference * accelerationRate * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    // Si hay un salto "guardado" (jump buffer) y todavía se permite saltar (suelo o coyote
    // time), ejecuta el salto y limpia ambos contadores para no saltar dos veces seguidas.
    private void TryConsumeJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    // Reglas para poder empezar un dash: la habilidad debe estar desbloqueada, no estar en
    // cooldown, y (si está en el aire) todavía tener la carga de dash aéreo disponible.
    private bool CanStartDash()
    {
        return canDash && dashCooldownTimer <= 0f && (isGrounded || airDashAvailable);
    }

    // Arranca el dash: fija la dirección (hacia donde mira el personaje), apaga la
    // gravedad, lanza la velocidad horizontal fija y arma los temporizadores de duración
    // y cooldown. Si se dashea en el aire, gasta la única carga aérea.
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirectionLocked = facingDirection;

        if (!isGrounded)
        {
            airDashAvailable = false;
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirectionLocked * dashSpeed, 0f);
    }

    // Termina el dash y devuelve la gravedad a como estaba antes de empezar.
    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = originalGravityScale;
    }

    // Permite que otro script (por ejemplo, una puerta de habilidad del hito 5) active o
    // desactive el dash en tiempo de ejecución.
    public void SetDashUnlocked(bool unlocked)
    {
        canDash = unlocked;
    }

    // OnDrawGizmos se dibuja siempre en el editor (en Play o no), para ajustar a ojo el
    // tamaño y la posición de groundCheck: verde si en este momento detecta suelo, rojo
    // si está en el aire.
    private void OnDrawGizmos()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
