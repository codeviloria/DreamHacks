using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using DreamHacks.World;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Caja de diálogo estilo Omori. Hannah a la IZQUIERDA, Iker a la DERECHA.
// Narrador: sin retrato, texto gris en cursiva. Hacker/Anónimo: sin retrato, texto verde.
// Se arma sola (Canvas propio). Avanza con Espacio / E / Enter / Z.
// Mientras DialogoUI.Activo == true el PlayerController no debe moverse.
public class DialogoUI : MonoBehaviour
{
    public static DialogoUI I { get; private set; }
    public static bool Activo { get; private set; }

    // El proyecto usa "Disable Domain Reload": los static NO se reinician solos
    // entre sesiones de Play. Si una sesión terminó con un diálogo abierto,
    // Activo quedaría en true y Mostrar() haría return para siempre. Reseteamos
    // al entrar en Play (mismo patrón que GameSignals).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetEstaticos() { I = null; Activo = false; }

    public Sprite retratoHannah;
    public Sprite retratoIker;
    [Header("Máquina de escribir")]
    public float letrasPorSegundo = 40f;
    [Tooltip("Pausa extra tras . , ! ? …")]
    public float pausaPuntuacion = 0.18f;
    [Tooltip("Sonido corto por letra (opcional)")]
    public AudioClip sonidoLetra;
    [Range(0, 1)] public float volumenLetra = 0.35f;
    [Tooltip("Cada cuántas letras suena el blip")]
    public int letrasPorSonido = 2;
    [Tooltip("El Hacker escribe con caracteres basura que se corrigen")]
    public bool glitchHacker = true;

    AudioSource audioSrc; Text indicador; bool hablaHacker;

    Image retIzq, retDer; Text nombre, cuerpo; GameObject panel;
    Font fuente;

    void Awake()
    {
        I = this;
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var c = new GameObject("CanvasDialogo", typeof(Canvas), typeof(CanvasScaler));
        c.transform.SetParent(transform, false);
        var cv = c.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 50;
        var sc = c.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720);

        panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(c.transform, false);
        Stretch((RectTransform)panel.transform);

        retIzq = Retrato(panel.transform, new Vector2(0, 0), new Vector2(40, 190));
        retDer = Retrato(panel.transform, new Vector2(1, 0), new Vector2(-40, 190));

        var caja = new GameObject("Caja", typeof(Image)).GetComponent<Image>();
        caja.transform.SetParent(panel.transform, false); caja.color = new Color(0.02f, 0.03f, 0.04f, 0.92f);
        var o = caja.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.9f, 0.95f, 0.92f); o.effectDistance = new Vector2(3, -3);
        var r = caja.rectTransform; r.anchorMin = new Vector2(0.05f, 0.03f); r.anchorMax = new Vector2(0.95f, 0.27f); r.offsetMin = r.offsetMax = Vector2.zero;

        nombre = Texto(caja.transform, 26, FontStyle.Bold);
        var nr = nombre.rectTransform; nr.anchorMin = new Vector2(0, 1); nr.anchorMax = new Vector2(1, 1); nr.pivot = new Vector2(0.5f, 1);
        nr.sizeDelta = new Vector2(-40, 36); nr.anchoredPosition = new Vector2(0, -10);
        cuerpo = Texto(caja.transform, 28, FontStyle.Normal);
        var cr = cuerpo.rectTransform; cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one; cr.offsetMin = new Vector2(24, 14); cr.offsetMax = new Vector2(-24, -50);

        indicador = Texto(caja.transform, 26, FontStyle.Bold);
        var ir = indicador.rectTransform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(1, 0);
        ir.sizeDelta = new Vector2(40, 34); ir.anchoredPosition = new Vector2(-16, 8);
        indicador.text = "▼"; indicador.alignment = TextAnchor.MiddleCenter;

        audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.playOnAwake = false;
        panel.SetActive(false);
    }

    Image Retrato(Transform p, Vector2 ancla, Vector2 pos)
    {
        var img = new GameObject("Retrato", typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(p, false); img.preserveAspect = true;
        var r = img.rectTransform; r.anchorMin = r.anchorMax = r.pivot = ancla;
        r.sizeDelta = new Vector2(220, 220); r.anchoredPosition = pos;
        return img;
    }

    Text Texto(Transform p, int tam, FontStyle st)
    {
        var t = new GameObject("Texto", typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(p, false); t.font = fuente; t.fontSize = tam; t.fontStyle = st; t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    public void Mostrar(List<LineaDialogo> lineas, Action alTerminar = null)
    {
        if (Activo) return;
        StartCoroutine(Reproducir(lineas, alTerminar));
    }

    public void MostrarUna(string quien, string texto, Action alTerminar = null) =>
        Mostrar(new List<LineaDialogo> { new LineaDialogo { quien = quien, texto = texto } }, alTerminar);

    // El proyecto usa el New Input System (activeInputHandler=1); leer con
    // UnityEngine.Input lanzaría excepción. Avanza con Espacio/E/Enter/Z o el
    // botón sur/start del gamepad.
    static bool Avanzar()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame
            || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame)) return true;
        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z);
