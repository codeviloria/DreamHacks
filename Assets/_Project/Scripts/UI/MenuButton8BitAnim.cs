using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamHacks.UI
{
    /// <summary>
    /// Animación estilo 8-bit para los botones del menú y el prisma central:
    /// - Espera a que termine la cortina de apertura (MenuIntroShutter8Bit.IsIntroFinished).
    /// - En reposo: sube y baja en sincronización usando pasos discretos de 8 bits.
    /// - En hover (si respondToHover está activo): ese botón detiene el vaivén y sube un poco.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MenuButton8BitAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Vaivén Sincronizado (Idle 8-bit)")]
        [Tooltip("Amplitud vertical en píxeles del movimiento arriba/abajo.")]
        [SerializeField] private float bobAmplitude = 5f;
        [Tooltip("Velocidad del ciclo de vaivén.")]
        [SerializeField] private float bobFrequency = 3.5f;
        [Tooltip("Frecuencia de actualización en FPS para dar efecto escalonado 8-bit.")]
        [SerializeField] private float retroFps = 12f;

        [Header("Animación al pasar el Cursor (Hover)")]
        [Tooltip("Si está activo, el elemento detiene el vaivén y sube al pasar el cursor.")]
        [SerializeField] private bool respondToHover = true;
        [Tooltip("Cuántos píxeles sube el botón cuando pones el cursor encima.")]
        [SerializeField] private float hoverRisePixels = 10f;
        [Tooltip("Escala ligera al subir para reforzar el feedback visual.")]
        [SerializeField] private float hoverScale = 1.05f;
        [Tooltip("Velocidad de la animación de subida al hacer hover.")]
        [SerializeField] private float hoverTransitionSpeed = 14f;

        private RectTransform _rect;
        private Vector2 _baseAnchoredPos;
        private Vector3 _baseScale;
        private bool _initialized;
        private bool _isHovered;
        private float _hoverProgress;

        private void Start()
        {
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            if (_initialized) return;
            _rect = GetComponent<RectTransform>();

            // Si el padre tiene un LayoutGroup, forzar cálculo inicial y desactivarlo
            // para permitir animar anchoredPosition libremente.
            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
            {
                var layoutGroup = parentRect.GetComponent<LayoutGroup>();
                if (layoutGroup != null && layoutGroup.enabled)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                    layoutGroup.enabled = false;
                }
            }

            _baseAnchoredPos = _rect.anchoredPosition;
            _baseScale = _rect.localScale;
            _initialized = true;
        }

        private void OnEnable()
        {
            _isHovered = false;
            _hoverProgress = 0f;
            if (_initialized && _rect != null)
            {
                _rect.anchoredPosition = _baseAnchoredPos;
                _rect.localScale = _baseScale;
            }
        }

        private void Update()
        {
            if (!_initialized) InitializeLayout();

            // Esperar a que la cortina negra 8-bit termine de abrirse por completo
            if (!MenuIntroShutter8Bit.IsIntroFinished)
            {
                _rect.anchoredPosition = _baseAnchoredPos;
                _rect.localScale = _baseScale;
                return;
            }

            if (respondToHover && _isHovered)
            {
                // Detiene el vaivén y despliega la animación de subir un poco
                _hoverProgress = Mathf.MoveTowards(_hoverProgress, 1f, Time.unscaledDeltaTime * hoverTransitionSpeed);
                float steppedProgress = Mathf.Round(_hoverProgress * 4f) / 4f;
                float yOffset = Mathf.Round(steppedProgress * hoverRisePixels);

                _rect.anchoredPosition = _baseAnchoredPos + new Vector2(0f, yOffset);
                _rect.localScale = Vector3.Lerp(_baseScale, _baseScale * hoverScale, steppedProgress);
            }
            else
            {
                _hoverProgress = Mathf.MoveTowards(_hoverProgress, 0f, Time.unscaledDeltaTime * hoverTransitionSpeed);

                float activeTime = Mathf.Max(0f, Time.unscaledTime - MenuIntroShutter8Bit.IntroFinishedTime);
                float steppedTime = Mathf.Floor(activeTime * retroFps) / retroFps;
                float syncBobY = Mathf.Round(Mathf.Sin(steppedTime * bobFrequency) * bobAmplitude);

                float currentY = Mathf.Lerp(syncBobY, hoverRisePixels, _hoverProgress);
                _rect.anchoredPosition = _baseAnchoredPos + new Vector2(0f, Mathf.Round(currentY));
                _rect.localScale = Vector3.Lerp(_baseScale, _baseScale * hoverScale, _hoverProgress);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (respondToHover) _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (respondToHover) _isHovered = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHovered = false;
        }
    }
}
