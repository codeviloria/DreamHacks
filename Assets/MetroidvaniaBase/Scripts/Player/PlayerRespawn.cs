using UnityEngine;

// Revisa la posición del jugador y, si cae por debajo de una altura límite (por ejemplo,
// se cae por un hueco entre plataformas), lo devuelve al último punto seguro con la
// velocidad en cero. Va en el mismo GameObject "Player" que PlayerController2D, junto a
// su Rigidbody2D.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    // Posición Y a partir de la cual se considera que el jugador "se cayó" del nivel.
    // Subirlo (ej. -5) hace el nivel más exigente con los huecos; bajarlo (ej. -20) da
    // más margen antes de reaparecer.
    [SerializeField, Tooltip("Si la posición Y del jugador baja de este valor, se considera una caída y vuelve al punto de reaparición.")]
    private float killY = -10f;

    private Rigidbody2D rb;

    // Último punto seguro guardado. Empieza siendo la posición inicial del jugador en la
    // escena y se actualiza cada vez que se llama a SetCheckpoint (ej. al tocar una zona
    // de guardado, o al recibir daño de un enemigo).
    private Vector2 checkpointPosition;

    // Awake corre una sola vez, al cargar la escena, antes que Update. Acá se guarda dónde
    // empieza el jugador para poder volver ahí más tarde.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        checkpointPosition = transform.position;
    }

    // FixedUpdate corre a ritmo fijo, igual que la física: es el lugar correcto para
    // revisar la posición del Rigidbody2D y decidir si corresponde hacer respawn.
    private void FixedUpdate()
    {
        if (transform.position.y < killY)
        {
            Respawn();
        }
    }

    // Mueve al jugador al último checkpoint guardado y detiene toda su velocidad (si no,
    // seguiría "arrastrando" la caída apenas reaparece).
    // Usa rb.position (no transform.position): este proyecto tiene
    // Physics2D.autoSyncTransforms en false, así que cambiar transform.position directo
    // en un Rigidbody2D Dynamic no mueve el cuerpo físico (la física lo pisa de nuevo con
    // su posición interna vieja). rb.position sí actualiza ambos.
    private void Respawn()
    {
        rb.position = checkpointPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    // Método público para que otros scripts (una zona de checkpoint, una puerta, un
    // enemigo al hacer daño, etc.) actualicen el punto de reaparición sin tocar
    // directamente las variables privadas de este componente.
    public void SetCheckpoint(Vector2 newCheckpoint)
    {
        checkpointPosition = newCheckpoint;
    }
}
