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
        public bool hasLostBossOnce; // true = el jefe actual ya se perdio una vez (opcional hasta vencerlo); se reinicia solo al vencerlo

        // Etapa 10: tutorial de los primeros 3 minutos. Un guardado viejo sin este campo
        // carga en false (default de JsonUtility), asi que un jugador que ya paso esa
        // etapa vuelve a ver el tutorial una vez tras la actualizacion — aceptable,
        // ocurre una sola vez y "Saltar" lo cierra de inmediato.
        public bool tutorialCompleted;

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

        // Perfil (Etapa 10+): nombre y presentacion pensados para cuando exista modo
        // online — hoy son solo texto local. Las skins son variantes de PlaceholderArt,
        // no arte final (ver ProfileManager).
        public string profileUsername = "";
        public string profileBio = "";
        public List<SkinSaveEntry> profileSkins = new List<SkinSaveEntry>();

        // Skin de espada del personaje principal — arte real, guardada aparte de
        // profileSkins porque usa WeaponSkinData.id, no PlaceholderArt (ver WeaponSkinManager).
        public string weaponSkinId = "";

        // Progreso del fondo de escenario (ver BackgroundController/ShuffleBag). Sin esto,
        // cerrar y reabrir la app siempre volvia a arrancar en el fondo 1 — con partidas
        // cortas casi nunca se llegaba a ver variedad real (motivo del reporte del usuario).
        // backgroundOrder vacio en un guardado viejo (de antes de este campo) es valido:
        // ShuffleBag.Restore lo ignora si el tamaño no coincide y arranca un barajo nuevo.
        public int backgroundChapter;
        public List<int> backgroundOrder = new List<int>();
        public int backgroundPosition;

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
        public int activeSlot = -1; // -1 = no equipada, 0/1 = lado (ver PetManager, hasta 2 a la vez)
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

    [Serializable]
    public class SkinSaveEntry
    {
        public string characterId; // ProfileManager.PlayerCharacterId, o el id de un HeroData/PetData
        public int skinIndex;
    }
}
