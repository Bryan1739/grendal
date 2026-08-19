using System.IO;
using System.Linq;
using TapLegends.Combat;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Crea los ScriptableObjects de arranque (Etapas 0/2/3 del plan): los 3 enemigos
    /// comunes y el jefe de bioma del Bosque Quebrado, los 4 heroes (con retrato real,
    /// ver Art/Heroes/), mas el asset unico de FeatureFlags. Nombres tomados del GDD
    /// borrador — cambialos libremente, es solo texto en un asset.
    /// </summary>
    public static class CreateInitialData
    {
        [MenuItem("Tap Legends/Setup/Create Initial Data (Bosque Quebrado)")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/_Project/Data/Enemies");
            EnsureFolder("Assets/_Project/Data/Heroes");
            EnsureFolder("Assets/_Project/Data/Skills");
            EnsureFolder("Assets/_Project/Data/Prestige");
            EnsureFolder("Assets/_Project/Data/Equipment");
            EnsureFolder("Assets/_Project/Data/Pets");
            EnsureFolder("Assets/_Project/Data/Quests");
            EnsureFolder("Assets/_Project/Data/Achievements");
            EnsureFolder("Assets/_Project/Data/WeaponSkins");
            EnsureFolder("Assets/_Project/Settings");

            CreateEnemy("wolf_corrupt", "Lobo Corrupto", EnemyTier.Common);
            CreateEnemy("bandit", "Bandido", EnemyTier.Common);
            CreateEnemy("spider_corrupt", "Araña Corrupta", EnemyTier.Common);

            // Varios jefes en vez de uno solo: EnemySpawner elige uno al azar del pool cada
            // vez que el jugador desafia al jefe de la oleada. Nombres del GDD (docs/GDD.md) —
            // sus biomas todavia no existen como tal, es solo variedad visual por ahora.
            CreateEnemy("grendal_boss", "Grendal, el Lobo Ceniza", EnemyTier.Boss, 96);
            CreateEnemy("morwen_boss", "Morwen, la Dama Palida", EnemyTier.Boss, 96);
            CreateEnemy("aldric_boss", "Sir Aldric, el Perjuro", EnemyTier.Boss, 100);
            CreateEnemy("ymira_boss", "Ymira, Reina de Escarcha", EnemyTier.Boss, 96);
            CreateEnemy("vorkath_boss", "Vorkath, el Oxido Encarnado", EnemyTier.Boss, 104);

            // Costes/DPS son valores de arranque sin balancear (Etapa 11 del plan los afina).
            // dpsGrowth es multiplicativo por nivel (ver analisis L1 en GameConstants.cs) — subido
            // bastante (antes 1.12-1.18) porque con esos valores la mejora por nivel era
            // practicamente imperceptible.
            // weapon/weaponTint alimentan el icono chico de arma (PlaceholderArt.Weapon,
            // sigue siendo procedural) — el retrato principal (artPath) ya es arte real.
            CreateHero("alice_veloz", "Alice, \"La Veloz\"", baseCost: 15d, costGrowth: 1.10d, baseDps: 0.5d, dpsGrowth: 1.20d,
                weapon: WeaponKind.Bow, weaponTint: new Color(0.55f, 0.42f, 0.25f),
                artPath: "Assets/_Project/Art/Heroes/alice_idle.png", attackArtPath: "Assets/_Project/Art/Heroes/alice_attack.png");
            CreateHero("callu_bueno", "Callu, \"El Bueno\"", baseCost: 50d, costGrowth: 1.11d, baseDps: 1.8d, dpsGrowth: 1.25d,
                weapon: WeaponKind.Axe, weaponTint: new Color(0.60f, 0.20f, 0.18f),
                artPath: "Assets/_Project/Art/Heroes/callu_idle.png", attackArtPath: "Assets/_Project/Art/Heroes/callu_attack.png");
            CreateHero("velaryon_justo", "Velaryon, \"El Justo\"", baseCost: 180d, costGrowth: 1.12d, baseDps: 6d, dpsGrowth: 1.30d,
                weapon: WeaponKind.Sword, weaponTint: new Color(0.55f, 0.58f, 0.62f),
                artPath: "Assets/_Project/Art/Heroes/velaryon_idle.png", attackArtPath: "Assets/_Project/Art/Heroes/velaryon_attack.png");
            CreateHero("ruijerd_dragon", "Ruijerd, \"El Dragón\"", baseCost: 600d, costGrowth: 1.13d, baseDps: 20d, dpsGrowth: 1.35d,
                weapon: WeaponKind.Dagger, weaponTint: new Color(0.75f, 0.15f, 0.12f),
                artPath: "Assets/_Project/Art/Heroes/ruijerd_idle.png", attackArtPath: "Assets/_Project/Art/Heroes/ruijerd_attack.png");

            // 5 habilidades para los 5 casilleros de la barra (ver TapLegends-Plan.md, Etapa 8).
            // InstantTapBurst/InstantHeroBurst pegan de una; TimedTapBuff/TimedGoldBuff quedan
            // activas un rato (buffDuration) en vez de golpear.
            CreateSkill("espadazo", "Espadazo", SkillKind.InstantTapBurst,
                damageMultiplier: 10d, multiplierGrowth: 1.15d, buffDuration: 0f,
                cooldown: 8f, baseUpgradeCost: 50d, upgradeCostGrowth: 1.15d,
                iconWeapon: WeaponKind.Sword, iconColor: new Color(1f, 0.55f, 0.2f),
                iconPath: "Assets/_Project/Art/Skills/espadazo.png",
                hotbarIconPath: "Assets/_Project/Art/Skills/espadazo_hotbar.png");
            CreateSkill("golpe_certero", "Golpe Certero", SkillKind.InstantTapBurst,
                damageMultiplier: 20d, multiplierGrowth: 1.18d, buffDuration: 0f,
                cooldown: 18f, baseUpgradeCost: 120d, upgradeCostGrowth: 1.18d,
                iconWeapon: WeaponKind.Dagger, iconColor: new Color(0.85f, 0.85f, 0.95f),
                iconPath: "Assets/_Project/Art/Skills/golpe_certero.png",
                hotbarIconPath: "Assets/_Project/Art/Skills/golpe_certero_hotbar.png");
            CreateSkill("lluvia_flechas", "Lluvia de Flechas", SkillKind.InstantHeroBurst,
                damageMultiplier: 6d, multiplierGrowth: 1.15d, buffDuration: 0f,
                cooldown: 12f, baseUpgradeCost: 90d, upgradeCostGrowth: 1.16d,
                iconWeapon: WeaponKind.Bow, iconColor: new Color(0.45f, 0.75f, 0.40f),
                iconPath: "Assets/_Project/Art/Skills/lluvia_flechas.png",
                hotbarIconPath: "Assets/_Project/Art/Skills/lluvia_flechas_hotbar.png");
            CreateSkill("furia_combate", "Furia de Combate", SkillKind.TimedTapBuff,
                damageMultiplier: 5d, multiplierGrowth: 1.10d, buffDuration: 10f,
                cooldown: 35f, baseUpgradeCost: 150d, upgradeCostGrowth: 1.18d,
                iconWeapon: WeaponKind.Axe, iconColor: new Color(0.90f, 0.25f, 0.20f),
                iconPath: "Assets/_Project/Art/Skills/furia_combate.png",
                hotbarIconPath: "Assets/_Project/Art/Skills/furia_combate_hotbar.png");
            CreateSkill("bendicion_dorada", "Bendición Dorada", SkillKind.TimedGoldBuff,
                damageMultiplier: 3d, multiplierGrowth: 1.10d, buffDuration: 20f,
                cooldown: 45f, baseUpgradeCost: 150d, upgradeCostGrowth: 1.18d,
                iconWeapon: WeaponKind.Staff, iconColor: new Color(0.95f, 0.80f, 0.20f),
                iconPath: "Assets/_Project/Art/Skills/bendicion_dorada.png",
                hotbarIconPath: "Assets/_Project/Art/Skills/bendicion_dorada_hotbar.png");

            // Mejoras permanentes de prestigio (se compran con reliquias, sobreviven a los
            // Renaceres siguientes). valuePerLevel es el bonus POR NIVEL: 0.05 = +5%.
            CreatePrestigeUpgrade("furia_batalla", "Furia de Batalla", PrestigeEffect.GlobalDamage,
                valuePerLevel: 0.05d, baseCost: 1d, costGrowth: 1.20d);
            CreatePrestigeUpgrade("codicia", "Codicia", PrestigeEffect.Gold,
                valuePerLevel: 0.05d, baseCost: 1d, costGrowth: 1.20d);
            CreatePrestigeUpgrade("fuerza_heroica", "Fuerza Heroica", PrestigeEffect.HeroDps,
                valuePerLevel: 0.06d, baseCost: 1d, costGrowth: 1.20d);
            CreatePrestigeUpgrade("reflejos", "Reflejos", PrestigeEffect.SkillCooldownReduction,
                valuePerLevel: 0.03d, baseCost: 2d, costGrowth: 1.25d);

            // Una pieza por slot: la RAREZA equipada es lo que sube con el botin de jefes,
            // no el objeto en si (ver EquipmentManager). valuePerLevel = bonus por nivel de
            // mejora DENTRO de la rareza actual.
            CreateEquipment("weapon_slot", "Espada de Aldemark", EquipmentSlot.Weapon, EquipmentEffect.TapDamage,
                valuePerLevel: 0.03d, baseFragmentCost: 3d, fragmentCostGrowth: 1.12d);
            CreateEquipment("helmet_slot", "Yelmo de Aldemark", EquipmentSlot.Helmet, EquipmentEffect.SkillDamage,
                valuePerLevel: 0.04d, baseFragmentCost: 3d, fragmentCostGrowth: 1.12d);
            CreateEquipment("armor_slot", "Coraza de Aldemark", EquipmentSlot.Armor, EquipmentEffect.Gold,
                valuePerLevel: 0.03d, baseFragmentCost: 3d, fragmentCostGrowth: 1.12d);
            CreateEquipment("boots_slot", "Botas de Aldemark", EquipmentSlot.Boots, EquipmentEffect.HeroDps,
                valuePerLevel: 0.04d, baseFragmentCost: 3d, fragmentCostGrowth: 1.12d);
            CreateEquipment("amulet_slot", "Amuleto de Aldemark", EquipmentSlot.Amulet, EquipmentEffect.SkillCooldown,
                valuePerLevel: 0.02d, baseFragmentCost: 4d, fragmentCostGrowth: 1.14d);

            // Mascotas con arte real (ver Art/Pets/), reemplazando el roster placeholder
            // original. Fenrir/Kitsune/Thing atacan solas ademas del bonus pasivo, el resto
            // solo da bonus (ver PetData.attackDps).
            CreatePet("fenrir", "Fenrir", PetEffect.TapDamage, baseBonus: 0.06d, bonusGrowth: 1.10d,
                attackDps: 1d, attackInterval: 4f, artPath: "Assets/_Project/Art/Pets/fenrir_idle.png",
                attackArtPath: "Assets/_Project/Art/Pets/fenrir_attack.png");
            CreatePet("kitsune", "Kitsune", PetEffect.Gold, baseBonus: 0.06d, bonusGrowth: 1.10d,
                artPath: "Assets/_Project/Art/Pets/kitsune_idle.png",
                attackArtPath: "Assets/_Project/Art/Pets/kitsune_attack.png");
            CreatePet("thing", "Thing", PetEffect.HeroDps, baseBonus: 0.05d, bonusGrowth: 1.10d,
                attackDps: 1.5d, attackInterval: 3.5f, artPath: "Assets/_Project/Art/Pets/thing_idle.png",
                attackArtPath: "Assets/_Project/Art/Pets/thing_attack.png");
            CreatePet("valeryon_pet", "Valeryon", PetEffect.TapDamage, baseBonus: 0.05d, bonusGrowth: 1.13d,
                artPath: "Assets/_Project/Art/Pets/valeryon_pet_idle.png",
                attackArtPath: "Assets/_Project/Art/Pets/valeryon_pet_attack.png");
            CreatePet("khaos", "Khaos", PetEffect.HeroDps, baseBonus: 0.06d, bonusGrowth: 1.14d,
                attackDps: 2d, attackInterval: 3f, artPath: "Assets/_Project/Art/Pets/khaos_idle.png",
                attackArtPath: "Assets/_Project/Art/Pets/khaos_attack.png");

            // Pool de misiones diarias: QuestManager elige 3 al azar de estas 8 cada dia.
            // Sin castigo por no completarlas (ver auditoria etica L4 en el plan).
            CreateQuest("quest_kill_200", "Mata 200 enemigos", QuestObjective.KillEnemies, 200d, 100d);
            CreateQuest("quest_kill_500", "Mata 500 enemigos", QuestObjective.KillEnemies, 500d, 250d);
            CreateQuest("quest_boss_3", "Derrota 3 jefes", QuestObjective.KillBosses, 3d, 200d);
            CreateQuest("quest_boss_8", "Derrota 8 jefes", QuestObjective.KillBosses, 8d, 500d);
            CreateQuest("quest_skill_5", "Usa 5 habilidades", QuestObjective.UseSkills, 5d, 80d);
            CreateQuest("quest_skill_15", "Usa 15 habilidades", QuestObjective.UseSkills, 15d, 200d);
            CreateQuest("quest_gold_500", "Gana 500 de oro", QuestObjective.EarnGold, 500d, 150d);
            CreateQuest("quest_gold_2000", "Gana 2000 de oro", QuestObjective.EarnGold, 2000d, 400d);

            // 30 logros en 4 tiers de dificultad — permanentes, se desbloquean solos.
            CreateAchievementTier("ach_kill", "enemigos derrotados", AchievementCategory.Combat, AchievementObjective.KillEnemies,
                new double[] { 100, 1000, 10000, 100000 }, new double[] { 50, 300, 2000, 15000 });
            CreateAchievementTier("ach_boss", "jefes derrotados", AchievementCategory.Combat, AchievementObjective.KillBosses,
                new double[] { 5, 25, 100 }, new double[] { 200, 1500, 10000 });
            CreateAchievementTier("ach_level", "nivel alcanzado", AchievementCategory.Combat, AchievementObjective.ReachLevel,
                new double[] { 10, 30, 60, 100, 200 }, new double[] { 50, 300, 1000, 4000, 20000 });
            CreateAchievementTier("ach_tapupg", "mejoras de daño de tap", AchievementCategory.Combat, AchievementObjective.UpgradeTapLevel,
                new double[] { 10, 50, 100 }, new double[] { 100, 800, 5000 });
            CreateAchievementTier("ach_gold", "oro ganado en total", AchievementCategory.Wealth, AchievementObjective.EarnGoldLifetime,
                new double[] { 1000, 100000, 10000000, 1000000000 }, new double[] { 100, 2000, 50000, 500000 });
            CreateAchievementTier("ach_heroes", "heroes conseguidos", AchievementCategory.Collection, AchievementObjective.OwnHeroes,
                new double[] { 1, 3 }, new double[] { 50, 500 });
            CreateAchievementTier("ach_equip", "piezas de equipo conseguidas", AchievementCategory.Collection, AchievementObjective.OwnEquipment,
                new double[] { 1, 3, 5 }, new double[] { 80, 400, 1500 });
            CreateAchievementTier("ach_pets", "mascotas conseguidas", AchievementCategory.Collection, AchievementObjective.OwnPets,
                new double[] { 1, 3, 6 }, new double[] { 80, 400, 1500 });
            CreateAchievementTier("ach_prestige", "Renaceres completados", AchievementCategory.Prestige, AchievementObjective.PrestigeCount,
                new double[] { 1, 5, 20 }, new double[] { 300, 3000, 30000 });

            // Skins de espada del personaje principal: arte real provisto por el usuario
            // (recortado de una hoja de sprites), no placeholder. Ver WeaponSkinData.
            CreateWeaponSkin("espada_madera", "Espada de Madera",
                "Assets/_Project/Art/Player/EspadaMadera", "Assets/_Project/Art/Player/Icons/espada_madera_icon.png");
            CreateWeaponSkin("espada_hierro", "Espada de Hierro",
                "Assets/_Project/Art/Player/EspadaHierro", "Assets/_Project/Art/Player/Icons/espada_hierro_icon.png");
            CreateWeaponSkin("espada_hierro_negra", "Espada de Hierro Negra",
                "Assets/_Project/Art/Player/EspadaHierroNegra", "Assets/_Project/Art/Player/Icons/espada_hierro_negra_icon.png");

            CreateFeatureFlags();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateInitialData] Datos de arranque creados en Assets/_Project/Data y Settings.");
        }

        /// <summary>Crea el asset si no existe, o actualiza sus valores si ya existia — asi
        /// re-correr este menu despues de ajustar balance (ver analisis L1) siempre deja los
        /// assets al dia, en vez de dejarlos pegados a los valores con los que se crearon.</summary>
        private static void CreateEnemy(string id, string displayName, EnemyTier tier, int size = 32)
        {
            string path = $"Assets/_Project/Data/Enemies/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<EnemyData>();

            data.id = id;
            data.displayName = displayName;
            data.tier = tier;
            data.placeholderSize = size;

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
        }

        private static void CreateHero(string id, string displayName, double baseCost, double costGrowth,
            double baseDps, double dpsGrowth, WeaponKind weapon, Color weaponTint, string artPath = null, string attackArtPath = null)
        {
            string path = $"Assets/_Project/Data/Heroes/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<HeroData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<HeroData>();

            data.id = id;
            data.displayName = displayName;
            data.baseCost = baseCost;
            data.costGrowth = costGrowth;
            data.baseDps = baseDps;
            data.dpsGrowthMultiplier = dpsGrowth;
            data.weaponKind = weapon;
            data.weaponTint = weaponTint;
            if (!string.IsNullOrEmpty(artPath)) data.art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (!string.IsNullOrEmpty(attackArtPath)) data.attackArt = AssetDatabase.LoadAssetAtPath<Sprite>(attackArtPath);

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);

            if (!string.IsNullOrEmpty(artPath) && data.art == null)
                Debug.LogWarning($"[CreateInitialData] Hero '{id}': no se encontro el sprite en {artPath}.");
            if (!string.IsNullOrEmpty(attackArtPath) && data.attackArt == null)
                Debug.LogWarning($"[CreateInitialData] Hero '{id}': no se encontro el sprite en {attackArtPath}.");
        }

        private static void CreateSkill(string id, string displayName, SkillKind kind, double damageMultiplier,
            double multiplierGrowth, float buffDuration, float cooldown, double baseUpgradeCost, double upgradeCostGrowth,
            WeaponKind iconWeapon, Color iconColor, string iconPath = null, string hotbarIconPath = null)
        {
            string path = $"Assets/_Project/Data/Skills/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<SkillData>();

            data.id = id;
            data.displayName = displayName;
            data.kind = kind;
            data.damageMultiplier = damageMultiplier;
            data.multiplierGrowthPerLevel = multiplierGrowth;
            data.buffDurationSeconds = buffDuration;
            data.cooldownSeconds = cooldown;
            data.baseUpgradeCost = baseUpgradeCost;
            data.upgradeCostGrowth = upgradeCostGrowth;
            data.iconWeapon = iconWeapon;
            data.iconColor = iconColor;
            if (!string.IsNullOrEmpty(iconPath)) data.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (!string.IsNullOrEmpty(hotbarIconPath)) data.hotbarIcon = AssetDatabase.LoadAssetAtPath<Sprite>(hotbarIconPath);

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);

            if (!string.IsNullOrEmpty(iconPath) && data.icon == null)
                Debug.LogWarning($"[CreateInitialData] Skill '{id}': no se encontro el sprite en {iconPath}.");
            if (!string.IsNullOrEmpty(hotbarIconPath) && data.hotbarIcon == null)
                Debug.LogWarning($"[CreateInitialData] Skill '{id}': no se encontro el sprite de barra en {hotbarIconPath}.");
        }

        private static void CreatePrestigeUpgrade(string id, string displayName, PrestigeEffect effect,
            double valuePerLevel, double baseCost, double costGrowth)
        {
            string path = $"Assets/_Project/Data/Prestige/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<PrestigeUpgradeData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<PrestigeUpgradeData>();

            data.id = id;
            data.displayName = displayName;
            data.effect = effect;
            data.valuePerLevel = valuePerLevel;
            data.baseCost = baseCost;
            data.costGrowth = costGrowth;

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
        }

        private static void CreateEquipment(string id, string displayName, EquipmentSlot slot, EquipmentEffect effect,
            double valuePerLevel, double baseFragmentCost, double fragmentCostGrowth)
        {
            string path = $"Assets/_Project/Data/Equipment/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<EquipmentData>();

            data.id = id;
            data.displayName = displayName;
            data.slot = slot;
            data.effect = effect;
            data.baseValuePerLevel = valuePerLevel;
            data.baseFragmentCost = baseFragmentCost;
            data.fragmentCostGrowth = fragmentCostGrowth;

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
        }

        /// <summary>artFolder tiene que contener idle_NN.png, ataque1_NN.png, ataque2_NN.png,
        /// ataque3_NN.png (ver Assets/_Project/Art/Player/) — LoadFrames junta cada set
        /// ordenado por nombre de archivo, que ya viene con padding de 2 digitos.</summary>
        private static void CreateWeaponSkin(string id, string displayName, string artFolder, string iconPath)
        {
            string path = $"Assets/_Project/Data/WeaponSkins/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponSkinData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<WeaponSkinData>();

            data.id = id;
            data.displayName = displayName;
            data.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            data.idleFrames = LoadFrames(artFolder, "idle_");
            data.attack1Frames = LoadFrames(artFolder, "ataque1_");
            data.attack2Frames = LoadFrames(artFolder, "ataque2_");
            data.attack3Frames = LoadFrames(artFolder, "ataque3_");

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);

            if (data.icon == null || data.idleFrames.Length == 0)
                Debug.LogWarning($"[CreateInitialData] WeaponSkin '{id}': faltan sprites en {artFolder} o {iconPath}.");
        }

        private static Sprite[] LoadFrames(string folder, string prefix)
        {
            return AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(prefix))
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToArray();
        }

        private static void CreatePet(string id, string displayName, PetEffect effect, double baseBonus, double bonusGrowth,
            double attackDps = 0d, float attackInterval = 3f, string artPath = null, string attackArtPath = null)
        {
            string path = $"Assets/_Project/Data/Pets/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<PetData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<PetData>();

            data.id = id;
            data.displayName = displayName;
            data.effect = effect;
            data.baseBonusPercent = baseBonus;
            data.bonusGrowthPerLevel = bonusGrowth;
            data.attackDps = attackDps;
            data.attackInterval = attackInterval;
            if (!string.IsNullOrEmpty(artPath)) data.art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (!string.IsNullOrEmpty(attackArtPath)) data.attackArt = AssetDatabase.LoadAssetAtPath<Sprite>(attackArtPath);

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);

            if (!string.IsNullOrEmpty(artPath) && data.art == null)
                Debug.LogWarning($"[CreateInitialData] Pet '{id}': no se encontro el sprite en {artPath}.");
            if (!string.IsNullOrEmpty(attackArtPath) && data.attackArt == null)
                Debug.LogWarning($"[CreateInitialData] Pet '{id}': no se encontro el sprite de ataque en {attackArtPath}.");
        }

        private static void CreateQuest(string id, string description, QuestObjective objective, double target, double rewardGold)
        {
            string path = $"Assets/_Project/Data/Quests/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<QuestData>();

            data.id = id;
            data.description = description;
            data.objective = objective;
            data.targetAmount = target;
            data.rewardGold = rewardGold;

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
        }

        /// <summary>Crea un tier de logros con el mismo objetivo y distinta cantidad — "Mata
        /// 100 enemigos", "Mata 1000 enemigos"... en vez de escribir cada uno a mano.</summary>
        private static void CreateAchievementTier(string idPrefix, string objectiveLabel, AchievementCategory category,
            AchievementObjective objective, double[] targets, double[] rewards)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                string id = $"{idPrefix}_{i}";
                string displayName = $"{FormatCount(targets[i])} {objectiveLabel}";

                string path = $"Assets/_Project/Data/Achievements/{id}.asset";
                var data = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
                bool isNew = data == null;
                if (isNew) data = ScriptableObject.CreateInstance<AchievementData>();

                data.id = id;
                data.displayName = displayName;
                data.category = category;
                data.objective = objective;
                data.targetAmount = targets[i];
                data.rewardGold = rewards[i];

                if (isNew) AssetDatabase.CreateAsset(data, path);
                else EditorUtility.SetDirty(data);
            }
        }

        private static string FormatCount(double value) =>
            value >= 1000000 ? $"{value / 1000000:0.#}M" : value >= 1000 ? $"{value / 1000:0.#}K" : $"{value:0}";

        private static void CreateFeatureFlags()
        {
            const string path = "Assets/_Project/Settings/FeatureFlags.asset";
            if (AssetDatabase.LoadAssetAtPath<FeatureFlags>(path) != null) return;

            var flags = ScriptableObject.CreateInstance<FeatureFlags>();
            AssetDatabase.CreateAsset(flags, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