#endif
    }

    IEnumerator Reproducir(List<LineaDialogo> lineas, Action alTerminar)
    {
        Activo = true; GameSignals.InputBlocked = true; panel.SetActive(true);
        yield return null; // evita que la tecla que abrió el diálogo lo salte
        foreach (var l in lineas)
        {
            Estilo(l.quien);
            indicador.enabled = false;
            yield return Escribir(l.texto);
            // indicador parpadeante: espera a que el jugador avance
            indicador.enabled = true;
            float tParpadeo = 0;
            while (!Avanzar())
            {
                tParpadeo += Time.unscaledDeltaTime;
                indicador.color = new Color(1, 1, 1, Mathf.PingPong(tParpadeo * 2.5f, 1f));
                yield return null;
            }
            yield return null;
        }
        panel.SetActive(false); Activo = false; GameSignals.InputBlocked = false;
        alTerminar?.Invoke();
    }

    const string BASURA = "#$%&@!?01<>/\\|*";

    IEnumerator Escribir(string texto)
    {
        cuerpo.text = "";
        float espera = 0;
        for (int n = 1; n <= texto.Length; n++)
        {
            if (Avanzar()) break; // salta: muestra todo
            char ch = texto[n - 1];
            string visible = texto.Substring(0, n);
            if (hablaHacker && glitchHacker && n < texto.Length && !char.IsWhiteSpace(ch))
                visible += BASURA[Random.Range(0, BASURA.Length)];
            cuerpo.text = visible;

            if (sonidoLetra && !char.IsWhiteSpace(ch) && n % Mathf.Max(1, letrasPorSonido) == 0)
            {
                audioSrc.pitch = hablaHacker ? Random.Range(0.6f, 0.8f) : Random.Range(0.95f, 1.1f);
                audioSrc.PlayOneShot(sonidoLetra, volumenLetra);
            }

            espera = 1f / Mathf.Max(1f, letrasPorSegundo);
            if (".,!?…:;".IndexOf(ch) >= 0) espera += pausaPuntuacion;
            float t = 0;
            while (t < espera) { if (Avanzar()) break; t += Time.unscaledDeltaTime; yield return null; }
            if (t < espera) break;
        }
        cuerpo.text = texto;
        yield return null; // que la misma tecla que saltó no avance también
    }

    void Estilo(string quien)
    {
        string q = (quien ?? "").ToLower();
        bool hannah = q.StartsWith("hannah"), iker = q.StartsWith("iker");
        bool narrador = q.StartsWith("narrador"), hacker = q.StartsWith("hacker") || q.StartsWith("anon") || q.StartsWith("anón");
        hablaHacker = hacker;

        retIzq.enabled = retratoHannah && (hannah || iker); retIzq.sprite = retratoHannah;
        retDer.enabled = retratoIker && (hannah || iker); retDer.sprite = retratoIker;
        // quien habla se ve brillante, el otro oscurecido
        retIzq.color = hannah ? Color.white : new Color(0.35f, 0.35f, 0.4f);
        retDer.color = iker ? Color.white : new Color(0.35f, 0.35f, 0.4f);

        nombre.text = narrador ? "" : hacker ? "ANÓNIMO" : quien;
        nombre.alignment = iker ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        cuerpo.alignment = narrador ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
        cuerpo.fontStyle = narrador ? FontStyle.Italic : FontStyle.Normal;
        cuerpo.color = narrador ? new Color(0.7f, 0.75f, 0.75f) : hacker ? new Color(0.3f, 1f, 0.55f) : Color.white;
        nombre.color = hacker ? new Color(0.3f, 1f, 0.55f) : hannah ? new Color(1f, 0.6f, 0.55f) : new Color(0.7f, 0.65f, 1f);
    }
}
