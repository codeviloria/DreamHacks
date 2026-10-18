using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DreamHacks.Combate;

namespace DreamHacks.Battle
{
    // Batalla por turnos v2 (modelo en CUARTOS, 10 corazones = 40 cuartos).
    // - Iker: 40 cuartos. Persistencia via BattleState.CurrentIkerHP.
    // - Enemigo (Goblin/Skel): 10 cuartos (override para "este nivel").
    // - Iker ataca -> enemigo -1 cuarto (+ _atkBuff).
    // - Enemigo ataca -> Iker -1 (Goblin) o -2 (Skel) cuartos, menos _defBuff (1 uso).
    // - Pociones (Roja/Azul/Rosa) desde Inventario (via BattleState Inv*).
    // - Combo QTE: pool random de pociones y de intentos por combate.
    [RequireComponent(typeof(BattleUI))]
    public class BattleManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float turnDelay = 1f;
        [SerializeField, Range(0f, 1f)] private float fleeChance = 0.5f;
        [SerializeField, Min(1f)] private float qteDuracion = 7f;

        [Header("Balance de nivel (cuartos)")]
        [SerializeField] private int ikerCuartosMax = 40;
        [SerializeField] private int enemigoCuartosMax = 10;
        [SerializeField] private int ikerDanioBase = 2;
        [SerializeField] private int pocionRosaCuartos = 4;
        [SerializeField] private int atkBuffMax = 3;
        [Tooltip("Golpes del enemigo necesarios para quitar 1 cuarto a Iker.")]
        [SerializeField, Min(1)] private int golpesPorCuartoRecibido = 2;

        private BattleUI _ui;
        private BattleAudio _audio;
        private VidaJugador _ikerVida;
        private VidaEnemigo _enemigoVida;
        private int _atkBuff;
        private int _defBuff;
        private int _golpesEnemigoAcum;
        private bool _acting;

        private readonly List<TipoPocion> _pocionesPool = new List<TipoPocion>();
        private int _intentosRestantes;

        private void Awake()
        {
            _ui = GetComponent<BattleUI>();
            _audio = gameObject.AddComponent<BattleAudio>();
            // VidaJugador vive en un GO propio de la escena Battle (sin SpriteRenderer).
            var ikerGO = new GameObject("IkerVidaProxy");
            ikerGO.transform.SetParent(transform, false);
            _ikerVida = ikerGO.AddComponent<VidaJugador>();
            _ikerVida.SetTestKeysEnabled(false);
            // Enemigo con 10 cuartos.
            var enGO = new GameObject("EnemigoVidaProxy");
            enGO.transform.SetParent(transform, false);
            _enemigoVida = enGO.AddComponent<VidaEnemigo>();
            _enemigoVida.Reconfigurar(enemigoCuartosMax);
        }

        private IEnumerator Start()
        {
            _ui.SetEnemyByKey(BattleState.EnemyId);
            _ui.AttachVidas(_ikerVida, _enemigoVida);
            _ui.LogText.text = "¡" + (BattleState.EnemyId ?? "Enemigo") + " aparece!";

            // Música de combate (hacker si el enemigo es de ese tipo).
            bool esHacker = !string.IsNullOrEmpty(BattleState.EnemyId)
                && (BattleState.EnemyId.StartsWith("Hacker") || BattleState.EnemyId.StartsWith("Skel"));
            _audio.ReproducirMusica(esHacker);

            // Pool aleatorio de pociones (0-3) y de intentos (2-4).
            _pocionesPool.Clear();
            int cuantas = Random.Range(0, 4);
            for (int i = 0; i < cuantas; i++)
                _pocionesPool.Add((TipoPocion)Random.Range(0, 3));
            _intentosRestantes = Random.Range(2, 5);
            Debug.Log("[Battle] Pool pociones=" + cuantas + " intentos=" + _intentosRestantes);

            // Cablea botones.
            _ui.AtacarButton.onClick.AddListener(() => StartCoroutine(OnAtacar()));
            _ui.PocionesButton.onClick.AddListener(OnAbrirPociones);
            _ui.ComboButton.onClick.AddListener(OnAbrirCombo);
            _ui.HuirButton.onClick.AddListener(() => StartCoroutine(OnHuir()));

            if (_ui.PocionMenu != null)
            {
                _ui.PocionMenu.onSeleccion += (t) => StartCoroutine(OnUsarPocion(t));
                _ui.PocionMenu.onVolver += CerrarPociones;
                _ui.PocionMenu.Refrescar(BattleState.InvRojas, BattleState.InvAzules, BattleState.InvRosas);
            }
            if (_ui.ComboQTE != null)
            {
                _ui.ComboQTE.onResultado += OnResultadoCombo;
                _ui.ComboQTE.onTimeout += () => Log("Se acabó el tiempo del combo.");
            }
            RefrescarCombo();

            yield return new WaitForSeconds(0.4f);
            Log("¿Qué hacer?");
        }

