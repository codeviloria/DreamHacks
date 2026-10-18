using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DreamHacks.Core;
using DreamHacks.Transitions;
using DreamHacks.World;

namespace DreamHacks.UI
{
    /// <summary>
    /// Menú principal con flujo en 2 pasos usando los assets pixel-art del proyecto:
    /// 1) Pantalla de Título: Fondo estrellado (Bg_Menu), cristal centrado detrás del logo PRISM (Logo_Prism),
    ///    y botones JUGAR, CRÉDITOS, ITCH.IO + cristal selector.
    /// 2) Al pulsar JUGAR -> Se abre la pantalla de Selección de Personaje estilo Cuphead (3 cartas: IKER, ???, ???).
    /// 3) Al seleccionar/confirmar personaje -> Carga Sandbox_Gino.unity con fundido a negro.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Destino al confirmar personaje")]
        [Tooltip("Nombre de la escena a cargar tras elegir personaje.")]
        [SerializeField] private string targetScene = "Sandbox_Gino";

        [Tooltip("Id del SpawnPoint inicial en la escena destino.")]
        [SerializeField] private string targetSpawnId = "default";

        [Tooltip("Duración del fundido a negro al iniciar el juego.")]
        [SerializeField] private float fadeDuration = 0.35f;

        [Tooltip("URL que abre el botón ITCH.IO.")]
        [SerializeField] private string itchIoUrl = "https://itch.io";

        [Header("Sprites del Menú (cargados también desde Resources/Menu)")]
        [SerializeField] private Sprite bgMenuSprite;
        [SerializeField] private Sprite logoPrismSprite;
        [SerializeField] private Sprite btnJugarSprite;
        [SerializeField] private Sprite btnCreditosSprite;
        [SerializeField] private Sprite btnItchIoSprite;
        [SerializeField] private Sprite iconDiamondSprite;

        [Header("Pantalla 1: Menú Principal")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject titleMenuPanel;
        [SerializeField] private Image titleCenterCrystalImage;
        [SerializeField] private Image titleLogoImage;
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button itchIoButton;
        [SerializeField] private RectTransform titleDiamondCursor;

        [Header("Pantalla 2: Selección de Personaje (Cuphead Style)")]
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private Button[] characterSlotButtons;
        [SerializeField] private Image[] characterSlotBackgrounds;
        [SerializeField] private Outline[] characterSlotOutlines;
        [SerializeField] private Text[] characterNameLabels;
        [SerializeField] private Image[] staticPreviewImages;
        [SerializeField] private Text[] staticQuestionLabels;
        [SerializeField] private Text selectionStatusText;
        [SerializeField] private RectTransform cupheadSelectorDiamond;
        [SerializeField] private Button confirmCharacterButton;
        [SerializeField] private Button backToTitleButton;

        [Header("Pantalla Modal: Créditos")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button closeCreditsButton;

        [Header("Animación de Iker")]
        [SerializeField] private Image heroPreviewImage;
        [SerializeField] private Sprite[] heroIdleFrames;
        [SerializeField] private float heroFrameSeconds = 0.40f;

        private const int StaticWidth = 24;
        private const int StaticHeight = 28;
        private const float StaticUpdateInterval = 0.05f;

        private static readonly float[] SlotBaseX = { -88f, 0f, 88f };
        private const float SlotBaseY = 8f;
        private const float SelectedOffsetY = 8f;
        private const float UnselectedOffsetY = -3f;

        private int _selectedCharacterIndex = 0;
        private bool _isTransitioning;
        private float _heroAnimTimer;
        private int _heroAnimIndex;
        private float _staticTimer;
        private int _staticTick;
        private float _shakeOffset;

        private Texture2D[] _staticTextures;
        private Sprite[] _staticSprites;
        private Color32[] _pixelBuffer;
        private Coroutine _shakeCoroutine;
        private InputAction _cancelAction;

        private static readonly Color SelectedCardBg = new Color(0.14f, 0.11f, 0.32f, 0.96f);
        private static readonly Color LockedSelectedCardBg = new Color(0.24f, 0.10f, 0.22f, 0.96f);
        private static readonly Color UnselectedCardBg = new Color(0.08f, 0.07f, 0.18f, 0.88f);

        private static readonly Color SelectedOutlineColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color LockedSelectedOutlineColor = new Color(1f, 0.38f, 0.52f, 1f);
        private static readonly Color UnselectedOutlineColor = new Color(0.45f, 0.38f, 0.68f, 0.65f);

        private void Awake()
        {
            Time.timeScale = 1f;
            GameSignals.InputBlocked = false;

            LoadMenuSpritesIfMissing();
            EnsureTwoStepMenuHierarchy();
            ApplySpritesToUI();
            WireButtons();
            InitStaticTextures();

            ShowTitleScreen();

            _cancelAction = new InputAction("MenuCancel");
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");
            _cancelAction.performed += _ => OnCancelPressed();
            _cancelAction.Enable();
        }

        private void Start()
        {
            FocusFirstTitleButton();
        }

        private void Update()
        {
            UpdateTitleScreenAnimation();

            if (IsCharacterSelectOpen)
            {
                UpdateHeroAnimation();
                UpdateStaticEffect();
                HandleCupheadCarouselInput();
                SyncSelectionFromFocusedSlot();
                UpdateCupheadCardAnimation();
            }

            EnsureNavigationSelection();
        }

        private void OnDestroy()
        {
            if (_cancelAction != null)
            {
                _cancelAction.Disable();
                _cancelAction.Dispose();
                _cancelAction = null;
            }

            if (_staticTextures != null)
            {
                for (int i = 0; i < _staticTextures.Length; i++)
                {
                    if (_staticTextures[i] != null)
                        Destroy(_staticTextures[i]);
                }
            }

            if (_staticSprites != null)
            {
                for (int i = 0; i < _staticSprites.Length; i++)
                {
                    if (_staticSprites[i] != null)
                        Destroy(_staticSprites[i]);
                }
            }
        }

        private bool IsCharacterSelectOpen => characterSelectPanel != null && characterSelectPanel.activeSelf;
        private bool IsCreditsOpen => creditsPanel != null && creditsPanel.activeSelf;

        // =====================================================================
        // Flujo de pantallas: Título -> Selección de Personaje -> Sandbox_Gino
        // =====================================================================

        /// <summary>
        /// Muestra la Pantalla 1 (Menú Principal con PRISM + cristal detrás + JUGAR, CRÉDITOS, ITCH.IO).
        /// </summary>
        public void ShowTitleScreen()
        {
            if (_isTransitioning) return;

            if (titleMenuPanel != null) titleMenuPanel.SetActive(true);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);

            FocusFirstTitleButton();
        }

        /// <summary>
        /// Al pulsar el botón JUGAR en el menú principal, abre la pantalla de
        /// selección de personajes estilo Cuphead.
        /// </summary>
        public void OpenCharacterSelect()
        {
            if (_isTransitioning) return;

            if (titleMenuPanel != null) titleMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);

            SelectCharacter(0, immediate: true);

            if (characterSlotButtons != null && characterSlotButtons.Length > 0 && characterSlotButtons[0] != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(characterSlotButtons[0].gameObject);
            }
        }

