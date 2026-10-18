using UnityEngine;

namespace DreamHacks.World
{
    // Música de fondo del nivel en loop. Hace "ducking": baja el volumen (sin
    // cortar) mientras hay un diálogo activo y lo restaura al cerrarse. El clip
    // se carga desde Resources para no depender de wiring en la escena.
    public class MusicaNivel : MonoBehaviour
    {
        [SerializeField] private string clipResources = "Audio/musica_hospital";
        [SerializeField, Range(0f, 1f)] private float volumenNormal = 0.5f;
        [SerializeField, Range(0f, 1f)] private float volumenDucking = 0.15f;
        [SerializeField, Min(0.1f)] private float velocidadFundido = 3f;

        private AudioSource _src;

        private void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.loop = true;
            _src.playOnAwake = false;
            _src.volume = volumenNormal;
            var clip = Resources.Load<AudioClip>(clipResources);
            if (clip != null) { _src.clip = clip; _src.Play(); }
            else Debug.LogWarning("[MusicaNivel] No encontré el clip en Resources/" + clipResources);
        }

        private void Update()
        {
            if (_src == null) return;
            // DialogoUI está en el namespace global.
            bool enDialogo = global::DialogoUI.Activo;
            float objetivo = enDialogo ? volumenDucking : volumenNormal;
            _src.volume = Mathf.MoveTowards(_src.volume, objetivo, velocidadFundido * Time.unscaledDeltaTime);
        }
    }
}
