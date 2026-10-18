using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DreamHacks.World;
using DreamHacks.Battle;
using DreamHacks.PlayerNS;

namespace DreamHacks.UI
{
    /// <summary>
    /// Controlador cinemático y de gameplay de IntroCutscene:
    /// Parte 1: Frase del prisma + Cuarto + Sala/Cocina + 3 Tareas + Puerta Glitch 0101.
    /// Parte 2: Tras el fade-off de la transición 0101 aparece el Bosque Colorido:
    ///   - Iker spawnea en el nicho superior derecho señalado.
    ///   - Iker confundido: "AGHHHH, ¿eh? ¿qué?" / "¿Dónde estoy? ¿Qué es este lugar?"
    ///   - Avanza un poco y obligatoriamente inicia la cinemática frente al Slime:
    ///     * Iker: "¿Qué es esa cosa?" / "Parece algún tipo de gelatina, parece inofensivo"
    ///     * El Slime intenta atacar a Iker pero Iker lo esquiva.
    ///     * Iker: "OH DIABLOS, está intentando MATARME"
    ///     * Narrador: "No te preocupes guerrero, toma esta espada y ábrete paso a través de todos los enemigos en este bosque encantado"
    ///     * La espada aparece flotando arriba de Iker.
    ///     * Iker: "¿De dónde viene esa voz?" / "Bueno, solo voy a tomar la espada y eliminar a esta cosa de gelatina"
    ///     * Iker absorbe la espada -> Mensaje del juego: "Recibiste una espada"
    ///     * En la esquina superior derecha aparecen 5 corazones mediante animación (vida de Iker = 5).
    /// </summary>
    public class IntroCutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public class DialogueEntry
        {
            public string speaker;
            [TextArea(2, 4)] public string text;
            public Sprite portrait;
            public bool isSystemMessage;
        }

        private enum PendingInteraction
        {
            None = 0,
            Bed = 1,
            Cereal = 2,
            Plant = 3,
            ExitDoor = 4
        }

        [Header("Navegación")]
        [SerializeField] private string nextSceneName = "Sandbox_Gino";
        [SerializeField] private bool transitionToForestAfterDoor = true;

        [Header("Tiempos de la Secuencia Inicial")]
        [SerializeField] private float initialBlackWait = 2.0f;
        [SerializeField] private float phraseFadeInDuration = 1.2f;
        [SerializeField] private float phraseDisplayDuration = 3.6f;
        [SerializeField] private float phraseFadeOutDuration = 1.2f;
        [SerializeField] private float preVibrationDelay = 0.5f;
        [SerializeField] private float roomRevealDuration = 1.15f;
        [SerializeField] private float dialogueWaitAfterRoom = 1.5f;

        [Header("Elementos Visuales - Frase y Habitaciones")]
        [SerializeField] private CanvasGroup phraseCanvasGroup;
        [SerializeField] private Text phraseText;
        [SerializeField] private RectTransform roomContainer;
        [SerializeField] private Image roomImage;

        [Tooltip("Raíz del Cuarto (Dormitorio) en el mundo 2D.")]
        [SerializeField] private GameObject bedroomRoot;
        [SerializeField] private SpriteRenderer roomSpriteRenderer;
        [SerializeField] private Sprite roomSleepingSprite;
        [SerializeField] private Sprite roomMessyBedSprite;
        [SerializeField] private Sprite roomMadeBedSprite;

        [Tooltip("Raíz de la Sala / Cocina en el mundo 2D.")]
        [SerializeField] private GameObject livingRoomRoot;
        [SerializeField] private SpriteRenderer livingRoomSpriteRenderer;
        [SerializeField] private Sprite livingRoomNormalSprite;
        [SerializeField] private Sprite livingRoomGlitchDoorSprite;

        [Header("Elementos Visuales - Bosque Colorido y Slime")]
        [Tooltip("Raíz del Bosque Colorido en el mundo 2D.")]
        [SerializeField] private GameObject forestRoot;
        [SerializeField] private SpriteRenderer forestSpriteRenderer;
        [SerializeField] private Sprite forestMapSprite;
        [SerializeField] private GameObject slimeObject;
        [SerializeField] private SpriteRenderer slimeSpriteRenderer;
        [SerializeField] private Sprite[] slimeIdleSprites;
        [SerializeField] private Sprite[] slimeAttackSprites;
        [SerializeField] private GameObject floatingSwordObject;
        [SerializeField] private Sprite swordSprite;

