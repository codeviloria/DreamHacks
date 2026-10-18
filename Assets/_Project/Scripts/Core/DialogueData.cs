using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamHacks.Core
{
    // Mapea Assets/_Project/Resources/Data/dialogues.json.
    // Las "scenes" son listas de nodos heterogéneos: cada nodo puede ser
    // una línea de diálogo {s, t}, un sfx {sfx}, o un evento {event}, por
    // eso lo guardamos crudo como JToken y lo interpretamos en el runner.

    public class DialogueData
    {
        [JsonProperty("biomes")]       public Dictionary<string, BiomeInfo> Biomes;
        [JsonProperty("speakers")]     public Dictionary<string, SpeakerInfo> Speakers;
        // battle_lines[character][set] = array de strings
        [JsonProperty("battle_lines")] public Dictionary<string, Dictionary<string, string[]>> BattleLines;
        // scenes[id] = lista de nodos heterogéneos (ver DialogueNode)
        [JsonProperty("scenes")]       public Dictionary<string, JArray> Scenes;
    }

    public class BiomeInfo
    {
        [JsonProperty("display")]      public string Display;
        [JsonProperty("unity_scenes")] public string[] UnityScenes;
        [JsonProperty("music")]        public string Music;
        [JsonProperty("corruption")]   public int Corruption;
        [JsonProperty("dialogues")]    public string[] Dialogues;
        [JsonProperty("battle_lines")] public string[] BattleLines;
        [JsonProperty("encounters")]   public string[] Encounters;
    }

    public class SpeakerInfo
    {
        [JsonProperty("blip_pitch")] public float BlipPitch = 1.0f;
        [JsonProperty("display")]    public string Display;
        [JsonProperty("glitch")]     public bool Glitch;
    }

    // Nodos ya desempaquetados listos para el runner de diálogo.
    public enum DialogueNodeKind { Line, Event, Sfx }

    public struct DialogueNode
    {
        public DialogueNodeKind Kind;
        public string Speaker;   // Line: id de speaker; otros: null
        public string Text;      // Line: texto; otros: null
        public string EventRaw;  // Event: "tasks:bed,breakfast,plant", "flag:hasSword", etc.
        public string Sfx;       // Sfx: id del clip ("alarm", ...)
    }
}
