using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using DreamHacks.World;

namespace DreamHacks.UI
{
    /// <summary>
    /// Valores de trucos que otros sistemas (combate, vida) pueden leer.
    /// </summary>
    public static class DevCheats
    {
        public static float DamageMultiplier = 1f;
        public static bool God;
        public static bool NoClip;
    }

    /// <summary>
    /// Consola de comandos oculta. Se abre con el código Konami:
    /// Izq Der Izq Der Arriba Abajo Arriba Abajo B A Enter.
    /// Se crea sola al iniciar el juego (no hace falta ponerla en ninguna escena).
    /// Escribe "help" dentro de la consola para ver los comandos.
    /// </summary>
    public class DevConsole : MonoBehaviour
    {
        private static readonly Key[] Code =
        {
            Key.LeftArrow, Key.RightArrow, Key.LeftArrow, Key.RightArrow,
            Key.UpArrow, Key.DownArrow, Key.UpArrow, Key.DownArrow,
            Key.B, Key.A, Key.Enter
        };

        private const string InputName = "DevConsoleInput";

        private bool _open;
        private int _codeIndex;
        private string _input = "";
        private Vector2 _scroll;
        private bool _focusPending;
        private readonly List<string> _log = new List<string>();
        private readonly List<string> _history = new List<string>();
        private int _historyPos;

        // Valores originales de campos modificados por reflexión, para "reset".
        private readonly Dictionary<(Component, string), object> _originals = new Dictionary<(Component, string), object>();

