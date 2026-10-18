using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DreamHacks.UI
{
    /// <summary>
    /// Controlador de la maqueta del Menú Principal.
    /// Gestiona los botones Jugar, Créditos y Salir, además del panel de Créditos
    /// y la música de fondo con fade-out al iniciar la partida.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Navegación de Escena")]
        [Tooltip("Nombre de la escena que se cargará al presionar Jugar.")]
        [SerializeField] private string gameSceneName = "IntroCutscene";

        [Header("Paneles")]
        [Tooltip("Panel contenedor de los botones principales del menú.")]
        [SerializeField] private GameObject mainPanel;
        [Tooltip("Panel emergente de Créditos.")]
        [SerializeField] private GameObject creditsPanel;

        [Header("Botones")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button closeCreditsButton;

        [Header("Música del Menú")]
        [Tooltip("AudioSource que reproduce la música del menú principal.")]
        [SerializeField] private AudioSource musicAudioSource;
        [Tooltip("Clip de música del menú principal.")]
        [SerializeField] private AudioClip menuMusicClip;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.8f;
        [Tooltip("Duración en segundos del fade-off de la música al presionar Jugar.")]
        [SerializeField] private float musicFadeOutDuration = 1.5f;

        [Header("Referencias de Arte (Para reemplazar luego)")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image logoImage;

        private bool _isTransitioningToPlay;

        private void Awake()
        {
            Time.timeScale = 1f;
            _isTransitioningToPlay = false;

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            if (closeCreditsButton != null)
                closeCreditsButton.onClick.AddListener(OnCloseCreditsClicked);

            if (creditsPanel != null)
                creditsPanel.SetActive(false);

            if (mainPanel != null)
                mainPanel.SetActive(true);

            InitializeMenuMusic();
        }

        private void InitializeMenuMusic()
        {
            if (musicAudioSource == null)
            {
                musicAudioSource = GetComponent<AudioSource>();
                if (musicAudioSource == null)
                {
                    musicAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (menuMusicClip != null)
            {
                musicAudioSource.clip = menuMusicClip;
            }

            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.volume = musicVolume;

            if (musicAudioSource.clip != null && !musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
        }

        private void Update()
        {
            if (creditsPanel != null && creditsPanel.activeSelf)
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                bool cancelPressed = (kb != null && kb.escapeKey.wasPressedThisFrame)
                                  || (gp != null && gp.buttonEast.wasPressedThisFrame);
                if (cancelPressed)
                {
                    OnCloseCreditsClicked();
                }
            }
        }

        public void OnPlayClicked()
        {
            if (_isTransitioningToPlay) return;

            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogWarning("[MainMenuUI] No se ha configurado el nombre de la escena de juego.");
                return;
            }

            _isTransitioningToPlay = true;
            if (playButton != null)
                playButton.interactable = false;

            StartCoroutine(FadeOutMusicAndLoadScene());
        }

        private IEnumerator FadeOutMusicAndLoadScene()
        {
            var shutter = GetComponent<MenuIntroShutter8Bit>();
            if (shutter != null)
            {
                shutter.CloseShutter(musicFadeOutDuration);
            }

            if (musicAudioSource != null && musicAudioSource.isPlaying && musicFadeOutDuration > 0f)
            {
                float startVolume = musicAudioSource.volume;
                float elapsed = 0f;

                while (elapsed < musicFadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / musicFadeOutDuration);
                    musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                    yield return null;
                }

                musicAudioSource.volume = 0f;
                musicAudioSource.Stop();
            }
            else if (musicFadeOutDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(musicFadeOutDuration);
            }

            Debug.Log($"[MainMenuUI] Cargando escena: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnCreditsClicked()
        {
            if (_isTransitioningToPlay) return;
            if (creditsPanel != null)
                creditsPanel.SetActive(true);
        }

        public void OnCloseCreditsClicked()
        {
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }

        public void OnExitClicked()
        {
            if (_isTransitioningToPlay) return;
            Debug.Log("[MainMenuUI] Saliendo del juego...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.RemoveListener(OnCloseCreditsClicked);
        }
    }
}
