using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DreamHacks.World;

namespace DreamHacks.Dialogue
{
    /// <summary>
    /// Caja de diálogo estilo manga: panel inferior con retrato a la izquierda,
    /// nombre del hablante y texto con efecto máquina de escribir.
    /// Singleton perezoso para que los NPCs la invoquen sin referencia directa.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;

        // Velocidad del typewriter en caracteres por segundo.
        [SerializeField] private float charsPerSecond = 32f;

        private IReadOnlyList<DialogueLine> _lines;
        private int _index;
        private Coroutine _typing;
        private bool _lineFinished;
        private InputAction _advanceAction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panel != null) panel.SetActive(false);

            // Usamos la acción Player/Interact del asset del proyecto para
            // avanzar / cerrar el diálogo con la misma tecla que lo abre.
            var actions = InputSystem.actions;
            if (actions != null) _advanceAction = actions.FindAction("Player/Interact");
        }

        public bool IsActive => panel != null && panel.activeSelf;

        public void Show(DialogueAsset dialogue)
        {
            if (dialogue == null || dialogue.Lines.Count == 0) return;
            _lines = dialogue.Lines;
            _index = 0;
            if (panel != null) panel.SetActive(true);
            GameSignals.InputBlocked = true;
            if (_advanceAction != null)
            {
                _advanceAction.started -= OnAdvance;
                _advanceAction.started += OnAdvance;
                _advanceAction.Enable();
            }
            StartLine();
        }

        private void OnAdvance(InputAction.CallbackContext ctx)
        {
            if (!IsActive) return;
            if (!_lineFinished)
            {
                // Primer press: completar línea instantáneamente.
                if (_typing != null) StopCoroutine(_typing);
                if (bodyText != null && _lines != null)
                    bodyText.text = _lines[_index].Text;
                _lineFinished = true;
                return;
            }

            _index++;
            if (_index >= _lines.Count) Close();
            else StartLine();
        }

        private void StartLine()
        {
            var line = _lines[_index];
            if (speakerText != null) speakerText.text = line.SpeakerName;
            if (portraitImage != null)
            {
                portraitImage.sprite = line.Portrait;
                portraitImage.enabled = line.Portrait != null;
            }
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(line.Text));
        }

        private IEnumerator TypeRoutine(string text)
        {
            _lineFinished = false;
            if (bodyText != null) bodyText.text = string.Empty;
            float t = 0f;
            int shown = 0;
            while (shown < text.Length)
            {
                t += Time.unscaledDeltaTime;
                int target = Mathf.Clamp(Mathf.FloorToInt(t * charsPerSecond), 0, text.Length);
                if (target > shown)
                {
                    shown = target;
                    if (bodyText != null) bodyText.text = text.Substring(0, shown);
                }
                yield return null;
            }
            _lineFinished = true;
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (_advanceAction != null) _advanceAction.started -= OnAdvance;
            GameSignals.InputBlocked = false;
            _lines = null;
            _lineFinished = false;
        }

        private void OnDestroy()
        {
            if (_advanceAction != null) _advanceAction.started -= OnAdvance;
            if (Instance == this) Instance = null;
        }
    }
}