        // Estado de noclip
        private Rigidbody2D _ncBody;
        private RigidbodyType2D _ncBodyType;
        private float _ncGravity;
        private readonly List<(Collider2D col, bool enabled)> _ncColliders = new List<(Collider2D, bool)>();
        private readonly List<Behaviour> _ncDisabled = new List<Behaviour>();
        private float _noclipSpeed = 10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<DevConsole>() != null) return;
            var go = new GameObject("DevConsole");
            DontDestroyOnLoad(go);
            go.AddComponent<DevConsole>();
        }

        private void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        private void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            // Los objetos del player cambian al cargar escena: se descarta el estado de noclip.
            _ncBody = null;
            _ncColliders.Clear();
            _ncDisabled.Clear();
            DevCheats.NoClip = false;
            _originals.Clear();
            if (_open) SetOpen(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (!_open) TrackKonami(kb);
            else if (kb.escapeKey.wasPressedThisFrame) SetOpen(false);

            if (DevCheats.NoClip && !_open) UpdateNoClipMovement(kb);
        }

        private void TrackKonami(Keyboard kb)
        {
            Key pressed = Key.None;
            foreach (var key in kb.allKeys)
            {
                if (key.wasPressedThisFrame) { pressed = key.keyCode; break; }
            }
            if (pressed == Key.None) return;
            if (pressed == Key.NumpadEnter) pressed = Key.Enter;

            if (pressed == Code[_codeIndex])
            {
                _codeIndex++;
                if (_codeIndex >= Code.Length)
                {
                    _codeIndex = 0;
                    SetOpen(true);
                }
            }
            else
            {
                _codeIndex = pressed == Code[0] ? 1 : 0;
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;
            GameSignals.InputBlocked = open;
            if (open)
            {
                _input = "";
                _focusPending = true;
                if (_log.Count == 0) Print("Consola de desarrollo. Escribe 'help'.");
            }
        }

        // ---------------------------------------------------------------- UI
        private void OnGUI()
        {
            if (!_open) return;

            float h = Mathf.Min(Screen.height * 0.45f, 360f);
            var rect = new Rect(10, 10, Mathf.Min(Screen.width - 20, 700), h);
            GUI.color = new Color(1, 1, 1, 0.97f);
            GUI.Box(rect, "CONSOLA  (Esc para cerrar)");

            var e = Event.current;
            if (e.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == InputName)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    Submit();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow && _history.Count > 0)
                {
                    _historyPos = Mathf.Max(0, _historyPos - 1);
                    _input = _history[_historyPos];
                    e.Use();
                }
                else if (e.keyCode == KeyCode.DownArrow && _history.Count > 0)
                {
                    _historyPos = Mathf.Min(_history.Count, _historyPos + 1);
                    _input = _historyPos >= _history.Count ? "" : _history[_historyPos];
                    e.Use();
                }
            }

            var logRect = new Rect(rect.x + 8, rect.y + 24, rect.width - 16, rect.height - 56);
            var content = new Rect(0, 0, logRect.width - 20, Mathf.Max(logRect.height, _log.Count * 18f));
            _scroll = GUI.BeginScrollView(logRect, _scroll, content);
            for (int i = 0; i < _log.Count; i++)
                GUI.Label(new Rect(0, i * 18f, content.width, 18f), _log[i]);
            GUI.EndScrollView();

            GUI.SetNextControlName(InputName);
            _input = GUI.TextField(new Rect(rect.x + 8, rect.yMax - 28, rect.width - 16, 22), _input);
            if (_focusPending)
            {
                GUI.FocusControl(InputName);
                if (e.type == EventType.Repaint) _focusPending = false;
            }
        }

        private void Print(string s)
        {
            _log.Add(s);
            if (_log.Count > 200) _log.RemoveAt(0);
            _scroll.y = float.MaxValue;
        }

        private void Submit()
        {
            var line = _input.Trim();
            _input = "";
            if (line.Length == 0) return;
            _history.Add(line);
            _historyPos = _history.Count;
            Print("> " + line);
            try { Execute(line); }
            catch (Exception ex) { Print("Error: " + ex.Message); }
        }

        // ---------------------------------------------------------- Comandos
        private void Execute(string line)
        {
            var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cmd = p[0].ToLowerInvariant();

            switch (cmd)
            {
                case "help":
                case "?":
                    Print("speed <n>      velocidad de movimiento");
                    Print("jump <n>       fuerza de salto");
                    Print("damage <n>     multiplicador de daño (x)");
                    Print("god [on|off]   invulnerabilidad");
                    Print("noclip [on|off] atravesar paredes y volar (WASD/flechas)");
                    Print("noclipspeed <n> velocidad en noclip");
                    Print("gravity <n>    multiplicador de gravedad");
                    Print("timescale <n>  velocidad del juego");
                    Print("tp <x> <y>     teletransportar");
                    Print("pos            mostrar posición");
                    Print("reset          restaurar valores");
                    Print("clear / exit");
                    break;

                case "speed":
                    SetField(Num(p, 1), "moveSpeed", "velocidad");
                    break;

                case "jump":
                    SetField(Num(p, 1), "jumpForce", "salto");
                    break;

                case "damage":
                    DevCheats.DamageMultiplier = Num(p, 1);
                    Print("daño x" + DevCheats.DamageMultiplier);
                    break;

                case "god":
                    DevCheats.God = Toggle(p, DevCheats.God);
                    Print("god " + (DevCheats.God ? "ON" : "OFF"));
                    break;

                case "noclip":
                    SetNoClip(Toggle(p, DevCheats.NoClip));
                    break;

                case "noclipspeed":
                    _noclipSpeed = Num(p, 1);
                    Print("noclip speed = " + _noclipSpeed);
                    break;

                case "gravity":
                {
                    float m = Num(p, 1);
                    foreach (var rb in PlayerBodies()) rb.gravityScale = m;
                    Print("gravedad del player x" + m);
                    break;
                }

                case "timescale":
                    Time.timeScale = Mathf.Max(0f, Num(p, 1));
                    Print("timeScale = " + Time.timeScale);
                    break;

                case "tp":
                {
                    var t = FindPlayer();
                    if (t == null) { Print("No encuentro al player."); break; }
                    t.position = new Vector3(Num(p, 1), Num(p, 2), t.position.z);
                    Print("tp -> " + t.position);
                    break;
                }

                case "pos":
                {
                    var t = FindPlayer();
                    Print(t == null ? "No encuentro al player." : t.position.ToString());
                    break;
                }

                case "reset":
                    ResetAll();
                    break;

                case "clear":
                    _log.Clear();
                    break;

                case "exit":
                case "close":
                    SetOpen(false);
                    break;

                default:
                    Print("Comando desconocido. Escribe 'help'.");
                    break;
            }
        }

        private static float Num(string[] p, int i)
        {
            if (p.Length <= i) throw new Exception("falta un número");
            if (!float.TryParse(p[i].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new Exception("'" + p[i] + "' no es un número");
            return v;
        }

        private static bool Toggle(string[] p, bool current)
        {
            if (p.Length < 2) return !current;
            var a = p[1].ToLowerInvariant();
            return a == "on" || a == "1" || a == "true" || a == "si";
        }

        // ----------------------------------------------------- Player helpers
        private Transform FindPlayer()
        {
            try
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) return tagged.transform;
            }
            catch (UnityException) { /* tag no definido */ }

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (mb == null) continue;
                var n = mb.GetType().Name;
                if (n == "PlayerController" || n == "PlayerController2D") return mb.transform;
            }
            return null;
        }

        private IEnumerable<Rigidbody2D> PlayerBodies()
        {
            var t = FindPlayer();
            if (t != null)
            {
                var rb = t.GetComponent<Rigidbody2D>();
                if (rb != null) yield return rb;
            }
        }

        /// <summary>Cambia un campo (privado o público) en los componentes del player por reflexión.</summary>
        private void SetField(float value, string field, string label)
        {
            var t = FindPlayer();
            if (t == null) { Print("No encuentro al player."); return; }

            bool done = false;
            foreach (var c in t.GetComponents<MonoBehaviour>())
            {
                if (c == null) continue;
                var f = c.GetType().GetField(field,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f == null || f.FieldType != typeof(float)) continue;

                var key = ((Component)c, field);
                if (!_originals.ContainsKey(key)) _originals[key] = f.GetValue(c);
                f.SetValue(c, value);
                done = true;
            }
            Print(done ? label + " = " + value : "El player no tiene '" + field + "'.");
        }

        private void ResetAll()
        {
            foreach (var kv in _originals)
            {
                var (comp, field) = kv.Key;
                if (comp == null) continue;
                var f = comp.GetType().GetField(field,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                f?.SetValue(comp, kv.Value);
            }
            _originals.Clear();
            DevCheats.DamageMultiplier = 1f;
            DevCheats.God = false;
            Time.timeScale = 1f;
            foreach (var rb in PlayerBodies()) rb.gravityScale = 1f;
            SetNoClip(false);
            Print("valores restaurados");
        }

        // ------------------------------------------------------------ Noclip
        private void SetNoClip(bool on)
        {
            if (on == DevCheats.NoClip) { Print("noclip " + (on ? "ON" : "OFF")); return; }

            var t = FindPlayer();
            if (t == null) { Print("No encuentro al player."); return; }

            if (on)
            {
                _ncBody = t.GetComponent<Rigidbody2D>();
                _ncColliders.Clear();
                _ncDisabled.Clear();

                foreach (var c in t.GetComponentsInChildren<Collider2D>())
                {
                    _ncColliders.Add((c, c.enabled));
                    c.enabled = false;
                }
                foreach (var mb in t.GetComponents<MonoBehaviour>())
                {
                    var n = mb.GetType().Name;
                    if (mb.enabled && (n == "PlayerController" || n == "PlayerController2D"))
                    {
                        mb.enabled = false;
                        _ncDisabled.Add(mb);
                    }
                }
                if (_ncBody != null)
                {
                    _ncBodyType = _ncBody.bodyType;
                    _ncGravity = _ncBody.gravityScale;
                    _ncBody.linearVelocity = Vector2.zero;
                    _ncBody.gravityScale = 0f;
                    _ncBody.bodyType = RigidbodyType2D.Kinematic;
                }
            }
            else
            {
                foreach (var (col, enabled) in _ncColliders) if (col != null) col.enabled = enabled;
                foreach (var mb in _ncDisabled) if (mb != null) mb.enabled = true;
                if (_ncBody != null)
                {
                    _ncBody.bodyType = _ncBodyType;
                    _ncBody.gravityScale = _ncGravity;
                    _ncBody.linearVelocity = Vector2.zero;
                }
                _ncColliders.Clear();
                _ncDisabled.Clear();
            }

            DevCheats.NoClip = on;
            Print("noclip " + (on ? "ON (WASD / flechas para volar)" : "OFF"));
        }

        private void UpdateNoClipMovement(Keyboard kb)
        {
            if (_ncBody == null) return;

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed || kb.spaceKey.isPressed ? 1 : 0)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);

            var dir = new Vector2(x, y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            _ncBody.transform.position += (Vector3)(dir * _noclipSpeed * Time.unscaledDeltaTime);
        }
    }
}
