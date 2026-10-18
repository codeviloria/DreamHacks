using NUnit.Framework;
using DreamHacks.Core;

namespace DreamHacks.Core.Tests
{
    // Verifica que stats.json se carga desde Resources/Data y que los campos
    // claves tienen los valores que espera el resto del juego. Si el JSON se
    // edita y queda incoherente con las fórmulas, estos tests avisan.
    public class StatsDBTests
    {
        [SetUp]
        public void Setup()
        {
            // Fuerza recarga en cada test: los tests son independientes.
            StatsDB.Reload();
        }

        [Test]
        public void Load_FillsAllTopLevelSections()
        {
            Assert.IsTrue(StatsDB.IsLoaded, "StatsDB.IsLoaded == false tras Reload()");
            Assert.IsNotNull(StatsDB.Data, "StatsDB.Data == null");
            Assert.IsNotNull(StatsDB.Data.Rules, "Rules faltante");
            Assert.IsNotNull(StatsDB.Data.Party, "Party faltante");
            Assert.IsNotNull(StatsDB.Data.Enemies, "Enemies faltante");
            Assert.IsNotNull(StatsDB.Data.Boss, "Boss faltante");
            Assert.IsNotNull(StatsDB.Data.Encounters, "Encounters faltante");
        }

        [Test]
        public void Rules_HasExpectedValues()
        {
            var r = StatsDB.Data.Rules;
            Assert.AreEqual(2, r.DamageVariance.Length);
            Assert.AreEqual(0.85f, r.DamageVariance[0], 0.0001f);
            Assert.AreEqual(1.15f, r.DamageVariance[1], 0.0001f);
            Assert.AreEqual(0.10f, r.MissChance, 0.0001f);
            Assert.AreEqual(0.05f, r.MissChanceHacked, 0.0001f);
            Assert.AreEqual(1, r.LevelupPerBattleWon);
            Assert.AreEqual(30, r.Items["Potion"]);
            Assert.AreEqual(20, r.Items["HealingFruit"]);
            Assert.AreEqual(15, r.Items["Food"]);
            Assert.AreEqual(25, r.HannahHeal.Amount);
            Assert.AreEqual(0.40f, r.HannahHeal.TriggerHpRatio, 0.0001f);
        }

        [Test]
        public void Party_IkerAndHannahPresent()
        {
            var iker = StatsDB.Party("Iker");
            var hannah = StatsDB.Party("Hannah");
            Assert.IsNotNull(iker, "No existe Iker en party");
            Assert.IsNotNull(hannah, "No existe Hannah en party");
            Assert.AreEqual(100, iker.Hp);
            Assert.AreEqual(35, iker.Atk);
            Assert.AreEqual(85, hannah.Hp);
            Assert.AreEqual(25, hannah.Atk);
        }

        [Test]
        public void Enemies_HannahEnemyCannotAttack()
        {
            var h = StatsDB.Enemy("HannahEnemy");
            Assert.IsNotNull(h);
            Assert.AreEqual(200, h.Hp);
            Assert.IsFalse(h.CanAttack ?? true, "HannahEnemy.canAttack debería ser false");
        }

        [Test]
        public void Boss_TwoPhasesWithFloatHp()
        {
            var b = StatsDB.Boss("HackerBoss");
            Assert.IsNotNull(b);
            Assert.AreEqual(2, b.Phases.Length);
            // HP flotantes a propósito: 300.14 / 500.14.
            Assert.AreEqual(300.14f, b.Phases[0].Hp, 0.0001f);
            Assert.AreEqual(500.14f, b.Phases[1].Hp, 0.0001f);
            Assert.AreEqual(3, b.Phases[0].Attacks.Length);
            Assert.AreEqual(2, b.Phases[1].Attacks.Length);
        }

        [Test]
        public void Encounters_LookupWorks()
        {
            Assert.AreEqual(new[] { "Slime" }, StatsDB.Encounter("S1_tutorial"));
            Assert.AreEqual(new[] { "SlimeHacked" }, StatsDB.Encounter("S3_path_punish"));
        }

        [Test]
        public void UnknownIds_ReturnNullOrZero()
        {
            Assert.IsNull(StatsDB.Enemy("Inexistente"));
            Assert.IsNull(StatsDB.Party("Inexistente"));
            Assert.AreEqual(0, StatsDB.ItemHeal("UNKNOWN"));
        }
    }
}
