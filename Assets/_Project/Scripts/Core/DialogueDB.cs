using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DreamHacks.Core
{
    // Carga diferida de dialogues.json y acceso tipado por los sistemas de
    // diálogo y batalla. API resumida:
    //   DialogueDB.Scene("id")              → List<DialogueNode> ya desempaquetado
    //   DialogueDB.Line("hacked", "idle")   → línea random del set
    //   DialogueDB.Speaker("Hannah")        → SpeakerInfo con blip_pitch, display, glitch
    //   DialogueDB.Biome("forest")          → BiomeInfo con música, encounters, etc.
    public static class DialogueDB
    {
        public const string ResourcePath = "Data/dialogues";

        private static DialogueData _data;
        public static DialogueData Data
        {
            get { if (_data == null) Load(); return _data; }
        }

        public static bool IsLoaded => _data != null;

        public static void Load()
        {
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError($"[DialogueDB] No se encontró el TextAsset en Resources/{ResourcePath}.json");
                return;
            }
            _data = JsonConvert.DeserializeObject<DialogueData>(ta.text);
            if (_data == null)
            {
                Debug.LogError("[DialogueDB] JSON inválido: deserialización devolvió null.");
            }
        }

        public static void Reload()
        {
            _data = null;
            Load();
        }

        public static BiomeInfo Biome(string key)
        {
            if (Data?.Biomes == null || string.IsNullOrEmpty(key)) return null;
            Data.Biomes.TryGetValue(key, out var b);
            return b;
        }

        public static BiomeInfo Biome(Biome b) => Biome(b.ToKey());

        public static SpeakerInfo Speaker(string id)
        {
            if (Data?.Speakers == null || string.IsNullOrEmpty(id)) return null;
            Data.Speakers.TryGetValue(id, out var s);
            return s;
        }

        // Devuelve una línea aleatoria del set (p. ej. "slime"/"idle"). Si no
        // existe el personaje o el set, devuelve string vacío para no crashear
        // el runner — se verá en la consola en modo DEBUG.
        public static string Line(string character, string set)
        {
            if (Data?.BattleLines == null) return string.Empty;
            if (!Data.BattleLines.TryGetValue(character, out var sets) || sets == null)
            {
                Debug.LogWarning($"[DialogueDB] No existen battle_lines para '{character}'.");
                return string.Empty;
            }
            if (!sets.TryGetValue(set, out var arr) || arr == null || arr.Length == 0)
            {
                Debug.LogWarning($"[DialogueDB] battle_lines['{character}'] no tiene set '{set}'.");
                return string.Empty;
            }
            return arr[Random.Range(0, arr.Length)];
        }

        // Convierte la escena cruda (JArray de objetos heterogéneos) a nodos
        // ya listos para el DialogueManager (F2).
        public static List<DialogueNode> Scene(string id)
        {
            if (Data?.Scenes == null || string.IsNullOrEmpty(id)) return null;
            if (!Data.Scenes.TryGetValue(id, out var raw) || raw == null) return null;

            var nodes = new List<DialogueNode>(raw.Count);
            foreach (var tok in raw)
            {
                if (!(tok is JObject obj)) continue;
                var node = new DialogueNode();

                var sfx = obj["sfx"];
                var ev  = obj["event"];
                var s   = obj["s"];
                var t   = obj["t"];

                if (sfx != null)
                {
                    node.Kind = DialogueNodeKind.Sfx;
                    node.Sfx  = sfx.ToString();
                }
                else if (ev != null)
                {
                    node.Kind     = DialogueNodeKind.Event;
                    node.EventRaw = ev.ToString();
                }
                else if (s != null || t != null)
                {
                    node.Kind    = DialogueNodeKind.Line;
                    node.Speaker = s?.ToString();
                    node.Text    = t?.ToString();
                }
                else
                {
                    // Nodo desconocido: lo saltamos silenciosamente.
                    continue;
                }
                nodes.Add(node);
            }
            return nodes;
        }

        // Útil para el futuro ChoiceUI: devuelve la lista de ids de escena
        // asociados a un bioma (p. ej. para pre-cachear o para validar).
        public static string[] DialogueIds(string biomeKey)
        {
            var b = Biome(biomeKey);
            return b?.Dialogues;
        }
    }
}
