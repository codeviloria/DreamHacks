using Newtonsoft.Json;
using UnityEngine;

namespace DreamHacks.Core
{
    // Carga diferida de stats.json desde Resources/Data y acceso tipado.
    // Es estática: cualquier sistema (BattleManager, LevelSystem, HUD) la
    // consulta sin inyección. Si se cambia el JSON en caliente desde el editor,
    // basta con llamar a Reload() desde un botón de inspector o un test.
    public static class StatsDB
    {
        public const string ResourcePath = "Data/stats";

        private static StatsData _data;
        public static StatsData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static bool IsLoaded => _data != null;

        public static void Load()
        {
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError($"[StatsDB] No se encontró el TextAsset en Resources/{ResourcePath}.json");
                return;
            }
            _data = JsonConvert.DeserializeObject<StatsData>(ta.text);
            if (_data == null)
            {
                Debug.LogError("[StatsDB] JSON inválido: deserialización devolvió null.");
            }
        }

        public static void Reload()
        {
            _data = null;
            Load();
        }

        // Devuelve null si no existe; el llamador decide qué hacer.
        public static EnemyStats Enemy(string id)
        {
            if (Data?.Enemies == null) return null;
            Data.Enemies.TryGetValue(id, out var e);
            return e;
        }

        public static PartyStats Party(string id)
        {
            if (Data?.Party == null) return null;
            Data.Party.TryGetValue(id, out var p);
            return p;
        }

        public static BossStats Boss(string id)
        {
            if (Data?.Boss == null) return null;
            Data.Boss.TryGetValue(id, out var b);
            return b;
        }

        public static string[] Encounter(string id)
        {
            if (Data?.Encounters == null) return null;
            Data.Encounters.TryGetValue(id, out var arr);
            return arr;
        }

        public static int ItemHeal(string itemId)
        {
            if (Data?.Rules?.Items == null) return 0;
            Data.Rules.Items.TryGetValue(itemId, out var v);
            return v;
        }
    }
}
