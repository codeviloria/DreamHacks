using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DreamHacks.Transitions;

namespace DreamHacks.UI
{
    /// <summary>
    /// Cuadrado azul del inicio: al tocarlo el Player se lanza la cinemática
    /// "Orden 3" y, al terminar, hace la transición a la escena destino
    /// (si targetScene está lleno), igual que SceneTransition.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Orden3Trigger : MonoBehaviour
    {
        [Tooltip("Escena a cargar cuando termina la cinemática. Vacío = quedarse en la escena.")]
        [SerializeField] private string targetScene;

        [Tooltip("Id del SpawnPoint en la escena destino.")]
        [SerializeField] private string targetSpawnId;

        [Tooltip("Solo acepta al GameObject con tag Player.")]
        [SerializeField] private bool onlyPlayer = true;

        [Tooltip("Duración del fade a negro antes de cargar la escena.")]
        [SerializeField] private float fadeDuration = 0.3f;

        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (onlyPlayer && !other.CompareTag("Player")) return;
            _triggered = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var cine = Orden3Cinematic.Create();
            if (cine != null)
                yield return new WaitUntil(() => cine == null || cine.Finished);

            if (string.IsNullOrEmpty(targetScene)) { _triggered = false; yield break; }

            TransitionState.NextSpawnId = targetSpawnId;
            if (ScreenFader.Instance != null)
                yield return ScreenFader.Instance.FadeTo(1f, fadeDuration);
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }
}