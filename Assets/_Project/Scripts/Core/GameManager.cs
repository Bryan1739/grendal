using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TapLegends.Combat;
using TapLegends.Progression;
using TapLegends.UI;
using UnityEngine;

namespace TapLegends.Core
{
    /// <summary>
    /// Punto de entrada de la escena Game. Orquesta la carga al iniciar (incluido el
    /// progreso offline), el autoguardado periodico y el guardado al pausar/cerrar
    /// (ver TapLegends-Plan.md, Etapa 4). No guarda el sistema de nadie mas: solo pide
    /// y entrega datos a traves de LoadState()/getters de cada sistema.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private FeatureFlags featureFlags;

        public FeatureFlags Flags => featureFlags;

        public void SetFeatureFlags(FeatureFlags flags) => featureFlags = flags;

        private void Awake()
        {
            Instance = this;

            // Ver Etapa 12 del plan: fijar el frame rate desde el dia 1 evita sorpresas de rendimiento mas tarde.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            LoadGame();
            StartCoroutine(AutosaveRoutine());
        }

        private void LoadGame()
        {
            var data = SaveSystem.Load();
            if (data == null) return; // partida nueva, nada que restaurar

            GoldWallet.Instance?.LoadState(data.gold);
            PlayerStats.Instance?.LoadState(data.tapUpgradeLevel);
            EnemySpawner.Instance?.LoadState(data.enemyLevel, data.killsInStage, data.hasLostBossOnce);
            HeroRoster.Instance?.LoadState(data.heroes);
            SkillManager.Instance?.LoadState(data.skills);
            PrestigeManager.Instance?.LoadState(data.relics, data.prestigeUpgrades);
            EquipmentManager.Instance?.LoadState(data.equipment);
            PetManager.Instance?.LoadState(data.pets);
            QuestManager.Instance?.LoadState(data.quests);
            AchievementManager.Instance?.LoadState(data.achievements);
            TutorialManager.Instance?.LoadState(data.tutorialCompleted);
            ProfileManager.Instance?.LoadState(data.profileUsername, data.profileBio, data.profileSkins);
            WeaponSkinManager.Instance?.LoadState(data.weaponSkinId);
            BackgroundController.Instance?.LoadState(data.backgroundChapter, data.backgroundOrder, data.backgroundPosition);

            float secondsAway = ComputeElapsedSeconds(data);
            double offlineGold = ComputeOfflineGold(data, secondsAway);
            if (offlineGold > 0d)
            {
                GoldWallet.Instance?.Add(offlineGold);
                EventBus.Publish(new OfflineProgressEvent { GoldEarned = offlineGold, SecondsAway = secondsAway });
            }
        }

        private static float ComputeElapsedSeconds(SaveData data)
        {
            if (string.IsNullOrEmpty(data.lastSaveTimestampUtc)) return 0f;
            if (!DateTime.TryParse(data.lastSaveTimestampUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var savedAt))
                return 0f;

            double elapsed = (DateTime.UtcNow - savedAt).TotalSeconds;
            if (elapsed < 0d) elapsed = 0d;
            if (elapsed > GameConstants.MaxOfflineSeconds) elapsed = GameConstants.MaxOfflineSeconds;
            return (float)elapsed;
        }

        /// <summary>Aproximacion: DPS de heroes convertido a oro usando la relacion oro/vida
        /// del nivel donde se guardo (no simula kill por kill offline, seria carisimo y no
        /// hace falta para el proposito de "que valga la pena volver").</summary>
        private static double ComputeOfflineGold(SaveData data, float secondsAway)
        {
            if (secondsAway <= 0f || HeroRoster.Instance == null) return 0d;

            double dps = HeroRoster.Instance.TotalDps;
            if (dps <= 0d) return 0d;

            int level = Mathf.Max(1, data.enemyLevel);
            return dps * secondsAway * GameConstants.OfflineDpsFactor * GoldPerDamageAtLevel(level);
        }