        private IEnumerator OnAtacar()
        {
            if (_acting) yield break;
            _acting = true;
            _ui.SetBotonesInteractivos(false);

            int dmg = ikerDanioBase + (BattleState.PlayerNivel - 1) + _atkBuff;
            _audio.Espada();
            _enemigoVida.RecibirDanio(dmg);
            Log("Iker (Nv " + BattleState.PlayerNivel + ") hace " + dmg + " cuartos de daño.");
            yield return _ui.FlashEnemy();
            yield return new WaitForSeconds(turnDelay);

            if (!_enemigoVida.EstaVivo) { yield return WinSequence(); yield break; }
            yield return EnemyTurn();
            _acting = false;
            _ui.SetBotonesInteractivos(true);
        }

        private void OnAbrirPociones()
        {
            if (_acting) return;
            if (_ui.PocionMenu == null) return;
            _ui.PocionMenu.Refrescar(BattleState.InvRojas, BattleState.InvAzules, BattleState.InvRosas);
            _ui.PocionMenu.SetVisible(true);
            _ui.SetBotonesInteractivos(false);
            var primero = _ui.PocionMenu.PrimerBotonUsable();
            if (primero != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(primero.gameObject);
        }

        private void CerrarPociones()
        {
            if (_ui.PocionMenu != null) _ui.PocionMenu.SetVisible(false);
            if (!_acting) _ui.SetBotonesInteractivos(true);
        }

        private IEnumerator OnUsarPocion(TipoPocion t)
        {
            if (_acting) yield break;
            _acting = true;
            if (_ui.PocionMenu != null) _ui.PocionMenu.SetVisible(false);

            bool ok = false;
            switch (t)
            {
                case TipoPocion.Roja:
                    if (BattleState.InvRojas > 0)
                    {
                        BattleState.InvRojas--;
                        _atkBuff = Mathf.Min(atkBuffMax, _atkBuff + 1);
                        Log("Poción Roja: ataque +1 (buff=" + _atkBuff + ").");
                        ok = true;
                    }
                    break;
                case TipoPocion.Azul:
                    if (BattleState.InvAzules > 0)
                    {
                        BattleState.InvAzules--;
                        _defBuff = Mathf.Min(4, _defBuff + 1);
                        Log("Poción Azul: defensa +1 (próximo golpe).");
                        ok = true;
                    }
                    break;
                case TipoPocion.Rosa:
                    if (BattleState.InvRosas > 0)
                    {
                        BattleState.InvRosas--;
                        _ikerVida.Curar(pocionRosaCuartos);
                        Log("Poción Rosa: curas 1 corazón.");
                        ok = true;
                    }
                    break;
            }
            if (!ok) { Log("No te queda esa poción."); _acting = false; _ui.SetBotonesInteractivos(true); yield break; }

            yield return new WaitForSeconds(turnDelay);
            yield return EnemyTurn();
            _acting = false;
            _ui.SetBotonesInteractivos(true);
        }

        private void OnAbrirCombo()
        {
            if (_acting) return;
            if (_intentosRestantes <= 0 || _pocionesPool.Count == 0)
            {
                Log("Ya no hay más intentos de combo.");
                return;
            }
            _acting = true;
            _ui.SetBotonesInteractivos(false);
            if (_ui.ComboQTE != null) _ui.ComboQTE.Iniciar(qteDuracion);
        }

        private void OnResultadoCombo(bool exito)
        {
            if (exito) _audio.Glitch();
            _intentosRestantes = Mathf.Max(0, _intentosRestantes - 1);
            if (exito && _pocionesPool.Count > 0)
            {
                int idx = Random.Range(0, _pocionesPool.Count);
                var tipo = _pocionesPool[idx];
                _pocionesPool.RemoveAt(idx);
                switch (tipo)
                {
                    case TipoPocion.Roja: BattleState.InvRojas++; break;
                    case TipoPocion.Azul: BattleState.InvAzules++; break;
                    case TipoPocion.Rosa: BattleState.InvRosas++; break;
                }
                Log("¡Ganaste una poción " + tipo + "!");
            }
            else
            {
                Log(exito ? "El combo salió, pero el pool está vacío." : "Fallaste el combo.");
            }
            if (_ui.PocionMenu != null)
                _ui.PocionMenu.Refrescar(BattleState.InvRojas, BattleState.InvAzules, BattleState.InvRosas);
            RefrescarCombo();
            StartCoroutine(TrasCombo());
        }

        private IEnumerator TrasCombo()
        {
            yield return new WaitForSeconds(turnDelay);
            yield return EnemyTurn();
            _acting = false;
            _ui.SetBotonesInteractivos(true);
            RefrescarCombo();
        }

        private void RefrescarCombo()
        {
            if (_ui.ComboButton != null)
                _ui.ComboButton.interactable = _intentosRestantes > 0 && _pocionesPool.Count > 0;
        }

        private IEnumerator OnHuir()
        {
            if (_acting) yield break;
            _acting = true;
            _ui.SetBotonesInteractivos(false);

            if (Random.value < fleeChance)
            {
                Log("Iker huye.");
                yield return new WaitForSeconds(turnDelay);
                yield return FleeSequence();
                yield break;
            }

            Log("¡No pudo huir!");
            yield return new WaitForSeconds(turnDelay);
            yield return EnemyTurn();
            _acting = false;
            _ui.SetBotonesInteractivos(true);
        }

        private IEnumerator EnemyTurn()
        {
            if (!_enemigoVida.EstaVivo) yield break;
            _golpesEnemigoAcum++;
            string quien = BattleState.EnemyId ?? "Enemigo";
            int dmg = 0;
            if (_golpesEnemigoAcum >= golpesPorCuartoRecibido)
            {
                dmg = Mathf.Max(0, 1 - _defBuff);
                _defBuff = 0;
                _golpesEnemigoAcum = 0;
            }
            if (dmg > 0)
            {
                _audio.Golpe();
                _ikerVida.RecibirDanio(dmg);
                Log(quien + " ataca y hace " + dmg + " cuartos de daño.");
                yield return _ui.FlashPlayer();
            }
            else
            {
                Log(quien + " golpea; la defensa aguanta (" + _golpesEnemigoAcum + "/" + golpesPorCuartoRecibido + ").");
                yield return _ui.FlashPlayer();
            }
            yield return new WaitForSeconds(turnDelay);
            if (!_ikerVida.EstaVivo) yield return LoseSequence();
        }

        private IEnumerator WinSequence()
        {
            Log("¡Victoria!");
            _audio.PararMusica();
            BattleState.CurrentIkerHP = _ikerVida.CuartosActual;
            int atkAnterior = ikerDanioBase + (BattleState.PlayerNivel - 1);
            // Cada victoria otorga 1 EXP (misma regla que PlayerNivel = 1 + PlayerExp).
            // Lo guardamos para mostrarlo en el cuadro de victoria al volver al overworld.
            const int expGanada = 1;
            BattleState.PlayerExp += expGanada;
            BattleState.LastExpGained = expGanada;
            int atkNuevo = ikerDanioBase + (BattleState.PlayerNivel - 1);
            // Dejar los datos del cuadro de victoria para el overworld (Hospital).
            // Los sprites son assets: sobreviven al cambio de escena.
            VictoriaResumenUI.CargarDatos(
                _ui.CorazonLleno, _ui.CorazonTresCuartos, _ui.CorazonMedio,
                _ui.CorazonUnCuarto, _ui.CorazonVacio,
                _ikerVida.CuartosActual, _ikerVida.CuartosMax);
            BattleState.EndWin();
            yield return _ui.MostrarBannerVictoria(
                BattleState.PlayerNivel,
                atkAnterior,
                atkNuevo,
                golpesPorCuartoRecibido,
                _ikerVida.CuartosActual,
                _ikerVida.CuartosMax);
            SceneManager.LoadScene(BattleState.ReturnScene);
        }

        private IEnumerator LoseSequence()
        {
            Log("Iker ha caído... HP restaurado.");
            _audio.Derrota();
            BattleState.EndLose();
            yield return _ui.MostrarBannerPixel(
                "¡DERROTA!",
                "(presioná cualquier tecla)",
                new Color(1f, 0.3f, 0.3f, 1f));
            SceneManager.LoadScene(BattleState.ReturnScene);
        }

        private IEnumerator FleeSequence()
        {
            BattleState.CurrentIkerHP = _ikerVida.CuartosActual;
            BattleState.EndFlee();
            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene(BattleState.ReturnScene);
        }

        private void Log(string msg)
        {
            if (_ui.LogText != null) _ui.LogText.text = msg;
            Debug.Log("[Battle] " + msg);
        }
    }
}