        [Header("Coordenadas en Apartamento y Bosque")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Vector3 playerSpawnPos = new Vector3(-0.8f, 0.4f, 0f);
        [SerializeField] private Vector3 bedroomDoorSpawnPos = new Vector3(-2.15f, -1.8f, 0f);
        [SerializeField] private Vector3 livingRoomDoorSpawnPos = new Vector3(3.75f, -1.75f, 0f);
        [Tooltip("Posición exacta en el nicho superior derecho del Bosque donde aparece Iker.")]
        [SerializeField] private Vector3 forestPlayerSpawnPos = new Vector3(6.12f, 2.88f, 0f);
        [Tooltip("Escala proporcional de Iker dentro del Bosque Colorido para ajustarse a los caminos y tiles.")]
        [SerializeField] private Vector3 forestPlayerScale = new Vector3(0.5f, 0.5f, 1f);
        [Tooltip("Posición del Slime frente al nicho de aparición de Iker.")]
        [SerializeField] private Vector3 forestSlimeSpawnPos = new Vector3(6.12f, 1.15f, 0f);

        [Header("Cortina 8-Bit y Transición Binaria 0101")]
        [SerializeField] private GameObject shutterContainer;
        [SerializeField] private RectTransform topShutterBar;
        [SerializeField] private RectTransform bottomShutterBar;
        [SerializeField] private Sprite binaryTransitionSprite;
        [SerializeField] private Image binaryOverlayImage;
        [SerializeField] private int retroSteps = 14;

        [Header("Audio - General y Tareas")]
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private AudioClip phoneVibrationClip;
        [SerializeField] private AudioSource apartmentMusicSource;
        [SerializeField] private AudioClip apartmentMusicClip;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.85f;

        [SerializeField] private AudioClip makeBedSfxClip;
        [SerializeField] private AudioClip makeBreakfastSfxClip;
        [SerializeField] private AudioClip waterPlantSfxClip;
        [SerializeField] private AudioClip glitchDoorMusicClip;
        [SerializeField] private float maxTaskAudioDuration = 3.0f;
        [SerializeField] private float taskAudioFadeOffDuration = 0.85f;

        [Header("Caja de Diálogo")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject continuePrompt;
        [SerializeField] private float charsPerSecond = 32f;

        [Header("UI Lista de Tareas (Esquina Superior Derecha)")]
        [SerializeField] private GameObject tasksPanel;
        [SerializeField] private Text tasksHeaderText;
        [SerializeField] private GameObject[] taskRowObjects;
        [SerializeField] private Text[] taskItemTexts;
        [SerializeField] private GameObject[] taskStrikeBars;

        [Header("UI Corazones de Vida (Esquina Superior Derecha - 5 Corazones)")]
        [SerializeField] private GameObject heartsPanel;
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;

        [Header("UI Mensaje de Sistema (Ej: Recibiste una espada)")]
        [SerializeField] private GameObject systemMessagePanel;
        [SerializeField] private Text systemMessageText;

        [Header("UI Caja de Decisión (SÍ / NO)")]
        [SerializeField] private GameObject decisionPanel;
        [SerializeField] private Text decisionQuestionText;
        [SerializeField] private Button decisionYesButton;
        [SerializeField] private Button decisionNoButton;
        [SerializeField] private Text decisionYesText;
        [SerializeField] private Text decisionNoText;

        [Header("Indicador Contextual de Interacción")]
        [SerializeField] private GameObject interactionHintPanel;
        [SerializeField] private Text interactionHintText;

        [Header("Portraits")]
        [SerializeField] private Sprite phonePortraitSprite;
        [SerializeField] private Sprite ikerPortraitSprite;
        [SerializeField] private Sprite hannahPortraitSprite;

        [Header("Hannah & Acompañante")]
        [SerializeField] private GameObject hannahObject;
        [SerializeField] private HannahFollower hannahFollower;

        [Header("Puzzles del Bosque")]
        [SerializeField] private PushableRock pushableRock;
        [SerializeField] private ForestSwitch forestSwitch;

        [Header("Hacker & Salidas del Bosque")]
        [SerializeField] private Sprite hackerPortraitSprite;
        [SerializeField] private AudioClip glitchDrumsMusicClip;
        [SerializeField] private Collider2D hackerTrigger;
        [SerializeField] private Collider2D forestExitTopTrigger;
        [SerializeField] private Collider2D forestExitBottomTrigger;

        [Header("Contenido Inicial")]
        [TextArea(2, 4)]
        [SerializeField] private string introPhrase = "Los traumas de la persona se ven reflejados en el mundo, como si fuera un prisma que le da color";

        [Header("Líneas de Diálogo Inicial")]
        [SerializeField] private DialogueEntry[] dialogueLines;

        // Estado interno de diálogos
        private DialogueEntry[] _activeDialogueQueue;
        private int _currentDialogueIndex;
        private bool _dialogueReadyToAdvance;
        private bool _isTyping;
        private Coroutine _typewriterCoroutine;
        private System.Action _onDialogueQueueFinished;

        // Estado del apartamento y bosque
        private bool _isInLivingRoom;
        private bool _isInForest;
        private bool _isGameplayActive;
        private bool _isBusyWithAction;
        private bool _isTransitioningRooms;

        // Estado en el bosque
        private bool _slimeCutsceneTriggered;
        private bool _hasSword;
        private bool _slimeDefeated;
        private bool _isSlimeAttacking;
        private float _slimeAnimTimer;
        private int _slimeIdleFrameIndex;
        private bool _isSwordFloatingAboveIker;
        private bool _hannahMet;
        private bool _forestRockMoved;
        private bool _forestSwitchActivated;
        private bool _hackerEventTriggered;
        private bool _forestExitTriggered;

        // Estado de tareas
        private readonly bool[] _tasksCompleted = new bool[3];
        private bool _allThreeTasksFinished;
        private bool _exitDoorUnlocked;

        // Estado de caja de decisión (SÍ / NO)
        private bool _isDecisionOpen;
        private int _decisionSelectedOption;
        private PendingInteraction _currentPendingInteraction = PendingInteraction.None;
        private PendingInteraction _dismissedInteraction = PendingInteraction.None;
        private PendingInteraction _nearbyInteraction = PendingInteraction.None;

        // Efectos de vibración / temblor
        private Vector2 _roomOriginalPos;
        private Vector3 _bedroomWorldOriginalPos;
        private Vector3 _livingRoomWorldOriginalPos;
        private Vector3 _cameraOriginalPos = new Vector3(0f, 0f, -10f);
        private bool _isVibrating;
        private float _shakeMagnitude;

        private Camera _mainCamera;

        private void Awake()
        {
            Time.timeScale = 1f;
            GameSignals.InputBlocked = true;
            _isGameplayActive = false;
            _isBusyWithAction = false;
            _isInLivingRoom = false;
            _isInForest = false;
            _slimeCutsceneTriggered = false;
            _hasSword = false;
            _slimeDefeated = false;
            _isDecisionOpen = false;
            _allThreeTasksFinished = false;
            _exitDoorUnlocked = false;
            _shakeMagnitude = 0f;

            _mainCamera = Camera.main;
            if (_mainCamera != null)
                _cameraOriginalPos = _mainCamera.transform.position;

            if (phraseText != null)
            {
                phraseText.verticalOverflow = VerticalWrapMode.Overflow;
                phraseText.horizontalOverflow = HorizontalWrapMode.Wrap;
                phraseText.text = introPhrase;
            }

            if (bodyText != null)
            {
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            if (phraseCanvasGroup != null)
                phraseCanvasGroup.alpha = 0f;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (continuePrompt != null)
                continuePrompt.SetActive(false);

            if (tasksPanel != null)
                tasksPanel.SetActive(false);

            if (heartsPanel != null)
                heartsPanel.SetActive(false);

            if (systemMessagePanel != null)
                systemMessagePanel.SetActive(false);

            if (decisionPanel != null)
                decisionPanel.SetActive(false);

            if (interactionHintPanel != null)
                interactionHintPanel.SetActive(false);

            if (binaryOverlayImage != null)
            {
                binaryOverlayImage.gameObject.SetActive(false);
                var c = binaryOverlayImage.color;
                c.a = 0f;
                binaryOverlayImage.color = c;
            }

            if (playerObject != null)
                playerObject.SetActive(false);

            if (bedroomRoot != null)
                bedroomRoot.SetActive(true);

            if (livingRoomRoot != null)
                livingRoomRoot.SetActive(false);

            if (forestRoot != null)
                forestRoot.SetActive(false);

            if (floatingSwordObject != null)
                floatingSwordObject.SetActive(false);

            if (roomContainer != null)
                _roomOriginalPos = roomContainer.anchoredPosition;

            if (roomSpriteRenderer != null)
                _bedroomWorldOriginalPos = roomSpriteRenderer.transform.position;

            if (livingRoomSpriteRenderer != null)
                _livingRoomWorldOriginalPos = livingRoomSpriteRenderer.transform.position;

            if (roomSleepingSprite != null)
            {
                if (roomImage != null) roomImage.sprite = roomSleepingSprite;
                if (roomSpriteRenderer != null) roomSpriteRenderer.sprite = roomSleepingSprite;
            }

            if (livingRoomNormalSprite != null && livingRoomSpriteRenderer != null)
            {
                livingRoomSpriteRenderer.sprite = livingRoomNormalSprite;
            }

            if (decisionYesButton != null)
            {
                decisionYesButton.onClick.RemoveAllListeners();
                decisionYesButton.onClick.AddListener(() => OnDecisionChosen(true));
            }
            if (decisionNoButton != null)
            {
                decisionNoButton.onClick.RemoveAllListeners();
                decisionNoButton.onClick.AddListener(() => OnDecisionChosen(false));
            }

            EnsureShutterBars();
            SetShutterClosure(1f);

            if (tasksPanel != null)
                tasksPanel.transform.SetAsFirstSibling();

            if (heartsPanel != null)
                heartsPanel.transform.SetAsFirstSibling();

            if (interactionHintPanel != null)
                interactionHintPanel.transform.SetAsFirstSibling();

            if (shutterContainer != null)
                shutterContainer.transform.SetSiblingIndex(3);

            if (phraseCanvasGroup != null)
                phraseCanvasGroup.transform.SetAsLastSibling();

            if (dialoguePanel != null)
                dialoguePanel.transform.SetAsLastSibling();

            if (systemMessagePanel != null)
                systemMessagePanel.transform.SetAsLastSibling();

            if (decisionPanel != null)
                decisionPanel.transform.SetAsLastSibling();

            InitializeDialogueLines();
            InitializeTasksUI();
        }

        private void InitializeDialogueLines()
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                dialogueLines = new DialogueEntry[]
                {
                    new DialogueEntry
                    {
                        speaker = "Mamá",
                        portrait = phonePortraitSprite,
                        text = "Iker, ¿cómo estás, hijo? Ha pasado mucho tiempo desde que escribiste o llamaste. Estoy muy preocupada, te extraño."
                    },
                    new DialogueEntry
                    {
                        speaker = "Iker",
                        portrait = ikerPortraitSprite,
                        text = "*Bostezo*"
                    },
                    new DialogueEntry
                    {
                        speaker = "Iker",
                        portrait = ikerPortraitSprite,
                        text = "Aghh, otra vez lo mismo. No puedo lidiar con esto ahora mismo, mejor empiezo el día."
                    }
                };
            }
        }

        private void InitializeTasksUI()
        {
            string[] initialLabels = new string[]
            {
                "1. Hacer la cama",
                "2. Preparar el desayuno",
                "3. Regar la planta."
            };

            if (taskItemTexts != null)
            {
                for (int i = 0; i < taskItemTexts.Length && i < initialLabels.Length; i++)
                {
                    if (taskItemTexts[i] != null)
                    {
                        taskItemTexts[i].text = initialLabels[i];
                        taskItemTexts[i].color = new Color(0.92f, 0.95f, 1f, 1f);
                    }
                }
            }

            if (taskRowObjects != null)
            {
                for (int i = 0; i < taskRowObjects.Length; i++)
                {
                    if (taskRowObjects[i] != null)
                    {
                        taskRowObjects[i].SetActive(true);
                        var cg = taskRowObjects[i].GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                    }
                }
            }

            if (taskStrikeBars != null)
            {
                for (int i = 0; i < taskStrikeBars.Length; i++)
                {
                    if (taskStrikeBars[i] != null)
                        taskStrikeBars[i].SetActive(false);
                }
            }
        }

        private void Start()
        {
            if (CheckReturningFromBattle())
                return;

            StartCoroutine(CutsceneSequenceRoutine());
        }

        private bool CheckReturningFromBattle()
        {
            if (BattleState.InBattle ||
                (!string.IsNullOrEmpty(BattleState.ReturnScene) && BattleState.ReturnScene.Contains("IntroCutscene")) ||
                !string.IsNullOrEmpty(BattleState.PendingDestroyEnemyName))
            {
                ResumeForestAfterBattle();
                return true;
            }
            return false;
        }

        private void ResumeForestAfterBattle()
        {
            StopAllCoroutines();

            _isInLivingRoom = false;
            _isInForest = true;
            _slimeCutsceneTriggered = true;
            _hasSword = true;
            _isDecisionOpen = false;
            _isBusyWithAction = false;
            _isGameplayActive = true;
            _isTransitioningRooms = false;
            GameSignals.InputBlocked = false;

            if (bedroomRoot != null) bedroomRoot.SetActive(false);
            if (livingRoomRoot != null) livingRoomRoot.SetActive(false);
            if (forestRoot != null) forestRoot.SetActive(true);

            if (phraseCanvasGroup != null) phraseCanvasGroup.alpha = 0f;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (systemMessagePanel != null) systemMessagePanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (interactionHintPanel != null) interactionHintPanel.SetActive(false);
            if (binaryOverlayImage != null) binaryOverlayImage.gameObject.SetActive(false);
            if (shutterContainer != null) shutterContainer.SetActive(false);

            bool justDefeatedInitialSlime = false;
            string enemyToDestroy = BattleState.PendingDestroyEnemyName;

            if (enemyToDestroy == "Slime_Bosque" ||
                (slimeObject != null && !string.IsNullOrEmpty(enemyToDestroy) && enemyToDestroy == slimeObject.name))
            {
                _slimeDefeated = true;
                if (slimeObject != null) slimeObject.SetActive(false);
                BattleState.PendingDestroyEnemyName = null;
                justDefeatedInitialSlime = true;
            }
            else if (!string.IsNullOrEmpty(enemyToDestroy))
            {
                if (forestRoot != null)
                {
                    var allChildren = forestRoot.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < allChildren.Length; i++)
                    {
                        if (allChildren[i].gameObject.name == enemyToDestroy)
                        {
                            allChildren[i].gameObject.SetActive(false);
                            break;
                        }
                    }
                }
                BattleState.PendingDestroyEnemyName = null;
            }

            Vector3 dest = BattleState.VolverAInicio ? forestPlayerSpawnPos : BattleState.ReturnPosition;
            if (dest == Vector3.zero) dest = forestPlayerSpawnPos;
            BattleState.VolverAInicio = false;
            BattleState.ReturnPosition = Vector3.zero;
            BattleState.ReturnScene = null;

            if (playerObject != null)
            {
                playerObject.transform.position = dest;
                playerObject.transform.localScale = forestPlayerScale;
                var cols = playerObject.GetComponents<Collider2D>();
                for (int i = 0; i < cols.Length; i++)
                    cols[i].enabled = true;

                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = dest;
                    rb.linearVelocity = Vector2.zero;
                }

                var pc = playerObject.GetComponent<PlayerController>();
                if (pc != null) pc.MoveSpeed = 2.6f;

                playerObject.SetActive(true);
            }

            if (floatingSwordObject != null)
                floatingSwordObject.SetActive(false);

