using NUnit.Framework;
using DreamHacks.Core;

namespace DreamHacks.Core.Tests
{
    public class DialogueDBTests
    {
        [SetUp]
        public void Setup()
        {
            DialogueDB.Reload();
        }

        [Test]
        public void Load_FillsAllTopLevelSections()
        {
            Assert.IsTrue(DialogueDB.IsLoaded, "DialogueDB.IsLoaded == false");
            Assert.IsNotNull(DialogueDB.Data.Biomes, "Biomes faltante");
            Assert.IsNotNull(DialogueDB.Data.Speakers, "Speakers faltante");
            Assert.IsNotNull(DialogueDB.Data.BattleLines, "BattleLines faltante");
            Assert.IsNotNull(DialogueDB.Data.Scenes, "Scenes faltante");
        }

        [Test]
        public void Biomes_FourKnownKeys()
        {
            foreach (var key in new[] { "apartment", "forest", "corrupted_forest", "cave" })
            {
                Assert.IsNotNull(DialogueDB.Biome(key), $"No existe el bioma '{key}'");
            }
            // Mapeo del enum debe coincidir con el id del JSON.
            Assert.IsNotNull(DialogueDB.Biome(Biome.CorruptedForest));
            Assert.AreEqual("corrupted_forest", Biome.CorruptedForest.ToKey());
        }

        [Test]
        public void Biomes_CorruptionLevelsMakeSense()
        {
            Assert.AreEqual(0, DialogueDB.Biome("apartment").Corruption);
            Assert.AreEqual(0, DialogueDB.Biome("forest").Corruption);
            Assert.AreEqual(2, DialogueDB.Biome("corrupted_forest").Corruption);
            Assert.AreEqual(3, DialogueDB.Biome("cave").Corruption);
        }

        [Test]
        public void Speakers_HackerIsGlitched()
        {
            var hacker = DialogueDB.Speaker("Hacker");
            Assert.IsNotNull(hacker);
            Assert.IsTrue(hacker.Glitch, "Hacker.glitch debería ser true");
            Assert.AreEqual(0.6f, hacker.BlipPitch, 0.0001f);

            var iker = DialogueDB.Speaker("Iker");
            Assert.AreEqual(1.0f, iker.BlipPitch, 0.0001f);
        }

        [Test]
        public void BattleLines_LineLookup()
        {
            // Una línea cualquiera del set "hacked/idle" debe venir de la lista.
            string[] expected = { "0101010100101010100101", "Help us, Iker... (sobbing)", "Ahhhhhfh... (sob)" };
            var line = DialogueDB.Line("hacked", "idle");
            Assert.Contains(line, expected);
        }

        [Test]
        public void BattleLines_UnknownReturnsEmpty()
        {
            // El loader loguea warning (visible en consola), pero el contrato
            // público aquí es: devolver string vacío sin crashear.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Assert.AreEqual(string.Empty, DialogueDB.Line("NoExiste", "idle"));
            Assert.AreEqual(string.Empty, DialogueDB.Line("hacked", "no_existe_set"));
        }

        [Test]
        public void Scene_ParsesLinesEventsAndSfx()
        {
            var nodes = DialogueDB.Scene("apt_wake");
            Assert.IsNotNull(nodes, "La escena 'apt_wake' debería existir");
            Assert.AreEqual(5, nodes.Count);
            Assert.AreEqual(DialogueNodeKind.Sfx, nodes[0].Kind);
            Assert.AreEqual("alarm", nodes[0].Sfx);
            Assert.AreEqual(DialogueNodeKind.Line, nodes[1].Kind);
            Assert.AreEqual("Iker", nodes[1].Speaker);
            Assert.AreEqual(DialogueNodeKind.Line, nodes[2].Kind);
            Assert.AreEqual("Phone", nodes[2].Speaker);
            Assert.AreEqual(DialogueNodeKind.Event, nodes[4].Kind);
            Assert.AreEqual("tasks:bed,breakfast,plant", nodes[4].EventRaw);
        }

        [Test]
        public void Scene_UnknownReturnsNull()
        {
            Assert.IsNull(DialogueDB.Scene("no_existe_id"));
        }
    }
}
