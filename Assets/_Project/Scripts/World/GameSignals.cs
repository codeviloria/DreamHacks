namespace DreamHacks.World
{
    /// <summary>
    /// Señales globales muy simples compartidas entre sistemas (sin DI).
    /// Pensado como "pizarra" para el jam: bandera booleana que el player
    /// consulta antes de moverse. Si crece, se migra a un bus real o SO events.
    /// </summary>
    public static class GameSignals
    {
        // Cualquier sistema (cámara, diálogo, cutscene) puede bloquear el input
        // del jugador poniendo este flag en true.
        public static bool InputBlocked;
    }
}
