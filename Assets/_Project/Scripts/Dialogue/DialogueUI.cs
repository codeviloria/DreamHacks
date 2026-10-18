using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DreamHacks.World;

namespace DreamHacks.Dialogue
{
    /// <summary>
    /// Caja de diálogo estilo manga con dos modos:
    ///   - Lineal (DialogueAsset): avanza con E, cierra al final.
    ///   - Bifurcado (DialogueNode): soporta elección entre varias opciones
    ///     que llevan a nodos distintos del lore.
    /// Esc (UI/Cancel) cierra siempre el diálogo, incluso en una elección.
    /// Singleton perezoso para que los NPCs la invoquen sin referencia directa.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [Tooltip("Retrato del NPC (lado izquierdo). Siempre visible durante el diálogo si está asignado.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("Retrato del protagonista (lado derecho). Siempre visible si está asignado.")]
        [SerializeField] private Image playerPortraitImage;
        [Tooltip("Sprite por defecto para el retrato del protagonista cuando el nodo no trae portrait propio.")]
        [SerializeField] private Sprite defaultPlayerPortrait;
        [Tooltip("Sprite por defecto para el retrato del NPC cuando el nodo no trae portrait propio.")]
        [SerializeField] private Sprite defaultNpcPortrait;
        [Tooltip("Nombres de speaker que se consideran protagonista. Si alguna línea los nombra, se fuerza defaultPlayerPortrait (ignorando el portrait del asset, por ejemplo si quedó el placeholder verde).")]
        [SerializeField] private string[] playerSpeakerNames = new[] { "Iker", "Protagonista", "Player", "Tú", "Tu" };
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;

        [Header("Elecciones")]
        [Tooltip("Contenedor donde se instancian los botones de elección. Vertical Layout recomendado.")]
        [SerializeField] private RectTransform choicesContainer;
        [Tooltip("Prefab de botón de elección. Button + Text hijo.")]
        [SerializeField] private Button choiceButtonPrefab;

        // Velocidad del typewriter en caracteres por segundo.
        [SerializeField] private float charsPerSecond = 32f;

        // Modo lineal
        private IReadOnlyList<DialogueLine> _lines;
        private int _index;

        // Modo bifurcado
        private DialogueNode _currentNode;

        // Estado compartido
        private Coroutine _typing;
        private bool _lineFinished;
        private bool _awaitingChoice;
        private readonly List<Button> _spawnedChoices = new List<Button>();

