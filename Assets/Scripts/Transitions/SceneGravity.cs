using UnityEngine;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Fija Physics2D.gravity al cargar la escena. Necesario porque el proyecto
    /// base es top-down y usa gravedad global (0, 0): los Rigidbody2D no caen
    /// aunque tengan gravityScale > 0. En las escenas de cueva (metroidvania)
    /// hace falta volver a poner la gravedad real; al salir, otra SceneGravity
    /// en la escena top-down la devuelve a cero.
    /// </summary>
    public class SceneGravity : MonoBehaviour
    {
        [Tooltip("Valor que se aplica a Physics2D.gravity al cargar esta escena.")]
        [SerializeField] private Vector2 gravity = new Vector2(0f, -30f);

        private void Awake()
        {
            Physics2D.gravity = gravity;
        }
    }
}
