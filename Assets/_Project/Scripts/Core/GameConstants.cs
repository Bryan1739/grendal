namespace TapLegends.Core
{
    /// <summary>
    /// Constantes y formulas de balance de la v0.1 (ver TapLegends-Plan.md, Etapa 2).
    /// Numeros de arranque; se reemplazan por curvas afinadas en hoja de calculo en la Etapa 11.
    /// </summary>
    public static class GameConstants
    {
        public const int PixelsPerUnit = 32;
        public const int ReferenceWidth = 720;
        public const int ReferenceHeight = 1280;

        public const int EnemiesPerStage = 10;
        public const float BossTimerSeconds = 30f;

        // Vida enemigo(nivel) = 10 * 1.15^(nivel-1)
        // Subido de 1.12 a 1.15: cuando se fijo 1.12, el unico multiplicador de daño del
        // jugador era la mejora de tap. Desde entonces se sumaron Prestigio, Equipo,
        // Mascotas y Habilidades — 4 multiplicadores independientes mas apilados sobre el
        // mismo numero. El enemigo tiene que crecer mas para seguirle el ritmo a esa
        // composicion, o la dificultad colapsa apenas esos sistemas tienen algo de
        // inversion (feedback del jugador: "pasa de muy dificil a muy facil muy rapido").
        public const double EnemyBaseHealth = 10d;
        public const double EnemyHealthGrowth = 1.15d;

        // Oro por enemigo(nivel) = 1.5 * 1.12^(nivel-1) — bajado de 1.14: por la misma razon
        // que la vida, pero al reves. Armadura, mascotas, Codicia (prestigio) y Bendicion
        // Dorada son 4 multiplicadores de oro extra que no existian cuando se fijo 1.14 —
        // la inflacion real que siente el jugador ya viene de ahi, no hace falta que la
        // curva base tambien corra rapido.
        public const double EnemyBaseGold = 1.5d;
        public const double EnemyGoldGrowth = 1.12d;

        // El jefe de oleada es mas duro y da mas recompensa que un enemigo comun del mismo nivel.
        public const double BossHealthMultiplier = 8d;
        public const double BossGoldMultiplier = 6d;

        // Coste mejora(n) = costeBase * 1.07^n
        public const double TapUpgradeBaseCost = 10d;
        public const double TapUpgradeCostGrowth = 1.07d;

        // Daño de tap(nivel) = StartingTapDamage * 1.10^nivel — multiplicativo, no aditivo
        // (ver nota de EnemyHealthGrowth: un +1 fijo no compone contra vida exponencial).
        public const double TapUpgradeDamageMultiplier = 1.10d;

        // Subido de 1 a 3: contra EnemyBaseHealth=10, un tap de daño 1 significaba 10 toques
        // para el primerisimo enemigo del juego sin ninguna mejora comprada todavia — la peor
        // proporcion daño/vida de toda la partida, justo en el momento en que se forma la
        // primera impresion (feedback del jugador: "el inicio se siente mas pesado de lo
        // que deberia"). Con 3 baja a ~4 toques, mucho mas cerca del "primera mejora ~15s"
        // que ya pedia el plan original.
        public const double StartingTapDamage = 3d;

        public static double EnemyHealthForLevel(int level) =>
            EnemyBaseHealth * System.Math.Pow(EnemyHealthGrowth, level - 1);

        public static double EnemyGoldForLevel(int level) =>
            EnemyBaseGold * System.Math.Pow(EnemyGoldGrowth, level - 1);

        public static double TapUpgradeCost(int currentUpgradeLevel) =>
            TapUpgradeBaseCost * System.Math.Pow(TapUpgradeCostGrowth, currentUpgradeLevel);

        // --- Guardado y progreso offline (Etapa 4) ---
        public const float AutosaveIntervalSeconds = 30f;
        public const float MaxOfflineSeconds = 8 * 3600f; // tope de 8h, ver TapLegends-Plan.md
        public const double OfflineDpsFactor = 0.5d; // los heroes rinden la mitad offline que en pantalla

        // --- Prestigio (Etapa 5) ---
        public const int PrestigeMinLevel = 30; // ver TapLegends-Plan.md, Etapa 5

        // Reliquias ganadas = 5 * (nivelMaximo/15)^1.4 — formula de arranque del plan.
        public static double RelicsForLevel(int maxLevel) =>
            maxLevel < PrestigeMinLevel ? 0d : 5d * System.Math.Pow(maxLevel / 15d, 1.4d);
    }
}