        /// <summary>Oro por punto de daño al nivel dado, con los MISMOS multiplicadores
        /// pasivos que ya aplica un kill activo (ver Enemy.Die): Prestigio, Equipo,
        /// Mascotas. A proposito NO incluye SkillManager.GoldBuffMultiplier (Bendicion
        /// Dorada): es un buff temporal de sesion activa, no una tasa permanente que tenga
        /// sentido seguir aplicando con la app cerrada. Ver docs/ludo/L4-economia-offline.md —
        /// sin esto, el oro offline se quedaba en la tasa BASE mientras el oro activo ya
        /// se beneficiaba de estos 3 multiplicadores, y la brecha entre "jugar activo" y
        /// "dejar el juego" se agrandaba cuanto mas invertia el jugador en esos sistemas.</summary>
        private static double GoldPerDamageAtLevel(int level)
        {
            double health = GameConstants.EnemyHealthForLevel(level);
            if (health <= 0d) return 0d;

            double gold = GameConstants.EnemyGoldForLevel(level);
            if (PrestigeManager.Instance != null) gold *= PrestigeManager.Instance.GoldMultiplier;
            if (EquipmentManager.Instance != null) gold *= EquipmentManager.Instance.GoldMultiplier;
            if (PetManager.Instance != null) gold *= PetManager.Instance.GoldMultiplier;

            return gold / health;
        }

        private IEnumerator AutosaveRoutine()
        {
            var wait = new WaitForSeconds(GameConstants.AutosaveIntervalSeconds);
            while (true)
            {
                yield return wait;
                SaveNow();
            }
        }

        public void SaveNow()
        {
            if (GoldWallet.Instance == null || PlayerStats.Instance == null || EnemySpawner.Instance == null) return;

            var data = new SaveData
            {
                gold = GoldWallet.Instance.Gold,
                tapUpgradeLevel = PlayerStats.Instance.TapUpgradeLevel,
                enemyLevel = EnemySpawner.Instance.CurrentLevel,
                killsInStage = EnemySpawner.Instance.KillsInStage,
                hasLostBossOnce = EnemySpawner.Instance.HasLostBossOnce,
                tutorialCompleted = TutorialManager.Instance != null && TutorialManager.Instance.IsCompleted,
                profileUsername = ProfileManager.Instance != null ? ProfileManager.Instance.Username : "",
                profileBio = ProfileManager.Instance != null ? ProfileManager.Instance.Bio : "",
                profileSkins = ProfileManager.Instance != null ? ProfileManager.Instance.GetSaveEntries() : new List<SkinSaveEntry>(),
                weaponSkinId = WeaponSkinManager.Instance != null ? WeaponSkinManager.Instance.GetSaveId() : "",
                backgroundChapter = BackgroundController.Instance != null ? BackgroundController.Instance.Chapter : 0,
                backgroundOrder = BackgroundController.Instance != null ? BackgroundController.Instance.GetSaveOrder() ?? new List<int>() : new List<int>(),
                backgroundPosition = BackgroundController.Instance != null ? BackgroundController.Instance.GetSavePosition() : 0,
                heroes = HeroRoster.Instance != null ? HeroRoster.Instance.GetSaveEntries() : new List<HeroSaveEntry>(),
                skills = SkillManager.Instance != null ? SkillManager.Instance.GetSaveEntries() : new List<SkillSaveEntry>(),
                relics = PrestigeManager.Instance != null ? PrestigeManager.Instance.Relics : 0d,
                prestigeUpgrades = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetSaveEntries() : new List<PrestigeUpgradeSaveEntry>(),
                equipment = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetSaveEntries() : new List<EquipmentSaveEntry>(),
                pets = PetManager.Instance != null ? PetManager.Instance.GetSaveEntries() : new List<PetSaveEntry>(),
                quests = QuestManager.Instance != null ? QuestManager.Instance.GetSaveData() : new QuestSaveData(),
                achievements = AchievementManager.Instance != null ? AchievementManager.Instance.GetSaveData() : new AchievementSaveData(),
            };

            SaveSystem.Save(data);
        }

        // En Android, la app casi nunca recibe OnApplicationQuit al salir (la manda el SO
        // a lo bruto) — OnApplicationPause es el que realmente importa para no perder progreso.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        /// <summary>Solo para el menu de debug: simula progreso offline sin esperar horas reales.</summary>
        public void DebugSimulateOfflineSeconds(float seconds)
        {
            if (EnemySpawner.Instance == null || HeroRoster.Instance == null) return;

            double dps = HeroRoster.Instance.TotalDps;
            if (dps <= 0d) return;

            double gold = dps * seconds * GameConstants.OfflineDpsFactor * GoldPerDamageAtLevel(EnemySpawner.Instance.CurrentLevel);
            if (gold <= 0d) return;

            GoldWallet.Instance?.Add(gold);
            EventBus.Publish(new OfflineProgressEvent { GoldEarned = gold, SecondsAway = seconds });
        }
    }
}
