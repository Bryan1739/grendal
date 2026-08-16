using System.Linq;
using TapLegends.Combat;
using TapLegends.Core;
using TapLegends.Progression;
using TapLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TapLegends.DebugTools;
#endif

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Construye las escenas Boot y Game por codigo (mas fiable que editar YAML de escena
    /// a mano). Requiere haber corrido antes "Tap Legends/Setup/Create Initial Data".
    /// Vuelve a ejecutarse tantas veces como haga falta: siempre parte de una escena nueva.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";

        [MenuItem("Tap Legends/Setup/Build Boot + Game Scenes (v0.1)")]
        public static void BuildScenes()
        {
            BuildGameScene();
            BuildBootScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/Boot.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Game.unity", true),
            };

            Debug.Log("[SceneBuilder] Boot.unity y Game.unity creadas y añadidas a Build Settings.");
        }

        private static void BuildBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var boot = new GameObject("BootLoader");
            boot.AddComponent<BootLoader>();

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Boot.unity");
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camara ---
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            // --- GameManager ---
            var gmGo = new GameObject("GameManager");
            var gameManager = gmGo.AddComponent<GameManager>();
            var flags = AssetDatabase.LoadAssetAtPath<FeatureFlags>("Assets/_Project/Settings/FeatureFlags.asset");
            if (flags != null) gameManager.SetFeatureFlags(flags);

            // --- Servicios de progresion ---
            var walletGo = new GameObject("GoldWallet");
            walletGo.AddComponent<GoldWallet>();

            var statsGo = new GameObject("PlayerStats");
            statsGo.AddComponent<PlayerStats>();

            // --- Canvas raiz ---
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameConstants.ReferenceWidth, GameConstants.ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystemGo = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // --- Fondo: paisaje montañoso generado por codigo (placeholder hasta Etapa 9) ---
            var backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = backgroundGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = backgroundGo.GetComponent<Image>();
            bgImage.sprite = PlaceholderArt.MountainLandscape("bosque_quebrado");
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;

            var backgroundController = backgroundGo.AddComponent<BackgroundController>();
            AssignSerialized(backgroundController, "backgroundImage", bgImage);

            // --- TopBar: oro + botones de Misiones/Logros ---
            var topBar = CreateStretchPanel(canvasGo.transform, "TopBar", new Vector2(0f, 0.90f), new Vector2(1f, 1f));
            var goldTextGo = CreateText(topBar, "GoldText", "Oro: 0", 36, TextAnchor.MiddleCenter);
            var goldDisplay = goldTextGo.transform.parent.gameObject.AddComponent<GoldDisplay>();
            AssignSerialized(goldDisplay, "goldText", goldTextGo.GetComponent<Text>());

            var questsButtonGo = new GameObject("QuestsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            questsButtonGo.transform.SetParent(topBar, false);
            var questsButtonRect = questsButtonGo.GetComponent<RectTransform>();
            questsButtonRect.anchorMin = new Vector2(0.02f, 0.18f);
            questsButtonRect.anchorMax = new Vector2(0.24f, 0.82f);
            questsButtonRect.offsetMin = Vector2.zero;
            questsButtonRect.offsetMax = Vector2.zero;
            questsButtonGo.GetComponent<Image>().color = new Color(0.20f, 0.30f, 0.22f);
            var questsButtonLabelGo = CreateText(questsButtonGo.transform, "Label", "Misiones", 15, TextAnchor.MiddleCenter);
            var questsButtonLabelRect = questsButtonLabelGo.GetComponent<RectTransform>();
            questsButtonLabelRect.anchorMin = Vector2.zero;
            questsButtonLabelRect.anchorMax = Vector2.one;
            questsButtonLabelRect.offsetMin = Vector2.zero;
            questsButtonLabelRect.offsetMax = Vector2.zero;

            var achievementsButtonGo = new GameObject("AchievementsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            achievementsButtonGo.transform.SetParent(topBar, false);
            var achievementsButtonRect = achievementsButtonGo.GetComponent<RectTransform>();
            achievementsButtonRect.anchorMin = new Vector2(0.76f, 0.18f);
            achievementsButtonRect.anchorMax = new Vector2(0.98f, 0.82f);
            achievementsButtonRect.offsetMin = Vector2.zero;
            achievementsButtonRect.offsetMax = Vector2.zero;
            achievementsButtonGo.GetComponent<Image>().color = new Color(0.30f, 0.26f, 0.12f);
            var achievementsButtonLabelGo = CreateText(achievementsButtonGo.transform, "Label", "Logros: 0/0", 13, TextAnchor.MiddleCenter);
            var achievementsButtonLabelRect = achievementsButtonLabelGo.GetComponent<RectTransform>();
            achievementsButtonLabelRect.anchorMin = Vector2.zero;
            achievementsButtonLabelRect.anchorMax = Vector2.one;
            achievementsButtonLabelRect.offsetMin = Vector2.zero;
            achievementsButtonLabelRect.offsetMax = Vector2.zero;
            var achievementCounter = achievementsButtonGo.AddComponent<AchievementCounterDisplay>();
            AssignSerialized(achievementCounter, "counterText", achievementsButtonLabelGo.GetComponent<Text>());

            // --- Nivel actual / temporizador de jefe (encima del enemigo) ---
            var levelLabelGo = CreateText(canvasGo.transform, "LevelLabel", "Nivel 1 — 0/10", 24, TextAnchor.MiddleCenter);
            var levelRect = levelLabelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0.80f);
            levelRect.anchorMax = new Vector2(0.5f, 0.80f);
            levelRect.anchoredPosition = Vector2.zero;
            levelRect.sizeDelta = new Vector2(300, 40);
            var stageStatus = levelLabelGo.AddComponent<StageStatusDisplay>();
            AssignSerialized(stageStatus, "statusText", levelLabelGo.GetComponent<Text>());

            // --- Boton "Volver a desafiar al jefe" (solo visible cuando esta disponible) ---
            var challengeBossGo = new GameObject("ChallengeBossButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            challengeBossGo.transform.SetParent(canvasGo.transform, false);
            var challengeBossRect = challengeBossGo.GetComponent<RectTransform>();
            challengeBossRect.anchorMin = new Vector2(0.5f, 0.745f);
            challengeBossRect.anchorMax = new Vector2(0.5f, 0.745f);
            challengeBossRect.anchoredPosition = Vector2.zero;
            challengeBossRect.sizeDelta = new Vector2(320, 46);
            challengeBossGo.GetComponent<Image>().color = new Color(0.55f, 0.15f, 0.55f);

            var challengeBossLabelGo = CreateText(challengeBossGo.transform, "Label", "Volver a desafiar al jefe", 18, TextAnchor.MiddleCenter);
            var challengeBossLabelRect = challengeBossLabelGo.GetComponent<RectTransform>();
            challengeBossLabelRect.anchorMin = Vector2.zero;
            challengeBossLabelRect.anchorMax = Vector2.one;
            challengeBossLabelRect.offsetMin = Vector2.zero;
            challengeBossLabelRect.offsetMax = Vector2.zero;

            challengeBossGo.AddComponent<ChallengeBossButton>();

            // --- Area del enemigo ---
            var enemyAreaGo = new GameObject("EnemyArea", typeof(RectTransform));
            enemyAreaGo.transform.SetParent(canvasGo.transform, false);
            var enemyAreaRect = enemyAreaGo.GetComponent<RectTransform>();
            enemyAreaRect.anchorMin = new Vector2(0.5f, 0.55f);
            enemyAreaRect.anchorMax = new Vector2(0.5f, 0.55f);
            enemyAreaRect.anchoredPosition = Vector2.zero;
            enemyAreaRect.sizeDelta = Vector2.zero;

            // --- Personaje del jugador (decorativo: el enemigo es lo que recibe los taps) ---
            var playerGo = new GameObject("PlayerCharacter", typeof(RectTransform), typeof(Image));
            playerGo.transform.SetParent(canvasGo.transform, false);
            var playerRect = playerGo.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.18f, 0.42f);
            playerRect.anchorMax = new Vector2(0.18f, 0.42f);
            playerRect.anchoredPosition = Vector2.zero;
            playerRect.sizeDelta = new Vector2(90, 90);
            var playerImage = playerGo.GetComponent<Image>();
            playerImage.sprite = PlaceholderArt.Creature("player_hero", 48, CreatureRole.Hero);
            playerImage.preserveAspect = true;
            playerImage.raycastTarget = false;

            var playerWeaponGo = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
            playerWeaponGo.transform.SetParent(playerGo.transform, false);
            var playerWeaponRect = playerWeaponGo.GetComponent<RectTransform>();
            playerWeaponRect.anchorMin = new Vector2(0.62f, 0f);
            playerWeaponRect.anchorMax = new Vector2(0.62f, 0f);
            playerWeaponRect.anchoredPosition = new Vector2(18, 10);
            playerWeaponRect.sizeDelta = new Vector2(40, 40);
            var playerWeaponImage = playerWeaponGo.GetComponent<Image>();
            playerWeaponImage.sprite = PlaceholderArt.Weapon("player_hero", WeaponKind.Sword, new Color(0.95f, 0.82f, 0.35f), 28);
            playerWeaponImage.preserveAspect = true;
            playerWeaponImage.raycastTarget = false;

            // --- Heroes visibles alrededor del monstruo (aparecen al comprarlos) ---
            var heroDataAssets = AssetDatabase.FindAssets("t:HeroData", new[] { "Assets/_Project/Data/Heroes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HeroData>)
                .Where(h => h != null)
                .ToArray();

            var heroFieldSlots = new (float x, float y)[]
            {
                (0.84f, 0.42f),
                (0.12f, 0.66f),
                (0.88f, 0.66f),
                (0.10f, 0.50f),
                (0.90f, 0.50f),
                (0.14f, 0.74f),
            };

            for (int i = 0; i < heroDataAssets.Length && i < heroFieldSlots.Length; i++)
            {
                var slot = heroFieldSlots[i];
                var heroFieldGo = new GameObject($"HeroField_{heroDataAssets[i].id}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                heroFieldGo.transform.SetParent(canvasGo.transform, false);
                var heroFieldRect = heroFieldGo.GetComponent<RectTransform>();
                heroFieldRect.anchorMin = new Vector2(slot.x, slot.y);
                heroFieldRect.anchorMax = new Vector2(slot.x, slot.y);
                heroFieldRect.anchoredPosition = Vector2.zero;
                heroFieldRect.sizeDelta = new Vector2(72, 72);
                var heroFieldImage = heroFieldGo.GetComponent<Image>();
                heroFieldImage.sprite = heroDataAssets[i].Art;
                heroFieldImage.preserveAspect = true;
                heroFieldImage.raycastTarget = false;
                heroFieldGo.GetComponent<CanvasGroup>().alpha = 0f; // invisible hasta comprarlo (HeroFieldSprite lo controla)

                var heroFieldWeaponGo = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
                heroFieldWeaponGo.transform.SetParent(heroFieldGo.transform, false);
                var heroFieldWeaponRect = heroFieldWeaponGo.GetComponent<RectTransform>();
                heroFieldWeaponRect.anchorMin = new Vector2(0.62f, 0f);
                heroFieldWeaponRect.anchorMax = new Vector2(0.62f, 0f);
                heroFieldWeaponRect.anchoredPosition = new Vector2(14, 8);
                heroFieldWeaponRect.sizeDelta = new Vector2(32, 32);
                var heroFieldWeaponImage = heroFieldWeaponGo.GetComponent<Image>();
                heroFieldWeaponImage.sprite = heroDataAssets[i].WeaponIcon;
                heroFieldWeaponImage.preserveAspect = true;
                heroFieldWeaponImage.raycastTarget = false;

                var heroFieldSprite = heroFieldGo.AddComponent<HeroFieldSprite>();
                AssignSerialized(heroFieldSprite, "hero", heroDataAssets[i]);
            }

            if (heroDataAssets.Length > heroFieldSlots.Length)
                Debug.LogWarning($"[SceneBuilder] Hay {heroDataAssets.Length} heroes pero solo " +
                                  $"{heroFieldSlots.Length} lugares en el campo de batalla — agrega mas slots en heroFieldSlots.");

            // --- Panel inferior: pestañas Mejoras / Heroes, colapsable para liberar pantalla ---
            var bottomPanel = CreateStretchPanel(canvasGo.transform, "BottomPanel", new Vector2(0f, 0f), new Vector2(1f, 0.34f));
            const float collapseHandleHeight = 36f;
            const float tabBarHeight = 52f;

            // Tira delgada siempre visible: tocarla colapsa/expande el resto del panel.
            var collapseHandleGo = new GameObject("CollapseHandle", typeof(RectTransform), typeof(Image), typeof(Button));
            collapseHandleGo.transform.SetParent(bottomPanel, false);
            var collapseHandleRect = collapseHandleGo.GetComponent<RectTransform>();
            collapseHandleRect.anchorMin = new Vector2(0f, 1f);
            collapseHandleRect.anchorMax = new Vector2(1f, 1f);
            collapseHandleRect.pivot = new Vector2(0.5f, 1f);
            collapseHandleRect.anchoredPosition = Vector2.zero;
            collapseHandleRect.sizeDelta = new Vector2(0f, collapseHandleHeight);
            collapseHandleGo.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.26f);

            var collapseLabelGo = CreateText(collapseHandleGo.transform, "Label", "▼", 20, TextAnchor.MiddleCenter);
            var collapseLabelRect = collapseLabelGo.GetComponent<RectTransform>();
            collapseLabelRect.anchorMin = Vector2.zero;
            collapseLabelRect.anchorMax = Vector2.one;
            collapseLabelRect.offsetMin = Vector2.zero;
            collapseLabelRect.offsetMax = Vector2.zero;

            var collapseButton = collapseHandleGo.GetComponent<Button>();

            // Pestañas: alto fijo, ancladas debajo del handle (no a una fraccion del panel,
            // que se encoge al colapsar).
            var tabBarGo = new GameObject("TabBar", typeof(RectTransform));
            tabBarGo.transform.SetParent(bottomPanel, false);
            var tabBarRect = tabBarGo.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = new Vector2(0f, -collapseHandleHeight);
            tabBarRect.sizeDelta = new Vector2(0f, tabBarHeight);

            var upgradesTabButton = CreateTabButton(tabBarGo.transform, "UpgradesTabButton", "Mejoras", 0, 5);
            var heroesTabButton = CreateTabButton(tabBarGo.transform, "HeroesTabButton", "Heroes", 1, 5);
            var equipmentTabButton = CreateTabButton(tabBarGo.transform, "EquipmentTabButton", "Equipo", 2, 5);
            var petsTabButton = CreateTabButton(tabBarGo.transform, "PetsTabButton", "Mascotas", 3, 5);
            var prestigeTabButton = CreateTabButton(tabBarGo.transform, "PrestigeTabButton", "Prestigio", 4, 5);

            // --- Sub-panel: mejora de daño de tap ---
            var upgradesPanelGo = new GameObject("UpgradesPanel", typeof(RectTransform));
            upgradesPanelGo.transform.SetParent(bottomPanel, false);
            var upgradesPanelRect = upgradesPanelGo.GetComponent<RectTransform>();
            upgradesPanelRect.anchorMin = new Vector2(0f, 0f);
            upgradesPanelRect.anchorMax = new Vector2(1f, 1f);
            upgradesPanelRect.offsetMin = Vector2.zero;
            upgradesPanelRect.offsetMax = new Vector2(0f, -(collapseHandleHeight + tabBarHeight));

            var upgradeButtonGo = new GameObject("UpgradeTapDamageButton", typeof(RectTransform), typeof(Image), typeof(Button));
            upgradeButtonGo.transform.SetParent(upgradesPanelGo.transform, false);
            var upgradeRect = upgradeButtonGo.GetComponent<RectTransform>();
            upgradeRect.anchorMin = new Vector2(0.05f, 0.79f);
            upgradeRect.anchorMax = new Vector2(0.95f, 0.96f);
            upgradeRect.offsetMin = Vector2.zero;
            upgradeRect.offsetMax = Vector2.zero;
            upgradeButtonGo.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f);

            var upgradeLabelGo = CreateText(upgradeButtonGo.transform, "Label", "Mejorar daño de tap", 20, TextAnchor.MiddleCenter);
            var upgradeLabelRect = upgradeLabelGo.GetComponent<RectTransform>();
            upgradeLabelRect.anchorMin = Vector2.zero;
            upgradeLabelRect.anchorMax = Vector2.one;
            upgradeLabelRect.offsetMin = Vector2.zero;
            upgradeLabelRect.offsetMax = Vector2.zero;

            var upgradeButtonComp = upgradeButtonGo.AddComponent<UpgradeTapDamageButton>();
            AssignSerialized(upgradeButtonComp, "label", upgradeLabelGo.GetComponent<Text>());

            // Mejora de cada habilidad, en la misma pestaña que la mejora de daño de tap —
            // una fila chica por habilidad en vez de un boton grande, ahora que son 5.
            var skillDataAssets = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/_Project/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillData>)
                .Where(s => s != null)
                .ToArray();

            float skillRowHeight = 0.75f / Mathf.Max(1, skillDataAssets.Length);
            for (int i = 0; i < skillDataAssets.Length; i++)
            {
                var skillItem = skillDataAssets[i];

                var skillRowGo = new GameObject($"SkillUpgradeRow_{skillItem.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                skillRowGo.transform.SetParent(upgradesPanelGo.transform, false);
                var skillRowRect = skillRowGo.GetComponent<RectTransform>();
                float top = 0.75f - i * skillRowHeight;
                float bottom = 0.75f - (i + 1) * skillRowHeight;
                skillRowRect.anchorMin = new Vector2(0.05f, bottom + 0.008f);
                skillRowRect.anchorMax = new Vector2(0.95f, top - 0.008f);
                skillRowRect.offsetMin = Vector2.zero;
                skillRowRect.offsetMax = Vector2.zero;
                skillRowGo.GetComponent<Image>().color = new Color(0.45f, 0.28f, 0.15f);

                var skillLabelGo = CreateText(skillRowGo.transform, "Label", skillItem.displayName, 15, TextAnchor.MiddleCenter);
                var skillLabelRect = skillLabelGo.GetComponent<RectTransform>();
                skillLabelRect.anchorMin = Vector2.zero;
                skillLabelRect.anchorMax = Vector2.one;
                skillLabelRect.offsetMin = Vector2.zero;
                skillLabelRect.offsetMax = Vector2.zero;

                var skillRowComp = skillRowGo.AddComponent<SkillUpgradeButton>();
                AssignSerialized(skillRowComp, "label", skillLabelGo.GetComponent<Text>());
                AssignSerialized(skillRowComp, "skill", skillItem);
            }

            if (skillDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontro SkillData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // --- Barra de habilidades: hasta 5 casilleros, siempre pegada arriba del panel
            // de Mejoras/Heroes. Es hija de bottomPanel y ancla a su borde superior (anchorMin/
            // Max en y=1) con pivot en y=0, asi que la sigue automaticamente cuando el panel
            // se colapsa o se expande — no hace falta ningun script para "reposicionarla".
            const int skillSlotCount = 5;
            const float skillBarHeight = 64f;
            var skillBarGo = new GameObject("SkillBar", typeof(RectTransform));
            skillBarGo.transform.SetParent(bottomPanel, false);
            var skillBarRect = skillBarGo.GetComponent<RectTransform>();
            skillBarRect.anchorMin = new Vector2(0f, 1f);
            skillBarRect.anchorMax = new Vector2(1f, 1f);
            skillBarRect.pivot = new Vector2(0.5f, 0f);
            skillBarRect.anchoredPosition = new Vector2(0f, 8f);
            skillBarRect.sizeDelta = new Vector2(0f, skillBarHeight);

            float slotWidth = 1f / skillSlotCount;
            for (int i = 0; i < skillSlotCount; i++)
            {
                var slotGo = new GameObject($"SkillSlot_{i}", typeof(RectTransform), typeof(Image));
                slotGo.transform.SetParent(skillBarGo.transform, false);
                var slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(i * slotWidth, 0f);
                slotRect.anchorMax = new Vector2((i + 1) * slotWidth, 1f);
                slotRect.offsetMin = new Vector2(4, 0);
                slotRect.offsetMax = new Vector2(-4, 0);

                var skillData = i < skillDataAssets.Length ? skillDataAssets[i] : null;
                var slotImage = slotGo.GetComponent<Image>();

                if (skillData == null)
                {
                    // Casillero vacio (habilidad futura): solo un cuadro apagado, sin boton.
                    slotImage.color = new Color(0.14f, 0.14f, 0.17f, 0.7f);
                    continue;
                }

                slotGo.AddComponent<Button>();
                slotImage.sprite = skillData.Icon;
                slotImage.preserveAspect = true;
                slotImage.color = Color.white;

                var overlayGo = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
                overlayGo.transform.SetParent(slotGo.transform, false);
                var overlayRect = overlayGo.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                var overlayImage = overlayGo.GetComponent<Image>();
                overlayImage.sprite = PlaceholderArt.SolidPixel();
                overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
                overlayImage.type = Image.Type.Filled;
                overlayImage.fillMethod = Image.FillMethod.Radial360;
                overlayImage.fillOrigin = 2; // Top
                overlayImage.fillClockwise = true;
                overlayImage.fillAmount = 0f;
                overlayImage.raycastTarget = false;

                var slotButton = slotGo.AddComponent<SkillSlotButton>();
                AssignSerialized(slotButton, "skill", skillData);
                AssignSerialized(slotButton, "icon", slotImage);
                AssignSerialized(slotButton, "cooldownOverlay", overlayImage);
            }

            var skillManagerGo = new GameObject("SkillManager");
            var skillManager = skillManagerGo.AddComponent<SkillManager>();
            skillManager.Configure(skillDataAssets);

            // --- Sub-panel: heroes de DPS pasivo ---
            var heroesPanelGo = new GameObject("HeroesPanel", typeof(RectTransform));
            heroesPanelGo.transform.SetParent(bottomPanel, false);
            var heroesPanelRect = heroesPanelGo.GetComponent<RectTransform>();
            heroesPanelRect.anchorMin = new Vector2(0f, 0f);
            heroesPanelRect.anchorMax = new Vector2(1f, 1f);
            heroesPanelRect.offsetMin = Vector2.zero;
            heroesPanelRect.offsetMax = new Vector2(0f, -(collapseHandleHeight + tabBarHeight));
            heroesPanelGo.SetActive(false); // Mejoras es la pestaña visible por defecto

            // heroDataAssets ya se cargo mas arriba, al crear los sprites del campo de batalla.
            float heroRowHeight = 1f / Mathf.Max(1, heroDataAssets.Length);
            for (int i = 0; i < heroDataAssets.Length; i++)
            {
                var heroRowGo = new GameObject($"HeroRow_{heroDataAssets[i].id}", typeof(RectTransform), typeof(Image), typeof(Button));
                heroRowGo.transform.SetParent(heroesPanelGo.transform, false);
                var rowRect = heroRowGo.GetComponent<RectTransform>();
                float top = 1f - i * heroRowHeight;
                float bottom = 1f - (i + 1) * heroRowHeight;
                rowRect.anchorMin = new Vector2(0.03f, bottom + 0.015f);
                rowRect.anchorMax = new Vector2(0.97f, top - 0.015f);
                rowRect.offsetMin = Vector2.zero;
                rowRect.offsetMax = Vector2.zero;
                heroRowGo.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f);

                // Retrato del heroe + arma superpuesta en la esquina (mismo criterio que el
                // personaje del jugador: diferenciar a simple vista sin arte final todavia).
                var heroPortraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                heroPortraitGo.transform.SetParent(heroRowGo.transform, false);
                var heroPortraitRect = heroPortraitGo.GetComponent<RectTransform>();
                heroPortraitRect.anchorMin = new Vector2(0f, 0.5f);
                heroPortraitRect.anchorMax = new Vector2(0f, 0.5f);
                heroPortraitRect.anchoredPosition = new Vector2(40, 0);
                heroPortraitRect.sizeDelta = new Vector2(54, 54);
                var heroPortraitImage = heroPortraitGo.GetComponent<Image>();
                heroPortraitImage.sprite = heroDataAssets[i].Art;
                heroPortraitImage.preserveAspect = true;
                heroPortraitImage.raycastTarget = false;

                var heroWeaponGo = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
                heroWeaponGo.transform.SetParent(heroPortraitGo.transform, false);
                var heroWeaponRect = heroWeaponGo.GetComponent<RectTransform>();
                heroWeaponRect.anchorMin = new Vector2(1f, 0f);
                heroWeaponRect.anchorMax = new Vector2(1f, 0f);
                heroWeaponRect.anchoredPosition = new Vector2(-4, 4);
                heroWeaponRect.sizeDelta = new Vector2(26, 26);
                var heroWeaponImage = heroWeaponGo.GetComponent<Image>();
                heroWeaponImage.sprite = heroDataAssets[i].WeaponIcon;
                heroWeaponImage.preserveAspect = true;
                heroWeaponImage.raycastTarget = false;

                var heroLabelGo = CreateText(heroRowGo.transform, "Label", "", 17, TextAnchor.MiddleLeft);
                var heroLabelRect = heroLabelGo.GetComponent<RectTransform>();
                heroLabelRect.anchorMin = Vector2.zero;
                heroLabelRect.anchorMax = Vector2.one;
                heroLabelRect.offsetMin = new Vector2(78, 0);
                heroLabelRect.offsetMax = new Vector2(-14, 0);

                var heroRow = heroRowGo.AddComponent<HeroRow>();
                AssignSerialized(heroRow, "label", heroLabelGo.GetComponent<Text>());
                AssignSerialized(heroRow, "hero", heroDataAssets[i]);
            }

            var heroRosterGo = new GameObject("HeroRoster");
            var heroRoster = heroRosterGo.AddComponent<HeroRoster>();
            heroRoster.Configure(heroDataAssets);

            if (heroDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron HeroData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // --- Sub-panel: Equipo (5 slots, botin de jefes) ---
            var equipmentPanelGo = new GameObject("EquipmentPanel", typeof(RectTransform));
            equipmentPanelGo.transform.SetParent(bottomPanel, false);
            var equipmentPanelRect = equipmentPanelGo.GetComponent<RectTransform>();
            equipmentPanelRect.anchorMin = new Vector2(0f, 0f);
            equipmentPanelRect.anchorMax = new Vector2(1f, 1f);
            equipmentPanelRect.offsetMin = Vector2.zero;
            equipmentPanelRect.offsetMax = new Vector2(0f, -(collapseHandleHeight + tabBarHeight));
            equipmentPanelGo.SetActive(false); // Mejoras es la pestaña visible por defecto

            var equipmentDataAssets = AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/_Project/Data/Equipment" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EquipmentData>)
                .Where(e => e != null)
                .ToArray();

            float equipmentRowHeight = 1f / Mathf.Max(1, equipmentDataAssets.Length);
            EquipmentData weaponSlotData = null;

            for (int i = 0; i < equipmentDataAssets.Length; i++)
            {
                var equipItem = equipmentDataAssets[i];
                if (equipItem.slot == EquipmentSlot.Weapon) weaponSlotData = equipItem;

                var equipRowGo = new GameObject($"EquipRow_{equipItem.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                equipRowGo.transform.SetParent(equipmentPanelGo.transform, false);
                var equipRowRect = equipRowGo.GetComponent<RectTransform>();
                float top = 1f - i * equipmentRowHeight;
                float bottom = 1f - (i + 1) * equipmentRowHeight;
                equipRowRect.anchorMin = new Vector2(0.03f, bottom + 0.01f);
                equipRowRect.anchorMax = new Vector2(0.97f, top - 0.01f);
                equipRowRect.offsetMin = Vector2.zero;
                equipRowRect.offsetMax = Vector2.zero;
                equipRowGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f);

                var equipIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                equipIconGo.transform.SetParent(equipRowGo.transform, false);
                var equipIconRect = equipIconGo.GetComponent<RectTransform>();
                equipIconRect.anchorMin = new Vector2(0f, 0.5f);
                equipIconRect.anchorMax = new Vector2(0f, 0.5f);
                equipIconRect.anchoredPosition = new Vector2(32, 0);
                equipIconRect.sizeDelta = new Vector2(44, 44);
                var equipIconImage = equipIconGo.GetComponent<Image>();
                equipIconImage.sprite = PlaceholderArt.Icon(equipItem.id, 32, Color.white);
                equipIconImage.preserveAspect = true;
                equipIconImage.raycastTarget = false;

                var equipLabelGo = CreateText(equipRowGo.transform, "Label", "", 15, TextAnchor.MiddleLeft);
                var equipLabelRect = equipLabelGo.GetComponent<RectTransform>();
                equipLabelRect.anchorMin = Vector2.zero;
                equipLabelRect.anchorMax = Vector2.one;
                equipLabelRect.offsetMin = new Vector2(62, 0);
                equipLabelRect.offsetMax = new Vector2(-10, 0);

                var equipRowComp = equipRowGo.AddComponent<EquipmentRow>();
                AssignSerialized(equipRowComp, "label", equipLabelGo.GetComponent<Text>());
                AssignSerialized(equipRowComp, "icon", equipIconImage);
                AssignSerialized(equipRowComp, "item", equipItem);
            }

            var equipmentManagerGo = new GameObject("EquipmentManager");
            var equipmentManager = equipmentManagerGo.AddComponent<EquipmentManager>();
            equipmentManager.Configure(equipmentDataAssets);

            if (equipmentDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron EquipmentData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // El arma del personaje cambia de color segun la rareza equipada (ver PlayerWeaponDisplay).
            if (weaponSlotData != null)
            {
                var playerWeaponDisplay = playerWeaponGo.AddComponent<PlayerWeaponDisplay>();
                AssignSerialized(playerWeaponDisplay, "weaponImage", playerWeaponImage);
                AssignSerialized(playerWeaponDisplay, "weaponItem", weaponSlotData);
            }

            // Aviso temporal de botin, arriba de todo.
            var lootToastPanel = CreateStretchPanel(canvasGo.transform, "LootToastPanel", new Vector2(0.1f, 0.83f), new Vector2(0.9f, 0.895f));
            lootToastPanel.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.05f, 0.95f);
            lootToastPanel.gameObject.SetActive(false);
            var lootToastTextGo = CreateText(lootToastPanel, "Message", "", 16, TextAnchor.MiddleCenter);
            var lootToastComp = lootToastPanel.gameObject.AddComponent<LootToast>();
            AssignSerialized(lootToastComp, "panel", lootToastPanel.gameObject);
            AssignSerialized(lootToastComp, "messageText", lootToastTextGo.GetComponent<Text>());

            // Flash de pantalla completa al activar una habilidad (ver SkillImpactFlash: no
            // es shake de camara porque el Canvas es Overlay y no depende de la camara).
            var skillFlashGo = new GameObject("SkillImpactFlash", typeof(RectTransform), typeof(Image));
            skillFlashGo.transform.SetParent(canvasGo.transform, false);
            var skillFlashRect = skillFlashGo.GetComponent<RectTransform>();
            skillFlashRect.anchorMin = Vector2.zero;
            skillFlashRect.anchorMax = Vector2.one;
            skillFlashRect.offsetMin = Vector2.zero;
            skillFlashRect.offsetMax = Vector2.zero;
            var skillFlashImage = skillFlashGo.GetComponent<Image>();
            skillFlashImage.color = new Color(1f, 0.85f, 0.4f, 0f);
            skillFlashImage.raycastTarget = false;
            var skillFlashComp = skillFlashGo.AddComponent<SkillImpactFlash>();
            AssignSerialized(skillFlashComp, "flashImage", skillFlashImage);

            // --- Sub-panel: Mascotas (coleccion, huevos de jefes) ---
            var petsPanelGo = new GameObject("PetsPanel", typeof(RectTransform));
            petsPanelGo.transform.SetParent(bottomPanel, false);
            var petsPanelRect = petsPanelGo.GetComponent<RectTransform>();
            petsPanelRect.anchorMin = new Vector2(0f, 0f);
            petsPanelRect.anchorMax = new Vector2(1f, 1f);
            petsPanelRect.offsetMin = Vector2.zero;
            petsPanelRect.offsetMax = new Vector2(0f, -(collapseHandleHeight + tabBarHeight));
            petsPanelGo.SetActive(false); // Mejoras es la pestaña visible por defecto

            var petDataAssets = AssetDatabase.FindAssets("t:PetData", new[] { "Assets/_Project/Data/Pets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PetData>)
                .Where(p => p != null)
                .ToArray();

            float petRowHeight = 1f / Mathf.Max(1, petDataAssets.Length);
            for (int i = 0; i < petDataAssets.Length; i++)
            {
                var petItem = petDataAssets[i];

                var petRowGo = new GameObject($"PetRow_{petItem.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                petRowGo.transform.SetParent(petsPanelGo.transform, false);
                var petRowRect = petRowGo.GetComponent<RectTransform>();
                float top = 1f - i * petRowHeight;
                float bottom = 1f - (i + 1) * petRowHeight;
                petRowRect.anchorMin = new Vector2(0.03f, bottom + 0.01f);
                petRowRect.anchorMax = new Vector2(0.97f, top - 0.01f);
                petRowRect.offsetMin = Vector2.zero;
                petRowRect.offsetMax = Vector2.zero;
                petRowGo.GetComponent<Image>().color = new Color(0.14f, 0.20f, 0.16f);

                var petIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                petIconGo.transform.SetParent(petRowGo.transform, false);
                var petIconRect = petIconGo.GetComponent<RectTransform>();
                petIconRect.anchorMin = new Vector2(0f, 0.5f);
                petIconRect.anchorMax = new Vector2(0f, 0.5f);
                petIconRect.anchoredPosition = new Vector2(28, 0);
                petIconRect.sizeDelta = new Vector2(40, 40);
                var petIconImage = petIconGo.GetComponent<Image>();
                petIconImage.sprite = petItem.Art;
                petIconImage.preserveAspect = true;
                petIconImage.raycastTarget = false;

                var petActivateGo = new GameObject("ActivateButton", typeof(RectTransform), typeof(Image), typeof(Button));
                petActivateGo.transform.SetParent(petRowGo.transform, false);
                var petActivateRect = petActivateGo.GetComponent<RectTransform>();
                petActivateRect.anchorMin = new Vector2(1f, 0.5f);
                petActivateRect.anchorMax = new Vector2(1f, 0.5f);
                petActivateRect.anchoredPosition = new Vector2(-52, 0);
                petActivateRect.sizeDelta = new Vector2(88, 34);
                petActivateGo.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.30f);
                var petActivateLabelGo = CreateText(petActivateGo.transform, "Label", "Activar", 13, TextAnchor.MiddleCenter);
                var petActivateLabelRect = petActivateLabelGo.GetComponent<RectTransform>();
                petActivateLabelRect.anchorMin = Vector2.zero;
                petActivateLabelRect.anchorMax = Vector2.one;
                petActivateLabelRect.offsetMin = Vector2.zero;
                petActivateLabelRect.offsetMax = Vector2.zero;

                var petLabelGo = CreateText(petRowGo.transform, "Label", "", 14, TextAnchor.MiddleLeft);
                var petLabelRect = petLabelGo.GetComponent<RectTransform>();
                petLabelRect.anchorMin = Vector2.zero;
                petLabelRect.anchorMax = Vector2.one;
                petLabelRect.offsetMin = new Vector2(56, 0);
                petLabelRect.offsetMax = new Vector2(-108, 0);

                var petRowComp = petRowGo.AddComponent<PetRow>();
                AssignSerialized(petRowComp, "label", petLabelGo.GetComponent<Text>());
                AssignSerialized(petRowComp, "icon", petIconImage);
                AssignSerialized(petRowComp, "activateButton", petActivateGo.GetComponent<Button>());
                AssignSerialized(petRowComp, "activateLabel", petActivateLabelGo.GetComponent<Text>());
                AssignSerialized(petRowComp, "pet", petItem);
            }

            var petManagerGo = new GameObject("PetManager");
            var petManager = petManagerGo.AddComponent<PetManager>();
            petManager.Configure(petDataAssets);

            if (petDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron PetData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // Mascota activa, flotando junto al personaje.
            var petFieldGo = new GameObject("PetField", typeof(RectTransform), typeof(Image));
            petFieldGo.transform.SetParent(canvasGo.transform, false);
            var petFieldRect = petFieldGo.GetComponent<RectTransform>();
            petFieldRect.anchorMin = new Vector2(0.30f, 0.53f);
            petFieldRect.anchorMax = new Vector2(0.30f, 0.53f);
            petFieldRect.anchoredPosition = Vector2.zero;
            petFieldRect.sizeDelta = new Vector2(50, 50);
            var petFieldImage = petFieldGo.GetComponent<Image>();
            petFieldImage.preserveAspect = true;
            petFieldImage.raycastTarget = false;
            petFieldImage.enabled = false;

            var petFieldSprite = petFieldGo.AddComponent<PetFieldSprite>();
            AssignSerialized(petFieldSprite, "icon", petFieldImage);

            // --- Sub-panel: Prestigio (reliquias, Renacer, mejoras permanentes) ---
            var prestigePanelGo = new GameObject("PrestigePanel", typeof(RectTransform));
            prestigePanelGo.transform.SetParent(bottomPanel, false);
            var prestigePanelRect = prestigePanelGo.GetComponent<RectTransform>();
            prestigePanelRect.anchorMin = new Vector2(0f, 0f);
            prestigePanelRect.anchorMax = new Vector2(1f, 1f);
            prestigePanelRect.offsetMin = Vector2.zero;
            prestigePanelRect.offsetMax = new Vector2(0f, -(collapseHandleHeight + tabBarHeight));
            prestigePanelGo.SetActive(false); // Mejoras es la pestaña visible por defecto

            var relicsLabelGo = CreateText(prestigePanelGo.transform, "RelicsLabel", "Reliquias: 0", 20, TextAnchor.MiddleCenter);
            var relicsLabelRect = relicsLabelGo.GetComponent<RectTransform>();
            relicsLabelRect.anchorMin = new Vector2(0f, 0.90f);
            relicsLabelRect.anchorMax = new Vector2(1f, 1f);
            relicsLabelRect.offsetMin = Vector2.zero;
            relicsLabelRect.offsetMax = Vector2.zero;
            var relicsDisplay = relicsLabelGo.AddComponent<RelicsDisplay>();
            AssignSerialized(relicsDisplay, "relicsText", relicsLabelGo.GetComponent<Text>());

            var renacerButtonGo = new GameObject("RenacerButton", typeof(RectTransform), typeof(Image), typeof(Button));
            renacerButtonGo.transform.SetParent(prestigePanelGo.transform, false);
            var renacerRect = renacerButtonGo.GetComponent<RectTransform>();
            renacerRect.anchorMin = new Vector2(0.05f, 0.71f);
            renacerRect.anchorMax = new Vector2(0.95f, 0.89f);
            renacerRect.offsetMin = Vector2.zero;
            renacerRect.offsetMax = Vector2.zero;
            renacerButtonGo.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.10f);

            var renacerLabelGo = CreateText(renacerButtonGo.transform, "Label", "Renacer", 20, TextAnchor.MiddleCenter);
            var renacerLabelRect = renacerLabelGo.GetComponent<RectTransform>();
            renacerLabelRect.anchorMin = Vector2.zero;
            renacerLabelRect.anchorMax = Vector2.one;
            renacerLabelRect.offsetMin = Vector2.zero;
            renacerLabelRect.offsetMax = Vector2.zero;

            var renacerButtonComp = renacerButtonGo.AddComponent<RenacerButton>();
            AssignSerialized(renacerButtonComp, "label", renacerLabelGo.GetComponent<Text>());

            // Modal de confirmacion (Renacer reinicia progreso, no dispara directo del boton).
            var prestigeConfirmPanel = CreateStretchPanel(canvasGo.transform, "PrestigeConfirmPanel", new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.68f));
            prestigeConfirmPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            prestigeConfirmPanel.gameObject.SetActive(false);

            var prestigeConfirmMessageGo = CreateText(prestigeConfirmPanel, "Message", "", 18, TextAnchor.MiddleCenter);
            var prestigeConfirmMessageRect = prestigeConfirmMessageGo.GetComponent<RectTransform>();
            prestigeConfirmMessageRect.anchorMin = new Vector2(0.06f, 0.30f);
            prestigeConfirmMessageRect.anchorMax = new Vector2(0.94f, 0.95f);
            prestigeConfirmMessageRect.offsetMin = Vector2.zero;
            prestigeConfirmMessageRect.offsetMax = Vector2.zero;

            var prestigeConfirmYesGo = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            prestigeConfirmYesGo.transform.SetParent(prestigeConfirmPanel, false);
            var prestigeConfirmYesRect = prestigeConfirmYesGo.GetComponent<RectTransform>();
            prestigeConfirmYesRect.anchorMin = new Vector2(0.08f, 0.06f);
            prestigeConfirmYesRect.anchorMax = new Vector2(0.48f, 0.24f);
            prestigeConfirmYesRect.offsetMin = Vector2.zero;
            prestigeConfirmYesRect.offsetMax = Vector2.zero;
            prestigeConfirmYesGo.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.10f);
            var prestigeConfirmYesLabelGo = CreateText(prestigeConfirmYesGo.transform, "Label", "Renacer", 18, TextAnchor.MiddleCenter);
            var prestigeConfirmYesLabelRect = prestigeConfirmYesLabelGo.GetComponent<RectTransform>();
            prestigeConfirmYesLabelRect.anchorMin = Vector2.zero;
            prestigeConfirmYesLabelRect.anchorMax = Vector2.one;
            prestigeConfirmYesLabelRect.offsetMin = Vector2.zero;
            prestigeConfirmYesLabelRect.offsetMax = Vector2.zero;

            var prestigeConfirmNoGo = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            prestigeConfirmNoGo.transform.SetParent(prestigeConfirmPanel, false);
            var prestigeConfirmNoRect = prestigeConfirmNoGo.GetComponent<RectTransform>();
            prestigeConfirmNoRect.anchorMin = new Vector2(0.52f, 0.06f);
            prestigeConfirmNoRect.anchorMax = new Vector2(0.92f, 0.24f);
            prestigeConfirmNoRect.offsetMin = Vector2.zero;
            prestigeConfirmNoRect.offsetMax = Vector2.zero;
            prestigeConfirmNoGo.GetComponent<Image>().color = new Color(0.35f, 0.2f, 0.2f);
            var prestigeConfirmNoLabelGo = CreateText(prestigeConfirmNoGo.transform, "Label", "Cancelar", 18, TextAnchor.MiddleCenter);
            var prestigeConfirmNoLabelRect = prestigeConfirmNoLabelGo.GetComponent<RectTransform>();
            prestigeConfirmNoLabelRect.anchorMin = Vector2.zero;
            prestigeConfirmNoLabelRect.anchorMax = Vector2.one;
            prestigeConfirmNoLabelRect.offsetMin = Vector2.zero;
            prestigeConfirmNoLabelRect.offsetMax = Vector2.zero;

            var prestigeConfirmComp = prestigeConfirmPanel.gameObject.AddComponent<PrestigeConfirmPanel>();
            AssignSerialized(prestigeConfirmComp, "panel", prestigeConfirmPanel.gameObject);
            AssignSerialized(prestigeConfirmComp, "messageText", prestigeConfirmMessageGo.GetComponent<Text>());
            AssignSerialized(prestigeConfirmComp, "confirmButton", prestigeConfirmYesGo.GetComponent<Button>());
            AssignSerialized(prestigeConfirmComp, "cancelButton", prestigeConfirmNoGo.GetComponent<Button>());

            AssignSerialized(renacerButtonComp, "confirmPanel", prestigeConfirmComp);

            // Filas de mejoras permanentes.
            var prestigeUpgradeAssets = AssetDatabase.FindAssets("t:PrestigeUpgradeData", new[] { "Assets/_Project/Data/Prestige" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PrestigeUpgradeData>)
                .Where(p => p != null)
                .ToArray();

            float prestigeRowHeight = 0.70f / Mathf.Max(1, prestigeUpgradeAssets.Length);
            for (int i = 0; i < prestigeUpgradeAssets.Length; i++)
            {
                var rowGo = new GameObject($"PrestigeRow_{prestigeUpgradeAssets[i].id}", typeof(RectTransform), typeof(Image), typeof(Button));
                rowGo.transform.SetParent(prestigePanelGo.transform, false);
                var rowRect = rowGo.GetComponent<RectTransform>();
                float top = 0.68f - i * prestigeRowHeight;
                float bottom = 0.68f - (i + 1) * prestigeRowHeight;
                rowRect.anchorMin = new Vector2(0.03f, bottom + 0.01f);
                rowRect.anchorMax = new Vector2(0.97f, top - 0.01f);
                rowRect.offsetMin = Vector2.zero;
                rowRect.offsetMax = Vector2.zero;
                rowGo.GetComponent<Image>().color = new Color(0.20f, 0.18f, 0.12f);

                var rowLabelGo = CreateText(rowGo.transform, "Label", "", 15, TextAnchor.MiddleCenter);
                var rowLabelRect = rowLabelGo.GetComponent<RectTransform>();
                rowLabelRect.anchorMin = Vector2.zero;
                rowLabelRect.anchorMax = Vector2.one;
                rowLabelRect.offsetMin = new Vector2(8, 0);
                rowLabelRect.offsetMax = new Vector2(-8, 0);

                var rowComp = rowGo.AddComponent<PrestigeUpgradeRow>();
                AssignSerialized(rowComp, "label", rowLabelGo.GetComponent<Text>());
                AssignSerialized(rowComp, "upgrade", prestigeUpgradeAssets[i]);
            }

            var prestigeManagerGo = new GameObject("PrestigeManager");
            var prestigeManager = prestigeManagerGo.AddComponent<PrestigeManager>();
            prestigeManager.Configure(prestigeUpgradeAssets);

            if (prestigeUpgradeAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron PrestigeUpgradeData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // El onClick de las pestañas y del handle de colapso se conecta dentro de
            // BottomTabController.Awake(), no aca (ver nota sobre AddListener mas abajo, en BuildDebugMenu).
            var tabController = bottomPanel.gameObject.AddComponent<BottomTabController>();
            AssignSerialized(tabController, "upgradesPanel", upgradesPanelGo);
            AssignSerialized(tabController, "heroesPanel", heroesPanelGo);
            AssignSerialized(tabController, "equipmentPanel", equipmentPanelGo);
            AssignSerialized(tabController, "petsPanel", petsPanelGo);
            AssignSerialized(tabController, "prestigePanel", prestigePanelGo);
            AssignSerialized(tabController, "upgradesTabButton", upgradesTabButton);
            AssignSerialized(tabController, "heroesTabButton", heroesTabButton);
            AssignSerialized(tabController, "equipmentTabButton", equipmentTabButton);
            AssignSerialized(tabController, "petsTabButton", petsTabButton);
            AssignSerialized(tabController, "prestigeTabButton", prestigeTabButton);
            AssignSerialized(tabController, "panelRect", bottomPanel.GetComponent<RectTransform>());
            AssignSerialized(tabController, "tabBar", tabBarGo);
            AssignSerialized(tabController, "collapseButton", collapseButton);
            AssignSerialized(tabController, "collapseLabel", collapseLabelGo.GetComponent<Text>());

            // --- Capa de numeros de daño (encima de todo) ---
            var popupLayerGo = new GameObject("PopupLayer", typeof(RectTransform));
            popupLayerGo.transform.SetParent(canvasGo.transform, false);
            var popupRect = popupLayerGo.GetComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
            var popupPool = popupLayerGo.AddComponent<DamageNumberPool>();
            popupPool.Init(popupRect);

            // --- EnemySpawner ---
            var spawnerGo = new GameObject("EnemySpawner");
            var spawner = spawnerGo.AddComponent<EnemySpawner>();
            var allEnemyData = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/_Project/Data/Enemies" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnemyData>)
                .Where(e => e != null)
                .ToArray();
            var commonEnemyData = allEnemyData.Where(e => e.tier != EnemyTier.Boss).ToArray();
            var bossEnemyPool = allEnemyData.Where(e => e.tier == EnemyTier.Boss).ToArray();
            spawner.Configure(enemyAreaRect, commonEnemyData, bossEnemyPool);

            if (commonEnemyData.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron EnemyData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");
            if (bossEnemyPool.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontro ningun EnemyData de tier Boss: las oleadas no tendran jefe.");

            // --- Popup de progreso offline ---
            var offlinePanel = CreateStretchPanel(canvasGo.transform, "OfflineProgressPanel", new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f));
            offlinePanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            offlinePanel.gameObject.SetActive(false);

            var offlineMessageGo = CreateText(offlinePanel, "Message", "", 22, TextAnchor.MiddleCenter);
            var offlineMessageRect = offlineMessageGo.GetComponent<RectTransform>();
            offlineMessageRect.anchorMin = new Vector2(0.05f, 0.35f);
            offlineMessageRect.anchorMax = new Vector2(0.95f, 0.95f);
            offlineMessageRect.offsetMin = Vector2.zero;
            offlineMessageRect.offsetMax = Vector2.zero;

            var offlineCloseGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            offlineCloseGo.transform.SetParent(offlinePanel, false);
            var offlineCloseRect = offlineCloseGo.GetComponent<RectTransform>();
            offlineCloseRect.anchorMin = new Vector2(0.25f, 0.08f);
            offlineCloseRect.anchorMax = new Vector2(0.75f, 0.28f);
            offlineCloseRect.offsetMin = Vector2.zero;
            offlineCloseRect.offsetMax = Vector2.zero;
            offlineCloseGo.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f);

            var offlineCloseLabelGo = CreateText(offlineCloseGo.transform, "Label", "Aceptar", 20, TextAnchor.MiddleCenter);
            var offlineCloseLabelRect = offlineCloseLabelGo.GetComponent<RectTransform>();
            offlineCloseLabelRect.anchorMin = Vector2.zero;
            offlineCloseLabelRect.anchorMax = Vector2.one;
            offlineCloseLabelRect.offsetMin = Vector2.zero;
            offlineCloseLabelRect.offsetMax = Vector2.zero;

            var offlinePopup = offlinePanel.gameObject.AddComponent<OfflineProgressPopup>();
            AssignSerialized(offlinePopup, "panel", offlinePanel.gameObject);
            AssignSerialized(offlinePopup, "messageText", offlineMessageGo.GetComponent<Text>());
            AssignSerialized(offlinePopup, "closeButton", offlineCloseGo.GetComponent<Button>());

            // --- Modal de Misiones diarias ---
            var questsPanel = CreateStretchPanel(canvasGo.transform, "QuestsPanel", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.75f));
            questsPanel.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.11f, 0.97f);

            var questsTitleGo = CreateText(questsPanel, "Title", "Misiones diarias", 20, TextAnchor.MiddleCenter);
            var questsTitleRect = questsTitleGo.GetComponent<RectTransform>();
            questsTitleRect.anchorMin = new Vector2(0f, 0.88f);
            questsTitleRect.anchorMax = new Vector2(1f, 1f);
            questsTitleRect.offsetMin = Vector2.zero;
            questsTitleRect.offsetMax = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                var questRowGo = new GameObject($"QuestRow_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                questRowGo.transform.SetParent(questsPanel, false);
                var questRowRect = questRowGo.GetComponent<RectTransform>();
                float top = 0.86f - i * 0.27f;
                float bottom = top - 0.25f;
                questRowRect.anchorMin = new Vector2(0.04f, bottom);
                questRowRect.anchorMax = new Vector2(0.96f, top);
                questRowRect.offsetMin = Vector2.zero;
                questRowRect.offsetMax = Vector2.zero;
                questRowGo.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.17f);

                var questLabelGo = CreateText(questRowGo.transform, "Label", "", 14, TextAnchor.MiddleCenter);
                var questLabelRect = questLabelGo.GetComponent<RectTransform>();
                questLabelRect.anchorMin = Vector2.zero;
                questLabelRect.anchorMax = Vector2.one;
                questLabelRect.offsetMin = new Vector2(8, 0);
                questLabelRect.offsetMax = new Vector2(-8, 0);

                var questSlotComp = questRowGo.AddComponent<QuestSlotDisplay>();
                AssignSerialized(questSlotComp, "label", questLabelGo.GetComponent<Text>());
                AssignSerializedInt(questSlotComp, "slotIndex", i);
            }

            var questsCloseGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            questsCloseGo.transform.SetParent(questsPanel, false);
            var questsCloseRect = questsCloseGo.GetComponent<RectTransform>();
            questsCloseRect.anchorMin = new Vector2(0.30f, 0.02f);
            questsCloseRect.anchorMax = new Vector2(0.70f, 0.10f);
            questsCloseRect.offsetMin = Vector2.zero;
            questsCloseRect.offsetMax = Vector2.zero;
            questsCloseGo.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.30f);
            var questsCloseLabelGo = CreateText(questsCloseGo.transform, "Label", "Cerrar", 16, TextAnchor.MiddleCenter);
            var questsCloseLabelRect = questsCloseLabelGo.GetComponent<RectTransform>();
            questsCloseLabelRect.anchorMin = Vector2.zero;
            questsCloseLabelRect.anchorMax = Vector2.one;
            questsCloseLabelRect.offsetMin = Vector2.zero;
            questsCloseLabelRect.offsetMax = Vector2.zero;

            var questsModal = questsButtonGo.AddComponent<SimpleModalPanel>();
            AssignSerialized(questsModal, "panel", questsPanel.gameObject);
            AssignSerialized(questsModal, "openButton", questsButtonGo.GetComponent<Button>());
            AssignSerialized(questsModal, "closeButton", questsCloseGo.GetComponent<Button>());

            var questManagerGo = new GameObject("QuestManager");
            var questManager = questManagerGo.AddComponent<QuestManager>();
            var questDataAssets = AssetDatabase.FindAssets("t:QuestData", new[] { "Assets/_Project/Data/Quests" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<QuestData>)
                .Where(q => q != null)
                .ToArray();
            questManager.Configure(questDataAssets);

            if (questDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron QuestData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // --- Modal de Logros ---
            var achievementsPanel = CreateStretchPanel(canvasGo.transform, "AchievementsPanel", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.75f));
            achievementsPanel.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.06f, 0.97f);

            var achievementsTitleGo = CreateText(achievementsPanel, "Title", "Logros", 20, TextAnchor.MiddleCenter);
            var achievementsTitleRect = achievementsTitleGo.GetComponent<RectTransform>();
            achievementsTitleRect.anchorMin = new Vector2(0f, 0.90f);
            achievementsTitleRect.anchorMax = new Vector2(1f, 1f);
            achievementsTitleRect.offsetMin = Vector2.zero;
            achievementsTitleRect.offsetMax = Vector2.zero;

            var achievementCategories = new[]
            {
                AchievementCategory.Combat, AchievementCategory.Wealth,
                AchievementCategory.Collection, AchievementCategory.Prestige,
            };

            for (int i = 0; i < achievementCategories.Length; i++)
            {
                var achRowGo = new GameObject($"AchievementRow_{achievementCategories[i]}", typeof(RectTransform), typeof(Image));
                achRowGo.transform.SetParent(achievementsPanel, false);
                var achRowRect = achRowGo.GetComponent<RectTransform>();
                float top = 0.88f - i * 0.20f;
                float bottom = top - 0.18f;
                achRowRect.anchorMin = new Vector2(0.04f, bottom);
                achRowRect.anchorMax = new Vector2(0.96f, top);
                achRowRect.offsetMin = Vector2.zero;
                achRowRect.offsetMax = Vector2.zero;
                achRowGo.GetComponent<Image>().color = new Color(0.20f, 0.17f, 0.09f);

                var achLabelGo = CreateText(achRowGo.transform, "Label", "", 14, TextAnchor.MiddleCenter);
                var achLabelRect = achLabelGo.GetComponent<RectTransform>();
                achLabelRect.anchorMin = Vector2.zero;
                achLabelRect.anchorMax = Vector2.one;
                achLabelRect.offsetMin = new Vector2(8, 0);
                achLabelRect.offsetMax = new Vector2(-8, 0);

                var achRowComp = achRowGo.AddComponent<AchievementCategoryRow>();
                AssignSerialized(achRowComp, "label", achLabelGo.GetComponent<Text>());
                AssignSerializedEnum(achRowComp, "category", (int)achievementCategories[i]);
            }

            var achievementsCloseGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            achievementsCloseGo.transform.SetParent(achievementsPanel, false);
            var achievementsCloseRect = achievementsCloseGo.GetComponent<RectTransform>();
            achievementsCloseRect.anchorMin = new Vector2(0.30f, 0.02f);
            achievementsCloseRect.anchorMax = new Vector2(0.70f, 0.10f);
            achievementsCloseRect.offsetMin = Vector2.zero;
            achievementsCloseRect.offsetMax = Vector2.zero;
            achievementsCloseGo.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.30f);
            var achievementsCloseLabelGo = CreateText(achievementsCloseGo.transform, "Label", "Cerrar", 16, TextAnchor.MiddleCenter);
            var achievementsCloseLabelRect = achievementsCloseLabelGo.GetComponent<RectTransform>();
            achievementsCloseLabelRect.anchorMin = Vector2.zero;
            achievementsCloseLabelRect.anchorMax = Vector2.one;
            achievementsCloseLabelRect.offsetMin = Vector2.zero;
            achievementsCloseLabelRect.offsetMax = Vector2.zero;

            var achievementsModal = achievementsButtonGo.AddComponent<SimpleModalPanel>();
            AssignSerialized(achievementsModal, "panel", achievementsPanel.gameObject);
            AssignSerialized(achievementsModal, "openButton", achievementsButtonGo.GetComponent<Button>());
            AssignSerialized(achievementsModal, "closeButton", achievementsCloseGo.GetComponent<Button>());

            var achievementManagerGo = new GameObject("AchievementManager");
            var achievementManager = achievementManagerGo.AddComponent<AchievementManager>();
            var achievementDataAssets = AssetDatabase.FindAssets("t:AchievementData", new[] { "Assets/_Project/Data/Achievements" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AchievementData>)
                .Where(a => a != null)
                .ToArray();
            achievementManager.Configure(achievementDataAssets);

            if (achievementDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron AchievementData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            BuildDebugMenu(canvasGo.transform);

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Game.unity");
        }

        private static void BuildDebugMenu(Transform canvasTransform)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Zona invisible arriba a la derecha: triple tap abre el panel.
            var triggerGo = new GameObject("DebugTriggerZone", typeof(RectTransform), typeof(Image));
            triggerGo.transform.SetParent(canvasTransform, false);
            var triggerRect = triggerGo.GetComponent<RectTransform>();
            triggerRect.anchorMin = new Vector2(0.82f, 0.94f);
            triggerRect.anchorMax = new Vector2(1f, 1f);
            triggerRect.offsetMin = Vector2.zero;
            triggerRect.offsetMax = Vector2.zero;
            var triggerImage = triggerGo.GetComponent<Image>();
            triggerImage.color = new Color(1f, 1f, 1f, 0.01f); // casi invisible pero recibe raycast

            var panelGo = CreateStretchPanel(canvasTransform, "DebugPanel", new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.70f));
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

            var menu = triggerGo.AddComponent<DebugMenuController>();
            AssignSerialized(menu, "panel", panelGo.gameObject);

            var triggerZone = triggerGo.AddComponent<DebugTriggerZone>();
            AssignSerialized(triggerZone, "menuController", menu);

            string[] cheatLabels = { "+1K oro", "+1M oro", "+1B oro", "+1T oro", "Daño tap =1000", "Borrar guardado", "Simular +1h offline" };
            var cheatButtons = new Button[cheatLabels.Length];
            float cheatRowHeight = 1f / cheatLabels.Length;

            for (int i = 0; i < cheatLabels.Length; i++)
            {
                var btnGo = new GameObject($"Cheat_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(panelGo, false);
                var rect = btnGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 1f - cheatRowHeight * (i + 1));
                rect.anchorMax = new Vector2(0.95f, 1f - cheatRowHeight * i - 0.02f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                btnGo.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);

                var lbl = CreateText(btnGo.transform, "Label", cheatLabels[i], 22, TextAnchor.MiddleCenter);
                var lblRect = lbl.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.offsetMin = Vector2.zero;
                lblRect.offsetMax = Vector2.zero;

                cheatButtons[i] = btnGo.GetComponent<Button>();
            }

            // El onClick se conecta dentro de DebugMenuController.Awake(), no aca: un
            // AddListener hecho desde un script de Editor no sobrevive la recarga de dominio
            // al entrar en Play mode. Aca solo pasamos las referencias a los botones.
            AssignSerializedArray(menu, "cheatButtons", cheatButtons);
