using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamHacks.UI
{
    /// <summary>
    /// Efecto de onda de serpiente (snake wave) estilo 8-bit para el logo de texto.
    /// De vez en cuando recorre horizontalmente las letras de "SHATTERED PRISM"
    /// elevándolas en onda y luego regresa suavemente a su estado natural plano.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class TitleSnakeWave8Bit : BaseMeshEffect
    {
        [Header("Temporización de la Onda")]
        [Tooltip("Segundos de espera entre cada onda de serpiente.")]
        [SerializeField] private float intervalBetweenWaves = 3.5f;
        [Tooltip("Duración de la animación de onda antes de volver al estado natural.")]
        [SerializeField] private float waveDuration = 1.1f;
        [Tooltip("Retraso inicial tras abrirse la cortina negra.")]
        [SerializeField] private float firstWaveDelay = 0.8f;

        [Header("Forma de la Serpiente 8-bit")]
        [Tooltip("Altura máxima en píxeles de la onda en las letras.")]
        [SerializeField] private float waveAmplitudePixels = 12f;
        [Tooltip("Número de crestas a lo largo del título.")]
        [SerializeField] private float waveFrequency = 2.2f;
        [Tooltip("Columnas de subdivisión (14 coincide con las letras de SHATTERED PRISM).")]
        [SerializeField] private int columns = 28;
        [Tooltip("FPS del efecto escalonado 8-bit.")]
        [SerializeField] private float retroFps = 15f;

        private float _timer;
        private bool _waitingFirstWave = true;
        private bool _isWaving;
        private float _waveTime;
        private int _lastQuantizedStep = -1;

        protected override void OnEnable()
        {
            base.OnEnable();
            _waitingFirstWave = true;
            _isWaving = false;
            _timer = 0f;
            _waveTime = 0f;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!MenuIntroShutter8Bit.IsIntroFinished) return;

            if (!_isWaving)
            {
                _timer += Time.unscaledDeltaTime;
                float targetWait = _waitingFirstWave ? firstWaveDelay : intervalBetweenWaves;
                if (_timer >= targetWait)
                {
                    _waitingFirstWave = false;
                    _timer = 0f;
                    _isWaving = true;
                    _waveTime = 0f;
                    _lastQuantizedStep = -1;
                }
            }
            else
            {
                _waveTime += Time.unscaledDeltaTime;
                if (_waveTime >= waveDuration)
                {
                    _isWaving = false;
                    _waveTime = 0f;
                    if (graphic != null) graphic.SetVerticesDirty();
                    return;
                }

                int step = Mathf.FloorToInt(_waveTime * retroFps);
                if (step != _lastQuantizedStep)
                {
                    _lastQuantizedStep = step;
                    if (graphic != null) graphic.SetVerticesDirty();
                }
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !_isWaving || !Application.isPlaying) return;
            if (vh.currentVertCount < 4) return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            if (verts.Count < 6) return;

            // Obtener los límites del quad original de la imagen
            UIVertex bl = verts[0];
            UIVertex tl = verts[1];
            UIVertex tr = verts[2];
            UIVertex br = verts[4];

            float minX = bl.position.x;
            float maxX = br.position.x;
            float minY = bl.position.y;
            float maxY = tl.position.y;

            Vector4 uvMin = bl.uv0;
            Vector4 uvMax = tr.uv0;
            Color32 color = bl.color;

            int sliceCount = Mathf.Clamp(columns, 8, 64);
            float normTime = Mathf.Clamp01(_waveTime / Mathf.Max(0.01f, waveDuration));
            float steppedNormTime = Mathf.Floor(normTime * retroFps * waveDuration) / Mathf.Max(1f, retroFps * waveDuration);

            // El pulso de la serpiente viaja de izquierda (0) a derecha (1)
            float waveCenter = Mathf.Lerp(-0.25f, 1.25f, steppedNormTime);
            // Envolvente global para que inicie y regrese al estado natural sin cortes
            float globalEnvelope = Mathf.Sin(normTime * Mathf.PI);

            vh.Clear();

            for (int i = 0; i < sliceCount; i++)
            {
                float t0 = (float)i / sliceCount;
                float t1 = (float)(i + 1) / sliceCount;
                float tMid = (t0 + t1) * 0.5f;

                // Usamos tMid para cada columna para que cada bloque/letra suba y baje con look 8-bit
                float distFromHead = tMid - waveCenter;
                float localWindow = Mathf.Exp(-distFromHead * distFromHead * 14f);
                float waveVal = Mathf.Sin((tMid * waveFrequency - steppedNormTime * 3.2f) * Mathf.PI * 2f);
                float yOffset = Mathf.Round((waveVal * localWindow * globalEnvelope * waveAmplitudePixels) / 2f) * 2f;

                float x0 = Mathf.Lerp(minX, maxX, t0);
                float x1 = Mathf.Lerp(minX, maxX, t1);
                float u0 = Mathf.Lerp(uvMin.x, uvMax.x, t0);
                float u1 = Mathf.Lerp(uvMin.x, uvMax.x, t1);

                int baseIdx = i * 4;

                UIVertex v0 = bl;
                v0.position = new Vector3(x0, minY + yOffset, 0f);
                v0.uv0 = new Vector4(u0, uvMin.y, 0f, 0f);
                v0.color = color;

                UIVertex v1 = tl;
                v1.position = new Vector3(x0, maxY + yOffset, 0f);
                v1.uv0 = new Vector4(u0, uvMax.y, 0f, 0f);
                v1.color = color;

                UIVertex v2 = tr;
                v2.position = new Vector3(x1, maxY + yOffset, 0f);
                v2.uv0 = new Vector4(u1, uvMax.y, 0f, 0f);
                v2.color = color;

                UIVertex v3 = br;
                v3.position = new Vector3(x1, minY + yOffset, 0f);
                v3.uv0 = new Vector4(u1, uvMin.y, 0f, 0f);
                v3.color = color;

                vh.AddVert(v0);
                vh.AddVert(v1);
                vh.AddVert(v2);
                vh.AddVert(v3);

                vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
            }
        }
    }
}
