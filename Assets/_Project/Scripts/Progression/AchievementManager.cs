using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Contadores de por vida (nunca se reinician, ni con Renacer) contra los que se
    /// comparan los logros. Se desbloquean solos apenas se cumple el objetivo — no hay
    /// "reclamar", la recompensa se da al instante (ver TapLegends-Plan.md, Etapa 9).
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [SerializeField] private AchievementData[] achievements = System.Array.Empty<AchievementData>();

        private readonly Dictionary<AchievementObjective, double> counters = new Dictionary<AchievementObjective, double>();
        private readonly HashSet<string> unlocked = new HashSet<string>();

        public IReadOnlyList<AchievementData> Achievements => achievements;
        public int UnlockedCount => unlocked.Count;
        public int TotalCount => achievements?.Length ?? 0;

        public void Configure(AchievementData[] roster) => achievements = roster ?? System.Array.Empty<AchievementData>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Subscribe<HeroChangedEvent>(HandleHeroChanged);
            EventBus.Subscribe<EquipmentChangedEvent>(HandleEquipmentChanged);
            EventBus.Subscribe<PetChangedEvent>(HandlePetChanged);
            EventBus.Subscribe<PrestigeCompletedEvent>(HandlePrestigeCompleted);
            EventBus.Subscribe<TapDamageUpgradedEvent>(HandleTapUpgraded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Unsubscribe<HeroChangedEvent>(HandleHeroChanged);
            EventBus.Unsubscribe<EquipmentChangedEvent>(HandleEquipmentChanged);
            EventBus.Unsubscribe<PetChangedEvent>(HandlePetChanged);
            EventBus.Unsubscribe<PrestigeCompletedEvent>(HandlePrestigeCompleted);
            EventBus.Unsubscribe<TapDamageUpgradedEvent>(HandleTapUpgraded);
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            Increment(AchievementObjective.KillEnemies, 1d);
            if (evt.WasBoss) Increment(AchievementObjective.KillBosses, 1d);
        }

        private void HandleGoldChanged(GoldChangedEvent evt)
        {
            if (evt.Delta > 0d) Increment(AchievementObjective.EarnGoldLifetime, evt.Delta);
        }

        private void HandleStageStatus(StageStatusEvent evt) => SetIfHigher(AchievementObjective.ReachLevel, evt.Level);
        private void HandleTapUpgraded(TapDamageUpgradedEvent evt) => SetIfHigher(AchievementObjective.UpgradeTapLevel, evt.NewLevel);
        private void HandlePrestigeCompleted(PrestigeCompletedEvent evt) => Increment(AchievementObjective.PrestigeCount, 1d);

        // Estos tres son recuentos ("cuantos tenes ahora"), no contadores incrementales,
        // asi que se recalculan enteros cada vez en lugar de sumar.
        private void HandleHeroChanged(HeroChangedEvent evt) => RecountOwned(AchievementObjective.OwnHeroes,
            HeroRoster.Instance != null ? CountOwnedHeroes() : 0);

        private void HandleEquipmentChanged(EquipmentChangedEvent evt) => RecountOwned(AchievementObjective.OwnEquipment,
            EquipmentManager.Instance != null ? CountOwnedEquipment() : 0);

        private void HandlePetChanged(PetChangedEvent evt) => RecountOwned(AchievementObjective.OwnPets,
            PetManager.Instance != null ? CountOwnedPets() : 0);

        private int CountOwnedHeroes()
        {
            int count = 0;
            foreach (var hero in HeroRoster.Instance.Heroes)
                if (HeroRoster.Instance.GetLevel(hero) > 0) count++;
            return count;
        }

        private int CountOwnedEquipment()
        {
            int count = 0;
            foreach (var item in EquipmentManager.Instance.Items)
                if (EquipmentManager.Instance.IsOwned(item)) count++;
            return count;
        }

        private int CountOwnedPets()
        {
            int count = 0;
            foreach (var pet in PetManager.Instance.Pets)
                if (PetManager.Instance.IsOwned(pet)) count++;
            return count;
        }

        private void Increment(AchievementObjective objective, double amount)
        {
            counters.TryGetValue(objective, out var current);
            counters[objective] = current + amount;
            CheckUnlocks(objective);
        }

        private void SetIfHigher(AchievementObjective objective, double value)
        {
            counters.TryGetValue(objective, out var current);
            if (value <= current) return;
            counters[objective] = value;
            CheckUnlocks(objective);
        }

        private void RecountOwned(AchievementObjective objective, double value) => SetIfHigher(objective, value);

        public double GetProgress(AchievementObjective objective) => counters.TryGetValue(objective, out var v) ? v : 0d;
        public bool IsUnlocked(AchievementData achievement) => achievement != null && unlocked.Contains(achievement.id);

        private void CheckUnlocks(AchievementObjective objective)
        {
            double value = GetProgress(objective);
            foreach (var achievement in achievements)
            {
                if (achievement == null || achievement.objective != objective) continue;
                if (unlocked.Contains(achievement.id)) continue;
                if (value < achievement.targetAmount) continue;

                unlocked.Add(achievement.id);
                GoldWallet.Instance?.Add(achievement.rewardGold);
                EventBus.Publish(new AchievementUnlockedEvent
                {
                    AchievementId = achievement.id,
                    DisplayName = achievement.displayName,
                    RewardGold = achievement.rewardGold,
                });
            }
        }

        /// <summary>Del pool de una categoria, el primero sin desbloquear con menor target
        /// (el "siguiente objetivo mas cercano") — usado por la UI para no listar los 30 juntos.</summary>
        public AchievementData GetNextLocked(AchievementCategory category)
        {
            AchievementData best = null;
            foreach (var achievement in achievements)
            {
                if (achievement == null || achievement.category != category || IsUnlocked(achievement)) continue;
                if (best == null || achievement.targetAmount < best.targetAmount) best = achievement;
            }
            return best;
        }

        public AchievementSaveData GetSaveData()
        {
            var data = new AchievementSaveData();
            foreach (var kvp in counters)
                data.counters.Add(new AchievementCounterEntry { objective = (int)kvp.Key, value = kvp.Value });
            data.unlockedIds.AddRange(unlocked);
            return data;
        }

        public void LoadState(AchievementSaveData saved)
        {
            if (saved == null) return;

            counters.Clear();
            foreach (var entry in saved.counters)
                counters[(AchievementObjective)entry.objective] = entry.value;

            unlocked.Clear();
            foreach (var id in saved.unlockedIds)
                unlocked.Add(id);
        }
    }
}
