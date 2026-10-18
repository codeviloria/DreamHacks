using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Efecto puramente visual que se instancia donde impactó un proyectil.
    /// Mantiene el sprite por 'lifetime' segundos y se autodestruye.
    /// </summary>
    public class ImpactEffect : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float lifetime = 0.35f;

        private void Awake()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