        private InputAction _advanceAction;
        private InputAction _cancelAction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panel != null) panel.SetActive(false);

            var actions = InputSystem.actions;
            if (actions != null)
            {
                _advanceAction = actions.FindAction("Player/Interact");
                _cancelAction = actions.FindAction("UI/Cancel");
            }
        }

        public bool IsActive => panel != null && panel.activeSelf;

        // --- API ---

        public void Show(DialogueAsset dialogue)
        {
            if (dialogue == null || dialogue.Lines.Count == 0) return;
            OpenPanel();
            _lines = dialogue.Lines;
            _index = 0;
            _currentNode = null;
            // En modo lineal mostramos solo el retrato que trae cada línea.
            if (playerPortraitImage != null) playerPortraitImage.enabled = false;
            StartLineLinear();
        }

        public void Show(DialogueNode startNode)
        {
            if (startNode == null) return;
            OpenPanel();
            _lines = null;
            EnterNode(startNode);
        }

        // --- Flujo común ---

        private void OpenPanel()
        {
            if (panel != null) panel.SetActive(true);
            GameSignals.InputBlocked = true;
            if (_advanceAction != null)
            {
                _advanceAction.started -= OnAdvance;
                _advanceAction.started += OnAdvance;
                _advanceAction.Enable();
            }
            if (_cancelAction != null)
            {
                _cancelAction.started -= OnCancel;
                _cancelAction.started += OnCancel;
                _cancelAction.Enable();
            }
        }

        private void OnAdvance(InputAction.CallbackContext ctx)
        {
            if (!IsActive) return;
            if (_awaitingChoice) return; // en elección avanza Submit sobre el botón

            if (!_lineFinished)
            {
                if (_typing != null) StopCoroutine(_typing);
                if (bodyText != null) bodyText.text = CurrentText();
                _lineFinished = true;
                MaybeShowChoices();
                return;
            }

            AdvanceToNext();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (IsActive) Close();
        }

        private string CurrentText()
        {
            if (_currentNode != null) return _currentNode.Text;
            if (_lines != null && _index < _lines.Count) return _lines[_index].Text;
            return string.Empty;
        }

        private void AdvanceToNext()
        {
            if (_currentNode != null)
            {
                var next = _currentNode.Next;
                if (next == null) Close();
                else EnterNode(next);
            }
            else
            {
                _index++;
                if (_lines == null || _index >= _lines.Count) Close();
                else StartLineLinear();
            }
        }

        // --- Modo lineal ---

        private void StartLineLinear()
        {
            var line = _lines[_index];
            SetSpeaker(line.SpeakerName, line.Portrait);
            StartTyping(line.Text);
        }

        // --- Modo bifurcado ---

        private void EnterNode(DialogueNode node)
        {
            _currentNode = node;
            UpdateTwoSidedPortraits(node);
            if (speakerText != null) speakerText.text = node.SpeakerName;
            StartTyping(node.Text);
        }

        // Muestra los dos retratos (NPC izquierda, player derecha) y resalta el
        // que está hablando ahora. El inactivo baja opacidad para que el ojo
        // sepa de un vistazo quién tiene el turno.
        private static readonly Color ActiveTint   = new Color(1f, 1f, 1f, 1f);
        private static readonly Color InactiveTint = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        private void UpdateTwoSidedPortraits(DialogueNode node)
        {
            // El lado del protagonista SIEMPRE prefiere defaultPlayerPortrait si
            // está asignado: así los DialogueNodes viejos que guardaban el cuadro
            // verde placeholder en 'portrait' pasan a mostrar la cara nueva sin
            // tocar cada ScriptableObject. Si no hay default, cae al portrait
            // del nodo cuando el nodo es del jugador.
            Sprite playerSprite = defaultPlayerPortrait != null
                ? defaultPlayerPortrait
                : (node.IsPlayer ? node.Portrait : null);

            // Para el NPC mantenemos el portrait del nodo (cada NPC tiene cara
            // distinta) y usamos defaultNpcPortrait como fallback.
            Sprite npcSprite = node.IsPlayer
                ? defaultNpcPortrait
                : (node.Portrait != null ? node.Portrait : defaultNpcPortrait);

            if (portraitImage != null)
            {
                portraitImage.sprite = npcSprite;
                portraitImage.enabled = npcSprite != null;
            }
            if (playerPortraitImage != null)
            {
                playerPortraitImage.sprite = playerSprite;
                playerPortraitImage.enabled = playerSprite != null;
            }
            SetActiveSide(playerActive: node.IsPlayer);
        }

        // Tinta un lado como activo y el otro como pasivo. No toca los sprites
        // asignados, sólo los colores: así se puede llamar también cuando el
        // jugador está eligiendo y no hay un DialogueNode "actual" propio.
        private void SetActiveSide(bool playerActive)
        {
            if (portraitImage != null)
                portraitImage.color = playerActive ? InactiveTint : ActiveTint;
            if (playerPortraitImage != null)
                playerPortraitImage.color = playerActive ? ActiveTint : InactiveTint;
        }

        private void MaybeShowChoices()
        {
            if (_currentNode == null || !_currentNode.HasChoices) return;
            SpawnChoices(_currentNode.Choices);
        }

        private void SpawnChoices(IReadOnlyList<DialogueChoice> choices)
        {
            ClearChoices();
            if (choicesContainer == null || choiceButtonPrefab == null) return;

            _awaitingChoice = true;
            // Mientras hay opciones ocultamos el body: la pregunta ya se leyó y
            // así los botones no se solapan con el texto.
            if (bodyText != null) bodyText.enabled = false;

            // Las opciones las "dice" el protagonista, así que iluminamos su
            // retrato y bajamos el del NPC para que se note de un vistazo
            // que ahora es el turno del jugador.
            SetActiveSide(playerActive: true);
            if (speakerText != null) speakerText.text = "Tú";
            foreach (var choice in choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                btn.gameObject.SetActive(true);
                var label = btn.GetComponentInChildren<Text>();
                if (label != null) label.text = choice.Text;
                var captured = choice; // cerradura
                btn.onClick.AddListener(() => OnChoicePicked(captured));
                _spawnedChoices.Add(btn);
            }

            // Focus en la primera opción para navegación con teclado/gamepad.
            if (_spawnedChoices.Count > 0 && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_spawnedChoices[0].gameObject);
            }
        }

        private void OnChoicePicked(DialogueChoice choice)
        {
            if (!_awaitingChoice) return;
            _awaitingChoice = false;
            ClearChoices();
            if (choice.Next == null) Close();
            else EnterNode(choice.Next);
        }

        private void ClearChoices()
        {
            foreach (var b in _spawnedChoices)
            {
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    Destroy(b.gameObject);
                }
            }
            _spawnedChoices.Clear();
            if (bodyText != null) bodyText.enabled = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        // --- Helpers ---

        private void SetSpeaker(string name, Sprite portrait)
        {
            if (speakerText != null) speakerText.text = name;
            if (portraitImage == null) return;

            // Si quien habla es el protagonista y tenemos default, lo preferimos
            // SIEMPRE sobre el portrait del asset: así los diálogos viejos que
            // guardaban el cuadrado verde placeholder pasan a mostrar la cara
            // nueva sin tocar cada DialogueAsset. Para los NPCs mantenemos el
            // comportamiento anterior (asset-portrait > defaultNpcPortrait).
            Sprite effective;
            if (IsPlayerSpeaker(name) && defaultPlayerPortrait != null)
            {
                effective = defaultPlayerPortrait;
            }
            else
            {
                effective = portrait != null ? portrait : defaultNpcPortrait;
            }

            portraitImage.sprite = effective;
            portraitImage.enabled = effective != null;
        }

        private bool IsPlayerSpeaker(string name)
        {
            if (string.IsNullOrEmpty(name) || playerSpeakerNames == null) return false;
            for (int i = 0; i < playerSpeakerNames.Length; i++)
            {
                if (string.Equals(name, playerSpeakerNames[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void StartTyping(string text)
        {
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(text));
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
            MaybeShowChoices();
        }

        private void Close()
        {
            ClearChoices();
            _awaitingChoice = false;
            if (panel != null) panel.SetActive(false);
            if (_advanceAction != null) _advanceAction.started -= OnAdvance;
            if (_cancelAction != null) _cancelAction.started -= OnCancel;
            GameSignals.InputBlocked = false;
            _lines = null;
            _currentNode = null;
            _lineFinished = false;
        }

        private void OnDestroy()
        {
            if (_advanceAction != null) _advanceAction.started -= OnAdvance;
            if (_cancelAction != null) _cancelAction.started -= OnCancel;
            if (Instance == this) Instance = null;
        }
    }
}
