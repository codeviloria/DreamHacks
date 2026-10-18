using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamHacks.Core
{
    // Estado global del juego: persistente entre escenas y resetea solo al
    // entrar a Play Mode (compatible con DisableDomainReload). Expone HP/ATK
    // de Iker y Hannah, flags narrativos, inventario y bioma actual.
    //
    // Uso típico:
    //   GameState.I.SetHp("Iker", nuevaHp);
    //   GameState.I.SetFlag("hasSword");
    //   if (GameState.I.HasFlag("hannahInParty")) { ... }
    //   GameState.I.AddItem("Potion", 2);
    //   GameState.OnHPChanged += (member, hp, maxHp) => hud.Refresh(hp, maxHp);
    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        // Evento global: (memberId, currentHp, maxHp). El HUD de corazones se
        // suscribe en F3 y se refresca sin tener que preguntar en Update.
        public static event Action<string, int, int> OnHPChanged;

        // Evento al cambiar el bioma (lo usa CorruptionController en F8).
        public static event Action<Biome> OnBiomeChanged;

        private readonly Dictionary<string, PartyMember> _party = new Dictionary<string, PartyMember>();
        private readonly Dictionary<string, int>        _inventory = new Dictionary<string, int>();
        private readonly HashSet<string>                _flags = new HashSet<string>();

        [SerializeField] private Biome currentBiome = Biome.Apartment;
        [SerializeField] private string lastChoice; // "YES" / "NO" tras red_static

        public Biome CurrentBiome => currentBiome;
        public string LastChoice => lastChoice;

        // --- Ciclo de vida ---

        // Punto de entrada que no depende de que exista un GameObject en la
        // escena inicial: crea el singleton al cargar la primera escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Reset de estado estático al re-entrar a Play (DisableDomainReload).
            I = null;
            OnHPChanged = null;
            OnBiomeChanged = null;

            var go = new GameObject("[GameState]");
            DontDestroyOnLoad(go);
            I = go.AddComponent<GameState>();
            I.InitFromStats();
        }

        private void InitFromStats()
        {
            // Carga los JSON si aún no están y mete a Iker y Hannah en la party
            // con sus HP máximos. Esto deja al resto del juego libre de tocar
            // los stats directamente.
            if (StatsDB.Data?.Party == null) return;
            foreach (var kv in StatsDB.Data.Party)
            {
                var ps = kv.Value;
                var m = new PartyMember(kv.Key, ps.Hp, ps.Atk, ps.HpPerLevel, ps.AtkPerLevel);
                _party[kv.Key] = m;
            }
        }

        // --- Party / HP ---

        public PartyMember GetMember(string id)
        {
            _party.TryGetValue(id, out var m);
            return m;
        }

        public bool TryGetHp(string id, out int hp, out int maxHp)
        {
            if (_party.TryGetValue(id, out var m))
            {
                hp = m.Hp; maxHp = m.MaxHp;
                return true;
            }
            hp = 0; maxHp = 0;
            return false;
        }

        public void SetHp(string id, int value)
        {
            if (!_party.TryGetValue(id, out var m)) return;
            m.Hp = Mathf.Clamp(value, 0, m.MaxHp);
            OnHPChanged?.Invoke(id, m.Hp, m.MaxHp);
        }

        public void Damage(string id, int amount) { if (_party.TryGetValue(id, out var m)) SetHp(id, m.Hp - amount); }
        public void Heal(string id, int amount)   { if (_party.TryGetValue(id, out var m)) SetHp(id, m.Hp + amount); }

        public void LevelUp(string id)
        {
            if (!_party.TryGetValue(id, out var m)) return;
            m.MaxHp += m.HpPerLevel;
            m.Atk   += m.AtkPerLevel;
            m.Hp   = m.MaxHp; // tras subir de nivel se cura al máximo
            OnHPChanged?.Invoke(id, m.Hp, m.MaxHp);
        }

        // --- Flags ---

        public void SetFlag(string key)   { if (!string.IsNullOrEmpty(key)) _flags.Add(key); }
        public void ClearFlag(string key) { _flags.Remove(key); }
        public bool HasFlag(string key)   => _flags.Contains(key);

        // --- Inventario ---

        public void AddItem(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            _inventory.TryGetValue(id, out var cur);
            _inventory[id] = cur + amount;
        }

        public bool ConsumeItem(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return false;
            if (!_inventory.TryGetValue(id, out var cur) || cur < amount) return false;
            cur -= amount;
            if (cur == 0) _inventory.Remove(id);
            else _inventory[id] = cur;
            return true;
        }

        public int ItemCount(string id)
        {
            _inventory.TryGetValue(id, out var v);
            return v;
        }

        // Órdenes del hacker: "drop:Potion,HealingFruit"
        public void DropAll(params string[] ids)
        {
            if (ids == null) return;
            foreach (var id in ids) _inventory.Remove(id);
        }

        // --- Bioma / choice ---

        public void SetBiome(Biome b)
        {
            if (currentBiome == b) return;
            currentBiome = b;
            OnBiomeChanged?.Invoke(b);
        }

        public void SetChoice(string value) { lastChoice = value; }

        // Helper para debug/tests: deja el estado limpio sin reiniciar la escena.
        public void ResetAll()
        {
            _party.Clear();
            _inventory.Clear();
            _flags.Clear();
            lastChoice = null;
            currentBiome = Biome.Apartment;
            InitFromStats();
        }
    }

    // Datos de un miembro de la party. Se expone como clase mutable porque
    // la vida útil es toda la partida y los setters pasan por GameState.
    public class PartyMember
    {
        public string Id;
        public int MaxHp;
        public int Hp;
        public int Atk;
        public int HpPerLevel;
        public int AtkPerLevel;

        public PartyMember(string id, int hp, int atk, int hpPerLevel, int atkPerLevel)
        {
            Id = id;
            MaxHp = hp;
            Hp = hp;
            Atk = atk;
            HpPerLevel = hpPerLevel;
            AtkPerLevel = atkPerLevel;
        }
    }
}
