using NUnit.Framework;
using TapLegends.Core;

namespace TapLegends.Tests
{
    /// <summary>
    /// Las formulas de balance son el codigo mas critico de proteger con tests: son las
    /// que se retocaron varias veces por feedback de sensacion de juego (ver
    /// docs/ludo/*.md) — un cambio sin querer en un exponente ahi es silencioso hasta que
    /// alguien juega horas y "algo se siente raro".
    /// </summary>
    public class GameConstantsTests
    {
        private const double Tolerance = 1e-6;

        [Test]
        public void EnemyHealthForLevel_Nivel1_EsLaVidaBase()
        {
            Assert.That(GameConstants.EnemyHealthForLevel(1), Is.EqualTo(GameConstants.EnemyBaseHealth).Within(Tolerance));
        }

        [Test]
        public void EnemyHealthForLevel_CreceMultiplicativamentePorNivel()
        {
            double nivel1 = GameConstants.EnemyHealthForLevel(1);
            double nivel2 = GameConstants.EnemyHealthForLevel(2);
            double nivel10 = GameConstants.EnemyHealthForLevel(10);

            Assert.That(nivel2, Is.EqualTo(nivel1 * GameConstants.EnemyHealthGrowth).Within(Tolerance));
            Assert.That(nivel10, Is.EqualTo(GameConstants.EnemyBaseHealth * System.Math.Pow(GameConstants.EnemyHealthGrowth, 9)).Within(Tolerance));
        }

        [Test]
        public void EnemyGoldForLevel_Nivel1_EsElOroBase()
        {
            Assert.That(GameConstants.EnemyGoldForLevel(1), Is.EqualTo(GameConstants.EnemyBaseGold).Within(Tolerance));
        }

        [Test]
        public void EnemyGoldForLevel_CreceMultiplicativamentePorNivel()
        {
            double nivel1 = GameConstants.EnemyGoldForLevel(1);
            double nivel5 = GameConstants.EnemyGoldForLevel(5);

            Assert.That(nivel5, Is.EqualTo(nivel1 * System.Math.Pow(GameConstants.EnemyGoldGrowth, 4)).Within(Tolerance));
        }

        [Test]
        public void TapUpgradeCost_Nivel0_EsElCosteBase()
        {
            Assert.That(GameConstants.TapUpgradeCost(0), Is.EqualTo(GameConstants.TapUpgradeBaseCost).Within(Tolerance));
        }

        [Test]
        public void TapUpgradeCost_CreceMultiplicativamentePorNivel()
        {
            double nivel0 = GameConstants.TapUpgradeCost(0);
            double nivel3 = GameConstants.TapUpgradeCost(3);

            Assert.That(nivel3, Is.EqualTo(nivel0 * System.Math.Pow(GameConstants.TapUpgradeCostGrowth, 3)).Within(Tolerance));
        }

        [Test]
        public void RelicsForLevel_AntesDelMinimoDePrestigio_EsCero()
        {
            Assert.That(GameConstants.RelicsForLevel(GameConstants.PrestigeMinLevel - 1), Is.EqualTo(0d));
        }

        [Test]
        public void RelicsForLevel_EnElMinimoDePrestigio_NoEsCero()
        {
            // No hay una razon de diseño para permitir Renacer sin ganar nada — si esto
            // da 0, el boton "Renacer" apenas se habilita seria una trampa.
            Assert.That(GameConstants.RelicsForLevel(GameConstants.PrestigeMinLevel), Is.GreaterThan(0d));
        }

        [Test]
        public void RelicsForLevel_MasNivelSiempreDaMasOIgualesReliquias()
        {
            double bajo = GameConstants.RelicsForLevel(GameConstants.PrestigeMinLevel);
            double alto = GameConstants.RelicsForLevel(GameConstants.PrestigeMinLevel + 100);

            Assert.That(alto, Is.GreaterThan(bajo));
        }

        [Test]
        public void EnemyHealthGrowth_EsMayorQueEnemyGoldGrowth()
        {
            // Decision explicita de la iteracion ludologica L1/L4 (ver comentarios en
            // GameConstants.cs): la vida tiene que crecer MAS rapido que el oro, o la
            // dificultad colapsa apenas Prestigio/Equipo/Mascotas aportan multiplicadores
            // de daño extra. Si alguien "arregla" esto igualandolos sin leer el porque,
            // este test lo va a marcar.
            Assert.That(GameConstants.EnemyHealthGrowth, Is.GreaterThan(GameConstants.EnemyGoldGrowth));
        }
    }
}
