using UnityEngine;

namespace DreamHacks.World
{
    /// <summary>
    /// Interruptor o placa de presión que avanza el SawPuzzleRoom correspondiente.
    /// Al entrar en contacto o pulsar interactuar, notifica al puzzle.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SawPuzzleSwitch : MonoBehaviour
    {
        [SerializeField] private SawPuzzleRoom targetPuzzle;
        [SerializeField] private bool triggerOnStep = true;
        [SerializeField] private SpriteRenderer visualIndicator;
        [SerializeField] private Color activatedColor = Color.cyan;

        private bool _activated;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated || !triggerOnStep) return;
            if (!other.CompareTag("Player")) return;

            Activate();
        }

        public void Activate()
        {
            if (_activated) return;
            _activated = true;

            if (visualIndicator != null)
            {
                visualIndicator.color = activatedColor;
            }

            if (targetPuzzle != null)
            {
                targetPuzzle.AdvancePuzzle();
            }
        }
    }
}