        /// <summary>
        /// Abre el modal de Créditos.
        /// </summary>
        public void OpenCredits()
        {
            if (_isTransitioning || creditsPanel == null) return;
            creditsPanel.SetActive(true);

            if (closeCreditsButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(closeCreditsButton.gameObject);
        }

        /// <summary>
        /// Cierra el modal de Créditos.
        /// </summary>
        public void CloseCredits()
        {
            if (creditsPanel == null) return;
            creditsPanel.SetActive(false);

            if (creditsButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(creditsButton.gameObject);
            else
                FocusFirstTitleButton();
        }

        /// <summary>
        /// Acción del botón ITCH.IO.
        /// </summary>
        public void OpenItchIo()
        {
            if (_isTransitioning) return;
            if (!string.IsNullOrEmpty(itchIoUrl))
                Application.OpenURL(itchIoUrl);
        }

        private void OnCancelPressed()
        {
            if (_isTransitioning) return;

            if (IsCreditsOpen)
            {
                CloseCredits();
            }
            else if (IsCharacterSelectOpen)
            {
                ShowTitleScreen();
            }
        }

        // =====================================================================
        // Selección de Personaje Estilo Cuphead (IKER, ???, ???)
        // =====================================================================

        public void SelectCharacter(int index, bool immediate = false)
        {
            if (_isTransitioning) return;
            int count = (characterSlotButtons != null && characterSlotButtons.Length > 0)
                ? characterSlotButtons.Length
                : 3;

            if (index < 0) index = count - 1;
            if (index >= count) index = 0;
            _selectedCharacterIndex = index;

            bool isUnlocked = (_selectedCharacterIndex == 0);

            for (int i = 0; i < count; i++)
            {
                bool isSelected = (i == _selectedCharacterIndex);
                bool slotUnlocked = (i == 0);

                if (characterSlotBackgrounds != null && i < characterSlotBackgrounds.Length && characterSlotBackgrounds[i] != null)
                {
                    if (isSelected)
                        characterSlotBackgrounds[i].color = slotUnlocked ? SelectedCardBg : LockedSelectedCardBg;
                    else
                        characterSlotBackgrounds[i].color = UnselectedCardBg;
                }

                if (characterSlotOutlines != null && i < characterSlotOutlines.Length && characterSlotOutlines[i] != null)
                {
                    if (isSelected)
                        characterSlotOutlines[i].effectColor = slotUnlocked ? SelectedOutlineColor : LockedSelectedOutlineColor;
                    else
                        characterSlotOutlines[i].effectColor = UnselectedOutlineColor;
                }

                if (characterNameLabels != null && i < characterNameLabels.Length && characterNameLabels[i] != null)
                {
                    if (isSelected)
                        characterNameLabels[i].color = slotUnlocked
                            ? new Color(1f, 0.90f, 0.45f, 1f)
                            : new Color(1f, 0.48f, 0.60f, 1f);
                    else
                        characterNameLabels[i].color = new Color(0.65f, 0.62f, 0.78f, 0.85f);
                }

                if (immediate && characterSlotButtons != null && i < characterSlotButtons.Length && characterSlotButtons[i] != null)
                {
                    characterSlotButtons[i].transform.localScale = isSelected
                        ? new Vector3(1.08f, 1.08f, 1f)
                        : new Vector3(0.92f, 0.92f, 1f);

                    var rt = characterSlotButtons[i].GetComponent<RectTransform>();
                    if (rt != null && i < SlotBaseX.Length)
                    {
                        float targetY = SlotBaseY + (isSelected ? SelectedOffsetY : UnselectedOffsetY);
                        rt.anchoredPosition = new Vector2(SlotBaseX[i], targetY);
                    }
                }
            }

            if (immediate && cupheadSelectorDiamond != null && _selectedCharacterIndex < SlotBaseX.Length)
            {
                cupheadSelectorDiamond.anchoredPosition = new Vector2(SlotBaseX[_selectedCharacterIndex], 68f);
            }

            if (selectionStatusText != null)
            {
                if (isUnlocked)
                {
                    selectionStatusText.text = "SOÑADOR: IKER  —  PULSA JUGAR O HAZ CLIC DE NUEVO";
                    selectionStatusText.color = new Color(0.96f, 0.88f, 1f, 1f);
                }
                else
                {
                    selectionStatusText.text = "SEÑAL PERDIDA: PERSONAJE \"???\" NO DISPONIBLE";
                    selectionStatusText.color = new Color(1f, 0.50f, 0.60f, 1f);
                }
            }

            if (confirmCharacterButton != null)
            {
                var img = confirmCharacterButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isUnlocked ? Color.white : new Color(0.55f, 0.45f, 0.60f, 0.70f);
                }
            }
        }

        public void SelectCharacter0() => OnCharacterSlotClicked(0);
        public void SelectCharacter1() => OnCharacterSlotClicked(1);
        public void SelectCharacter2() => OnCharacterSlotClicked(2);

        public void OnCharacterSlotClicked(int index)
        {
            if (_isTransitioning) return;

            bool wasAlreadySelected = (_selectedCharacterIndex == index);
            SelectCharacter(index);

            if (index == 0)
            {
                // Si el usuario hace clic sobre Iker cuando ya estaba seleccionado, inicia el juego
                if (wasAlreadySelected)
                {
                    StartSandboxGino();
                }
            }
            else
            {
                TriggerLockedFeedback(index);
            }
        }

        /// <summary>
        /// Confirma el personaje seleccionado y carga Sandbox_Gino.unity.
        /// </summary>
        public void ConfirmCharacterAndPlay()
        {
            if (_isTransitioning) return;

            if (_selectedCharacterIndex != 0)
            {
                // Si estaba en ???, seleccionar a Iker automáticamente e iniciar
                SelectCharacter(0);
            }

            StartSandboxGino();
        }

        // Compatibilidad con llamadas antiguas
        public void OnPlayClicked() => OpenCharacterSelect();

        private void StartSandboxGino()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            TransitionState.NextSpawnId = targetSpawnId;

            StartCoroutine(TransitionToSandboxRoutine());
        }

