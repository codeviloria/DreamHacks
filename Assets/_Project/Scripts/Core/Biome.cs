namespace DreamHacks.Core
{
    // Identificadores de los cuatro biomas narrativos.
    // Se corresponden 1:1 con las llaves del índice "biomes" de dialogues.json.
    // El valor se usa como string al consultar DialogueDB.Biome(...).
    public enum Biome
    {
        Apartment,
        Forest,
        CorruptedForest,
        Cave
    }

    public static class BiomeExtensions
    {
        // Mapea el enum al id string del JSON (snake_case).
        public static string ToKey(this Biome b)
        {
            switch (b)
            {
                case Biome.Apartment: return "apartment";
                case Biome.Forest: return "forest";
                case Biome.CorruptedForest: return "corrupted_forest";
                case Biome.Cave: return "cave";
                default: return "apartment";
            }
        }
    }
}
