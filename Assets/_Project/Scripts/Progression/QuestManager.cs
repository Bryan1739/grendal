using System;
using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// 3 misiones diarias elegidas al azar de un pool, con progreso que se reinicia solo
    /// cuando cambia la fecha (calendario real, no un timer de sesion). Sin castigo por no
    /// completarlas — si no se reclaman, simplemente se pierden al otro dia (ver
    /// TapLegends-Plan.md, Etapa 9 y la auditoria etica de L4: nada de urgencia artificial).
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        private const int ActiveQuestCount = 3;

        [SerializeField] private QuestData[] questPool = System.Array.Empty<QuestData>();

        private readonly QuestData[] activeQuests = new QuestData[ActiveQuestCount];
        private readonly double[] progress = new double[ActiveQuestCount];
        private readonly bool[] claimed = new bool[ActiveQuestCount];
        private string currentDayKey = "";

        public void Configure(QuestData[] roster) => questPool = roster ?? System.Array.Empty<QuestData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            EnsureTodaysQuests();
        }

        private void Update()
        {
            // Revisa el cambio de dia una vez por minuto; no hace falta mas seguido que eso.
            if (Time.frameCount % 3600 == 0) EnsureTodaysQuests();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Subscribe<SkillActivatedEvent>(HandleSkillActivated);
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Unsubscribe<SkillActivatedEvent>(HandleSkillActivated);
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            AddProgress(QuestObjective.KillEnemies, 1d);
            if (evt.WasBoss) AddProgress(QuestObjective.KillBosses, 1d);
        }

        private void HandleSkillActivated(SkillActivatedEvent evt) => AddProgress(QuestObjective.UseSkills, 1d);

        private void HandleGoldChanged(GoldChangedEvent evt)
        {
            if (evt.Delta > 0d) AddProgress(QuestObjective.EarnGold, evt.Delta);
        }

        private void AddProgress(QuestObjective objective, double amount)
        {
            bool changed = false;
            for (int i = 0; i < ActiveQuestCount; i++)
            {
                if (activeQuests[i] == null || claimed[i] || activeQuests[i].objective != objective) continue;
                progress[i] = Math.Min(activeQuests[i].targetAmount, progress[i] + amount);
                changed = true;
            }
            if (changed) EventBus.Publish(new QuestsChangedEvent());
        }

        private void EnsureTodaysQuests()
        {
            string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
            if (todayKey == currentDayKey) return;

            currentDayKey = todayKey;
            AssignNewQuests(new System.Random(todayKey.GetHashCode()));
        }

        private void AssignNewQuests(System.Random rng)
        {
            if (questPool == null || questPool.Length == 0) return;

            var pool = new List<QuestData>(questPool);
            for (int i = 0; i < ActiveQuestCount; i++)
            {
                if (pool.Count == 0) pool.AddRange(questPool); // pool chico: permite repetidas antes que dejar slots vacios

                int index = rng.Next(pool.Count);
                activeQuests[i] = pool[index];
                pool.RemoveAt(index);
                progress[i] = 0d;
                claimed[i] = false;
            }

            EventBus.Publish(new QuestsChangedEvent());
        }

        public QuestData GetQuest(int slot) => slot >= 0 && slot < ActiveQuestCount ? activeQuests[slot] : null;
        public double GetProgress(int slot) => slot >= 0 && slot < ActiveQuestCount ? progress[slot] : 0d;
        public bool IsClaimed(int slot) => slot >= 0 && slot < ActiveQuestCount && claimed[slot];

        public bool IsComplete(int slot) =>
            slot >= 0 && slot < ActiveQuestCount && activeQuests[slot] != null && progress[slot] >= activeQuests[slot].targetAmount;

        public bool TryClaim(int slot)
        {
            if (!IsComplete(slot) || IsClaimed(slot)) return false;

            claimed[slot] = true;
            GoldWallet.Instance?.Add(activeQuests[slot].rewardGold);
            EventBus.Publish(new QuestsChangedEvent());
            return true;
        }

        public QuestSaveData GetSaveData()
        {
            var entries = new List<QuestSlotSaveEntry>();
            for (int i = 0; i < ActiveQuestCount; i++)
            {
                entries.Add(new QuestSlotSaveEntry
                {
                    questId = activeQuests[i] != null ? activeQuests[i].id : "",
                    progress = progress[i],
                    claimed = claimed[i],
                });
            }
            return new QuestSaveData { dayKey = currentDayKey, slots = entries };
        }

        public void LoadState(QuestSaveData saved)
        {
            if (saved == null || string.IsNullOrEmpty(saved.dayKey)) return;

            // Si el guardado es de un dia anterior, EnsureTodaysQuests() (llamada en Start,
            // despues de este LoadState) va a pisar esto con misiones nuevas — correcto.
            currentDayKey = saved.dayKey;
            for (int i = 0; i < ActiveQuestCount && i < saved.slots.Count; i++)
            {
                var entry = saved.slots[i];
                activeQuests[i] = System.Array.Find(questPool, q => q != null && q.id == entry.questId);
                progress[i] = entry.progress;
                claimed[i] = entry.claimed;
            }
            EventBus.Publish(new QuestsChangedEvent());
        }
    }
}
