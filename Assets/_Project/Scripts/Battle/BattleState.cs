using UnityEngine;

namespace DreamHacks.Battle
{
    /// <summary>
    /// Estado estático que persiste entre la escena del overworld y la escena
    /// Battle. Al tocar un enemigo se escriben estos campos y se carga Battle;
    /// al salir de Battle (win/lose/huir), BattleReturn los lee para restaurar
    /// la posición del Player y, si corresponde, destruir el enemigo tocado.
    /// </summary>
    public static class BattleState
    {
        public static bool InBattle;
        public static string EnemyId;             // clave en stats.json
        public static string EnemyGameObjectName; // nombre del GO en el overworld (único)
        public static string ReturnScene;
        public static Vector3 ReturnPosition;
        public static string PendingDestroyEnemyName; // enemigo a destruir al volver
        public static int CurrentIkerHP = -1;     // CUARTOS (10 corazones = 40 cuartos). -1 = sin inicializar (= full).

        // Si true, al volver al overworld el Player es reposicionado al spawn
        // inicial (p. ej. tras morir), no a la posición donde tocó al enemigo.
        public static bool VolverAInicio;

        // Inventario de pociones (persistente entre overworld y Battle).
        public static int InvRojas;
        public static int InvAzules;
        public static int InvRosas;

        // Progreso: cada victoria suma 1 EXP; el nivel es 1 + EXP.
        public static int PlayerExp;
        public static int PlayerNivel => 1 + PlayerExp;

        public static void StartEncounter(string enemyId, string enemyGoName, string returnScene, Vector3 returnPos)
        {
            InBattle = true;
            EnemyId = enemyId;
            EnemyGameObjectName = enemyGoName;
            ReturnScene = returnScene;
            ReturnPosition = returnPos;
            PendingDestroyEnemyName = null;
        }

        public static void EndWin()
        {
            PendingDestroyEnemyName = EnemyGameObjectName;
            InBattle = false;
        }

        public static void EndLose()
        {
            PendingDestroyEnemyName = null;
            CurrentIkerHP = -1; // reset HP
            VolverAInicio = true;
            InBattle = false;
        }

        public static void EndFlee()
        {
            PendingDestroyEnemyName = null;
            InBattle = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStaticsOnDomainReload()
        {
            InBattle = false;
            EnemyId = null;
            EnemyGameObjectName = null;
            ReturnScene = null;
            ReturnPosition = Vector3.zero;
            PendingDestroyEnemyName = null;
            CurrentIkerHP = -1;
            VolverAInicio = false;
            InvRojas = 0;
            InvAzules = 0;
            InvRosas = 0;
            PlayerExp = 0;
        }
    }
}
