using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamHacks.UI
{
    // Fondo tipo "Matrix": lineas pequenas en diagonal cayendo. 3 capas con
    // velocidades distintas (parallax). Se construye por codigo y se pone como
    // hijo del Canvas, por encima del Fondo oscuro y debajo del resto de UI.
    public class MatrixRain : MonoBehaviour
    {
        [SerializeField, Min(10)] private int glifosPorCapa = 40;
        [SerializeField] private Color colorBase = new Color(0.3f, 1f, 0.5f, 0.85f);
        [SerializeField] private float anguloGrados = 20f;
        [SerializeField] private float velocidadMin = 60f;
        [SerializeField] private float velocidadMax = 240f;
        [SerializeField] private int tamanoFuente = 14;

        private RectTransform _rt;
        private readonly List<Glifo> _glifos = new List<Glifo>();
        private Font _font;
        private const string CHARS = "01アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモ";

        private class Glifo
        {
            public RectTransform rt;
            public Text txt;
            public float velocidad;
            public float capaAlpha;
        }

        public void Construir(RectTransform parentCanvas)
        {
            var go = new GameObject("MatrixRain", typeof(RectTransform));
            go.transform.SetParent(parentCanvas, false);
            _rt = (RectTransform)go.transform;
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.one;
            _rt.offsetMin = _rt.offsetMax = Vector2.zero;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int capa = 0; capa < 3; capa++)
            {
                float alpha = Mathf.Lerp(0.25f, 1f, (capa + 1) / 3f);
                float velMul = Mathf.Lerp(0.5f, 1.3f, capa / 2f);
                for (int i = 0; i < glifosPorCapa; i++) SpawnGlifo(alpha, velMul);
            }
        }

        private void SpawnGlifo(float capaAlpha, float velMul)
        {
            var g = new GameObject("g", typeof(RectTransform), typeof(Text));
            g.transform.SetParent(_rt, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(24f, 24f);
            r.localRotation = Quaternion.Euler(0f, 0f, -anguloGrados);
            var t = g.GetComponent<Text>();
            t.font = _font;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = Mathf.RoundToInt(tamanoFuente * Mathf.Lerp(0.75f, 1.25f, capaAlpha));
            t.text = CHARS[Random.Range(0, CHARS.Length)].ToString();
            var c = colorBase;
            c.a *= capaAlpha;
            t.color = c;
            t.raycastTarget = false;

            float ancho = _rt.rect.width;
            float alto = _rt.rect.height;
            r.anchoredPosition = new Vector2(Random.Range(0f, ancho), -Random.Range(0f, alto));

            _glifos.Add(new Glifo
            {
                rt = r,
                txt = t,
                velocidad = Random.Range(velocidadMin, velocidadMax) * velMul,
                capaAlpha = capaAlpha
            });
        }

        private void Update()
        {
            if (_rt == null || _glifos.Count == 0) return;
            float alto = _rt.rect.height;
            float ancho = _rt.rect.width;
            // Caen en diagonal: dir = (sin(a), -cos(a)) para abajo-derecha.
            float ang = anguloGrados * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang));
            for (int i = 0; i < _glifos.Count; i++)
            {
                var g = _glifos[i];
                var p = g.rt.anchoredPosition;
                p += dir * g.velocidad * Time.deltaTime;
                if (p.y < -alto - 32f || p.x > ancho + 32f)
                {
                    p = new Vector2(Random.Range(-32f, ancho - 32f), Random.Range(-16f, 16f));
                    g.txt.text = CHARS[Random.Range(0, CHARS.Length)].ToString();
                }
                g.rt.anchoredPosition = p;
            }
        }
    }
}
