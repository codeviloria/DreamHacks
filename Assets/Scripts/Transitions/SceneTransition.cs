using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamHacks.Transitions
{
    /// <summary>
    /// Trigger de cambio de escena. Cuando el jugador entra en el collider:
    /// 1) marca TransitionState.NextSpawnId con el id destino,
    /// 2) hace fade-out 0.3s,
    /// 3) carga la escena destino,
    /// 4) hace fade-in 0.3s (lo dispara el fader en la nueva escena).
    /// El Collider2D debe ser isTrigger. Si onlyPlayer está activo, solo el
    /// GameObject con tag "Player" dispara la transición.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransition : MonoBehaviour
    {
        [Tooltip("Nombre de la escena destino, tal cual aparece en Build Settings.")]
        [SerializeField] private string targetScene;

        [Tooltip("Id del SpawnPoint donde el jugador aparecerá en la escena destino.")]
        [SerializeField] private string targetSpawnId;

        [Tooltip("Solo acepta disparos del GameObject con tag Player.")]
        [SerializeField] private bool onlyPlayer = true;

        [Tooltip("Duración del fade a negro (y de vuelta) en segundos.")]
        [SerializeField] private float fadeDuration = 0.3f;

        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (onlyPlayer && !other.CompareTag("Player")) return;
            if (string.IsNullOrEmpty(targetScene)) return;

            _triggered = true;
            StartCoroutine(DoTransition());
        }

        private IEnumerator DoTransition()
        {
            TransitionState.NextSpawnId = targetSpawnId;
            yield return ScreenFader.Instance.FadeTo(1f, fadeDuration);
            // El fade-in post-load lo dispara ScreenFader.OnSceneLoaded, porque
            // esta corrutina muere junto con el GameObject al cargar la nueva
            // escena en modo Single.
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }
}