#endif
        }

        /// <summary>Boton de pestaña: divide al padre en `count` franjas iguales, esta ocupa la `index`-esima.</summary>
        private static Button CreateTabButton(Transform parent, string name, string label, int index, int count = 2)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            float width = 1f / count;
            rect.anchorMin = new Vector2(index * width, 0f);
            rect.anchorMax = new Vector2((index + 1) * width, 1f);
            rect.offsetMin = new Vector2(2, 0);
            rect.offsetMax = new Vector2(-2, 0);
            go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f);

            var labelGo = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Transform CreateStretchPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
            return go.transform;
        }

        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>Asigna un campo [SerializeField] privado via SerializedObject (forma correcta en Unity, sin reflection cruda).</summary>
        private static void AssignSerialized(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Campo '{fieldName}' no encontrado en {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        /// <summary>Igual que AssignSerialized pero para un campo de array (T[]) serializado.</summary>
        private static void AssignSerializedArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Campo '{fieldName}' no encontrado en {target.GetType().Name}.");
                return;
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedProperties();
        }

        /// <summary>Igual que AssignSerialized pero para un campo int (AssignSerialized solo
        /// sirve para referencias a Object).</summary>
        private static void AssignSerializedInt(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Campo '{fieldName}' no encontrado en {target.GetType().Name}.");
                return;
            }
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }

        /// <summary>Igual que AssignSerialized pero para un campo enum simple (declarado sin
        /// valores custom, asi que el indice de enumValueIndex coincide con (int)valor).</summary>
        private static void AssignSerializedEnum(Object target, string fieldName, int enumIntValue)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Campo '{fieldName}' no encontrado en {target.GetType().Name}.");
                return;
            }
            prop.enumValueIndex = enumIntValue;
            so.ApplyModifiedProperties();
        }
    }
}