        private IEnumerator TransitionToSandboxRoutine()
        {
            GameSignals.InputBlocked = true;

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeTo(1f, fadeDuration);
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.15f);
            }

            GameSignals.InputBlocked = false;

#if UNITY_EDITOR
            const string sandboxScenePath = "Assets/_Project/Scenes/_Sandbox/Sandbox_Gino.unity";
            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            }
            else
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    sandboxScenePath,
                    new LoadSceneParameters(LoadSceneMode.Single)
                );
            }
#else
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
#endif
        }

        private void TriggerLockedFeedback(int slotIndex)
        {
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeCardRoutine());
        }

        private IEnumerator ShakeCardRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - (elapsed / duration);
                _shakeOffset = Mathf.Sin(elapsed * 58f) * 5.5f * damp;
                yield return null;
            }
            _shakeOffset = 0f;
            _shakeCoroutine = null;
        }

        // =====================================================================
        // Animaciones de UI (Cristal Central, Logo PRISM, Cartas Cuphead y TV)
        // =====================================================================

        private void UpdateTitleScreenAnimation()
        {
            if (titleMenuPanel == null || !titleMenuPanel.activeSelf) return;

            Button[] titleBtns = { playButton, creditsButton, itchIoButton };
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            float dt = Time.unscaledDeltaTime * 14f;

            RectTransform activeBtnRT = null;
            for (int i = 0; i < titleBtns.Length; i++)
            {
                if (titleBtns[i] == null) continue;
                bool isSel = (selected == titleBtns[i].gameObject);
                Vector3 targetScale = isSel ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                titleBtns[i].transform.localScale = Vector3.Lerp(titleBtns[i].transform.localScale, targetScale, dt);
                if (isSel) activeBtnRT = titleBtns[i].GetComponent<RectTransform>();
            }

            if (activeBtnRT == null && playButton != null)
                activeBtnRT = playButton.GetComponent<RectTransform>();

            // Animación flotante suave del cristal central (detrás del logo PRISM) y del logo
            if (titleCenterCrystalImage != null)
            {
                var crystalRT = titleCenterCrystalImage.rectTransform;
                float crystalY = 60f + Mathf.Sin(Time.unscaledTime * 2.4f) * 2.5f;
                crystalRT.anchoredPosition = new Vector2(0f, crystalY);
            }

            if (titleLogoImage != null)
            {
                var logoRT = titleLogoImage.rectTransform;
                float logoY = 60f + Mathf.Sin(Time.unscaledTime * 2.4f - 0.4f) * 1.4f;
                logoRT.anchoredPosition = new Vector2(0f, logoY);
            }

            if (titleDiamondCursor != null && activeBtnRT != null)
            {
                float bobX = -72f + Mathf.Sin(Time.unscaledTime * 5f) * 2.5f;
                float targetY = activeBtnRT.anchoredPosition.y;
                Vector2 cur = titleDiamondCursor.anchoredPosition;
                titleDiamondCursor.anchoredPosition = new Vector2(
                    bobX,
                    Mathf.Lerp(cur.y, targetY, dt)
                );
            }
        }

        private void UpdateCupheadCardAnimation()
        {
            if (characterSlotButtons == null) return;

            float dt = Time.unscaledDeltaTime * 14f;
            for (int i = 0; i < characterSlotButtons.Length; i++)
            {
                if (characterSlotButtons[i] == null) continue;
                bool isSelected = (i == _selectedCharacterIndex);

                Vector3 targetScale = isSelected
                    ? new Vector3(1.08f, 1.08f, 1f)
                    : new Vector3(0.92f, 0.92f, 1f);
                characterSlotButtons[i].transform.localScale = Vector3.Lerp(
                    characterSlotButtons[i].transform.localScale,
                    targetScale,
                    dt
                );

                var rt = characterSlotButtons[i].GetComponent<RectTransform>();
                if (rt != null && i < SlotBaseX.Length)
                {
                    float targetX = SlotBaseX[i] + (isSelected ? _shakeOffset : 0f);
                    float bobY = isSelected ? Mathf.Sin(Time.unscaledTime * 4.5f) * 1.5f : 0f;
                    float targetY = SlotBaseY + (isSelected ? SelectedOffsetY : UnselectedOffsetY) + bobY;
                    Vector2 nextPos = Vector2.Lerp(rt.anchoredPosition, new Vector2(targetX, targetY), dt);
                    if (isSelected && Mathf.Abs(_shakeOffset) > 0.01f)
                        nextPos.x = targetX;
                    rt.anchoredPosition = nextPos;
                }
            }

            if (cupheadSelectorDiamond != null && _selectedCharacterIndex < SlotBaseX.Length)
            {
                float targetX = SlotBaseX[_selectedCharacterIndex];
                float bobY = 68f + Mathf.Sin(Time.unscaledTime * 6f) * 2.5f;
                Vector2 cur = cupheadSelectorDiamond.anchoredPosition;
                cupheadSelectorDiamond.anchoredPosition = new Vector2(
                    Mathf.Lerp(cur.x, targetX, dt),
                    bobY
                );
            }
        }

        private void HandleCupheadCarouselInput()
        {
            if (!IsCharacterSelectOpen || _isTransitioning) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectAndFocusSlot(0);
                else if (kb.digit2Key.wasPressedThisFrame) SelectAndFocusSlot(1);
                else if (kb.digit3Key.wasPressedThisFrame) SelectAndFocusSlot(2);
                else if (kb.eKey.wasPressedThisFrame)
                {
                    if (_selectedCharacterIndex == 0)
                        StartSandboxGino();
                    else
                        TriggerLockedFeedback(_selectedCharacterIndex);
                }
            }
        }

        private void SelectAndFocusSlot(int index)
        {
            SelectCharacter(index);
            if (characterSlotButtons != null
                && _selectedCharacterIndex >= 0
                && _selectedCharacterIndex < characterSlotButtons.Length
                && characterSlotButtons[_selectedCharacterIndex] != null
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(characterSlotButtons[_selectedCharacterIndex].gameObject);
            }
        }

        private void SyncSelectionFromFocusedSlot()
        {
            if (EventSystem.current == null || characterSlotButtons == null) return;
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current == null) return;

            for (int i = 0; i < characterSlotButtons.Length; i++)
            {
                if (characterSlotButtons[i] != null && characterSlotButtons[i].gameObject == current)
                {
                    if (_selectedCharacterIndex != i)
                    {
                        SelectCharacter(i);
                    }
                    break;
                }
            }
        }

        private void EnsureNavigationSelection()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null)
                return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool navPressed =
                (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                                kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                                kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                                kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                                kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) ||
                (gp != null && (gp.dpad.ReadValue().sqrMagnitude > 0.1f || gp.leftStick.ReadValue().sqrMagnitude > 0.1f));

            if (!navPressed) return;

            if (IsCreditsOpen && closeCreditsButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeCreditsButton.gameObject);
            }
            else if (IsCharacterSelectOpen && characterSlotButtons != null && _selectedCharacterIndex < characterSlotButtons.Length)
            {
                EventSystem.current.SetSelectedGameObject(characterSlotButtons[_selectedCharacterIndex].gameObject);
            }
            else
            {
                FocusFirstTitleButton();
            }
        }

        private void FocusFirstTitleButton()
        {
            if (playButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void UpdateHeroAnimation()
        {
            if (heroPreviewImage == null || heroIdleFrames == null || heroIdleFrames.Length == 0)
                return;

            _heroAnimTimer += Time.unscaledDeltaTime;
            if (_heroAnimTimer >= heroFrameSeconds)
            {
                _heroAnimTimer = 0f;
                _heroAnimIndex = (_heroAnimIndex + 1) % heroIdleFrames.Length;
                heroPreviewImage.sprite = heroIdleFrames[_heroAnimIndex];
            }
        }

        // =====================================================================
        // Carga de Sprites y Construcción / Auto-cableado de UI
        // =====================================================================

        private void LoadMenuSpritesIfMissing()
        {
            if (bgMenuSprite == null) bgMenuSprite = LoadSpriteResource("Menu/Bg_Menu");
            if (logoPrismSprite == null) logoPrismSprite = LoadSpriteResource("Menu/Logo_Prism");
            if (btnJugarSprite == null) btnJugarSprite = LoadSpriteResource("Menu/Btn_Jugar");
            if (btnCreditosSprite == null) btnCreditosSprite = LoadSpriteResource("Menu/Btn_Creditos");
            if (btnItchIoSprite == null) btnItchIoSprite = LoadSpriteResource("Menu/Btn_ItchIo");
            if (iconDiamondSprite == null) iconDiamondSprite = LoadSpriteResource("Menu/Icon_Diamond");
        }

        private static Sprite LoadSpriteResource(string path)
        {
            var s = Resources.Load<Sprite>(path);
            if (s != null) return s;

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
            }
            return null;
        }

        private void ApplySpritesToUI()
        {
            if (backgroundImage != null && bgMenuSprite != null)
            {
                backgroundImage.sprite = bgMenuSprite;
                backgroundImage.color = Color.white;
            }

            if (titleCenterCrystalImage != null && iconDiamondSprite != null)
            {
                titleCenterCrystalImage.sprite = iconDiamondSprite;
                titleCenterCrystalImage.color = Color.white;
            }

            if (titleLogoImage != null && logoPrismSprite != null)
            {
                titleLogoImage.sprite = logoPrismSprite;
                titleLogoImage.color = Color.white;
            }

            ApplyButtonSprite(playButton, btnJugarSprite);
            ApplyButtonSprite(creditsButton, btnCreditosSprite);
            ApplyButtonSprite(itchIoButton, btnItchIoSprite);
            ApplyButtonSprite(confirmCharacterButton, btnJugarSprite);

            if (titleDiamondCursor != null && iconDiamondSprite != null)
            {
                var img = titleDiamondCursor.GetComponent<Image>();
                if (img != null) img.sprite = iconDiamondSprite;
            }

            if (cupheadSelectorDiamond != null && iconDiamondSprite != null)
            {
                var img = cupheadSelectorDiamond.GetComponent<Image>();
                if (img != null) img.sprite = iconDiamondSprite;
            }
        }

        private static void ApplyButtonSprite(Button btn, Sprite sprite)
        {
            if (btn == null || sprite == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = string.Empty;
            }
        }

        private void WireButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OpenCharacterSelect);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveAllListeners();
                creditsButton.onClick.AddListener(OpenCredits);
            }

            if (itchIoButton != null)
            {
                itchIoButton.onClick.RemoveAllListeners();
                itchIoButton.onClick.AddListener(OpenItchIo);
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.RemoveAllListeners();
                closeCreditsButton.onClick.AddListener(CloseCredits);
            }

            if (confirmCharacterButton != null)
            {
                confirmCharacterButton.onClick.RemoveAllListeners();
                confirmCharacterButton.onClick.AddListener(ConfirmCharacterAndPlay);
            }

            if (backToTitleButton != null)
            {
                backToTitleButton.onClick.RemoveAllListeners();
                backToTitleButton.onClick.AddListener(ShowTitleScreen);
            }

            if (characterSlotButtons != null)
            {
                for (int i = 0; i < characterSlotButtons.Length; i++)
                {
                    int capturedIndex = i;
                    if (characterSlotButtons[i] != null)
                    {
                        characterSlotButtons[i].onClick.RemoveAllListeners();
                        characterSlotButtons[i].onClick.AddListener(() => OnCharacterSlotClicked(capturedIndex));
                    }
                }
            }
        }

        private void EnsureTwoStepMenuHierarchy()
        {
            if (backgroundImage == null)
            {
                var bgTr = transform.Find("Background");
                if (bgTr != null) backgroundImage = bgTr.GetComponent<Image>();
            }

            if (titleMenuPanel == null)
            {
                var tr = transform.Find("TitleMenuPanel");
                if (tr != null) titleMenuPanel = tr.gameObject;
            }

            if (characterSelectPanel == null)
            {
                var tr = transform.Find("CharacterSelectPanel");
                if (tr != null) characterSelectPanel = tr.gameObject;
            }

            if (creditsPanel == null)
            {
                var tr = transform.Find("CreditsPanel") ?? transform.Find("ControlsPanel");
                if (tr != null) creditsPanel = tr.gameObject;
            }

            if (titleMenuPanel == null || characterSelectPanel == null)
            {
                BuildTwoStepMenuFromScratch();
            }
            else
            {
                EnsurePrismLogoAndCrystal(titleMenuPanel.GetComponent<RectTransform>());
            }
        }

        private void EnsurePrismLogoAndCrystal(RectTransform titleRT)
        {
            if (titleRT == null) return;

            string[] oldTextNames = { "TitleText", "SubtitleText", "TitleShadow" };
            foreach (string oldName in oldTextNames)
            {
                var oldTr = titleRT.Find(oldName);
                if (oldTr != null) oldTr.gameObject.SetActive(false);
            }

            if (titleCenterCrystalImage == null)
            {
                var tr = titleRT.Find("TitleCenterCrystal");
                if (tr != null)
                {
                    titleCenterCrystalImage = tr.GetComponent<Image>();
                }
                else
                {
                    var crystalGO = new GameObject("TitleCenterCrystal");
                    crystalGO.transform.SetParent(titleRT, false);
                    var cRT = crystalGO.AddComponent<RectTransform>();
                    cRT.anchorMin = new Vector2(0.5f, 0.5f);
                    cRT.anchorMax = new Vector2(0.5f, 0.5f);
                    cRT.anchoredPosition = new Vector2(0f, 60f);
                    cRT.sizeDelta = new Vector2(56f, 73f);
                    titleCenterCrystalImage = crystalGO.AddComponent<Image>();
                    titleCenterCrystalImage.sprite = iconDiamondSprite;
                    titleCenterCrystalImage.preserveAspect = true;
                    titleCenterCrystalImage.raycastTarget = false;
                }
            }

            if (titleLogoImage == null)
            {
                var tr = titleRT.Find("TitleLogoPrism");
                if (tr != null)
                {
                    titleLogoImage = tr.GetComponent<Image>();
                }
                else
                {
                    var logoGO = new GameObject("TitleLogoPrism");
                    logoGO.transform.SetParent(titleRT, false);
                    var lRT = logoGO.AddComponent<RectTransform>();
                    lRT.anchorMin = new Vector2(0.5f, 0.5f);
                    lRT.anchorMax = new Vector2(0.5f, 0.5f);
                    lRT.anchoredPosition = new Vector2(0f, 60f);
                    lRT.sizeDelta = new Vector2(138f, 44f);
                    titleLogoImage = logoGO.AddComponent<Image>();
                    titleLogoImage.sprite = logoPrismSprite;
                    titleLogoImage.preserveAspect = true;
                    titleLogoImage.raycastTarget = false;
                }
            }

            if (titleCenterCrystalImage != null && titleLogoImage != null)
            {
                titleCenterCrystalImage.transform.SetSiblingIndex(0);
                titleLogoImage.transform.SetSiblingIndex(1);
            }
        }

        private void BuildTwoStepMenuFromScratch()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var oldCard = transform.Find("MenuCard");
            if (oldCard != null) oldCard.gameObject.SetActive(false);

            // 1. TitleMenuPanel
            titleMenuPanel = new GameObject("TitleMenuPanel");
            titleMenuPanel.transform.SetParent(transform, false);
            var titleRT = titleMenuPanel.AddComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            EnsurePrismLogoAndCrystal(titleRT);

            playButton = CreateSpriteButton(titleRT, "PlayButton", btnJugarSprite, new Vector2(0f, 2f), new Vector2(116f, 29f));
            creditsButton = CreateSpriteButton(titleRT, "CreditsButton", btnCreditosSprite, new Vector2(0f, -30f), new Vector2(116f, 29f));
            itchIoButton = CreateSpriteButton(titleRT, "ItchIoButton", btnItchIoSprite, new Vector2(0f, -62f), new Vector2(116f, 29f));

            var cursorGO = new GameObject("TitleDiamondCursor");
            cursorGO.transform.SetParent(titleRT, false);
            titleDiamondCursor = cursorGO.AddComponent<RectTransform>();
            titleDiamondCursor.anchorMin = new Vector2(0.5f, 0.5f);
            titleDiamondCursor.anchorMax = new Vector2(0.5f, 0.5f);
            titleDiamondCursor.anchoredPosition = new Vector2(-72f, 2f);
            titleDiamondCursor.sizeDelta = new Vector2(16f, 21f);
            var cursorImg = cursorGO.AddComponent<Image>();
            cursorImg.sprite = iconDiamondSprite;
            cursorImg.preserveAspect = true;
            cursorImg.raycastTarget = false;

            // 2. CharacterSelectPanel (Cuphead style)
            characterSelectPanel = new GameObject("CharacterSelectPanel");
            characterSelectPanel.transform.SetParent(transform, false);
            var csRT = characterSelectPanel.AddComponent<RectTransform>();
            csRT.anchorMin = Vector2.zero;
            csRT.anchorMax = Vector2.one;
            csRT.offsetMin = Vector2.zero;
            csRT.offsetMax = Vector2.zero;
            var csDim = characterSelectPanel.AddComponent<Image>();
            csDim.color = new Color(0.03f, 0.02f, 0.08f, 0.72f);

            var csHeaderGO = new GameObject("HeaderText");
            csHeaderGO.transform.SetParent(csRT, false);
            var cshRT = csHeaderGO.AddComponent<RectTransform>();
            cshRT.anchorMin = new Vector2(0.5f, 1f);
            cshRT.anchorMax = new Vector2(0.5f, 1f);
            cshRT.anchoredPosition = new Vector2(0f, -18f);
            cshRT.sizeDelta = new Vector2(290f, 20f);
            var cshTxt = csHeaderGO.AddComponent<Text>();
            cshTxt.font = font;
            cshTxt.fontSize = 11;
            cshTxt.fontStyle = FontStyle.Bold;
            cshTxt.alignment = TextAnchor.MiddleCenter;
            cshTxt.color = new Color(0.98f, 0.86f, 0.45f, 1f);
            cshTxt.text = "- ELIGE UN SOÑADOR -";
            cshTxt.raycastTarget = false;

            characterSlotButtons = new Button[3];
            characterSlotBackgrounds = new Image[3];
            characterSlotOutlines = new Outline[3];
            characterNameLabels = new Text[3];
            staticPreviewImages = new Image[2];
            staticQuestionLabels = new Text[2];

            for (int i = 0; i < 3; i++)
            {
                bool isIker = (i == 0);
                var slotGO = new GameObject(isIker ? "CharSlot_0_Iker" : "CharSlot_" + i + "_Mystery");
                slotGO.transform.SetParent(csRT, false);
                var rt = slotGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(SlotBaseX[i], SlotBaseY);
                rt.sizeDelta = new Vector2(78f, 92f);

                var bg = slotGO.AddComponent<Image>();
                bg.color = isIker ? SelectedCardBg : UnselectedCardBg;
                var btn = slotGO.AddComponent<Button>();
                btn.targetGraphic = bg;
                var outline = slotGO.AddComponent<Outline>();

                var prevGO = new GameObject(isIker ? "HeroPreview" : "StaticPreview_" + i);
                prevGO.transform.SetParent(rt, false);
                var prevRT = prevGO.AddComponent<RectTransform>();
                prevRT.anchorMin = new Vector2(0.5f, 0.5f);
                prevRT.anchorMax = new Vector2(0.5f, 0.5f);
                prevRT.anchoredPosition = new Vector2(0f, 8f);
                prevRT.sizeDelta = new Vector2(52f, 60f);
                var prevImg = prevGO.AddComponent<Image>();
                prevImg.preserveAspect = true;
                prevImg.raycastTarget = false;

                if (isIker)
                {
                    if (heroIdleFrames != null && heroIdleFrames.Length > 0)
                        prevImg.sprite = heroIdleFrames[0];
                    heroPreviewImage = prevImg;
                }
                else
                {
                    staticPreviewImages[i - 1] = prevImg;

                    var qGO = new GameObject("StaticQuestion_" + i);
                    qGO.transform.SetParent(rt, false);
                    var qRT = qGO.AddComponent<RectTransform>();
                    qRT.anchorMin = new Vector2(0.5f, 0.5f);
                    qRT.anchorMax = new Vector2(0.5f, 0.5f);
                    qRT.anchoredPosition = new Vector2(0f, 8f);
                    qRT.sizeDelta = new Vector2(52f, 30f);
                    var qTxt = qGO.AddComponent<Text>();
                    qTxt.font = font;
                    qTxt.fontSize = 12;
                    qTxt.fontStyle = FontStyle.Bold;
                    qTxt.alignment = TextAnchor.MiddleCenter;
                    qTxt.text = "???";
                    qTxt.raycastTarget = false;
                    staticQuestionLabels[i - 1] = qTxt;
                }

                var nameGO = new GameObject("NameLabel");
                nameGO.transform.SetParent(rt, false);
                var nameRT = nameGO.AddComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0f, 0f);
                nameRT.anchorMax = new Vector2(1f, 0f);
                nameRT.pivot = new Vector2(0.5f, 0f);
                nameRT.anchoredPosition = new Vector2(0f, 4f);
                nameRT.sizeDelta = new Vector2(0f, 14f);
                var nameTxt = nameGO.AddComponent<Text>();
                nameTxt.font = font;
                nameTxt.fontSize = 8;
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.text = isIker ? "IKER" : "???";
                nameTxt.raycastTarget = false;

                characterSlotButtons[i] = btn;
                characterSlotBackgrounds[i] = bg;
                characterSlotOutlines[i] = outline;
                characterNameLabels[i] = nameTxt;
            }

            var selGemGO = new GameObject("CupheadSelectorDiamond");
            selGemGO.transform.SetParent(csRT, false);
            cupheadSelectorDiamond = selGemGO.AddComponent<RectTransform>();
            cupheadSelectorDiamond.anchorMin = new Vector2(0.5f, 0.5f);
            cupheadSelectorDiamond.anchorMax = new Vector2(0.5f, 0.5f);
            cupheadSelectorDiamond.anchoredPosition = new Vector2(SlotBaseX[0], 68f);
            cupheadSelectorDiamond.sizeDelta = new Vector2(18f, 24f);
            var selGemImg = selGemGO.AddComponent<Image>();
            selGemImg.sprite = iconDiamondSprite;
            selGemImg.preserveAspect = true;
            selGemImg.raycastTarget = false;

            var stGO = new GameObject("SelectionStatusText");
            stGO.transform.SetParent(csRT, false);
            var stRT = stGO.AddComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.5f, 0.5f);
            stRT.anchorMax = new Vector2(0.5f, 0.5f);
            stRT.anchoredPosition = new Vector2(0f, -52f);
            stRT.sizeDelta = new Vector2(290f, 14f);
            selectionStatusText = stGO.AddComponent<Text>();
            selectionStatusText.font = font;
            selectionStatusText.fontSize = 7;
            selectionStatusText.fontStyle = FontStyle.Bold;
            selectionStatusText.alignment = TextAnchor.MiddleCenter;
            selectionStatusText.raycastTarget = false;

            confirmCharacterButton = CreateSpriteButton(csRT, "ConfirmCharacterButton", btnJugarSprite, new Vector2(-42f, -82f), new Vector2(104f, 26f));

            var backGO = new GameObject("BackToTitleButton");
            backGO.transform.SetParent(csRT, false);
            var backRT = backGO.AddComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0.5f);
            backRT.anchorMax = new Vector2(0.5f, 0.5f);
            backRT.anchoredPosition = new Vector2(62f, -82f);
            backRT.sizeDelta = new Vector2(76f, 24f);
            var backImg = backGO.AddComponent<Image>();
            backImg.color = new Color(0.14f, 0.11f, 0.28f, 0.95f);
            backToTitleButton = backGO.AddComponent<Button>();
            backToTitleButton.targetGraphic = backImg;
            var backOut = backGO.AddComponent<Outline>();
            backOut.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.9f);

            var backLblGO = new GameObject("Label");
            backLblGO.transform.SetParent(backRT, false);
            var backLblRT = backLblGO.AddComponent<RectTransform>();
            backLblRT.anchorMin = Vector2.zero;
            backLblRT.anchorMax = Vector2.one;
            backLblRT.offsetMin = Vector2.zero;
            backLblRT.offsetMax = Vector2.zero;
            var backLbl = backLblGO.AddComponent<Text>();
            backLbl.font = font;
            backLbl.fontSize = 7;
            backLbl.fontStyle = FontStyle.Bold;
            backLbl.alignment = TextAnchor.MiddleCenter;
            backLbl.color = new Color(0.95f, 0.88f, 0.65f, 1f);
            backLbl.text = "< VOLVER";
            backLbl.raycastTarget = false;
        }

        private static Button CreateSpriteButton(RectTransform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.78f, 1f);
            colors.selectedColor = new Color(1f, 0.95f, 0.78f, 1f);
            colors.pressedColor = new Color(0.82f, 0.75f, 0.95f, 1f);
            btn.colors = colors;
            return btn;
        }

        // =====================================================================
        // Efecto de Estática de TV (Personajes "???")
        // =====================================================================

        private void InitStaticTextures()
        {
            if (staticPreviewImages == null || staticPreviewImages.Length == 0) return;

            int count = staticPreviewImages.Length;
            _staticTextures = new Texture2D[count];
            _staticSprites = new Sprite[count];
            _pixelBuffer = new Color32[StaticWidth * StaticHeight];

            for (int i = 0; i < count; i++)
            {
                var tex = new Texture2D(StaticWidth, StaticHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _staticTextures[i] = tex;
                _staticSprites[i] = Sprite.Create(
                    tex,
                    new Rect(0, 0, StaticWidth, StaticHeight),
                    new Vector2(0.5f, 0.5f),
                    16f
                );

                if (staticPreviewImages[i] != null)
                {
                    staticPreviewImages[i].sprite = _staticSprites[i];
                    staticPreviewImages[i].color = Color.white;
                }
            }

            RenderAllStaticTextures();
        }

        private void UpdateStaticEffect()
        {
            if (_staticTextures == null || _staticTextures.Length == 0) return;

            _staticTimer += Time.unscaledDeltaTime;
            if (_staticTimer < StaticUpdateInterval) return;

            _staticTimer = 0f;
            _staticTick++;
            RenderAllStaticTextures();

            if (staticQuestionLabels != null)
            {
                for (int i = 0; i < staticQuestionLabels.Length; i++)
                {
                    if (staticQuestionLabels[i] == null) continue;
                    bool isSlotSelected = (_selectedCharacterIndex == i + 1);
                    float alpha = isSlotSelected
                        ? (0.78f + 0.22f * Mathf.Sin(_staticTick * 0.9f + i))
                        : (0.45f + 0.25f * Mathf.Sin(_staticTick * 0.5f + i));
                    staticQuestionLabels[i].color = isSlotSelected
                        ? new Color(1f, 0.45f, 0.55f, alpha)
                        : new Color(0.85f, 0.9f, 1f, alpha);
                }
            }
        }

        private void RenderAllStaticTextures()
        {
            for (int i = 0; i < _staticTextures.Length; i++)
            {
                if (_staticTextures[i] == null) continue;
                bool isSelected = (_selectedCharacterIndex == i + 1);
                FillStaticBuffer(i, isSelected);
                _staticTextures[i].SetPixels32(_pixelBuffer);
                _staticTextures[i].Apply(false);
            }
        }

        private void FillStaticBuffer(int slotOffset, bool isSelected)
        {
            int tearRowA = (_staticTick * 3 + slotOffset * 9) % StaticHeight;
            int tearRowB = (_staticTick * 5 + slotOffset * 13 + 7) % StaticHeight;

            for (int y = 0; y < StaticHeight; y++)
            {
                bool isScanline = (y % 2 == 0);
                bool isTearBar = (y == tearRowA || y == tearRowB);
                int rowShift = isTearBar ? ((_staticTick + slotOffset) % 3 - 1) * 2 : 0;

                for (int x = 0; x < StaticWidth; x++)
                {
                    int idx = y * StaticWidth + x;
                    bool insideSilhouette = IsInsideMysterySilhouette(x + rowShift, y);

                    uint hash = (uint)((x * 374761393) ^ (y * 668265263) ^ ((_staticTick + slotOffset * 31) * 1442695041));
                    hash = (hash ^ (hash >> 13)) * 1274126177u;
                    byte rnd = (byte)((hash >> 24) & 0xFF);

                    if (isTearBar && (rnd > 90))
                    {
                        byte burst = (byte)(175 + (rnd % 75));
                        if (isSelected)
                            _pixelBuffer[idx] = new Color32(burst, (byte)(burst * 0.55f), (byte)(burst * 0.78f), 255);
                        else
                            _pixelBuffer[idx] = new Color32((byte)(burst * 0.78f), (byte)(burst * 0.85f), burst, 255);
                        continue;
                    }

                    if (insideSilhouette)
                    {
                        byte v = (byte)(22 + (rnd % 70));
                        if (isScanline) v = (byte)(v * 3 / 4);

                        if (isSelected)
                        {
                            _pixelBuffer[idx] = new Color32(
                                (byte)Mathf.Min(255, v + 52),
                                (byte)(v * 0.60f),
                                (byte)Mathf.Min(255, v + 38),
                                255
                            );
                        }
                        else
                        {
                            _pixelBuffer[idx] = new Color32(
                                (byte)(v * 0.80f),
                                (byte)(v * 0.86f),
                                (byte)Mathf.Min(255, v + 32),
                                255
                            );
                        }
                    }
                    else
                    {
                        byte n = (byte)(50 + (rnd % 155));
                        if (isScanline) n = (byte)(n * 4 / 5);

                        if (isSelected)
                        {
                            _pixelBuffer[idx] = new Color32(
                                n,
                                (byte)(n * 0.72f),
                                (byte)Mathf.Min(255, n + 20),
                                242
                            );
                        }
                        else
                        {
                            _pixelBuffer[idx] = new Color32(
                                (byte)(n * 0.78f),
                                (byte)(n * 0.84f),
                                n,
                                232
                            );
                        }
                    }
                }
            }
        }

        private static bool IsInsideMysterySilhouette(int x, int y)
        {
            float hx = (x - 11.5f) / 4.8f;
            float hy = (y - 19.0f) / 5.2f;
            if (hx * hx + hy * hy <= 1.0f)
                return true;

            float bx = (x - 11.5f) / 6.5f;
            float by = (y - 8.5f) / 6.8f;
            if (y <= 14 && (bx * bx + by * by <= 1.0f))
                return true;

            return false;
        }
    }
}
