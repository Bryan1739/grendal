using System;
using System.Collections.Generic;

namespace TapLegends.Core
{
    /// <summary>
    /// Estado serializable completo de una partida. JsonUtility no soporta DateTime ni
    /// Dictionary, por eso el timestamp es string ISO-8601 y los heroes son una lista
    /// de pares (id, nivel) en vez de un diccionario.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;

        public double gold;
        public int tapUpgradeLevel; // TapDamage se recalcula desde el nivel, no se guarda por separado

        public int enemyLevel;
        public int killsInStage;
        public bool hasLostBossOnce; // una vez true, todos los jefes futuros son opcionales

        public List<HeroSaveEntry> heroes = new List<HeroSaveEntry>();
        public List<SkillSaveEntry> skills = new List<SkillSaveEntry>();
        public List<EquipmentSaveEntry> equipment = new List<EquipmentSaveEntry>();
        public List<PetSaveEntry> pets = new List<PetSaveEntry>();

        public QuestSaveData quests = new QuestSaveData();
        public AchievementSaveData achievements = new AchievementSaveData();

        // Reliquias y mejoras de prestigio sobreviven a los Renaceres — no se tocan ahi,
        // solo se acumulan (ver PrestigeManager.TryPrestige).
        public double relics;
        public List<PrestigeUpgradeSaveEntry> prestigeUpgrades = new List<PrestigeUpgradeSaveEntry>();

        /// <summary>UTC en formato "o" (round-trip). Vacio en una partida nueva.</summary>
        public string lastSaveTimestampUtc = "";
    }

    [Serializable]
    public class HeroSaveEntry
    {
        public string heroId;
        public int level;
    }

    [Serializable]
    public class SkillSaveEntry
    {
        public string skillId;
        public int level;
    }

    [Serializable]
    public class PrestigeUpgradeSaveEntry
    {
        public string upgradeId;
        public int level;
    }

    [Serializable]
    public class EquipmentSaveEntry
    {
        public string equipmentId;
        public int rarity; // (int)EquipmentRarity — Core no puede referenciar Progression
        public int level;
        public int fragments;
    }

    [Serializable]
    public class PetSaveEntry
    {
        public string petId;
        public int level;
        public int fragments;
        public bool isActive;
    }

    [Serializable]
    public class QuestSaveData
    {
        public string dayKey = "";
        public List<QuestSlotSaveEntry> slots = new List<QuestSlotSaveEntry>();
    }

    [Serializable]
    public class QuestSlotSaveEntry
    {
        public string questId;
        public double progress;
        public bool claimed;
    }

    [Serializable]
    public class AchievementSaveData
    {
        public List<AchievementCounterEntry> counters = new List<AchievementCounterEntry>();
        public List<string> unlockedIds = new List<string>();
    }

    [Serializable]
    public class AchievementCounterEntry
    {
        public int objective; // (int)AchievementObjective — Core no puede referenciar Progression
        public double value;
    }
}
