using System.Collections.Generic;
using Newtonsoft.Json;

namespace DreamHacks.Core
{
    // Mapea 1:1 el contenido de Assets/_Project/Resources/Data/stats.json.
    // Clases "pasivas": solo datos, sin lógica. Las decisiones (daño, fallo,
    // nivelado) se calculan en los sistemas que consumen estas tablas.

    public class StatsData
    {
        [JsonProperty("rules")]      public RulesData Rules;
        [JsonProperty("party")]      public Dictionary<string, PartyStats> Party;
        [JsonProperty("enemies")]    public Dictionary<string, EnemyStats> Enemies;
        [JsonProperty("boss")]       public Dictionary<string, BossStats> Boss;
        [JsonProperty("encounters")] public Dictionary<string, string[]> Encounters;
    }

    public class RulesData
    {
        [JsonProperty("damage_variance")]       public float[] DamageVariance;
        [JsonProperty("miss_chance")]           public float MissChance;
        [JsonProperty("miss_chance_hacked")]    public float MissChanceHacked;
        [JsonProperty("levelup_per_battle_won")] public int LevelupPerBattleWon;
        [JsonProperty("items")]                 public Dictionary<string, int> Items;
        [JsonProperty("hannah_heal")]           public HannahHealRule HannahHeal;
        [JsonProperty("boss_iframes_seconds")]  public float BossIframesSeconds;
    }

    public class HannahHealRule
    {
        [JsonProperty("amount")]          public int Amount;
        [JsonProperty("uses_per_battle")] public int UsesPerBattle;
        [JsonProperty("trigger_hp_ratio")] public float TriggerHpRatio;
    }

    public class PartyStats
    {
        [JsonProperty("hp")]            public int Hp;
        [JsonProperty("atk")]           public int Atk;
        [JsonProperty("hp_per_level")]  public int HpPerLevel;
        [JsonProperty("atk_per_level")] public int AtkPerLevel;
    }

    public class EnemyStats
    {
        [JsonProperty("display")]   public string Display;
        [JsonProperty("hp")]        public int Hp;
        [JsonProperty("atk")]       public int Atk;
        [JsonProperty("hacked")]    public bool Hacked;
        [JsonProperty("lines")]     public string Lines;
        // Valor opcional: ausente para la mayoría; Hannah enemiga lo trae en false.
        [JsonProperty("canAttack")] public bool? CanAttack;
    }

    public class BossStats
    {
        [JsonProperty("phases")] public BossPhase[] Phases;
    }

    public class BossPhase
    {
        // El JSON usa 300.14 / 500.14 adrede (detalle "glitch" en la UI del boss).
        [JsonProperty("hp")]      public float Hp;
        [JsonProperty("attacks")] public BossAttack[] Attacks;
    }

    public class BossAttack
    {
        [JsonProperty("id")]  public string Id;
        [JsonProperty("dmg")] public int Dmg;
    }
}
