using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Sub-pestaña "Estadísticas" del Perfil: lee directo los contadores de por
    /// vida que AchievementManager ya lleva (nunca se reinician, ni con Renacer) — no hace
    /// falta un sistema de tracking nuevo, esto es solo mostrar lo que ya existe. Se
    /// refresca al abrir, no en vivo: no hace falta tiempo real para ver estadisticas.</summary>
    public class ProfileStatsPanel : MonoBehaviour
    {
        [SerializeField] private Text statsText;

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            if (statsText == null || AchievementManager.Instance == null) return;

            double kills = AchievementManager.Instance.GetProgress(AchievementObjective.KillEnemies);
            double bosses = AchievementManager.Instance.GetProgress(AchievementObjective.KillBosses);
            double maxLevel = AchievementManager.Instance.GetProgress(AchievementObjective.ReachLevel);
            double goldLifetime = AchievementManager.Instance.GetProgress(AchievementObjective.EarnGoldLifetime);
            double prestiges = AchievementManager.Instance.GetProgress(AchievementObjective.PrestigeCount);
            double heroes = AchievementManager.Instance.GetProgress(AchievementObjective.OwnHeroes);
            double equipment = AchievementManager.Instance.GetProgress(AchievementObjective.OwnEquipment);
            double pets = AchievementManager.Instance.GetProgress(AchievementObjective.OwnPets);

            statsText.text =
                $"Monstruos vencidos: {NumberFormatter.Format(kills)}\n" +
                $"Jefes vencidos: {NumberFormatter.Format(bosses)}\n" +
                $"Nivel máximo alcanzado: {NumberFormatter.Format(maxLevel)}\n" +
                $"Oro ganado en total: {NumberFormatter.Format(goldLifetime)}\n" +
                $"Renaceres: {NumberFormatter.Format(prestiges)}\n" +
                $"Héroes reclutados: {NumberFormatter.Format(heroes)}\n" +
                $"Piezas de equipo conseguidas: {NumberFormatter.Format(equipment)}\n" +
                $"Mascotas conseguidas: {NumberFormatter.Format(pets)}";
        }
    }
}
