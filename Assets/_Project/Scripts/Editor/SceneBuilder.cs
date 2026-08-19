using System.Linq;
using TapLegends.Audio;
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

            // --- Fondo: 20 escenarios reales (Art/Backgrounds/, provistos por el usuario),
            // uno por jefe vencido, ciclando con modulo — cae al paisaje procedural si por
            // algun motivo no se encuentran (proyecto recien clonado sin ese arte, etc). Se
            // estira para llenar la pantalla completa (preserveAspect=false): los fondos
            // tienen su propia proporcion (mas angosta que 720x1280) y a proposito NO se
            // cambio la resolucion de referencia del juego para esto (decision del usuario:
            // el estiramiento leve es aceptable, cambiar la resolucion de referencia hubiera
            // significado re-revisar el tamaño en pixeles de practicamente todo el resto de
            // la UI sin poder verificarlo en vivo).
            var backgroundSprites = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/_Project/Art/Backgrounds" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToArray();

            var backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = backgroundGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = backgroundGo.GetComponent<Image>();
            bgImage.sprite = backgroundSprites.Length > 0 ? backgroundSprites[0] : PlaceholderArt.MountainLandscape("bosque_quebrado");
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;

            var backgroundController = backgroundGo.AddComponent<BackgroundController>();
            AssignSerialized(backgroundController, "backgroundImage", bgImage);
            backgroundController.Configure(backgroundSprites);

            // --- Captura de tap en toda la pantalla (estilo Tap Titans) ---
            // Va INMEDIATAMENTE despues del fondo y ANTES de todo lo demas (TopBar,
            // BottomPanel, botones, paneles): asi cualquier UI que se agregue de aca en
            // adelante queda al frente y se queda con sus propios clicks, sin que este
            // captor interfiera. Ver TapCatcher.cs para el porque exacto.
            var tapCatcherGo = new GameObject("TapCatcher", typeof(RectTransform), typeof(Image));
            tapCatcherGo.transform.SetParent(canvasGo.transform, false);
            var tapCatcherRect = tapCatcherGo.GetComponent<RectTransform>();
            tapCatcherRect.anchorMin = Vector2.zero;
            tapCatcherRect.anchorMax = Vector2.one;
            tapCatcherRect.offsetMin = Vector2.zero;
            tapCatcherRect.offsetMax = Vector2.zero;
            var tapCatcherImage = tapCatcherGo.GetComponent<Image>();
            tapCatcherImage.color = Color.clear; // invisible pero recibe raycast
            tapCatcherImage.raycastTarget = true;
            tapCatcherGo.AddComponent<TapCatcher>();

            // Iconos reales de Misiones/Logros/Ajustes y de las 5 pestañas del panel
            // inferior (arte provisto por el usuario, ver Art/TabIcons/). Rutas explicitas
            // (no FindAssets+orden) porque cada archivo va a un boton especifico distinto,
            // no una lista intercambiable como heroes/mascotas/fondos.
            Sprite LoadTabIcon(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/TabIcons/{fileName}.png");
            var questsIcon = LoadTabIcon("quests");
            var achievementsIcon = LoadTabIcon("achievements");
            var settingsIcon = LoadTabIcon("settings");
            var upgradesTabIcon = LoadTabIcon("upgrades");
            var heroesTabIcon = LoadTabIcon("heroes");
            var equipmentTabIcon = LoadTabIcon("equipment");
            var petsTabIcon = LoadTabIcon("pets");
            var prestigeTabIcon = LoadTabIcon("prestige");
            var exitIcon = LoadTabIcon("exit");

            // --- TopBar: oro + botones de Misiones/Logros ---
            var topBar = CreateStretchPanel(canvasGo.transform, "TopBar", new Vector2(0f, 0.90f), new Vector2(1f, 1f));
            // Recorta el borde superior al safe area (notch / camara en isla) — Etapa 10.
            topBar.gameObject.AddComponent<SafeAreaFitter>().Configure(top: true, bottom: false);
            var goldTextGo = CreateText(topBar, "GoldText", "Oro: 0", 36, TextAnchor.MiddleCenter);
            var goldDisplay = goldTextGo.transform.parent.gameObject.AddComponent<GoldDisplay>();
            AssignSerialized(goldDisplay, "goldText", goldTextGo.GetComponent<Text>());

            // Misiones ya NO vive en TopBar — el usuario pidio ponerla "en fila a un lado
            // del menu que saca los ajustes" (el boton ☰, MenuButton, mas abajo en este
            // metodo — abre el menu de pausa, que tiene Ajustes). Se ancla en pixeles al
            // mismo estilo que MenuButton/ProfileButton (esquina superior izquierda,
            // pivote (0,1)) en vez de a fracciones de TopBar, para quedar en la misma fila.
            // Tambien mas grande que antes (pedido: "se ven muy pequeñas") — alto 60 (vs los
            // ~41px que tenia en TopBar), ancho derivado de la proporcion real del listón.
            var questsButtonGo = new GameObject("QuestsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            questsButtonGo.transform.SetParent(canvasGo.transform, false);
            var questsButtonRect = questsButtonGo.GetComponent<RectTransform>();
            questsButtonRect.anchorMin = new Vector2(0f, 1f);
            questsButtonRect.anchorMax = new Vector2(0f, 1f);
            questsButtonRect.pivot = new Vector2(0f, 1f);
            questsButtonRect.anchoredPosition = new Vector2(120f, -2f);
            const float questsButtonHeight = 60f;
            float questsAspect = questsIcon != null ? questsIcon.rect.width / questsIcon.rect.height : 1f;
            questsButtonRect.sizeDelta = new Vector2(questsButtonHeight * questsAspect, questsButtonHeight);
            // Icono real (pergamino+poción) reemplaza el fondo de color solido + texto
            // "Misiones" — el ícono ya se lee solo, sin hacer falta la palabra (mismo
            // criterio que las pestañas de abajo, que traen su nombre horneado adentro).
            var questsButtonImage = questsButtonGo.GetComponent<Image>();
            if (questsIcon != null) { questsButtonImage.sprite = questsIcon; questsButtonImage.preserveAspect = true; }
            else questsButtonImage.color = new Color(0.20f, 0.30f, 0.22f);

            var achievementsButtonGo = new GameObject("AchievementsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            achievementsButtonGo.transform.SetParent(topBar, false);
            var achievementsButtonRect = achievementsButtonGo.GetComponent<RectTransform>();
            // Mas grande que antes (pedido: "se ven muy pequeñas") — 60% del alto de TopBar
            // (antes 32%), ancho ajustado para mantener la proporcion del listón (~2.7:1).
            achievementsButtonRect.anchorMin = new Vector2(0.69f, 0.20f);
            achievementsButtonRect.anchorMax = new Vector2(0.98f, 0.80f);
            achievementsButtonRect.offsetMin = Vector2.zero;
            achievementsButtonRect.offsetMax = Vector2.zero;
            var achievementsButtonImage = achievementsButtonGo.GetComponent<Image>();
            if (achievementsIcon != null) { achievementsButtonImage.sprite = achievementsIcon; achievementsButtonImage.preserveAspect = true; }
            else achievementsButtonImage.color = new Color(0.30f, 0.26f, 0.12f);
            // El icono ya trae "LOGROS" horneado — el contador dinamico (X/Y) sigue
            // haciendo falta (es informacion real, no se puede hornear), pero se reduce a
            // solo el numero (antes "Logros: X/Y") para no repetir la palabra. Vive AFUERA
            // del boton (hermano en topBar, no hijo) en la franja libre justo debajo — el
            // listón ya usa casi toda la altura del boton, superponer texto ahí se veía
            // amontonado.
            var achievementsButtonLabelGo = CreateText(topBar, "AchievementsCount", "0/0", 13, TextAnchor.MiddleCenter);
            var achievementsButtonLabelRect = achievementsButtonLabelGo.GetComponent<RectTransform>();
            achievementsButtonLabelRect.anchorMin = new Vector2(0.69f, 0.02f);
            achievementsButtonLabelRect.anchorMax = new Vector2(0.98f, 0.18f);
            achievementsButtonLabelRect.offsetMin = Vector2.zero;
            achievementsButtonLabelRect.offsetMax = Vector2.zero;
            var achievementsLabelText = achievementsButtonLabelGo.GetComponent<Text>();
            achievementsLabelText.fontSize = 12;
            var achievementCounter = achievementsButtonGo.AddComponent<AchievementCounterDisplay>();
            AssignSerialized(achievementCounter, "counterText", achievementsLabelText);

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

            // Alto del panel inferior ("menu de mejoras") como fraccion de pantalla — bajado
            // de 0.34 a 0.28 (pedido del usuario): que ocupe solo la franja MAS PLANA de
            // cada fondo (Art/Backgrounds/), no toda la franja de "piso" (que incluye pasto/
            // rocas mas detallados, mas alta). Declarado ANTES de PlayerCharacter porque
            // ambos lo usan (el personaje se para justo en el borde superior del panel).
            const float BottomPanelHeightFraction = 0.28f;

            // --- Area del enemigo ---
            var enemyAreaGo = new GameObject("EnemyArea", typeof(RectTransform));
            enemyAreaGo.transform.SetParent(canvasGo.transform, false);
            var enemyAreaRect = enemyAreaGo.GetComponent<RectTransform>();
            enemyAreaRect.anchorMin = new Vector2(0.5f, 0.55f);
            enemyAreaRect.anchorMax = new Vector2(0.5f, 0.55f);
            enemyAreaRect.anchoredPosition = Vector2.zero;
            enemyAreaRect.sizeDelta = Vector2.zero;

            // --- Personaje del jugador (decorativo: el enemigo es lo que recibe los taps) ---
            // Arte real (no placeholder) desde el dia 1: PlayerAnimator reproduce Idle en
            // loop y alterna los 3 ataques al tocar, usando la skin de espada elegida
            // (WeaponSkinManager). La espada ya viene dibujada en cada cuadro — no hace
            // falta una imagen de arma superpuesta como con los heroes contratados.
            var playerGo = new GameObject("PlayerCharacter", typeof(RectTransform), typeof(Image));
            playerGo.transform.SetParent(canvasGo.transform, false);
            var playerRect = playerGo.GetComponent<RectTransform>();
            // Pivote abajo-centro (no el default centro-centro): asi el ancla Y representa
            // directamente donde pisan los pies, no el centro del sprite.
            playerRect.pivot = new Vector2(0.5f, 0f);
            // X=0.5 (centro de pantalla, no 0.18) e Y=BottomPanelHeight (ver mas abajo, la
            // franja MAS PLANA de cada fondo — no toda la franja de "piso" con pasto/rocas,
            // que es mas alta): pedido explicito del usuario. El enemigo ya estaba centrado
            // en X (0.5, mas arriba) — con el jugador tambien centrado pero mucho mas abajo
            // no se pisan, queda "protagonista abajo, monstruo arriba", ambos en el eje
            // central de la pantalla.
            playerRect.anchorMin = new Vector2(0.5f, BottomPanelHeightFraction);
            playerRect.anchorMax = new Vector2(0.5f, BottomPanelHeightFraction);
            playerRect.anchoredPosition = Vector2.zero;
            // 210x110 (no 110x110): los 4 tipos de animacion (idle/ataque1/2/3) ahora
            // comparten UN SOLO canvas de origen (mismo ancho/alto para los 4, ancho por
            // el espadazo mas amplio) para que preserveAspect escale exactamente igual en
            // todos — con una caja cuadrada, ese canvas mas ancho que alto se aplastaba y
            // el personaje se veia mas chico/lejos al atacar. El alto se mantiene en 110
            // (igual que antes) y el ancho sigue la proporcion real del canvas fuente.
            // 238 (no 210) para que Hierro Negra quepa: su lienzo compartido es mas ancho
            // (210x98, por el espadazo giratorio de ataque3) que el de Madera/Hierro
            // (185x98) — con una caja de 210 de ancho, ese canvas mas ancho quedaba
            // LIMITADO por ancho en vez de por alto y el personaje se veia ~11% mas chico
            // al equipar esa espada. 238 = 210 * (210/185) mantiene el alto como limite
            // para los 3 skins por igual (mismo factor de escala 110/98 en los 3).
            playerRect.sizeDelta = new Vector2(238, 110);
            var playerImage = playerGo.GetComponent<Image>();
            playerImage.preserveAspect = true;
            playerImage.raycastTarget = false;
            var playerAnimator = playerGo.AddComponent<PlayerAnimator>();
            AssignSerialized(playerAnimator, "bodyImage", playerImage);

            // Estela decorativa del personaje al enemigo en cada tap (pedido del usuario:
            // "que parezca que si le estamos dando" en vez de atacar al aire).
            var playerAttackEffect = playerGo.AddComponent<PlayerAttackEffect>();
            AssignSerialized(playerAttackEffect, "effectOrigin", playerRect);
            AssignSerialized(playerAttackEffect, "effectTarget", enemyAreaRect);
            AssignSerialized(playerAttackEffect, "effectParent", canvasGo.transform);

            var weaponSkinAssets = AssetDatabase.FindAssets("t:WeaponSkinData", new[] { "Assets/_Project/Data/WeaponSkins" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<WeaponSkinData>)
                .Where(s => s != null)
                .ToArray();

            var weaponSkinManagerGo = new GameObject("WeaponSkinManager");
            var weaponSkinManager = weaponSkinManagerGo.AddComponent<WeaponSkinManager>();
            weaponSkinManager.Configure(weaponSkinAssets);

            if (weaponSkinAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron WeaponSkinData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // --- Heroes visibles alrededor del monstruo (aparecen al comprarlos) ---
            // OrderBy baseCost: AssetDatabase.FindAssets no garantiza ningun orden en
            // particular (ni alfabetico ni de creacion) — sin esto la lista de heroes
            // salia desordenada en el menu (reportado por el usuario).
            var heroDataAssets = AssetDatabase.FindAssets("t:HeroData", new[] { "Assets/_Project/Data/Heroes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HeroData>)
                .Where(h => h != null)
                .OrderBy(h => h.baseCost)
                .ToArray();

            // Los primeros 4 slots son 2 pares IZQUIERDA/DERECHA simetricos (misma Y en cada
            // par) a proposito: con exactamente 4 heroes (el roster actual) los slots se
            // asignan en orden 0,1,2,3, asi que esos 4 tienen que formar 2 parejas completas
            // — antes el orden mezclaba 1 slot suelto (0.84,0.42) con solo LA MITAD de un
            // par (0.10,0.50 sin su pareja 0.90,0.50), y el resultado se veia disparejo:
            // "hay unos mas abajo y otros mas arriba" (reportado por el usuario). Los 2
            // slots sueltos de altura unica quedan al final (indices 4/5), para cuando haya
            // un 5to/6to heroe.
            var heroFieldSlots = new (float x, float y)[]
            {
                (0.12f, 0.66f),
                (0.88f, 0.66f),
                (0.10f, 0.50f),
                (0.90f, 0.50f),
                (0.84f, 0.42f),
                (0.14f, 0.74f),
            };

            // Pedestales bajo cada heroe (recortados de una hoja de props de biomas provista
            // por el usuario: "haz que los heroes ayudantes esten parados sobre bases...
            // toma las que consideres mejores y que interfieran menos con el ambiente").
            // Uno fijo por SLOT (no por bioma/fondo — los heroes no estan atados a un fondo
            // en particular), variedad de material (tronco/hielo/arena/roca flotante/piedra/
            // tierra agrietada) para que las 6 no se vean repetidas entre si.
            var heroBaseSprites = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/_Project/Art/HeroBases" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToArray();
            const float baseHeight = 22f;

            for (int i = 0; i < heroDataAssets.Length && i < heroFieldSlots.Length; i++)
            {
                var slot = heroFieldSlots[i];

                // Alto objetivo por heroe, ancho derivado de la proporcion real del sprite
                // (no una caja fija para todos): con una caja igual para los 4, el que
                // tuviera el lienzo mas ANCHO (el dragon — su aliento de fuego estira el
                // lienzo compartido idle/ataque mucho mas que a los demas, ver mas abajo)
                // terminaba limitado por ancho en vez de por alto, y se renderizaba mas
                // CHICO que el resto pese a ser conceptualmente el heroe mas grande
                // (reportado por el usuario). Fijando el ALTO por heroe y derivando el
                // ancho de su propio sprite.rect, el alto es SIEMPRE el limite para los 4
                // — se ven consistentes entre si sin importar cuanto se estire cada lienzo
                // hacia los costados.
                // 76 (no 64): el usuario pidio "un poquitin mas grandes" para los 3 que no
                // son el dragon, manteniendolo a el como el mas grande de los 4.
                float fieldHeight = heroDataAssets[i].id == "ruijerd_dragon" ? 100f : 76f;
                float spriteAspect = heroDataAssets[i].Art != null
                    ? heroDataAssets[i].Art.rect.width / heroDataAssets[i].Art.rect.height
                    : 1f;

                CanvasGroup heroBaseGroup = null;
                if (heroBaseSprites.Length > 0)
                {
                    // Creado ANTES que HeroField_ (mismo padre, canvasGo) para que quede
                    // primero en el orden de hijos y se dibuje DETRAS del heroe — un hijo de
                    // HeroField_ se dibujaria ENCIMA de su Image (el sprite del heroe vive en
                    // el root de ese GameObject, no en un hijo), asi que tiene que ser un
                    // hermano separado, no un hijo.
                    var heroBaseGo = new GameObject($"HeroBase_{i}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    heroBaseGo.transform.SetParent(canvasGo.transform, false);
                    var heroBaseRect = heroBaseGo.GetComponent<RectTransform>();
                    heroBaseRect.anchorMin = new Vector2(slot.x, slot.y);
                    heroBaseRect.anchorMax = new Vector2(slot.x, slot.y);
                    // Pivote arriba-centro con un offset hacia abajo de fieldHeight/2 (la
                    // mitad de la caja del heroe, que esta centrada verticalmente en el
                    // ancla — pivote (0,0.5)): asi el borde SUPERIOR de la base queda justo
                    // en el borde INFERIOR de la caja del heroe, es decir, a la altura de
                    // sus pies.
                    heroBaseRect.pivot = new Vector2(0.5f, 1f);
                    heroBaseRect.anchoredPosition = new Vector2(0f, -fieldHeight / 2f);
                    var baseSprite = heroBaseSprites[i % heroBaseSprites.Length];
                    float baseAspect = baseSprite.rect.width / baseSprite.rect.height;
                    heroBaseRect.sizeDelta = new Vector2(baseHeight * baseAspect, baseHeight);

                    var heroBaseImage = heroBaseGo.GetComponent<Image>();
                    heroBaseImage.sprite = baseSprite;
                    heroBaseImage.preserveAspect = true;
                    heroBaseImage.raycastTarget = false;
                    heroBaseGroup = heroBaseGo.GetComponent<CanvasGroup>();
                    heroBaseGroup.alpha = 0f; // invisible hasta comprarlo, igual que el heroe
                }

                var heroFieldGo = new GameObject($"HeroField_{heroDataAssets[i].id}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                heroFieldGo.transform.SetParent(canvasGo.transform, false);
                var heroFieldRect = heroFieldGo.GetComponent<RectTransform>();
                heroFieldRect.anchorMin = new Vector2(slot.x, slot.y);
                heroFieldRect.anchorMax = new Vector2(slot.x, slot.y);
                heroFieldRect.anchoredPosition = Vector2.zero;
                // Pivote a la izquierda (0, 0.5) en vez del centro: la caja crece hacia la
                // DERECHA desde su anchor. Combinado con el flip de mas abajo (localScale.x=-1
                // para slots del lado derecho), el flip espeja la caja alrededor de ese mismo
                // pivote — asi para CUALQUIER hero la caja siempre crece hacia el CENTRO de la
                // pantalla, nunca hacia el borde mas cercano. Sin esto, el dragon (caja mucho
                // mas ancha por su fieldHeight=100) quedaba centrado en un slot pegado al borde
                // izquierdo (x=0.10) y la mitad del cuerpo se salia de pantalla (reportado).
                heroFieldRect.pivot = new Vector2(0f, 0.5f);
                heroFieldRect.sizeDelta = new Vector2(spriteAspect * fieldHeight, fieldHeight);

                // El arte de heroes siempre mira/ataca hacia la DERECHA (mismo criterio que
                // el personaje principal). Los slots del lado derecho del monstruo (x>0.5)
                // quedan de espaldas a el sin esto — se ven pegandole a la pared en vez de
                // al monstruo (reportado por el usuario). Espejar todo el GameObject
                // (localScale.x=-1) da vuelta el sprite Y el icono de arma hijo juntos, sin
                // tocar los assets ni duplicar arte para "version espejada".
                if (slot.x > 0.5f) heroFieldRect.localScale = new Vector3(-1f, 1f, 1f);
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
                AssignSerialized(heroFieldSprite, "bodyImage", heroFieldImage);
                if (heroBaseGroup != null) AssignSerialized(heroFieldSprite, "baseGroup", heroBaseGroup);
            }

            if (heroDataAssets.Length > heroFieldSlots.Length)
                Debug.LogWarning($"[SceneBuilder] Hay {heroDataAssets.Length} heroes pero solo " +
                                  $"{heroFieldSlots.Length} lugares en el campo de batalla — agrega mas slots en heroFieldSlots.");

            // --- Panel inferior: pestañas Mejoras / Heroes, colapsable para liberar pantalla ---
            var bottomPanel = CreateStretchPanel(canvasGo.transform, "BottomPanel", new Vector2(0f, 0f), new Vector2(1f, BottomPanelHeightFraction));
            // Recorta el borde inferior al safe area (barra de gestos de Android) — Etapa 10.
            bottomPanel.gameObject.AddComponent<SafeAreaFitter>().Configure(top: false, bottom: true);
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

            var upgradesTabButton = CreateTabButton(tabBarGo.transform, "UpgradesTabButton", "Mejoras", 0, 5, upgradesTabIcon);
            var heroesTabButton = CreateTabButton(tabBarGo.transform, "HeroesTabButton", "Heroes", 1, 5, heroesTabIcon);
            var equipmentTabButton = CreateTabButton(tabBarGo.transform, "EquipmentTabButton", "Equipo", 2, 5, equipmentTabIcon);
            var petsTabButton = CreateTabButton(tabBarGo.transform, "PetsTabButton", "Mascotas", 3, 5, petsTabIcon);
            var prestigeTabButton = CreateTabButton(tabBarGo.transform, "PrestigeTabButton", "Prestigio", 4, 5, prestigeTabIcon);

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

            // fontSize 16, no 20: el label son 3 lineas (nombre, X -> Y, Coste) y a 20 no
            // entraban en la altura de este boton — el "Coste:" quedaba invisible (Truncate).
            var upgradeLabelGo = CreateText(upgradeButtonGo.transform, "Label", "Mejorar daño de tap", 16, TextAnchor.MiddleCenter);
            var upgradeLabelRect = upgradeLabelGo.GetComponent<RectTransform>();
            upgradeLabelRect.anchorMin = Vector2.zero;
            upgradeLabelRect.anchorMax = Vector2.one;
            upgradeLabelRect.offsetMin = Vector2.zero;
            upgradeLabelRect.offsetMax = Vector2.zero;

            var upgradeButtonComp = upgradeButtonGo.AddComponent<UpgradeTapDamageButton>();
            AssignSerialized(upgradeButtonComp, "label", upgradeLabelGo.GetComponent<Text>());
            upgradeButtonGo.AddComponent<HoldToRepeatButton>();

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

                var skillIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                skillIconGo.transform.SetParent(skillRowGo.transform, false);
                var skillIconRect = skillIconGo.GetComponent<RectTransform>();
                skillIconRect.anchorMin = new Vector2(0f, 0.5f);
                skillIconRect.anchorMax = new Vector2(0f, 0.5f);
                skillIconRect.anchoredPosition = new Vector2(20, 0);
                skillIconRect.sizeDelta = new Vector2(28, 28);
                var skillIconImage = skillIconGo.GetComponent<Image>();
                skillIconImage.sprite = skillItem.Icon;
                skillIconImage.preserveAspect = true;
                skillIconImage.raycastTarget = false;

                // fontSize 13, no 15: el label son 3 lineas y las filas de habilidad son
                // bajas (hasta 5 comparten el panel) — a 15 el "Coste:" no entraba.
                var skillLabelGo = CreateText(skillRowGo.transform, "Label", skillItem.displayName, 13, TextAnchor.MiddleCenter);
                var skillLabelRect = skillLabelGo.GetComponent<RectTransform>();
                skillLabelRect.anchorMin = Vector2.zero;
                skillLabelRect.anchorMax = Vector2.one;
                // Deja hueco a la izquierda para el icono (36 = 20 de offset + 28/2 de medio
                // icono + margen chico) — sin esto el texto se dibuja encima del icono.
                skillLabelRect.offsetMin = new Vector2(36, 0);
                skillLabelRect.offsetMax = Vector2.zero;

                var skillRowComp = skillRowGo.AddComponent<SkillUpgradeButton>();
                AssignSerialized(skillRowComp, "label", skillLabelGo.GetComponent<Text>());
                AssignSerialized(skillRowComp, "icon", skillIconImage);
                AssignSerialized(skillRowComp, "skill", skillItem);
                skillRowGo.AddComponent<HoldToRepeatButton>();
            }

            if (skillDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontro SkillData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // --- Barra de habilidades: hasta 5 casilleros, siempre pegada arriba del panel
            // de Mejoras/Heroes. Es hija de bottomPanel y ancla a su borde superior (anchorMin/
            // Max en y=1) con pivot en y=0, asi que la sigue automaticamente cuando el panel
            // se colapsa o se expande — no hace falta ningun script para "reposicionarla".
            const int skillSlotCount = 5;
            // 84 (no 64): el usuario reporto que los casilleros se veian "un poco chiquitos
            // en pantalla".
            const float skillBarHeight = 84f;
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
                // HotbarIcon (version grande/detallada), no Icon (version chica de la fila
                // de Mejoras) — el usuario proveyo 2 tamaños distintos para cada uso.
                slotImage.sprite = skillData.HotbarIcon;
                slotImage.preserveAspect = true;
                slotImage.color = Color.white;

                var overlayGo = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
                overlayGo.transform.SetParent(slotGo.transform, false);
                var overlayRect = overlayGo.GetComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.anchoredPosition = Vector2.zero;
                // Mismo tamaño que el icono REALMENTE dibujado, no todo el casillero: con
                // preserveAspect, un icono mas angosto que alto (aspecto ~0.8) queda mas
                // chico que la caja del casillero (mas ancha que alta), dejando margen
                // transparente a los costados — antes el overlay estiraba el radial sobre
                // TODO el casillero, incluido ese margen vacio, y el circulo de cooldown se
                // veia desalineado del icono (reportado por el usuario: "que sea del mismo
                // tamaño de la imagen para que se vea bien").
                float hotbarAspect = skillData.HotbarIcon != null
                    ? skillData.HotbarIcon.rect.width / skillData.HotbarIcon.rect.height
                    : 1f;
                overlayRect.sizeDelta = new Vector2(skillBarHeight * hotbarAspect, skillBarHeight);
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
                heroRowGo.AddComponent<HoldToRepeatButton>();
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

            for (int i = 0; i < equipmentDataAssets.Length; i++)
            {
                var equipItem = equipmentDataAssets[i];

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
                equipRowGo.AddComponent<HoldToRepeatButton>();
            }

            var equipmentManagerGo = new GameObject("EquipmentManager");
            var equipmentManager = equipmentManagerGo.AddComponent<EquipmentManager>();
            equipmentManager.Configure(equipmentDataAssets);

            if (equipmentDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron EquipmentData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // Nota: antes el arma del personaje cambiaba de color segun la rareza equipada
            // (PlayerWeaponDisplay, sobre una imagen de arma superpuesta). Con arte real
            // ya no hay imagen de arma separada — la espada viene dibujada en cada cuadro
            // de PlayerAnimator, elegida por skin (WeaponSkinManager), no por rareza de
            // equipo. La pieza "Espada de Aldemark" sigue existiendo como stat de daño en
            // el inventario, solo perdio esta representacion visual.

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
                // Solo en la fila (mejorar con fragmentos) — activar mascota no es una
                // accion que tenga sentido repetir manteniendo pulsado.
                petRowGo.AddComponent<HoldToRepeatButton>();
            }

            var petManagerGo = new GameObject("PetManager");
            var petManager = petManagerGo.AddComponent<PetManager>();
            petManager.Configure(petDataAssets);

            if (petDataAssets.Length == 0)
                Debug.LogWarning("[SceneBuilder] No se encontraron PetData. Ejecuta primero " +
                                  "'Tap Legends/Setup/Create Initial Data' y vuelve a construir la escena.");

            // Hasta 2 mascotas activas a la vez, una a cada lado del personaje principal,
            // paradas en el piso (pedido del usuario: antes era 1 sola mascota flotando en
            // (0.30, 0.53), lejos de donde quedo el heroe tras bajarlo al piso). Mismo ancla
            // Y que PlayerCharacter (BottomPanelHeightFraction, pivote abajo) para que paren
            // a la misma altura de piso; el offset en X es en anchoredPosition (pixeles), no
            // en la fraccion de ancla, para no depender de la resolucion de referencia.
            // -180/180, no -150/150: PlayerCharacter mide 238 de ancho (subio de 210 para
            // el arreglo de Hierro Negra, ver mas arriba) y esta centrado, asi que su borde
            // queda en +/-119 — a -150 la mascota (medio ancho ~50-52) invadia el borde
            // izquierdo del personaje (reportado por el usuario: "que no choque con el
            // heroe"). -180 deja un margen visible de sobra en los 2 lados.
            var petSlotOffsets = new[] { -180f, 180f };
            for (int slot = 0; slot < petSlotOffsets.Length; slot++)
            {
                var petFieldGo = new GameObject($"PetField_{slot}", typeof(RectTransform), typeof(Image));
                petFieldGo.transform.SetParent(canvasGo.transform, false);
                var petFieldRect = petFieldGo.GetComponent<RectTransform>();
                petFieldRect.anchorMin = new Vector2(0.5f, BottomPanelHeightFraction);
                petFieldRect.anchorMax = new Vector2(0.5f, BottomPanelHeightFraction);
                petFieldRect.pivot = new Vector2(0.5f, 0f);
                petFieldRect.anchoredPosition = new Vector2(petSlotOffsets[slot], 0f);
                petFieldRect.sizeDelta = new Vector2(70, 70);
                var petFieldImage = petFieldGo.GetComponent<Image>();
                petFieldImage.preserveAspect = true;
                petFieldImage.raycastTarget = false;
                petFieldImage.enabled = false;

                var petFieldSprite = petFieldGo.AddComponent<PetFieldSprite>();
                AssignSerialized(petFieldSprite, "icon", petFieldImage);
                AssignSerializedInt(petFieldSprite, "slot", slot);
            }

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

                // fontSize 13, no 15: mismo problema que las filas de habilidad — 3 lineas
                // (nombre, efecto, "Coste: X reliquias") no entraban a 15 en filas de solo 4.
                var rowLabelGo = CreateText(rowGo.transform, "Label", "", 13, TextAnchor.MiddleCenter);
                var rowLabelRect = rowLabelGo.GetComponent<RectTransform>();
                rowLabelRect.anchorMin = Vector2.zero;
                rowLabelRect.anchorMax = Vector2.one;
                rowLabelRect.offsetMin = new Vector2(8, 0);
                rowLabelRect.offsetMax = new Vector2(-8, 0);

                var rowComp = rowGo.AddComponent<PrestigeUpgradeRow>();
                AssignSerialized(rowComp, "label", rowLabelGo.GetComponent<Text>());
                AssignSerialized(rowComp, "upgrade", prestigeUpgradeAssets[i]);
                rowGo.AddComponent<HoldToRepeatButton>();
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

            // --- Musica: tema base siempre sonando, tema de jefe durante la pelea ---
            var baseMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/BaseTheme.mp3");
            var bossMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/BossBattle.mp3");

            var musicGo = new GameObject("MusicManager", typeof(AudioSource));
            var musicSource = musicGo.GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.7f;

            var musicManager = musicGo.AddComponent<MusicManager>();
            AssignSerialized(musicManager, "source", musicSource);
            AssignSerialized(musicManager, "baseMusic", baseMusicClip);
            AssignSerialized(musicManager, "bossMusic", bossMusicClip);

            if (baseMusicClip == null || bossMusicClip == null)
                Debug.LogWarning("[SceneBuilder] Falta BaseTheme.mp3 o BossBattle.mp3 en Assets/_Project/Audio/Music/.");

            // --- SFX: tap, mejora de nivel y sonido puntual al vencer a un jefe ---
            var tapClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Tap.wav");
            var powerUpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/PowerUp.wav");
            var bossDefeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/BossDefeat.wav");

            var sfxGo = new GameObject("SfxManager", typeof(AudioSource));
            var sfxSource = sfxGo.GetComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.9f;

            var sfxManager = sfxGo.AddComponent<SfxManager>();
            AssignSerialized(sfxManager, "source", sfxSource);
            AssignSerialized(sfxManager, "tapClip", tapClip);
            AssignSerialized(sfxManager, "powerUpClip", powerUpClip);
            AssignSerialized(sfxManager, "bossDefeatClip", bossDefeatClip);

            if (tapClip == null || powerUpClip == null || bossDefeatClip == null)
                Debug.LogWarning("[SceneBuilder] Falta algun clip de SFX en Assets/_Project/Audio/SFX/ (Tap.wav, PowerUp.wav, BossDefeat.wav).");

            // --- Boton de menu (esquina superior izquierda) + menu de pausa ---
            // Etapa 10: el boton de retroceso de Android ya no cierra la app, abre este
            // menu (ver PauseMenuController). El boton en pantalla es la forma de abrirlo
            // sin depender del boton fisico.
            var menuButtonGo = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            menuButtonGo.transform.SetParent(canvasGo.transform, false);
            var menuButtonRect = menuButtonGo.GetComponent<RectTransform>();
            menuButtonRect.anchorMin = new Vector2(0f, 1f);
            menuButtonRect.anchorMax = new Vector2(0f, 1f);
            menuButtonRect.pivot = new Vector2(0f, 1f);
            menuButtonRect.anchoredPosition = new Vector2(4f, -4f);
            menuButtonRect.sizeDelta = new Vector2(52, 52);
            menuButtonGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 0.92f);
            var menuButtonLabelGo = CreateText(menuButtonGo.transform, "Label", "☰", 30, TextAnchor.MiddleCenter);
            var menuButtonLabelRect = menuButtonLabelGo.GetComponent<RectTransform>();
            menuButtonLabelRect.anchorMin = Vector2.zero;
            menuButtonLabelRect.anchorMax = Vector2.one;
            menuButtonLabelRect.offsetMin = Vector2.zero;
            menuButtonLabelRect.offsetMax = Vector2.zero;

            // --- Boton de perfil, justo al lado del de menu ---
            var profileButtonGo = new GameObject("ProfileButton", typeof(RectTransform), typeof(Image), typeof(Button));
            profileButtonGo.transform.SetParent(canvasGo.transform, false);
            var profileButtonRect = profileButtonGo.GetComponent<RectTransform>();
            profileButtonRect.anchorMin = new Vector2(0f, 1f);
            profileButtonRect.anchorMax = new Vector2(0f, 1f);
            profileButtonRect.pivot = new Vector2(0f, 1f);
            profileButtonRect.anchoredPosition = new Vector2(60f, -4f);
            profileButtonRect.sizeDelta = new Vector2(52, 52);
            profileButtonGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 0.92f);
            var profileButtonLabelGo = CreateText(profileButtonGo.transform, "Label", "👤", 26, TextAnchor.MiddleCenter);
            var profileButtonLabelRect = profileButtonLabelGo.GetComponent<RectTransform>();
            profileButtonLabelRect.anchorMin = Vector2.zero;
            profileButtonLabelRect.anchorMax = Vector2.one;
            profileButtonLabelRect.offsetMin = Vector2.zero;
            profileButtonLabelRect.offsetMax = Vector2.zero;

            var pausePanel = CreateStretchPanel(canvasGo.transform, "PausePanel", new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.62f));
            pausePanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            pausePanel.gameObject.SetActive(false);
            var pauseTitleGo = CreateText(pausePanel, "Title", "Pausa", 22, TextAnchor.MiddleCenter);
            var pauseTitleRect = pauseTitleGo.GetComponent<RectTransform>();
            pauseTitleRect.anchorMin = new Vector2(0f, 0.68f);
            pauseTitleRect.anchorMax = new Vector2(1f, 1f);
            pauseTitleRect.offsetMin = Vector2.zero;
            pauseTitleRect.offsetMax = Vector2.zero;
            // 3 franjas iguales (antes 2: Continuar/Ajustes) para dejarle lugar a "Salir"
            // (pedido del usuario: "usa la de salir que no teníamos" — antes no existía un
            // botón de salir de la app en el menú de pausa). Ajustes/Salir ahora usan el
            // ícono listón completo (ya trae el nombre horneado, mismo criterio que las
            // pestañas de abajo) en vez del truco anterior de ícono chico + texto aparte —
            // el listón nuevo ya tiene la proporción ancha que ese truco imitaba a mano.
            var resumeButton = CreateMenuButton(pausePanel, "ResumeButton", "Continuar",
                new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.64f), new Color(0.2f, 0.45f, 0.25f));
            var openSettingsButton = CreateMenuButton(pausePanel, "SettingsButton", "Ajustes",
                new Vector2(0.10f, 0.25f), new Vector2(0.90f, 0.43f), new Color(0.16f, 0.16f, 0.20f), settingsIcon);
            var quitButton = CreateMenuButton(pausePanel, "QuitButton", "Salir",
                new Vector2(0.10f, 0.04f), new Vector2(0.90f, 0.22f), new Color(0.35f, 0.16f, 0.16f), exitIcon);

            var settingsPanel = CreateStretchPanel(canvasGo.transform, "SettingsPanel", new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.78f));
            settingsPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            settingsPanel.gameObject.SetActive(false);
            var settingsTitleGo = CreateText(settingsPanel, "Title", "Ajustes", 22, TextAnchor.MiddleCenter);
            var settingsTitleRect = settingsTitleGo.GetComponent<RectTransform>();
            settingsTitleRect.anchorMin = new Vector2(0f, 0.88f);
            settingsTitleRect.anchorMax = new Vector2(1f, 1f);
            settingsTitleRect.offsetMin = Vector2.zero;
            settingsTitleRect.offsetMax = Vector2.zero;
            var vibrationToggleButton = CreateMenuButton(settingsPanel, "VibrationToggleButton", "Vibracion: activada",
                new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.84f), new Color(0.18f, 0.22f, 0.30f));
            var openCreditsButton = CreateMenuButton(settingsPanel, "CreditsButton", "Creditos",
                new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.66f), new Color(0.18f, 0.22f, 0.30f));
            var deleteSaveButton = CreateMenuButton(settingsPanel, "DeleteSaveButton", "Borrar partida",
                new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.48f), new Color(0.5f, 0.15f, 0.15f));
            var settingsBackButton = CreateMenuButton(settingsPanel, "BackButton", "Atras",
                new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.20f), new Color(0.16f, 0.16f, 0.20f));

            var creditsPanel = CreateStretchPanel(canvasGo.transform, "CreditsPanel", new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.72f));
            creditsPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            creditsPanel.gameObject.SetActive(false);
            var creditsTitleGo = CreateText(creditsPanel, "Title", "Creditos", 22, TextAnchor.MiddleCenter);
            var creditsTitleRect = creditsTitleGo.GetComponent<RectTransform>();
            creditsTitleRect.anchorMin = new Vector2(0f, 0.82f);
            creditsTitleRect.anchorMax = new Vector2(1f, 1f);
            creditsTitleRect.offsetMin = Vector2.zero;
            creditsTitleRect.offsetMax = Vector2.zero;
            var creditsBodyGo = CreateText(creditsPanel, "Body",
                "Tap Legends\nUn idle medieval hecho con Unity.\n\nDiseno y desarrollo: Bryan\nCon asistencia de Claude (Anthropic)",
                15, TextAnchor.MiddleCenter);
            var creditsBodyRect = creditsBodyGo.GetComponent<RectTransform>();
            creditsBodyRect.anchorMin = new Vector2(0.06f, 0.26f);
            creditsBodyRect.anchorMax = new Vector2(0.94f, 0.80f);
            creditsBodyRect.offsetMin = Vector2.zero;
            creditsBodyRect.offsetMax = Vector2.zero;
            var creditsBackButton = CreateMenuButton(creditsPanel, "BackButton", "Atras",
                new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.20f), new Color(0.16f, 0.16f, 0.20f));

            var deleteConfirmPanel = CreateStretchPanel(canvasGo.transform, "DeleteConfirmPanel", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.66f));
            deleteConfirmPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            deleteConfirmPanel.gameObject.SetActive(false);
            var deleteConfirmMessageGo = CreateText(deleteConfirmPanel, "Message",
                "¿Borrar tu partida guardada?\nEsta accion no se puede deshacer.", 17, TextAnchor.MiddleCenter);
            var deleteConfirmMessageRect = deleteConfirmMessageGo.GetComponent<RectTransform>();
            deleteConfirmMessageRect.anchorMin = new Vector2(0.06f, 0.32f);
            deleteConfirmMessageRect.anchorMax = new Vector2(0.94f, 0.95f);
            deleteConfirmMessageRect.offsetMin = Vector2.zero;
            deleteConfirmMessageRect.offsetMax = Vector2.zero;
            var deleteConfirmYesButton = CreateMenuButton(deleteConfirmPanel, "ConfirmButton", "Si, borrar",
                new Vector2(0.08f, 0.06f), new Vector2(0.48f, 0.26f), new Color(0.5f, 0.15f, 0.15f));
            var deleteConfirmNoButton = CreateMenuButton(deleteConfirmPanel, "CancelButton", "Cancelar",
                new Vector2(0.52f, 0.06f), new Vector2(0.92f, 0.26f), new Color(0.2f, 0.45f, 0.25f));

            var pauseMenuGo = new GameObject("PauseMenu");
            var pauseMenu = pauseMenuGo.AddComponent<PauseMenuController>();
            AssignSerialized(pauseMenu, "menuButton", menuButtonGo.GetComponent<Button>());
            AssignSerialized(pauseMenu, "pausePanel", pausePanel.gameObject);
            AssignSerialized(pauseMenu, "resumeButton", resumeButton);
            AssignSerialized(pauseMenu, "openSettingsButton", openSettingsButton);
            AssignSerialized(pauseMenu, "quitButton", quitButton);
            AssignSerialized(pauseMenu, "settingsPanel", settingsPanel.gameObject);
            AssignSerialized(pauseMenu, "settingsBackButton", settingsBackButton);
            AssignSerialized(pauseMenu, "vibrationToggleButton", vibrationToggleButton);
            AssignSerialized(pauseMenu, "vibrationToggleLabel", vibrationToggleButton.GetComponentInChildren<Text>());
            AssignSerialized(pauseMenu, "openCreditsButton", openCreditsButton);
            AssignSerialized(pauseMenu, "deleteSaveButton", deleteSaveButton);
            AssignSerialized(pauseMenu, "creditsPanel", creditsPanel.gameObject);
            AssignSerialized(pauseMenu, "creditsBackButton", creditsBackButton);
            AssignSerialized(pauseMenu, "deleteConfirmPanel", deleteConfirmPanel.gameObject);
            AssignSerialized(pauseMenu, "deleteConfirmYesButton", deleteConfirmYesButton);
            AssignSerialized(pauseMenu, "deleteConfirmNoButton", deleteConfirmNoButton);

            // --- Tutorial de los primeros 3 minutos (Etapa 10) ---
            // Sin Image de fondo en la raiz: no debe bloquear el juego debajo, solo la
            // burbuja de texto y "Saltar" reciben toques (el resto del overlay es decorativo).
            var tutorialOverlayGo = new GameObject("TutorialOverlay", typeof(RectTransform));
            tutorialOverlayGo.transform.SetParent(canvasGo.transform, false);
            var tutorialOverlayRect = tutorialOverlayGo.GetComponent<RectTransform>();
            tutorialOverlayRect.anchorMin = Vector2.zero;
            tutorialOverlayRect.anchorMax = Vector2.one;
            tutorialOverlayRect.offsetMin = Vector2.zero;
            tutorialOverlayRect.offsetMax = Vector2.zero;

            var tutorialBubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            tutorialBubbleGo.transform.SetParent(tutorialOverlayGo.transform, false);
            var tutorialBubbleRect = tutorialBubbleGo.GetComponent<RectTransform>();
            tutorialBubbleRect.anchorMin = new Vector2(0.06f, 0.82f);
            tutorialBubbleRect.anchorMax = new Vector2(0.70f, 0.895f);
            tutorialBubbleRect.offsetMin = Vector2.zero;
            tutorialBubbleRect.offsetMax = Vector2.zero;
            tutorialBubbleGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            var tutorialTextGo = CreateText(tutorialBubbleGo.transform, "Text", "", 16, TextAnchor.MiddleCenter);
            var tutorialTextRect = tutorialTextGo.GetComponent<RectTransform>();
            tutorialTextRect.anchorMin = Vector2.zero;
            tutorialTextRect.anchorMax = Vector2.one;
            tutorialTextRect.offsetMin = new Vector2(10, 0);
            tutorialTextRect.offsetMax = new Vector2(-10, 0);

            var tutorialSkipGo = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            tutorialSkipGo.transform.SetParent(tutorialOverlayGo.transform, false);
            var tutorialSkipRect = tutorialSkipGo.GetComponent<RectTransform>();
            tutorialSkipRect.anchorMin = new Vector2(0.72f, 0.82f);
            tutorialSkipRect.anchorMax = new Vector2(0.94f, 0.895f);
            tutorialSkipRect.offsetMin = Vector2.zero;
            tutorialSkipRect.offsetMax = Vector2.zero;
            tutorialSkipGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 0.9f);
            var tutorialSkipLabelGo = CreateText(tutorialSkipGo.transform, "Label", "Saltar", 14, TextAnchor.MiddleCenter);
            var tutorialSkipLabelRect = tutorialSkipLabelGo.GetComponent<RectTransform>();
            tutorialSkipLabelRect.anchorMin = Vector2.zero;
            tutorialSkipLabelRect.anchorMax = Vector2.one;
            tutorialSkipLabelRect.offsetMin = Vector2.zero;
            tutorialSkipLabelRect.offsetMax = Vector2.zero;

            var tutorialPointerGo = new GameObject("Pointer", typeof(RectTransform), typeof(Text));
            tutorialPointerGo.transform.SetParent(tutorialOverlayGo.transform, false);
            var tutorialPointerRect = tutorialPointerGo.GetComponent<RectTransform>();
            tutorialPointerRect.anchorMin = new Vector2(0.5f, 0.5f);
            tutorialPointerRect.anchorMax = new Vector2(0.5f, 0.5f);
            tutorialPointerRect.sizeDelta = new Vector2(60, 60);
            var tutorialPointerText = tutorialPointerGo.GetComponent<Text>();
            tutorialPointerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tutorialPointerText.text = "▼";
            tutorialPointerText.fontSize = 36;
            tutorialPointerText.alignment = TextAnchor.MiddleCenter;
            tutorialPointerText.color = new Color(1f, 0.85f, 0.2f);
            tutorialPointerText.raycastTarget = false;

            var tutorialManagerGo = new GameObject("TutorialManager");
            var tutorialManager = tutorialManagerGo.AddComponent<TutorialManager>();
            AssignSerialized(tutorialManager, "overlayRoot", tutorialOverlayGo);
            AssignSerialized(tutorialManager, "overlayRect", tutorialOverlayRect);
            AssignSerialized(tutorialManager, "bubbleText", tutorialTextGo.GetComponent<Text>());
            AssignSerialized(tutorialManager, "skipButton", tutorialSkipGo.GetComponent<Button>());
            AssignSerialized(tutorialManager, "pointerArrow", tutorialPointerRect);
            AssignSerialized(tutorialManager, "stepEnemyTarget", enemyAreaRect);
            AssignSerialized(tutorialManager, "stepUpgradeTarget", upgradeRect);
            AssignSerialized(tutorialManager, "stepHeroesTarget", heroesTabButton.GetComponent<RectTransform>());

            // --- Perfil (nombre, presentacion, estadisticas, skins) ---
            var profileManagerGo = new GameObject("ProfileManager");
            profileManagerGo.AddComponent<ProfileManager>();
            BuildProfilePanel(canvasGo.transform, profileButtonGo.GetComponent<Button>(), heroDataAssets, petDataAssets, weaponSkinAssets);

            BuildDebugMenu(canvasGo.transform);

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Game.unity");
        }

        /// <summary>Panel de Perfil: sub-pestañas Info (nombre/presentacion), Estadísticas
        /// (lee AchievementManager, no trackea nada nuevo) y Skins (personaje + cada heroe
        /// + cada mascota). Ver ProfileManager para el porque de las skins como sufijo de
        /// PlaceholderArt en vez de arte real todavia.</summary>
        private static void BuildProfilePanel(Transform canvasTransform, Button profileButton, HeroData[] heroDataAssets, PetData[] petDataAssets, WeaponSkinData[] weaponSkinAssets)
        {
            var profilePanel = CreateStretchPanel(canvasTransform, "ProfilePanel", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            profilePanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            profilePanel.gameObject.SetActive(false);

            var titleGo = CreateText(profilePanel, "Title", "Perfil", 22, TextAnchor.MiddleCenter);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.92f);
            titleRect.anchorMax = new Vector2(0.82f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var closeButton = CreateMenuButton(profilePanel, "CloseButton", "X",
                new Vector2(0.84f, 0.93f), new Vector2(0.98f, 0.995f), new Color(0.35f, 0.2f, 0.2f));

            var subTabBarGo = new GameObject("SubTabBar", typeof(RectTransform));
            subTabBarGo.transform.SetParent(profilePanel, false);
            var subTabBarRect = subTabBarGo.GetComponent<RectTransform>();
            subTabBarRect.anchorMin = new Vector2(0f, 0.80f);
            subTabBarRect.anchorMax = new Vector2(1f, 0.90f);
            subTabBarRect.offsetMin = Vector2.zero;
            subTabBarRect.offsetMax = Vector2.zero;

            var infoTabButton = CreateTabButton(subTabBarGo.transform, "InfoTabButton", "Info", 0, 3);
            var statsTabButton = CreateTabButton(subTabBarGo.transform, "StatsTabButton", "Estadísticas", 1, 3);
            var skinsTabButton = CreateTabButton(subTabBarGo.transform, "SkinsTabButton", "Skins", 2, 3);

            // --- Sub-panel Info: preview del personaje + nombre + presentacion ---
            var infoPanelGo = new GameObject("InfoPanel", typeof(RectTransform));
            infoPanelGo.transform.SetParent(profilePanel, false);
            var infoPanelRect = infoPanelGo.GetComponent<RectTransform>();
            infoPanelRect.anchorMin = new Vector2(0f, 0f);
            infoPanelRect.anchorMax = new Vector2(1f, 0.79f);
            infoPanelRect.offsetMin = Vector2.zero;
            infoPanelRect.offsetMax = Vector2.zero;

            var previewGo = new GameObject("CharacterPreview", typeof(RectTransform), typeof(Image));
            previewGo.transform.SetParent(infoPanelGo.transform, false);
            var previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.70f);
            previewRect.anchorMax = new Vector2(0.5f, 0.70f);
            previewRect.anchoredPosition = Vector2.zero;
            previewRect.sizeDelta = new Vector2(210, 110); // ver comentario en PlayerCharacter mas arriba
            var previewImage = previewGo.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
            // Mismo componente que el personaje real en juego (no un preview aparte): asi
            // el Perfil muestra exactamente la skin de espada elegida, animada, en vez de
            // un aspecto placeholder que no coincide con lo que se ve jugando.
            var previewAnimator = previewGo.AddComponent<PlayerAnimator>();
            AssignSerialized(previewAnimator, "bodyImage", previewImage);

            var usernameLabelGo = CreateText(infoPanelGo.transform, "UsernameLabel", "Nombre", 14, TextAnchor.MiddleLeft);
            var usernameLabelRect = usernameLabelGo.GetComponent<RectTransform>();
            usernameLabelRect.anchorMin = new Vector2(0.06f, 0.42f);
            usernameLabelRect.anchorMax = new Vector2(0.94f, 0.48f);
            usernameLabelRect.offsetMin = Vector2.zero;
            usernameLabelRect.offsetMax = Vector2.zero;

            var usernameField = CreateInputField(infoPanelGo.transform, "UsernameField", "Tu nombre...", false,
                new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.42f));
            usernameField.characterLimit = 20;

            var bioLabelGo = CreateText(infoPanelGo.transform, "BioLabel",
                "Presentación (mas adelante la van a poder leer otros jugadores)", 12, TextAnchor.MiddleLeft);
            var bioLabelRect = bioLabelGo.GetComponent<RectTransform>();
            bioLabelRect.anchorMin = new Vector2(0.06f, 0.24f);
            bioLabelRect.anchorMax = new Vector2(0.94f, 0.30f);
            bioLabelRect.offsetMin = Vector2.zero;
            bioLabelRect.offsetMax = Vector2.zero;

            var bioField = CreateInputField(infoPanelGo.transform, "BioField", "Escribí algo sobre vos...", true,
                new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.23f));
            bioField.characterLimit = 140;

            var infoPanelComp = infoPanelGo.AddComponent<ProfileInfoPanel>();
            AssignSerialized(infoPanelComp, "usernameField", usernameField);
            AssignSerialized(infoPanelComp, "bioField", bioField);

            // --- Sub-panel Estadísticas: lee AchievementManager, no trackea nada nuevo ---
            var statsPanelGo = new GameObject("StatsPanel", typeof(RectTransform));
            statsPanelGo.transform.SetParent(profilePanel, false);
            var statsPanelRect = statsPanelGo.GetComponent<RectTransform>();
            statsPanelRect.anchorMin = new Vector2(0f, 0f);
            statsPanelRect.anchorMax = new Vector2(1f, 0.79f);
            statsPanelRect.offsetMin = Vector2.zero;
            statsPanelRect.offsetMax = Vector2.zero;
            statsPanelGo.SetActive(false);

            var statsTextGo = CreateText(statsPanelGo.transform, "StatsText", "", 16, TextAnchor.UpperLeft);
            var statsTextRect = statsTextGo.GetComponent<RectTransform>();
            statsTextRect.anchorMin = new Vector2(0.08f, 0.05f);
            statsTextRect.anchorMax = new Vector2(0.92f, 0.95f);
            statsTextRect.offsetMin = Vector2.zero;
            statsTextRect.offsetMax = Vector2.zero;

            var statsPanelComp = statsPanelGo.AddComponent<ProfileStatsPanel>();
            AssignSerialized(statsPanelComp, "statsText", statsTextGo.GetComponent<Text>());

            // --- Sub-panel Skins: una fila por personaje (jugador + cada heroe + cada mascota) ---
            var skinsPanelGo = new GameObject("SkinsPanel", typeof(RectTransform));
            skinsPanelGo.transform.SetParent(profilePanel, false);
            var skinsPanelRect = skinsPanelGo.GetComponent<RectTransform>();
            skinsPanelRect.anchorMin = new Vector2(0f, 0f);
            skinsPanelRect.anchorMax = new Vector2(1f, 0.79f);
            skinsPanelRect.offsetMin = Vector2.zero;
            skinsPanelRect.offsetMax = Vector2.zero;
            skinsPanelGo.SetActive(false);

            int totalRows = 1 + heroDataAssets.Length + petDataAssets.Length;
            float rowHeight = 1f / Mathf.Max(1, totalRows);
            int rowIndex = 0;

            CreateWeaponSkinRow(skinsPanelGo.transform, "Espada", weaponSkinAssets,
                1f - rowIndex * rowHeight, 1f - (rowIndex + 1) * rowHeight);
            rowIndex++;

            foreach (var hero in heroDataAssets)
            {
                CreateSkinRow(skinsPanelGo.transform, hero.id, hero.displayName, CreatureRole.Hero,
                    1f - rowIndex * rowHeight, 1f - (rowIndex + 1) * rowHeight);
                rowIndex++;
            }

            foreach (var pet in petDataAssets)
            {
                CreateSkinRow(skinsPanelGo.transform, pet.id, pet.displayName, CreatureRole.Pet,
                    1f - rowIndex * rowHeight, 1f - (rowIndex + 1) * rowHeight);
                rowIndex++;
            }

            var profileTabController = profilePanel.gameObject.AddComponent<ProfileTabController>();
            AssignSerialized(profileTabController, "infoPanel", infoPanelGo);
            AssignSerialized(profileTabController, "statsPanel", statsPanelGo);
            AssignSerialized(profileTabController, "skinsPanel", skinsPanelGo);
            AssignSerialized(profileTabController, "infoTabButton", infoTabButton);
            AssignSerialized(profileTabController, "statsTabButton", statsTabButton);
            AssignSerialized(profileTabController, "skinsTabButton", skinsTabButton);

            var profileModal = profileButton.gameObject.AddComponent<SimpleModalPanel>();
            AssignSerialized(profileModal, "panel", profilePanel.gameObject);
            AssignSerialized(profileModal, "openButton", profileButton);
            AssignSerialized(profileModal, "closeButton", closeButton);
        }

        /// <summary>Fila de skins de espada del personaje principal: un casillero por
        /// WeaponSkinData (arte real, no PlaceholderArt) — mismo layout que CreateSkinRow
        /// pero leyendo el icono real de cada asset en vez de generar un preview.</summary>
        private static void CreateWeaponSkinRow(Transform parent, string displayName, WeaponSkinData[] skins, float top, float bottom)
        {
            var rowGo = new GameObject($"SkinRow_{displayName}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, bottom + 0.005f);
            rowRect.anchorMax = new Vector2(1f, top - 0.005f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var labelGo = CreateText(rowGo.transform, "Label", displayName, 13, TextAnchor.MiddleLeft);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.30f, 1f);
            labelRect.offsetMin = new Vector2(6, 0);
            labelRect.offsetMax = Vector2.zero;

            int count = Mathf.Max(1, skins.Length);
            const float swatchAreaStart = 0.32f;
            float swatchWidth = (1f - swatchAreaStart) / count;

            for (int i = 0; i < skins.Length; i++)
            {
                var swatchGo = new GameObject($"Skin_{skins[i].id}", typeof(RectTransform), typeof(Image), typeof(Button));
                swatchGo.transform.SetParent(rowGo.transform, false);
                var swatchRect = swatchGo.GetComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(swatchAreaStart + i * swatchWidth, 0.10f);
                swatchRect.anchorMax = new Vector2(swatchAreaStart + (i + 1) * swatchWidth - 0.01f, 0.90f);
                swatchRect.offsetMin = Vector2.zero;
                swatchRect.offsetMax = Vector2.zero;
                swatchGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f);

                var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(Image));
                previewGo.transform.SetParent(swatchGo.transform, false);
                var previewRect = previewGo.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0.12f, 0.12f);
                previewRect.anchorMax = new Vector2(0.88f, 0.88f);
                previewRect.offsetMin = Vector2.zero;
                previewRect.offsetMax = Vector2.zero;
                var previewImage = previewGo.GetComponent<Image>();
                previewImage.sprite = skins[i].icon;
                previewImage.preserveAspect = true;
                previewImage.raycastTarget = false;

                var swatchComp = swatchGo.AddComponent<WeaponSkinSwatchButton>();
                AssignSerialized(swatchComp, "skin", skins[i]);
            }
        }

        /// <summary>Una fila de la pestaña Skins: nombre del personaje + SkinCount(4)
        /// casilleros, cada uno con un preview real (PlaceholderArt con el sufijo de esa
        /// skin) — no un color plano, para que se vea de verdad que va a mostrar cada uno.</summary>
        private static void CreateSkinRow(Transform parent, string characterId, string displayName, CreatureRole role, float top, float bottom)
        {
            var rowGo = new GameObject($"SkinRow_{characterId}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, bottom + 0.005f);
            rowRect.anchorMax = new Vector2(1f, top - 0.005f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var labelGo = CreateText(rowGo.transform, "Label", displayName, 13, TextAnchor.MiddleLeft);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.30f, 1f);
            labelRect.offsetMin = new Vector2(6, 0);
            labelRect.offsetMax = Vector2.zero;

            const int swatchCount = 4; // ProfileManager.SkinCount — SceneBuilder no puede leerlo de una instancia en Editor
            const float swatchAreaStart = 0.32f;
            float swatchWidth = (1f - swatchAreaStart) / swatchCount;

            for (int i = 0; i < swatchCount; i++)
            {
                var swatchGo = new GameObject($"Skin_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                swatchGo.transform.SetParent(rowGo.transform, false);
                var swatchRect = swatchGo.GetComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(swatchAreaStart + i * swatchWidth, 0.10f);
                swatchRect.anchorMax = new Vector2(swatchAreaStart + (i + 1) * swatchWidth - 0.01f, 0.90f);
                swatchRect.offsetMin = Vector2.zero;
                swatchRect.offsetMax = Vector2.zero;
                swatchGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f);

                var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(Image));
                previewGo.transform.SetParent(swatchGo.transform, false);
                var previewRect = previewGo.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0.12f, 0.12f);
                previewRect.anchorMax = new Vector2(0.88f, 0.88f);
                previewRect.offsetMin = Vector2.zero;
                previewRect.offsetMax = Vector2.zero;
                var previewImage = previewGo.GetComponent<Image>();
                previewImage.sprite = PlaceholderArt.Creature(ProfileManager.SkinnedId(characterId, i), 24, role);
                previewImage.preserveAspect = true;
                previewImage.raycastTarget = false;

                var swatchComp = swatchGo.AddComponent<SkinSwatchButton>();
                AssignSerializedString(swatchComp, "characterId", characterId);
                AssignSerializedInt(swatchComp, "skinIndex", i);
            }
        }

        /// <summary>Campo de texto editable (InputField legacy uGUI) con placeholder —
        /// usado por la pestaña Info del Perfil.</summary>
        private static InputField CreateInputField(Transform parent, string name, string placeholderText, bool multiline, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var textGo = CreateText(go.transform, "Text", "", 15, TextAnchor.UpperLeft);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -6);
            var textComp = textGo.GetComponent<Text>();
            textComp.supportRichText = false;

            var placeholderGo = CreateText(go.transform, "Placeholder", placeholderText, 15, TextAnchor.UpperLeft);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -6);
            var placeholderComp = placeholderGo.GetComponent<Text>();
            placeholderComp.color = new Color(1f, 1f, 1f, 0.35f);
            placeholderComp.fontStyle = FontStyle.Italic;

            var inputField = go.GetComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = placeholderComp;
            inputField.targetGraphic = go.GetComponent<Image>();
            inputField.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            if (multiline) textComp.alignment = TextAnchor.UpperLeft;

            return inputField;
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

            string[] cheatLabels = { "+1K oro", "+1M oro", "+1B oro", "+1T oro", "Daño tap =1000", "Borrar guardado", "Simular +1h offline", "Reset auto-jefe" };
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
        private static Button CreateTabButton(Transform parent, string name, string label, int index, int count = 2, Sprite icon = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            float width = 1f / count;
            rect.anchorMin = new Vector2(index * width, 0f);
            rect.anchorMax = new Vector2((index + 1) * width, 1f);
            rect.offsetMin = new Vector2(2, 0);
            rect.offsetMax = new Vector2(-2, 0);

            if (icon != null)
            {
                // El icono ya trae su nombre horneado adentro (ver Art/TabIcons/) — sin
                // fondo de color solido ni Text por separado, se repetiria el texto.
                var image = go.GetComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
            }
            else
            {
                go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f);
                var labelGo = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return go.GetComponent<Button>();
        }

        /// <summary>Boton rectangular con anclas y color explicitos (a diferencia de
        /// CreateTabButton, que reparte franjas iguales) — usado por los paneles de pausa,
        /// ajustes, creditos y confirmaciones.</summary>
        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, Sprite icon = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (icon != null)
            {
                // Igual criterio que CreateTabButton: el icono ya trae el nombre horneado
                // adentro en formato listón ancho (ver Art/TabIcons/) — reemplaza fondo de
                // color Y Text, no se superponen.
                var image = go.GetComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
            }
            else
            {
                go.GetComponent<Image>().color = color;
                var labelGo = CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

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
            // Overflow, no Truncate (el default de Unity): varias filas de esta escena meten
            // 2-3 lineas de texto (nombre, stat, "Coste: X") en filas mas bajas de lo que esa
            // cantidad de lineas necesita a ese fontSize. Con Truncate, la ultima linea
            // (casi siempre el precio) desaparecia sin ningun error ni warning visible — el
            // bug real detras de "no me dice el precio de nada". Overflow nunca oculta texto:
            // en el peor caso se sale un poco de su fila, pero el precio siempre se ve.
            text.verticalOverflow = VerticalWrapMode.Overflow;

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

        /// <summary>Igual que AssignSerialized pero para un campo string.</summary>
        private static void AssignSerializedString(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Campo '{fieldName}' no encontrado en {target.GetType().Name}.");
                return;
            }
            prop.stringValue = value;
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