            if (_mainCamera != null)
            {
                _mainCamera.orthographicSize = 2.6f;
                _mainCamera.backgroundColor = Color.black;
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                float clampedX = Mathf.Clamp(dest.x, -5.5f, 5.5f);
                float clampedY = Mathf.Clamp(dest.y, -2.45f, 2.45f);
                _cameraOriginalPos = new Vector3(clampedX, clampedY, -10f);
                _mainCamera.transform.position = _cameraOriginalPos;
            }

            if (heartsPanel != null)
            {
                heartsPanel.SetActive(true);
                heartsPanel.transform.SetAsLastSibling();
                if (heartImages != null)
                {
                    for (int i = 0; i < heartImages.Length; i++)
                    {
                        if (heartImages[i] != null)
                        {
                            heartImages[i].gameObject.SetActive(true);
                            heartImages[i].transform.localScale = Vector3.one;
                            if (fullHeartSprite != null) heartImages[i].sprite = fullHeartSprite;
                        }
                    }
                }
            }

            HookForestPuzzles();

            if (justDefeatedInitialSlime && !_hannahMet)
            {
                StartCoroutine(HannahEncounterCutsceneRoutine(dest));
            }
            else if (_hannahMet)
            {
                if (hannahObject != null)
                {
                    hannahObject.transform.position = dest + new Vector3(-0.65f, 0f, 0f);
                    hannahObject.SetActive(true);
                    if (hannahFollower != null && playerObject != null)
                    {
                        hannahFollower.Target = playerObject.transform;
                        hannahFollower.enabled = true;
                    }
                }
                ShowForestTasks();
            }
        }

        private IEnumerator HannahEncounterCutsceneRoutine(Vector3 playerPos)
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;

            yield return new WaitForSeconds(0.35f);

            if (hannahObject != null)
            {
                hannahObject.transform.position = playerPos + new Vector3(0.85f, -0.1f, 0f);
                hannahObject.SetActive(true);
            }

            var encounterLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Hannah",
                    portrait = hannahPortraitSprite,
                    text = "¡Oye! ¿Tú también caíste aquí?"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "¿Eres humana? Estaba en mi pasillo y de repente peleo con monstruos."
                },
                new DialogueEntry
                {
                    speaker = "Hannah",
                    portrait = hannahPortraitSprite,
                    text = "Yo preparaba el desayuno y... ¡pum, píxeles! Soy Hannah."
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Soy Iker."
                },
                new DialogueEntry
                {
                    speaker = "Hannah",
                    portrait = hannahPortraitSprite,
                    text = "¿Iker? Me suena ese nombre... Una voz dijo que avancemos. Juntos saldremos más rápido."
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Tiene sentido. Mejor que morir solo. Vamos."
                }
            };

            bool dialogueDone = false;
            StartDialogueQueue(encounterLines, () => { dialogueDone = true; });
            while (!dialogueDone)
                yield return null;

            _hannahMet = true;

            if (hannahFollower != null && playerObject != null)
            {
                hannahFollower.Target = playerObject.transform;
                hannahFollower.enabled = true;
            }

            ShowForestTasks();

            _isBusyWithAction = false;
            GameSignals.InputBlocked = false;
        }

        private void ShowForestTasks()
        {
            if (tasksPanel == null) return;

            tasksPanel.SetActive(true);
            tasksPanel.transform.SetAsLastSibling();

            if (tasksHeaderText != null)
                tasksHeaderText.text = "Tareas del Bosque:";

            if (taskItemTexts != null)
            {
                if (taskItemTexts.Length > 0 && taskItemTexts[0] != null)
                {
                    taskItemTexts[0].text = "1. Mueve la roca";
                    taskItemTexts[0].color = _forestRockMoved ? new Color(0.55f, 0.85f, 0.62f, 0.9f) : new Color(0.92f, 0.95f, 1f, 1f);
                }
                if (taskItemTexts.Length > 1 && taskItemTexts[1] != null)
                {
                    taskItemTexts[1].text = "2. Presiona el interruptor y busca la salida";
                    taskItemTexts[1].color = _forestSwitchActivated ? new Color(0.55f, 0.85f, 0.62f, 0.9f) : new Color(0.92f, 0.95f, 1f, 1f);
                }
            }

            if (taskRowObjects != null)
            {
                if (taskRowObjects.Length > 0 && taskRowObjects[0] != null)
                {
                    taskRowObjects[0].SetActive(true);
                    var cg = taskRowObjects[0].GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = _forestRockMoved ? 0.4f : 1f;
                }
                if (taskRowObjects.Length > 1 && taskRowObjects[1] != null)
                {
                    taskRowObjects[1].SetActive(true);
                    var cg = taskRowObjects[1].GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = _forestSwitchActivated ? 0.4f : 1f;
                }
                if (taskRowObjects.Length > 2 && taskRowObjects[2] != null)
                {
                    taskRowObjects[2].SetActive(false);
                }
            }

            if (taskStrikeBars != null)
            {
                if (taskStrikeBars.Length > 0 && taskStrikeBars[0] != null)
                    taskStrikeBars[0].SetActive(_forestRockMoved);
                if (taskStrikeBars.Length > 1 && taskStrikeBars[1] != null)
                    taskStrikeBars[1].SetActive(_forestSwitchActivated);
                if (taskStrikeBars.Length > 2 && taskStrikeBars[2] != null)
                    taskStrikeBars[2].SetActive(false);
            }
        }

        private void HookForestPuzzles()
        {
            if (pushableRock != null)
            {
                pushableRock.OnRockPushed -= HandleRockPushed;
                pushableRock.OnRockPushed += HandleRockPushed;
            }
            if (forestSwitch != null)
            {
                forestSwitch.OnSwitchActivated -= HandleSwitchActivated;
                forestSwitch.OnSwitchActivated += HandleSwitchActivated;
            }
        }

        private void HandleRockPushed()
        {
            _forestRockMoved = true;
            StartCoroutine(CrossOutAndHideTaskRoutine(0));
        }

        private void HandleSwitchActivated()
        {
            _forestSwitchActivated = true;
            StartCoroutine(CrossOutAndHideTaskRoutine(1));
        }

        private void Update()
        {
            UpdateShakeEffect();
            UpdateForestVisuals();

            var kb = Keyboard.current;

            // 1. Si la caja de decisión (SÍ / NO) está abierta
            if (_isDecisionOpen)
            {
                HandleDecisionInput(kb);
                return;
            }

            // 2. Si el panel de diálogo está activo
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                bool advancePressed = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                                   || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                                   || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame));

                if (advancePressed)
                {
                    AdvanceDialogue();
                }
                return;
            }

            // 3. Gameplay activo
            if (_isGameplayActive && !_isBusyWithAction && !_isTransitioningRooms && playerObject != null)
            {
                if (!_isInForest)
                {
                    CheckRoomDoorways();
                    CheckObjectInteractions(kb);
                }
                else
                {
                    UpdateForestGameplay(kb);
                }
            }
        }

        private void LateUpdate()
        {
            // En el Bosque, la cámara sigue suavemente a Iker con zoom aumentado
            if (_isInForest && _mainCamera != null && playerObject != null)
            {
                Vector3 targetPos = playerObject.transform.position;
                float clampedX = Mathf.Clamp(targetPos.x, -5.5f, 5.5f);
                float clampedY = Mathf.Clamp(targetPos.y, -2.45f, 2.45f);
                Vector3 desiredCam = new Vector3(clampedX, clampedY, -10f);
                _cameraOriginalPos = Vector3.Lerp(_cameraOriginalPos, desiredCam, Time.deltaTime * 7f);
                if (_shakeMagnitude <= 0.01f)
                    _mainCamera.transform.position = _cameraOriginalPos;
            }
        }

        private void UpdateForestVisuals()
        {
            if (!_isInForest) return;

            // Animación idle del Slime cuando no está en pleno salto de ataque
            if (slimeObject != null && slimeObject.activeSelf && !_isSlimeAttacking && slimeSpriteRenderer != null && slimeIdleSprites != null && slimeIdleSprites.Length > 0)
            {
                _slimeAnimTimer += Time.deltaTime;
                if (_slimeAnimTimer >= 0.18f)
                {
                    _slimeAnimTimer = 0f;
                    _slimeIdleFrameIndex = (_slimeIdleFrameIndex + 1) % slimeIdleSprites.Length;
                    if (slimeIdleSprites[_slimeIdleFrameIndex] != null)
                        slimeSpriteRenderer.sprite = slimeIdleSprites[_slimeIdleFrameIndex];
                }
            }

            // Si la espada está flotando arriba de Iker antes de ser absorbida, mantenerla levitando sobre él
            if (_isSwordFloatingAboveIker && floatingSwordObject != null && floatingSwordObject.activeSelf && playerObject != null)
            {
                float bob = Mathf.Sin(Time.time * 5.5f) * 0.08f;
                floatingSwordObject.transform.position = playerObject.transform.position + new Vector3(0f, 0.72f + bob, 0f);
            }
        }

        private void UpdateShakeEffect()
        {
            float activeShake = _isVibrating ? 2.5f : _shakeMagnitude;
            if (activeShake > 0.01f)
            {
                float offsetX = (Mathf.PerlinNoise(Time.time * 38f, 0f) - 0.5f) * 2f * activeShake;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 38f) - 0.5f) * 2f * activeShake;

                if (roomContainer != null)
                    roomContainer.anchoredPosition = _roomOriginalPos + new Vector2(Mathf.Round(offsetX), Mathf.Round(offsetY));

                if (roomSpriteRenderer != null && bedroomRoot != null && bedroomRoot.activeSelf)
                    roomSpriteRenderer.transform.position = _bedroomWorldOriginalPos + new Vector3(offsetX * 0.02f, offsetY * 0.02f, 0f);

                if (livingRoomSpriteRenderer != null && livingRoomRoot != null && livingRoomRoot.activeSelf)
                    livingRoomSpriteRenderer.transform.position = _livingRoomWorldOriginalPos + new Vector3(offsetX * 0.025f, offsetY * 0.025f, 0f);

                if (_mainCamera != null && _shakeMagnitude > 0.01f)
                    _mainCamera.transform.position = _cameraOriginalPos + new Vector3(offsetX * 0.025f, offsetY * 0.025f, 0f);
            }
            else
            {
                if (roomContainer != null)
                    roomContainer.anchoredPosition = _roomOriginalPos;

                if (roomSpriteRenderer != null)
                    roomSpriteRenderer.transform.position = _bedroomWorldOriginalPos;

                if (livingRoomSpriteRenderer != null)
                    livingRoomSpriteRenderer.transform.position = _livingRoomWorldOriginalPos;

                if (_mainCamera != null)
                    _mainCamera.transform.position = _cameraOriginalPos;
            }
        }

        // =====================================================================
        // SECUENCIA INICIAL DE INTRO
        // =====================================================================

        private IEnumerator CutsceneSequenceRoutine()
        {
            yield return new WaitForSeconds(initialBlackWait);

            float elapsed = 0f;
            while (elapsed < phraseFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (phraseCanvasGroup != null)
                    phraseCanvasGroup.alpha = Mathf.Clamp01(elapsed / phraseFadeInDuration);
                yield return null;
            }
            if (phraseCanvasGroup != null)
                phraseCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(phraseDisplayDuration);

            elapsed = 0f;
            while (elapsed < phraseFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (phraseCanvasGroup != null)
                    phraseCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / phraseFadeOutDuration);
                yield return null;
            }
            if (phraseCanvasGroup != null)
                phraseCanvasGroup.alpha = 0f;

            yield return new WaitForSeconds(preVibrationDelay);

            if (sfxAudioSource != null && phoneVibrationClip != null)
            {
                sfxAudioSource.clip = phoneVibrationClip;
                sfxAudioSource.volume = 1f;
                sfxAudioSource.loop = true;
                sfxAudioSource.Play();
            }
            _isVibrating = true;

            yield return new WaitForSeconds(0.35f);

            yield return StartCoroutine(AnimateShutterRoutine(open: true, duration: roomRevealDuration));

            yield return new WaitForSeconds(dialogueWaitAfterRoom);

            _isVibrating = false;
            if (sfxAudioSource != null && sfxAudioSource.isPlaying)
                sfxAudioSource.Stop();

            StartDialogueQueue(dialogueLines, () =>
            {
                StartCoroutine(TransitionToPlayableRoomRoutine());
            });
        }

        private IEnumerator TransitionToPlayableRoomRoutine()
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            yield return StartCoroutine(AnimateShutterRoutine(open: false, duration: 0.8f));

            yield return new WaitForSeconds(0.25f);

            if (roomMessyBedSprite != null)
            {
                if (roomImage != null) roomImage.sprite = roomMessyBedSprite;
                if (roomSpriteRenderer != null) roomSpriteRenderer.sprite = roomMessyBedSprite;
            }

            if (playerObject != null)
            {
                playerObject.transform.position = playerSpawnPos;
                playerObject.transform.localScale = Vector3.one;
                playerObject.SetActive(true);
            }

            if (tasksPanel != null)
                tasksPanel.SetActive(true);

            if (apartmentMusicSource != null && apartmentMusicClip != null)
            {
                apartmentMusicSource.clip = apartmentMusicClip;
                apartmentMusicSource.loop = true;
                apartmentMusicSource.volume = musicVolume;
                apartmentMusicSource.Play();
            }

            yield return StartCoroutine(AnimateShutterRoutine(open: true, duration: 0.8f));

            _dismissedInteraction = PendingInteraction.Bed;
            _isGameplayActive = true;
            _isBusyWithAction = false;
            GameSignals.InputBlocked = false;
        }

        // =====================================================================
        // NAVEGACIÓN ENTRE HABITACIONES DEL APARTAMENTO
        // =====================================================================

        private void CheckRoomDoorways()
        {
            Vector3 p = playerObject.transform.position;

            if (!_isInLivingRoom)
            {
                if (p.x < -2.85f && p.y > -3.2f && p.y < -0.8f)
                {
                    StartCoroutine(SwitchRoomRoutine(toLivingRoom: true));
                }
            }
            else
            {
                if (p.x > 4.35f && p.y > -2.75f && p.y < -0.85f)
                {
                    StartCoroutine(SwitchRoomRoutine(toLivingRoom: false));
                }
            }
        }

        private IEnumerator SwitchRoomRoutine(bool toLivingRoom)
        {
            _isTransitioningRooms = true;
            GameSignals.InputBlocked = true;
            HideInteractionHint();

            yield return StartCoroutine(AnimateShutterRoutine(open: false, duration: 0.35f));

            _isInLivingRoom = toLivingRoom;
            _dismissedInteraction = PendingInteraction.None;
            _nearbyInteraction = PendingInteraction.None;

            if (bedroomRoot != null)
                bedroomRoot.SetActive(!toLivingRoom);

            if (livingRoomRoot != null)
                livingRoomRoot.SetActive(toLivingRoom);

            if (_mainCamera != null)
            {
                _mainCamera.orthographicSize = toLivingRoom ? 4.4f : 5.15f;
            }

            if (playerObject != null)
            {
                playerObject.transform.position = toLivingRoom ? livingRoomDoorSpawnPos : bedroomDoorSpawnPos;
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(0.1f);

            yield return StartCoroutine(AnimateShutterRoutine(open: true, duration: 0.35f));

            _isTransitioningRooms = false;
            GameSignals.InputBlocked = false;
        }

        // =====================================================================
        // INTERACCIÓN CON OBJETOS DEL APARTAMENTO Y CAJA DE DECISIÓN (SÍ / NO)
        // =====================================================================

        private void CheckObjectInteractions(Keyboard kb)
        {
            Vector3 p = playerObject.transform.position;
            PendingInteraction detected = PendingInteraction.None;
            string hintLabel = string.Empty;

            if (!_isInLivingRoom)
            {
                if (!_tasksCompleted[0])
                {
                    if (p.x >= -2.85f && p.x <= 0.15f && p.y >= 0.25f && p.y <= 3.9f)
                    {
                        detected = PendingInteraction.Bed;
                        hintLabel = "[E] Hacer la cama";
                    }
                }
            }
            else
            {
                if (!_tasksCompleted[1] && p.x >= -3.55f && p.x <= -1.95f && p.y >= 1.15f && p.y <= 2.45f)
                {
                    detected = PendingInteraction.Cereal;
                    hintLabel = "[E] Preparar el desayuno";
                }
                else if (!_tasksCompleted[2] && p.x >= 0.25f && p.x <= 2.05f && p.y >= 1.75f && p.y <= 3.15f)
                {
                    detected = PendingInteraction.Plant;
                    hintLabel = "[E] Regar la planta";
                }
                else if (_exitDoorUnlocked && p.x >= -2.55f && p.x <= -0.85f && p.y >= -3.95f && p.y <= -2.55f)
                {
                    detected = PendingInteraction.ExitDoor;
                    hintLabel = "[E] Salir a la lavandería";
                }
            }

            if (detected == PendingInteraction.None)
            {
                _nearbyInteraction = PendingInteraction.None;
                _dismissedInteraction = PendingInteraction.None;
                HideInteractionHint();
                return;
            }

            _nearbyInteraction = detected;
            ShowInteractionHint(hintLabel);

            bool interactKeyPressed = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                                   || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (interactKeyPressed || _dismissedInteraction != detected)
            {
                OpenDecisionBox(detected);
            }
        }

        private void ShowInteractionHint(string text)
        {
            if (interactionHintPanel != null && !interactionHintPanel.activeSelf)
                interactionHintPanel.SetActive(true);

            if (interactionHintText != null)
                interactionHintText.text = text;
        }

        private void HideInteractionHint()
        {
            if (interactionHintPanel != null && interactionHintPanel.activeSelf)
                interactionHintPanel.SetActive(false);
        }

        private void OpenDecisionBox(PendingInteraction interaction)
        {
            _currentPendingInteraction = interaction;
            _isDecisionOpen = true;
            _decisionSelectedOption = 0;
            GameSignals.InputBlocked = true;

            if (playerObject != null)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            HideInteractionHint();

            string question = "";
            switch (interaction)
            {
                case PendingInteraction.Bed:
                    question = "¿Ordenar la cama?";
                    break;
                case PendingInteraction.Cereal:
                    question = "¿Preparar el desayuno?";
                    break;
                case PendingInteraction.Plant:
                    question = "¿Regar la planta?";
                    break;
                case PendingInteraction.ExitDoor:
                    question = "SALIR";
                    break;
            }

            if (decisionQuestionText != null)
                decisionQuestionText.text = question;

            if (decisionPanel != null)
            {
                decisionPanel.SetActive(true);
                decisionPanel.transform.SetAsLastSibling();
            }

            UpdateDecisionHighlight();
        }

        private void HandleDecisionInput(Keyboard kb)
        {
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                {
                    _decisionSelectedOption = 0;
                    UpdateDecisionHighlight();
                }
                else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                {
                    _decisionSelectedOption = 1;
                    UpdateDecisionHighlight();
                }
                else if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    OnDecisionChosen(_decisionSelectedOption == 0);
                }
                else if (kb.escapeKey.wasPressedThisFrame)
                {
                    OnDecisionChosen(false);
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame)
                {
                    _decisionSelectedOption = 0;
                    UpdateDecisionHighlight();
                }
                else if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame)
                {
                    _decisionSelectedOption = 1;
                    UpdateDecisionHighlight();
                }
                else if (gp.buttonSouth.wasPressedThisFrame)
                {
                    OnDecisionChosen(_decisionSelectedOption == 0);
                }
                else if (gp.buttonEast.wasPressedThisFrame)
                {
                    OnDecisionChosen(false);
                }
            }
        }

        private void UpdateDecisionHighlight()
        {
            Color activeColor = new Color(1f, 0.92f, 0.38f, 1f);
            Color normalColor = new Color(0.75f, 0.80f, 0.88f, 1f);

            if (decisionYesText != null)
            {
                decisionYesText.text = _decisionSelectedOption == 0 ? "> SÍ <" : "SÍ";
                decisionYesText.color = _decisionSelectedOption == 0 ? activeColor : normalColor;
            }

            if (decisionNoText != null)
            {
                decisionNoText.text = _decisionSelectedOption == 1 ? "> NO <" : "NO";
                decisionNoText.color = _decisionSelectedOption == 1 ? activeColor : normalColor;
            }
        }

        public void OnDecisionChosen(bool choseYes)
        {
            if (!_isDecisionOpen) return;

            _isDecisionOpen = false;
            if (decisionPanel != null)
                decisionPanel.SetActive(false);

            PendingInteraction chosenInteraction = _currentPendingInteraction;
            _currentPendingInteraction = PendingInteraction.None;

            if (!choseYes)
            {
                _dismissedInteraction = chosenInteraction;
                GameSignals.InputBlocked = false;
                return;
            }

            switch (chosenInteraction)
            {
                case PendingInteraction.Bed:
                    StartCoroutine(ExecuteTaskRoutine(
                        taskIndex: 0,
                        taskSfx: makeBedSfxClip,
                        completionDialogue: "Cama, Lista.",
                        onMidTransition: () =>
                        {
                            if (roomMadeBedSprite != null)
                            {
                                if (roomImage != null) roomImage.sprite = roomMadeBedSprite;
                                if (roomSpriteRenderer != null) roomSpriteRenderer.sprite = roomMadeBedSprite;
                            }
                        },
                        onAfterShutterOpen: null
                    ));
                    break;

                case PendingInteraction.Cereal:
                    StartCoroutine(ExecuteTaskRoutine(
                        taskIndex: 1,
                        taskSfx: makeBreakfastSfxClip,
                        completionDialogue: "Desayuno listo.",
                        onMidTransition: null,
                        onAfterShutterOpen: null
                    ));
                    break;

                case PendingInteraction.Plant:
                    StartCoroutine(ExecuteTaskRoutine(
                        taskIndex: 2,
                        taskSfx: waterPlantSfxClip,
                        completionDialogue: "Planta lista.",
                        onMidTransition: null,
                        onAfterShutterOpen: () =>
                        {
                            StartCoroutine(PlayPlantBinaryAndHpEffectRoutine());
                        }
                    ));
                    break;

                case PendingInteraction.ExitDoor:
                    StartCoroutine(GlitchDoorAbsorptionRoutine());
                    break;
            }
        }

        // =====================================================================
        // EJECUCIÓN DE TAREAS
        // =====================================================================

        private IEnumerator ExecuteTaskRoutine(
            int taskIndex,
            AudioClip taskSfx,
            string completionDialogue,
            System.Action onMidTransition,
            System.Action onAfterShutterOpen)
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;

            yield return StartCoroutine(AnimateShutterRoutine(open: false, duration: 1.05f));

            onMidTransition?.Invoke();

            yield return StartCoroutine(PlayTaskAudioWithFadeOffRoutine(taskSfx, maxTaskAudioDuration, taskAudioFadeOffDuration));

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(AnimateShutterRoutine(open: true, duration: 1.05f));

            onAfterShutterOpen?.Invoke();

            var singleLine = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = completionDialogue
                }
            };

            bool dialogueDone = false;
            StartDialogueQueue(singleLine, () => { dialogueDone = true; });
            while (!dialogueDone)
                yield return null;

            _tasksCompleted[taskIndex] = true;
            yield return StartCoroutine(CrossOutAndHideTaskRoutine(taskIndex));

            if (_tasksCompleted[0] && _tasksCompleted[1] && _tasksCompleted[2] && !_allThreeTasksFinished)
            {
                _allThreeTasksFinished = true;
                yield return StartCoroutine(AllTasksCompletedSequenceRoutine());
            }
            else
            {
                _isBusyWithAction = false;
                GameSignals.InputBlocked = false;
            }
        }

        private IEnumerator PlayTaskAudioWithFadeOffRoutine(AudioClip clip, float maxDuration, float fadeOffDuration)
        {
            if (sfxAudioSource == null || clip == null)
            {
                yield return new WaitForSeconds(1.8f);
                yield break;
            }

            float originalMusicVol = apartmentMusicSource != null ? apartmentMusicSource.volume : musicVolume;
            if (apartmentMusicSource != null)
                apartmentMusicSource.volume = originalMusicVol * 0.35f;

            float totalPlayDuration = Mathf.Min(clip.length, Mathf.Max(1.2f, maxDuration));
            float actualFadeDuration = Mathf.Min(fadeOffDuration, totalPlayDuration * 0.45f);
            float fullVolumeDuration = Mathf.Max(0.1f, totalPlayDuration - actualFadeDuration);

            sfxAudioSource.Stop();
            sfxAudioSource.clip = clip;
            sfxAudioSource.loop = false;
            sfxAudioSource.volume = 1f;
            sfxAudioSource.Play();

            yield return new WaitForSeconds(fullVolumeDuration);

            float t = 0f;
            while (t < actualFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                sfxAudioSource.volume = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / actualFadeDuration));
                yield return null;
            }

            sfxAudioSource.Stop();
            sfxAudioSource.volume = 1f;

            if (apartmentMusicSource != null)
                apartmentMusicSource.volume = originalMusicVol;
        }

        private IEnumerator CrossOutAndHideTaskRoutine(int taskIndex)
        {
            if (taskIndex < 0) yield break;

            if (taskStrikeBars != null && taskIndex < taskStrikeBars.Length && taskStrikeBars[taskIndex] != null)
                taskStrikeBars[taskIndex].SetActive(true);

            if (taskItemTexts != null && taskIndex < taskItemTexts.Length && taskItemTexts[taskIndex] != null)
                taskItemTexts[taskIndex].color = new Color(0.55f, 0.85f, 0.62f, 0.9f);

            yield return new WaitForSeconds(0.75f);

            if (taskRowObjects != null && taskIndex < taskRowObjects.Length && taskRowObjects[taskIndex] != null)
            {
                var rowGo = taskRowObjects[taskIndex];
                var cg = rowGo.GetComponent<CanvasGroup>();
                if (cg == null) cg = rowGo.AddComponent<CanvasGroup>();

                float d = 0.38f;
                float elapsed = 0f;
                while (elapsed < d)
                {
                    elapsed += Time.unscaledDeltaTime;
                    cg.alpha = 1f - Mathf.Clamp01(elapsed / d);
                    yield return null;
                }
                cg.alpha = 0f;
                rowGo.SetActive(false);
            }
        }

        private IEnumerator PlayPlantBinaryAndHpEffectRoutine()
        {
            if (livingRoomRoot == null) yield break;

            var fxRoot = new GameObject("FX_PlantaRPG");
            fxRoot.transform.SetParent(livingRoomRoot.transform, false);
            fxRoot.transform.position = new Vector3(1.13f, 3.55f, 0f);

            var worldCanvas = fxRoot.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 15;

            var rt = fxRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 200f);
            rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int dropCount = 5;
            Text[] drops = new Text[dropCount];
            Vector2[] startPositions = new Vector2[]
            {
                new Vector2(-14f, 24f),
                new Vector2(6f, 28f),
                new Vector2(-4f, 18f),
                new Vector2(14f, 22f),
                new Vector2(0f, 30f)
            };

            for (int i = 0; i < dropCount; i++)
            {
                var dropGo = new GameObject($"BitDrop_{i}", typeof(RectTransform));
                dropGo.transform.SetParent(fxRoot.transform, false);
                var dropRt = dropGo.GetComponent<RectTransform>();
                dropRt.sizeDelta = new Vector2(30f, 30f);
                dropRt.anchoredPosition = startPositions[i];

                var txt = dropGo.AddComponent<Text>();
                txt.font = font;
                txt.text = (i % 2 == 0) ? "0" : "1";
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.25f, 1f, 0.45f, 0f);
                drops[i] = txt;
            }

            float dropDuration = 0.85f;
            float elapsed = 0f;
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < dropCount; i++)
                {
                    float delay = i * 0.11f;
                    float localT = Mathf.Clamp01((elapsed - delay) / (dropDuration - delay));
                    if (localT > 0f)
                    {
                        var dropRt = drops[i].GetComponent<RectTransform>();
                        dropRt.anchoredPosition = startPositions[i] + new Vector2(0f, -38f * localT);
                        float alpha = localT < 0.3f ? (localT / 0.3f) : (1f - (localT - 0.3f) / 0.7f);
                        drops[i].color = new Color(0.25f, 1f, 0.45f, alpha * 0.88f);
                    }
                }
                yield return null;
            }

            for (int i = 0; i < dropCount; i++)
            {
                if (drops[i] != null) Destroy(drops[i].gameObject);
            }

            var hpGo = new GameObject("Txt_FloatingHP", typeof(RectTransform));
            hpGo.transform.SetParent(fxRoot.transform, false);
            var hpRt = hpGo.GetComponent<RectTransform>();
            hpRt.sizeDelta = new Vector2(120f, 40f);
            hpRt.anchoredPosition = new Vector2(0f, 8f);

            var hpText = hpGo.AddComponent<Text>();
            hpText.font = font;
            hpText.text = "+1 HP";
            hpText.fontSize = 16;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = new Color(0.32f, 1f, 0.52f, 0.92f);

            var outline = hpGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.12f, 0.05f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            float hpDuration = 1.35f;
            elapsed = 0f;
            while (elapsed < hpDuration)
            {
                elapsed += Time.deltaTime;
                float norm = Mathf.Clamp01(elapsed / hpDuration);
                hpRt.anchoredPosition = new Vector2(0f, 8f + norm * 42f);
                float alpha = norm < 0.2f ? (norm / 0.2f) : (1f - Mathf.Pow(norm, 1.6f));
                hpText.color = new Color(0.32f, 1f, 0.52f, alpha * 0.92f);
                yield return null;
            }

            Destroy(fxRoot);
        }

        private IEnumerator AllTasksCompletedSequenceRoutine()
        {
            yield return new WaitForSeconds(0.25f);

            var postTaskLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Bueno, todo listo, debo prepararme para ir a trabajar,"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Pero primero necesito llevar toda mi ropa a la lavandería."
                }
            };

            bool done = false;
            StartDialogueQueue(postTaskLines, () => { done = true; });
            while (!done)
                yield return null;

            if (taskRowObjects != null && taskRowObjects.Length > 0 && taskRowObjects[0] != null)
            {
                var row0 = taskRowObjects[0];
                row0.SetActive(true);
                var cg = row0.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;

                if (taskStrikeBars != null && taskStrikeBars.Length > 0 && taskStrikeBars[0] != null)
                    taskStrikeBars[0].SetActive(false);

                if (taskItemTexts != null && taskItemTexts.Length > 0 && taskItemTexts[0] != null)
                {
                    taskItemTexts[0].text = "• Busca tu ropa en la lavandería";
                    taskItemTexts[0].color = new Color(1f, 0.94f, 0.55f, 1f);
                }
            }

            _exitDoorUnlocked = true;
            _isBusyWithAction = false;
            GameSignals.InputBlocked = false;
        }

        // =====================================================================
        // SECUENCIA DE ABSORCIÓN EN LA PUERTA -> TRANSICIÓN 0101 -> BOSQUE COLORIDO
        // =====================================================================

        private IEnumerator GlitchDoorAbsorptionRoutine()
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;
            HideInteractionHint();

            if (tasksPanel != null)
                tasksPanel.SetActive(false);

            _shakeMagnitude = 4.2f;

            if (livingRoomGlitchDoorSprite != null && livingRoomSpriteRenderer != null)
            {
                livingRoomSpriteRenderer.sprite = livingRoomGlitchDoorSprite;
            }

            if (apartmentMusicSource != null)
            {
                apartmentMusicSource.Stop();
                if (glitchDoorMusicClip != null)
                {
                    apartmentMusicSource.clip = glitchDoorMusicClip;
                    apartmentMusicSource.loop = true;
                    apartmentMusicSource.volume = 0.95f;
                    apartmentMusicSource.Play();
                }
            }

            yield return new WaitForSeconds(0.45f);

            var shockDialogue = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "¿EH? QUÉ, QUÉ ESTÁ PASANDO"
                }
            };

            bool shockDialogueFinished = false;
            StartDialogueQueue(shockDialogue, () => { shockDialogueFinished = true; });

            float waitTimer = 0f;
            while (!shockDialogueFinished && waitTimer < 3.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            _shakeMagnitude = 7.5f;

            var screamDialogue = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "ESTA COSA ME ESTÁ M*** ABSORBIENDO, AYUDAAA, AYUDA!!!!!, AGHHHHHHHH"
                }
            };

            StartDialogueQueue(screamDialogue, null);

            Vector3 doorPortalPos = new Vector3(-1.72f, -3.95f, 0f);
            Vector3 startPlayerPos = playerObject != null ? playerObject.transform.position : new Vector3(-1.72f, -2.9f, 0f);
            Vector3 startScale = playerObject != null ? playerObject.transform.localScale : Vector3.one;

            if (playerObject != null)
            {
                var cols = playerObject.GetComponents<Collider2D>();
                for (int i = 0; i < cols.Length; i++)
                    cols[i].enabled = false;
            }

            float absorbDuration = 3.4f;
            float elapsed = 0f;
            while (elapsed < absorbDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / absorbDuration);
                float pullCurve = t * t;

                if (playerObject != null)
                {
                    Vector3 jitter = new Vector3(
                        Mathf.Sin(Time.time * 55f) * 0.06f * (1f - t * 0.5f),
                        Mathf.Cos(Time.time * 48f) * 0.04f * (1f - t * 0.5f),
                        0f
                    );
                    playerObject.transform.position = Vector3.Lerp(startPlayerPos, doorPortalPos, pullCurve) + jitter;

                    float scaleFactor = Mathf.Lerp(1f, 0.18f, Mathf.Pow(t, 2.2f));
                    playerObject.transform.localScale = startScale * scaleFactor;
                }

                yield return null;
            }

            if (playerObject != null)
                playerObject.SetActive(false);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // Cerrar transición con patrón 0101
            yield return StartCoroutine(AnimateBinaryShutterCloseRoutine(1.25f));

            _shakeMagnitude = 0f;

            if (apartmentMusicSource != null && apartmentMusicSource.isPlaying)
            {
                float startVol = apartmentMusicSource.volume;
                float fadeT = 0f;
                while (fadeT < 0.85f)
                {
                    fadeT += Time.unscaledDeltaTime;
                    apartmentMusicSource.volume = Mathf.Lerp(startVol, 0f, fadeT / 0.85f);
                    yield return null;
                }
                apartmentMusicSource.Stop();
            }

            if (transitionToForestAfterDoor)
            {
                yield return StartCoroutine(EnterForestMapRoutine());
            }
            else if (!string.IsNullOrEmpty(nextSceneName))
            {
                yield return new WaitForSeconds(0.5f);
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private IEnumerator AnimateBinaryShutterCloseRoutine(float duration)
        {
            EnsureShutterBars();

            if (binaryTransitionSprite != null)
            {
                if (topShutterBar != null)
                {
                    var topImg = topShutterBar.GetComponent<Image>();
                    if (topImg != null)
                    {
                        topImg.sprite = binaryTransitionSprite;
                        topImg.color = Color.white;
                    }
                }
                if (bottomShutterBar != null)
                {
                    var botImg = bottomShutterBar.GetComponent<Image>();
                    if (botImg != null)
                    {
                        botImg.sprite = binaryTransitionSprite;
                        botImg.color = Color.white;
                    }
                }
            }

            if (shutterContainer != null)
                shutterContainer.transform.SetAsLastSibling();

            yield return StartCoroutine(AnimateShutterRoutine(open: false, duration: duration));

            if (binaryOverlayImage != null && binaryTransitionSprite != null)
            {
                binaryOverlayImage.sprite = binaryTransitionSprite;
                binaryOverlayImage.gameObject.SetActive(true);
                binaryOverlayImage.transform.SetAsLastSibling();

                float t = 0f;
                while (t < 0.45f)
                {
                    t += Time.unscaledDeltaTime;
                    var c = binaryOverlayImage.color;
                    c.a = Mathf.Clamp01(t / 0.45f);
                    binaryOverlayImage.color = c;
                    yield return null;
                }
            }
        }

        // =====================================================================
        // NUEVO MAPA: BOSQUE COLORIDO + ENCUENTRO CON SLIME + ESPADA + 5 CORAZONES
        // =====================================================================

        private IEnumerator EnterForestMapRoutine()
        {
            // Activar el mapa del Bosque Colorido detrás de la transición 0101
            if (bedroomRoot != null)
                bedroomRoot.SetActive(false);

            if (livingRoomRoot != null)
                livingRoomRoot.SetActive(false);

            if (forestRoot != null)
                forestRoot.SetActive(true);

            _isInLivingRoom = false;
            _isInForest = true;
            _slimeCutsceneTriggered = false;
            _hasSword = false;
            _slimeDefeated = false;

            // Posicionar a Iker en el nicho superior derecho señalado
            if (playerObject != null)
            {
                playerObject.transform.position = forestPlayerSpawnPos;
                playerObject.transform.localScale = forestPlayerScale;
                var cols = playerObject.GetComponents<Collider2D>();
                for (int i = 0; i < cols.Length; i++)
                    cols[i].enabled = true;

                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;

                var pc = playerObject.GetComponent<PlayerController>();
                if (pc != null) pc.MoveSpeed = 2.6f;

                playerObject.SetActive(true);
            }

            // Posicionar al Slime frente al nicho de salida
            if (slimeObject != null)
            {
                slimeObject.transform.position = forestSlimeSpawnPos;
                slimeObject.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
                slimeObject.SetActive(true);
            }

            if (floatingSwordObject != null)
                floatingSwordObject.SetActive(false);

            if (_mainCamera != null)
            {
                _mainCamera.orthographicSize = 2.6f;
                _mainCamera.backgroundColor = Color.black;
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                float clampedX = Mathf.Clamp(forestPlayerSpawnPos.x, -5.5f, 5.5f);
                float clampedY = Mathf.Clamp(forestPlayerSpawnPos.y, -2.45f, 2.45f);
                _cameraOriginalPos = new Vector3(clampedX, clampedY, -10f);
                _mainCamera.transform.position = _cameraOriginalPos;
            }

            yield return new WaitForSeconds(0.55f);

            // Fade off de la transición 0101 para revelar el Bosque Colorido
            if (binaryOverlayImage != null && binaryOverlayImage.gameObject.activeSelf)
            {
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.unscaledDeltaTime;
                    var c = binaryOverlayImage.color;
                    c.a = 1f - Mathf.Clamp01(t / 0.5f);
                    binaryOverlayImage.color = c;
                    yield return null;
                }
                binaryOverlayImage.gameObject.SetActive(false);
            }

            yield return StartCoroutine(AnimateShutterRoutine(open: true, duration: 1.0f));

            // Restaurar color negro en las barras de cortina para futuros usos
            if (topShutterBar != null)
            {
                var topImg = topShutterBar.GetComponent<Image>();
                if (topImg != null) { topImg.sprite = null; topImg.color = Color.black; }
            }
            if (bottomShutterBar != null)
            {
                var botImg = bottomShutterBar.GetComponent<Image>();
                if (botImg != null) { botImg.sprite = null; botImg.color = Color.black; }
            }

            yield return new WaitForSeconds(0.25f);

            // Diálogos de Iker confundido al aparecer en el bosque
            var arrivalLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "AGHHHH, ¿eh? ¿qué?"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "¿Dónde estoy? ¿Qué es este lugar?"
                }
            };

            bool arrivalDone = false;
            StartDialogueQueue(arrivalLines, () => { arrivalDone = true; });
            while (!arrivalDone)
                yield return null;

            // Permitir que Iker avance un poco hacia abajo desde su punto de spawn
            _isBusyWithAction = false;
            _isGameplayActive = true;
            GameSignals.InputBlocked = false;
        }

        private void UpdateForestGameplay(Keyboard kb)
        {
            Vector3 p = playerObject.transform.position;

            // 1. Antes de cualquier otra cosa, al avanzar un poco desde el nicho de spawn comienza obligatoriamente la cinemática del Slime
            if (!_slimeCutsceneTriggered)
            {
                float distMoved = Vector2.Distance(new Vector2(p.x, p.y), new Vector2(forestPlayerSpawnPos.x, forestPlayerSpawnPos.y));
                if (p.y <= 2.35f || distMoved >= 0.48f)
                {
                    _slimeCutsceneTriggered = true;
                    StartCoroutine(SlimeEncounterCutsceneRoutine());
                    return;
                }
            }

            // 2. Una vez recibida la espada, si el jugador se acerca o colisiona con el Slime -> entra inmediatamente en combate (escena Battle)
            if (_hasSword && !_slimeDefeated && slimeObject != null && slimeObject.activeSelf)
            {
                float distToSlime = Vector2.Distance(p, slimeObject.transform.position);
                if (distToSlime <= 0.85f)
                {
                    StartSlimeBattleEncounter();
                    return;
                }

                if (distToSlime <= 1.8f)
                {
                    ShowInteractionHint("[Espacio / E o Tocar] Luchar contra el Slime");
                    bool attackPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                                      || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                                      || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
                    if (attackPressed)
                    {
                        StartSlimeBattleEncounter();
                        return;
                    }
                }
                else
                {
                    HideInteractionHint();
                }
            }

            // 3. Evento del Hacker al cruzar el paso central ("DIALOGOS AQUI")
            if (!_hackerEventTriggered && _hannahMet)
            {
                bool inZone = (p.x >= -1.6f && p.x <= 1.1f && p.y >= -0.6f && p.y <= 2.4f);
                if (inZone)
                {
                    _hackerEventTriggered = true;
                    StartCoroutine(HackerEncounterRoutine());
                    return;
                }
            }

            // 4. Salidas del bosque en el extremo izquierdo
            if (!_forestExitTriggered)
            {
                bool atTopExit = (p.x <= -8.2f && p.y >= 1.8f);
                bool atBottomExit = (p.x <= -8.2f && p.y <= -1.8f);
                if (atTopExit || atBottomExit)
                {
                    _forestExitTriggered = true;
                    StartCoroutine(ForestExitSequenceRoutine());
                    return;
                }
            }
        }

        private IEnumerator HackerEncounterRoutine()
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;
            HideInteractionHint();

            if (playerObject != null)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            // 1. Sonido de glitch inicial
            if (sfxAudioSource != null)
            {
                if (glitchDoorMusicClip != null)
                    sfxAudioSource.PlayOneShot(glitchDoorMusicClip, 0.95f);
                else if (phoneVibrationClip != null)
                    sfxAudioSource.PlayOneShot(phoneVibrationClip, 1f);
            }

            yield return new WaitForSeconds(0.35f);

            // 2. Activar temblor continuo de pantalla durante toda la conversación
            _shakeMagnitude = 2.4f;

            // 3. Canción de fondo "Glitch Drums" únicamente durante esta conversación
            if (apartmentMusicSource != null && glitchDrumsMusicClip != null)
            {
                apartmentMusicSource.Stop();
                apartmentMusicSource.clip = glitchDrumsMusicClip;
                apartmentMusicSource.loop = true;
                apartmentMusicSource.volume = 0.9f;
                apartmentMusicSource.Play();
            }

            // 4. Diálogos exactos entre (???), Hannah e Iker
            var hackerLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "???",
                    portrait = hackerPortraitSprite,
                    text = "Hola, Iker. Hola, Hannah. Sigan mis comandos para salir del bucle. Confíen en mí."
                },
                new DialogueEntry
                {
                    speaker = "Hannah",
                    portrait = hannahPortraitSprite,
                    text = "¿Qué fue eso? Esa voz me dio escalofríos."
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Sabe nuestros nombres... No me da buena espina, pero avancemos alerta."
                },
                new DialogueEntry
                {
                    speaker = "Hannah",
                    portrait = hannahPortraitSprite,
                    text = "Vale, te sigo."
                }
            };

            bool dialogueDone = false;
            StartDialogueQueue(hackerLines, () => { dialogueDone = true; });
            while (!dialogueDone)
                yield return null;

            // 5. Al acabar los diálogos: cesa el temblor y se detiene la música
            _shakeMagnitude = 0f;

            if (apartmentMusicSource != null && apartmentMusicSource.isPlaying)
            {
                float t = 0f;
                float startVol = apartmentMusicSource.volume;
                while (t < 0.6f)
                {
                    t += Time.deltaTime;
                    apartmentMusicSource.volume = Mathf.Lerp(startVol, 0f, t / 0.6f);
                    yield return null;
                }
                apartmentMusicSource.Stop();
                apartmentMusicSource.volume = musicVolume;
            }

            _isBusyWithAction = false;
            GameSignals.InputBlocked = false;
        }

        private IEnumerator ForestExitSequenceRoutine()
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;
            HideInteractionHint();

            if (playerObject != null)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            // Reproduce la animación retro de cortina binaria cerrándose
            yield return StartCoroutine(AnimateBinaryShutterCloseRoutine(1.25f));
        }

        private IEnumerator SlimeEncounterCutsceneRoutine()
        {
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;
            HideInteractionHint();

            if (playerObject != null)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            // 1. Primeros diálogos de Iker al ver al Slime enfrente
            var inspectSlimeLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "¿Qué es esa cosa?"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Parece algún tipo de gelatina, parece inofensivo"
                }
            };

            bool step1Done = false;
            StartDialogueQueue(inspectSlimeLines, () => { step1Done = true; });
            while (!step1Done)
                yield return null;

            // 2. El Slime hace una animación de intentar atacar a Iker, pero Iker lo esquiva
            yield return StartCoroutine(SlimeAttackAndIkerDodgeRoutine());

            // 3. Iker reacciona al ataque del Slime
            var panicLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "OH DIABLOS, está intentando MATARME"
                }
            };

            bool step3Done = false;
            StartDialogueQueue(panicLines, () => { step3Done = true; });
            while (!step3Done)
                yield return null;

            // 4. Aparece la espada arriba de Iker mientras habla el Narrador
            if (floatingSwordObject != null && playerObject != null)
            {
                if (swordSprite != null)
                {
                    var sr = floatingSwordObject.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = swordSprite;
                }
                floatingSwordObject.transform.position = playerObject.transform.position + new Vector3(0f, 0.72f, 0f);
                floatingSwordObject.transform.localScale = Vector3.zero;
                floatingSwordObject.SetActive(true);
                _isSwordFloatingAboveIker = true;

                // Animación de aparición de la espada sobre la cabeza de Iker
                float appearDur = 0.4f;
                float t = 0f;
                while (t < appearDur)
                {
                    t += Time.deltaTime;
                    float s = Mathf.SmoothStep(0f, 0.85f, t / appearDur);
                    floatingSwordObject.transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
            }

            var narratorAndIkerLines = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speaker = "Narrador",
                    portrait = null,
                    text = "No te preocupes guerrero, toma esta espada y ábrete paso a través de todos los enemigos en este bosque encantado"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "¿De dónde viene esa voz?"
                },
                new DialogueEntry
                {
                    speaker = "Iker",
                    portrait = ikerPortraitSprite,
                    text = "Bueno, solo voy a tomar la espada y eliminar a esta cosa de gelatina"
                }
            };

            bool step4Done = false;
            StartDialogueQueue(narratorAndIkerLines, () => { step4Done = true; });
            while (!step4Done)
                yield return null;

            // 5. Iker absorbe la espada que estaba flotando arriba de él
            yield return StartCoroutine(AbsorbSwordAndShowRewardRoutine());

            // 6. En la esquina superior derecha aparecen 5 corazones mediante animación (vida de Iker = 5)
            yield return StartCoroutine(AnimateFiveHeartsAppearRoutine());

            _hasSword = true;
            _isBusyWithAction = false;
            GameSignals.InputBlocked = false;
        }

        private IEnumerator SlimeAttackAndIkerDodgeRoutine()
        {
            if (slimeObject == null || playerObject == null)
                yield break;

            _isSlimeAttacking = true;
            Vector3 slimeStart = slimeObject.transform.position;
            Vector3 slimeBaseScale = slimeObject.transform.localScale;
            Vector3 ikerStart = playerObject.transform.position;

            // Preparación del Slime (frame de carga)
            if (slimeSpriteRenderer != null && slimeAttackSprites != null && slimeAttackSprites.Length > 0 && slimeAttackSprites[0] != null)
                slimeSpriteRenderer.sprite = slimeAttackSprites[0];

            // Pequeño encogimiento previo al salto
            float windup = 0.28f;
            float t = 0f;
            while (t < windup)
            {
                t += Time.deltaTime;
                slimeObject.transform.localScale = new Vector3(slimeBaseScale.x * 1.15f, slimeBaseScale.y * 0.85f, 1f);
                yield return null;
            }

            // Frame de mordisco/embestida
            if (slimeSpriteRenderer != null && slimeAttackSprites != null && slimeAttackSprites.Length > 1 && slimeAttackSprites[1] != null)
                slimeSpriteRenderer.sprite = slimeAttackSprites[1];

            Vector3 lungeTarget = ikerStart + new Vector3(0f, -0.1f, 0f);
            Vector3 dodgePos = ikerStart + new Vector3(-0.42f, 0.08f, 0f);
            Vector3 lungeScale = new Vector3(slimeBaseScale.x * 0.92f, slimeBaseScale.y * 1.18f, 1f);

            // Embestida rápida del Slime hacia Iker mientras Iker esquiva hacia un lado
            float lungeDuration = 0.26f;
            t = 0f;
            while (t < lungeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / lungeDuration);
                slimeObject.transform.localScale = lungeScale;
                slimeObject.transform.position = Vector3.Lerp(slimeStart, lungeTarget, p);

                // Iker esquiva rápidamente cuando el Slime se acerca
                float dodgeP = Mathf.Clamp01((p - 0.15f) / 0.85f);
                playerObject.transform.position = Vector3.Lerp(ikerStart, dodgePos, Mathf.Sin(dodgeP * Mathf.PI * 0.5f));
                yield return null;
            }

            if (slimeSpriteRenderer != null && slimeAttackSprites != null && slimeAttackSprites.Length > 2 && slimeAttackSprites[2] != null)
                slimeSpriteRenderer.sprite = slimeAttackSprites[2];

            yield return new WaitForSeconds(0.12f);

            // El Slime regresa a su sitio e Iker vuelve a su posición frente a él
            float recoverDuration = 0.32f;
            t = 0f;
            while (t < recoverDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / recoverDuration);
                slimeObject.transform.position = Vector3.Lerp(lungeTarget, slimeStart, p);
                slimeObject.transform.localScale = Vector3.Lerp(lungeScale, slimeBaseScale, p);
                playerObject.transform.position = Vector3.Lerp(dodgePos, ikerStart, p);
                yield return null;
            }

            slimeObject.transform.position = slimeStart;
            slimeObject.transform.localScale = slimeBaseScale;
            playerObject.transform.position = ikerStart;
            _isSlimeAttacking = false;
        }

        private IEnumerator AbsorbSwordAndShowRewardRoutine()
        {
            _isSwordFloatingAboveIker = false;

            if (floatingSwordObject != null && playerObject != null)
            {
                Vector3 swordStart = floatingSwordObject.transform.position;
                Vector3 swordTarget = playerObject.transform.position + new Vector3(0f, 0.1f, 0f);
                Vector3 startScale = floatingSwordObject.transform.localScale;

                float absorbDur = 0.55f;
                float t = 0f;
                while (t < absorbDur)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / absorbDur);
                    float curve = p * p;
                    floatingSwordObject.transform.position = Vector3.Lerp(swordStart, swordTarget, curve);
                    floatingSwordObject.transform.localScale = Vector3.Lerp(startScale, new Vector3(0.15f, 0.15f, 1f), curve);
                    floatingSwordObject.transform.rotation = Quaternion.Euler(0f, 0f, p * 360f);
                    yield return null;
                }

                floatingSwordObject.transform.rotation = Quaternion.identity;
                floatingSwordObject.SetActive(false);

                // Pequeño destello de absorción en Iker
                var playerSr = playerObject.GetComponent<SpriteRenderer>();
                if (playerSr != null)
                {
                    Color orig = playerSr.color;
                    playerSr.color = new Color(1f, 0.96f, 0.55f, 1f);
                    yield return new WaitForSeconds(0.15f);
                    playerSr.color = orig;
                }
            }

            // Mostrar mensaje del juego: "Recibiste una espada"
            if (systemMessagePanel != null)
            {
                if (systemMessageText != null)
                    systemMessageText.text = "¡Recibiste una espada!";

                systemMessagePanel.SetActive(true);
                systemMessagePanel.transform.SetAsLastSibling();
                systemMessagePanel.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                float popDur = 0.25f;
                float t = 0f;
                while (t < popDur)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(0.8f, 1.0f, t / popDur);
                    systemMessagePanel.transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                systemMessagePanel.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator AnimateFiveHeartsAppearRoutine()
        {
            if (heartsPanel == null || heartImages == null || heartImages.Length == 0)
            {
                yield return new WaitForSeconds(1.2f);
                if (systemMessagePanel != null) systemMessagePanel.SetActive(false);
                yield break;
            }

            heartsPanel.SetActive(true);
            heartsPanel.transform.SetAsLastSibling();

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    if (fullHeartSprite != null)
                        heartImages[i].sprite = fullHeartSprite;
                    heartImages[i].transform.localScale = Vector3.zero;
                    heartImages[i].gameObject.SetActive(true);
                }
            }

            // Animar la aparición secuencial de los 5 corazones con rebote elástico
            for (int i = 0; i < heartImages.Length; i++)
            {
                var hImg = heartImages[i];
                if (hImg == null) continue;

                float popTime = 0.24f;
                float t = 0f;
                while (t < popTime)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / popTime);
                    float scale = p < 0.65f
                        ? Mathf.Lerp(0f, 1.35f, p / 0.65f)
                        : Mathf.Lerp(1.35f, 1.0f, (p - 0.65f) / 0.35f);
                    hImg.transform.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
                hImg.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(0.08f);
            }

            // Esperar a que el jugador presione E/Espacio/Clic o pasen 1.6s para cerrar el mensaje "Recibiste una espada"
            float wait = 0f;
            while (wait < 1.8f)
            {
                wait += Time.deltaTime;
                var kb = Keyboard.current;
                bool pressed = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                if (pressed && wait > 0.3f)
                    break;
                yield return null;
            }

            if (systemMessagePanel != null)
                systemMessagePanel.SetActive(false);
        }

        public void StartSlimeBattleEncounter()
        {
            if (BattleState.InBattle || _slimeDefeated) return;

            HideInteractionHint();
            _isBusyWithAction = true;
            GameSignals.InputBlocked = true;

            Vector3 playerPos = playerObject != null ? playerObject.transform.position : forestPlayerSpawnPos;
            BattleState.StartEncounter("Slime", "Slime_Bosque", "IntroCutscene", playerPos);
            SceneManager.LoadScene("Battle");
        }

        private IEnumerator AttackAndDefeatSlimeRoutine()
        {
            StartSlimeBattleEncounter();
            yield break;
        }

        // =====================================================================
        // SISTEMA DE COLA DE DIÁLOGOS CON TYPEWRITER
        // =====================================================================

        private void StartDialogueQueue(DialogueEntry[] entries, System.Action onFinished)
        {
            if (entries == null || entries.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            _activeDialogueQueue = entries;
            _currentDialogueIndex = 0;
            _onDialogueQueueFinished = onFinished;
            GameSignals.InputBlocked = true;

            ShowCurrentDialogue();
        }

        private void ShowCurrentDialogue()
        {
            if (_activeDialogueQueue == null || _currentDialogueIndex >= _activeDialogueQueue.Length) return;

            var current = _activeDialogueQueue[_currentDialogueIndex];

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                dialoguePanel.transform.SetAsLastSibling();
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = current.portrait;
                portraitImage.enabled = current.portrait != null;
            }

            if (speakerText != null)
                speakerText.text = current.speaker;

            if (continuePrompt != null)
                continuePrompt.SetActive(false);

            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(current.text));
        }

        private void AdvanceDialogue()
        {
            if (_activeDialogueQueue == null) return;

            if (_isTyping)
            {
                if (_typewriterCoroutine != null)
                    StopCoroutine(_typewriterCoroutine);

                if (bodyText != null && _currentDialogueIndex < _activeDialogueQueue.Length)
                    bodyText.text = _activeDialogueQueue[_currentDialogueIndex].text;

                _isTyping = false;
                _dialogueReadyToAdvance = true;
                if (continuePrompt != null)
                    continuePrompt.SetActive(true);
            }
            else if (_dialogueReadyToAdvance)
            {
                _dialogueReadyToAdvance = false;
                _currentDialogueIndex++;

                if (_currentDialogueIndex < _activeDialogueQueue.Length)
                {
                    ShowCurrentDialogue();
                }
                else
                {
                    if (dialoguePanel != null)
                        dialoguePanel.SetActive(false);

                    var callback = _onDialogueQueueFinished;
                    _onDialogueQueueFinished = null;
                    _activeDialogueQueue = null;
                    callback?.Invoke();
                }
            }
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            _dialogueReadyToAdvance = false;
            if (bodyText != null)
                bodyText.text = string.Empty;

            float t = 0f;
            int shown = 0;
            while (shown < text.Length)
            {
                t += Time.unscaledDeltaTime;
                int target = Mathf.Clamp(Mathf.FloorToInt(t * charsPerSecond), 0, text.Length);
                if (target > shown)
                {
                    shown = target;
                    if (bodyText != null)
                        bodyText.text = text.Substring(0, shown);
                }
                yield return null;
            }

            _isTyping = false;
            _dialogueReadyToAdvance = true;
            if (continuePrompt != null)
                continuePrompt.SetActive(true);
        }

        // =====================================================================
        // CORTINA 8-BIT (SHUTTER)
        // =====================================================================

        private IEnumerator AnimateShutterRoutine(bool open, float duration)
        {
            EnsureShutterBars();
            if (shutterContainer != null)
                shutterContainer.SetActive(true);

            int steps = Mathf.Max(4, retroSteps);
            float stepTime = duration / steps;

            for (int i = 0; i <= steps; i++)
            {
                float progress = (float)i / steps;
                float closure = open ? (1f - progress) : progress;
                SetShutterClosure(closure);
                if (i < steps)
                    yield return new WaitForSecondsRealtime(stepTime);
            }

            SetShutterClosure(open ? 0f : 1f);
        }

        private void SetShutterClosure(float closure)
        {
            closure = Mathf.Clamp01(closure);
            float halfClosure = closure * 0.5f;

            if (topShutterBar != null)
            {
                topShutterBar.anchorMin = new Vector2(0f, 1f - halfClosure);
                topShutterBar.anchorMax = new Vector2(1f, 1f);
                topShutterBar.offsetMin = Vector2.zero;
                topShutterBar.offsetMax = Vector2.zero;
            }

            if (bottomShutterBar != null)
            {
                bottomShutterBar.anchorMin = new Vector2(0f, 0f);
                bottomShutterBar.anchorMax = new Vector2(1f, halfClosure);
                bottomShutterBar.offsetMin = Vector2.zero;
                bottomShutterBar.offsetMax = Vector2.zero;
            }
        }

        private void EnsureShutterBars()
        {
            if (shutterContainer != null && topShutterBar != null && bottomShutterBar != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var overlayGo = new GameObject("Overlay_Cortina8Bit", typeof(RectTransform));
            overlayGo.transform.SetParent(canvas.transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            shutterContainer = overlayGo;

            var topGo = new GameObject("Cortina_Superior", typeof(RectTransform), typeof(Image));
            topGo.transform.SetParent(overlayGo.transform, false);
            topGo.GetComponent<Image>().color = Color.black;
            topShutterBar = topGo.GetComponent<RectTransform>();

            var botGo = new GameObject("Cortina_Inferior", typeof(RectTransform), typeof(Image));
            botGo.transform.SetParent(overlayGo.transform, false);
            botGo.GetComponent<Image>().color = Color.black;
            bottomShutterBar = botGo.GetComponent<RectTransform>();
        }
    }
}
