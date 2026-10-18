using NUnit.Framework;
using UnityEngine;
using DreamHacks.Core;

namespace DreamHacks.Core.Tests
{
    // El bootstrap de GameState corre con [RuntimeInitializeOnLoadMethod], que
    // sólo dispara en Play Mode. Para los tests EditMode creamos manualmente
    // el GameObject y simulamos la inicialización desde StatsDB.
    public class GameStateTests
    {
        private GameState _gs;

        [SetUp]
        public void Setup()
        {
            StatsDB.Reload();

            // Limpia la instancia global y los suscriptores entre tests.
            if (GameState.I != null) Object.DestroyImmediate(GameState.I.gameObject);
            typeof(GameState).GetProperty("I").SetValue(null, null);
            typeof(GameState).GetField("OnHPChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);
            typeof(GameState).GetField("OnBiomeChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            var go = new GameObject("[GameState-Test]");
            _gs = go.AddComponent<GameState>();
            typeof(GameState).GetMethod("InitFromStats",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_gs, null);
            typeof(GameState).GetProperty("I").SetValue(null, _gs);
        }

        [TearDown]
        public void Teardown()
        {
            if (_gs != null) Object.DestroyImmediate(_gs.gameObject);
            typeof(GameState).GetProperty("I").SetValue(null, null);
        }

        [Test]
        public void Party_InitFromStats()
        {
            Assert.IsTrue(_gs.TryGetHp("Iker", out var hp, out var maxHp));
            Assert.AreEqual(100, hp);
            Assert.AreEqual(100, maxHp);
            Assert.IsTrue(_gs.TryGetHp("Hannah", out hp, out maxHp));
            Assert.AreEqual(85, hp);
        }

        [Test]
        public void Damage_FiresOnHPChanged_AndClampsToZero()
        {
            string gotMember = null; int gotHp = -1, gotMax = -1;
            void Handler(string id, int h, int m) { gotMember = id; gotHp = h; gotMax = m; }
            GameState.OnHPChanged += Handler;

            _gs.Damage("Iker", 150); // más que su HP máximo

            Assert.AreEqual("Iker", gotMember);
            Assert.AreEqual(0, gotHp);
            Assert.AreEqual(100, gotMax);
            GameState.OnHPChanged -= Handler;
        }

        [Test]
        public void Heal_ClampsToMaxHp()
        {
            _gs.Damage("Iker", 40); // HP = 60
            _gs.Heal("Iker", 999);
            _gs.TryGetHp("Iker", out var hp, out _);
            Assert.AreEqual(100, hp);
        }

        [Test]
        public void LevelUp_IncreasesMaxHpAndAtk_AndRestoresHp()
        {
            _gs.Damage("Iker", 50); // HP = 50
            _gs.LevelUp("Iker");
            var m = _gs.GetMember("Iker");
            Assert.AreEqual(115, m.MaxHp); // 100 + 15
            Assert.AreEqual(40, m.Atk);    // 35 + 5
            Assert.AreEqual(115, m.Hp);    // restaurado al máximo
        }

        [Test]
        public void Flags_SetAndCheck()
        {
            Assert.IsFalse(_gs.HasFlag("hasSword"));
            _gs.SetFlag("hasSword");
            Assert.IsTrue(_gs.HasFlag("hasSword"));
            _gs.ClearFlag("hasSword");
            Assert.IsFalse(_gs.HasFlag("hasSword"));
        }

        [Test]
        public void Inventory_AddConsumeAndDropAll()
        {
            _gs.AddItem("Potion", 2);
            _gs.AddItem("Potion", 1);
            Assert.AreEqual(3, _gs.ItemCount("Potion"));
            Assert.IsTrue(_gs.ConsumeItem("Potion", 2));
            Assert.AreEqual(1, _gs.ItemCount("Potion"));
            Assert.IsFalse(_gs.ConsumeItem("Potion", 5)); // no alcanza
            _gs.AddItem("HealingFruit", 2);
            _gs.DropAll("Potion", "HealingFruit");
            Assert.AreEqual(0, _gs.ItemCount("Potion"));
            Assert.AreEqual(0, _gs.ItemCount("HealingFruit"));
        }

        [Test]
        public void Biome_FiresEventOnChange()
        {
            Biome? got = null;
            void Handler(Biome b) { got = b; }
            GameState.OnBiomeChanged += Handler;
            _gs.SetBiome(Biome.Forest);
            Assert.AreEqual(Biome.Forest, got);
            // No vuelve a disparar si no cambia.
            got = null;
            _gs.SetBiome(Biome.Forest);
            Assert.IsNull(got);
            GameState.OnBiomeChanged -= Handler;
        }
    }
}
