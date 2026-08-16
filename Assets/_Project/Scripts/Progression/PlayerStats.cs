using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>Daño de tap del jugador y su mejora. DPS pasivo de heroes esta en HeroRoster.</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        public int TapUpgradeLevel { get; private set; } = 0;

        // Multiplicativo (StartingTapDamage * multiplier^nivel), no aditivo — tiene que
        // componer contra la vida del enemigo, que tambien crece exponencial (ver GameConstants).
        // Los multiplicadores de prestigio y equipo se aplican ENCIMA: las mejoras
        // permanentes de Renacer y el arma equipada hacen mas fuerte el tap para siempre.
        public double TapDamage =>
            GameConstants.StartingTapDamage * System.Math.Pow(GameConstants.TapUpgradeDamageMultiplier, TapUpgradeLevel) * ExternalDamageMultiplier;

        public double NextUpgradeCost => GameConstants.TapUpgradeCost(TapUpgradeLevel);
        public double NextTapDamage =>
            GameConstants.StartingTapDamage * System.Math.Pow(GameConstants.TapUpgradeDamageMultiplier, TapUpgradeLevel + 1) * ExternalDamageMultiplier;

        private static double ExternalDamageMultiplier =>
            (PrestigeManager.Instance != null ? PrestigeManager.Instance.DamageMultiplier : 1d) *
            (EquipmentManager.Instance != null ? EquipmentManager.Instance.TapDamageMultiplier : 1d) *
            (PetManager.Instance != null ? PetManager.Instance.TapDamageMultiplier : 1d) *
            (SkillManager.Instance != null ? SkillManager.Instance.TapBuffMultiplier : 1d);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Intenta comprar la siguiente mejora de daño de tap. Devuelve si tuvo exito.</summary>
        public bool TryUpgradeTapDamage()
        {
            double cost = NextUpgradeCost;
            if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(cost)) return false;

            TapUpgradeLevel++;
            EventBus.Publish(new TapDamageUpgradedEvent { NewTapDamage = TapDamage, NewLevel = TapUpgradeLevel });
            return true;
        }

        /// <summary>Solo para el menu de debug: convierte el daño deseado al nivel de mejora equivalente.</summary>
        public void DebugSetTapDamage(double value)
        {
            if (value <= GameConstants.StartingTapDamage)
            {
                TapUpgradeLevel = 0;
                return;
            }

            double level = System.Math.Log(value / GameConstants.StartingTapDamage) /
                           System.Math.Log(GameConstants.TapUpgradeDamageMultiplier);
            TapUpgradeLevel = Mathf.Max(0, Mathf.CeilToInt((float)level));
        }

        /// <summary>Restaura el nivel de mejora desde un guardado (TapDamage se recalcula solo).</summary>
        public void LoadState(int savedUpgradeLevel)
        {
            TapUpgradeLevel = Mathf.Max(0, savedUpgradeLevel);
        }
    }
}
