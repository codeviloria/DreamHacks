using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamHacks.World
{
    /// <summary>
    /// Puzzle estilo Saw Game interactuable:
    /// Interruptores de terminal, placas de presión y puertas electrificadas.
    /// Al resolverse, desactiva la barrera/puerta que bloquea el camino.
    /// </summary>
    public class SawPuzzleRoom : MonoBehaviour
    {
        [Header("Configuración del Puzzle")]
        [Tooltip("Nombre de este puzzle (ej. 'Terminal de Código', 'Placas de Seguridad').")]
        [SerializeField] private string puzzleName = "Saw Puzzle";

        [Tooltip("Puerta o barrera con collider que se abre al resolver el puzzle.")]
        [SerializeField] private GameObject doorBarrier;

        [Tooltip("Número de pasos o interruptores requeridos para resolverlo.")]
        [SerializeField] private int requiredSteps = 2;

        [Header("Feedback Visual")]
        [SerializeField] private SpriteRenderer statusLight;
        [SerializeField] private Color lockedColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color solvedColor = new Color(0.2f, 1f, 0.4f);

        private int _currentSteps;
        private bool _isSolved;

        private void Start()
        {
            UpdateLight();
        }

        /// <summary>
        /// Llamado por un botón/placa/trigger de la sala cuando el jugador interactúa.
        /// </summary>
        public void AdvancePuzzle()
        {
            if (_isSolved) return;

            _currentSteps++;
            Debug.Log($"[SawPuzzle] {puzzleName}: Paso {_currentSteps}/{requiredSteps}");

            if (_currentSteps >= requiredSteps)
            {
                Solve();
            }
        }

        private void Solve()
        {
            _isSolved = true;
            Debug.Log($"[SawPuzzle] ¡{puzzleName} resuelto! Abriendo camino...");

            if (doorBarrier != null)
            {
                // Desactivar la barrera con un breve efecto
                StartCoroutine(OpenBarrierRoutine());
            }

            UpdateLight();
        }

        private IEnumerator OpenBarrierRoutine()
        {
            var sr = doorBarrier.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float t = 0f;
                Color initColor = sr.color;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    sr.color = Color.Lerp(initColor, new Color(initColor.r, initColor.g, initColor.b, 0f), t / 0.5f);
                    yield return null;
                }
            }
            doorBarrier.SetActive(false);
        }

        private void UpdateLight()
        {
            if (statusLight != null)
            {
                statusLight.color = _isSolved ? solvedColor : lockedColor;
            }
        }
    }
}
