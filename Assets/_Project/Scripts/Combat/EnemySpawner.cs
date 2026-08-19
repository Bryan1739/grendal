using System.Collections;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.Combat
{
    /// <summary>
    /// Genera enemigos comunes sin fin. El nivel se congela en multiplos exactos de
    /// GameConstants.EnemiesPerStage (10, 20, 30...). Cada jefe de oleada arranca su pelea
    /// SOLO la primera vez que queda disponible (para que el jugador vea el mecanismo del
    /// temporizador sin tener que descubrir un boton). Si esa pelea se pierde (escapa por
    /// tiempo), los reintentos de ESE MISMO jefe pasan a ser opcionales via ChallengeBoss()
    /// ("Volver a desafiar al jefe" en la UI) hasta vencerlo. Vencerlo reinicia el ciclo:
    /// el proximo jefe (10 niveles despues) vuelve a pelear solo otra vez, sin importar si
    /// este se gano en el primer intento o al decimo. El jefe #1, #11, #21... (cada 10
    /// oleadas) es siempre Grendal; el resto sale al azar de bossPool.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        private const string GrendalId = "grendal_boss";

        [SerializeField] private RectTransform enemyArea;
        [SerializeField] private EnemyData[] enemyPool;
        [SerializeField] private EnemyData[] bossPool;
        [SerializeField] private float delayBetweenSpawns = 0.25f;

        private int currentLevel = 1;
        private int killsInStage = 0;
        private bool bossAvailable = false;
        private bool pendingBossChallenge = false;
        private bool isBossFight = false;
        private bool hasLostBossOnce = false;
        private Enemy currentEnemy;
        private Coroutine bossTimerCoroutine;

        public int CurrentLevel => currentLevel;
        public int KillsInStage => killsInStage;
        public bool BossAvailable => bossAvailable;
        public bool HasLostBossOnce => hasLostBossOnce;
        public Enemy CurrentEnemyInstance => currentEnemy;

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Conexion desde herramientas de editor (SceneBuilder) — no hay prefab que arrastrar en el inspector todavia.</summary>
        public void Configure(RectTransform area, EnemyData[] pool, EnemyData[] bosses)
        {
            enemyArea = area;
            enemyPool = pool;
            bossPool = bosses;
        }

        /// <summary>Restaura nivel y progreso de oleada desde un guardado. Llamar antes de Start() (en Awake de GameManager).</summary>
        public void LoadState(int savedLevel, int savedKillsInStage, bool savedHasLostBossOnce)
        {
            currentLevel = Mathf.Max(1, savedLevel);
            killsInStage = Mathf.Clamp(savedKillsInStage, 0, GameConstants.EnemiesPerStage);
            bossAvailable = killsInStage >= GameConstants.EnemiesPerStage;
            hasLostBossOnce = savedHasLostBossOnce;

            // Si se guardo justo en la ventana entre "jefe disponible" y que el auto-combate
            // arranque, no perder esa garantia al recargar.
            pendingBossChallenge = bossAvailable && !hasLostBossOnce;
        }

        private void Start()
        {
            SpawnNext();
        }

        /// <summary>Solo para el menu de debug: vuelve a poner hasLostBossOnce en false sin
        /// tocar nivel, oro ni ningun otro progreso — sirve para volver a probar el mecanismo
        /// de "el primer jefe de cada oleada pelea solo" sin tener que borrar la partida
        /// entera. Si ya hay un jefe disponible esperando, lo activa de inmediato.</summary>
        public void DebugResetBossAutoFight()
        {
            hasLostBossOnce = false;
            if (bossAvailable && !isBossFight) pendingBossChallenge = true;
            PublishStageStatus();
        }

        /// <summary>Llamado por el boton "Volver a desafiar al jefe". Devuelve si el desafio arranco.</summary>
        public bool ChallengeBoss()
        {
            if (!bossAvailable || isBossFight || bossPool == null || bossPool.Length == 0) return false;

            pendingBossChallenge = true;
            if (currentEnemy != null)
            {
                currentEnemy.OnDied -= HandleEnemyDied;
                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
            }

            StopAllCoroutines();
            SpawnNext();
            return true;
        }

        private void SpawnNext()
        {
            if (enemyPool == null || enemyPool.Length == 0)
            {
                Debug.LogWarning("EnemySpawner sin EnemyData asignados.");
                return;
            }

            bool spawningBoss = pendingBossChallenge && bossPool != null && bossPool.Length > 0;
            pendingBossChallenge = false;

            EnemyData data = spawningBoss ? PickBossData() : enemyPool[Random.Range(0, enemyPool.Length)];

            currentEnemy = CreateEnemyGameObject(spawningBoss);
            currentEnemy.Setup(data, currentLevel);
            currentEnemy.OnDied += HandleEnemyDied;

            isBossFight = spawningBoss;
            PublishStageStatus();

            if (spawningBoss) bossTimerCoroutine = StartCoroutine(BossTimerRoutine());
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            enemy.OnDied -= HandleEnemyDied;
            bool wasBoss = enemy.IsBoss;
            Destroy(enemy.gameObject, 0.05f);
            // Limpiar la referencia YA, no cuando el proximo SpawnNext() la reemplace
            // (~0.25s despues): mientras tanto currentEnemy apunta a un GameObject
            // destruido, y CurrentEnemyInstance?.OnPointerDown(...) (TapCatcher) SI llama
            // al metodo igual — el operador ?. compara referencia C# cruda, no respeta el
            // "fake-null" de UnityEngine.Object, y explota con MissingReferenceException.
            currentEnemy = null;

            if (wasBoss)
            {
                StopBossTimer();
                isBossFight = false;
                bossAvailable = false;
                killsInStage = 0;
                currentLevel++; // arranca la oleada siguiente (11, 21, 31...)

                // Vencer al jefe reinicia el ciclo: el PROXIMO jefe (10 niveles despues)
                // vuelve a pelear solo, sin importar si este se gano en el primer intento
                // automatico o despues de varios reintentos opcionales. hasLostBossOnce
                // describe "ya se perdio ESTE jefe alguna vez", no "algun jefe alguna vez
                // en toda la partida" — por eso se limpia aca, no solo en Renacer.
                hasLostBossOnce = false;
            }
            else if (!bossAvailable)
            {
                killsInStage++;
                if (killsInStage >= GameConstants.EnemiesPerStage)
                {
                    // Se congela EXACTAMENTE en el multiplo de 10 (10, 20, 30...): no se
                    // incrementa currentLevel en el kill que completa la oleada.
                    bossAvailable = true;

                    // La primera vez que ESTE jefe queda disponible, la pelea arranca sola —
                    // recien despues de perderlo se vuelve opcional (ChallengeBoss()), y
                    // vencerlo reinicia hasLostBossOnce para el proximo ciclo (ver HandleEnemyDied).
                    if (!hasLostBossOnce) pendingBossChallenge = true;
                }
                else
                {
                    currentLevel++;
                }
            }

            PublishStageStatus();
            StartCoroutine(SpawnAfterDelay());
        }

        /// <summary>El jefe #1, #11, #21... (cada 10 oleadas) es siempre Grendal; el resto,
        /// al azar del pool. currentLevel esta congelado en un multiplo de 10 en este punto.</summary>
        private EnemyData PickBossData()
        {
            bool forceGrendal = currentLevel % 100 == 10;
            if (forceGrendal)
            {
                var grendal = System.Array.Find(bossPool, b => b != null && b.id == GrendalId);
                if (grendal != null) return grendal;
            }
            return bossPool[Random.Range(0, bossPool.Length)];
        }

        private IEnumerator SpawnAfterDelay()
        {
            yield return new WaitForSeconds(delayBetweenSpawns);
            SpawnNext();
        }

        private IEnumerator BossTimerRoutine()
        {
            float remaining = GameConstants.BossTimerSeconds;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                PublishStageStatus(Mathf.Max(0f, remaining));
                yield return null;
            }

            HandleBossEscaped();
        }

        /// <summary>Se acabo el tiempo: el jefe escapa. Sigue disponible para reintentar
        /// cuando el jugador quiera — no hay que repetir la oleada.</summary>
        private void HandleBossEscaped()
        {
            bossTimerCoroutine = null;
            isBossFight = false;
            hasLostBossOnce = true; // los reintentos de ESTE jefe son opcionales hasta vencerlo (ver HandleEnemyDied)

            if (currentEnemy != null)
            {
                currentEnemy.OnDied -= HandleEnemyDied;
                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
            }

            EventBus.Publish(new BossEscapedEvent());
            PublishStageStatus();
            StartCoroutine(SpawnAfterDelay());
        }

        private void StopBossTimer()
        {
            if (bossTimerCoroutine == null) return;
            StopCoroutine(bossTimerCoroutine);
            bossTimerCoroutine = null;
        }

        private void PublishStageStatus(float bossSecondsRemaining = 0f)
        {
            EventBus.Publish(new StageStatusEvent
            {
                Level = currentLevel,
                KillsInStage = killsInStage,
                EnemiesPerStage = GameConstants.EnemiesPerStage,
                BossActive = isBossFight,
                BossSecondsRemaining = bossSecondsRemaining,
                BossAvailable = bossAvailable,
                HasLostBossOnce = hasLostBossOnce,
            });
        }

        private Enemy CreateEnemyGameObject(bool isBoss)
        {
            var root = new GameObject(isBoss ? "Boss" : "Enemy", typeof(RectTransform), typeof(Image), typeof(Enemy));
            root.transform.SetParent(enemyArea, false);

            var rootRect = root.GetComponent<RectTransform>();
            float displaySize = isBoss ? 220f : 160f;
            rootRect.sizeDelta = new Vector2(displaySize, displaySize);
            rootRect.anchoredPosition = Vector2.zero;

            var bodyImage = root.GetComponent<Image>();
            bodyImage.preserveAspect = true;
            bodyImage.raycastTarget = true;

            // Barra de vida: fondo + relleno, sobre el enemigo
            var barBg = new GameObject("HealthBarBG", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(root.transform, false);
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.5f, 1f);
            barBgRect.anchorMax = new Vector2(0.5f, 1f);
            barBgRect.anchoredPosition = new Vector2(0, 24);
            barBgRect.sizeDelta = new Vector2(isBoss ? 180 : 120, 14);
            barBg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var barFill = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = new Vector2(2, 2);
            barFillRect.offsetMax = new Vector2(-2, -2);
            var fillImage = barFill.GetComponent<Image>();
            // Image.Type.Filled necesita un sprite para rellenar bien (sin uno, Unity
            // no expone ni respeta el Fill Amount correctamente): un pixel blanco 1x1 alcanza.
            fillImage.sprite = PlaceholderArt.SolidPixel();
            fillImage.color = isBoss ? new Color(0.75f, 0.15f, 0.65f, 1f) : new Color(0.8f, 0.2f, 0.2f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            var nameLabelGo = new GameObject("NameLabel", typeof(RectTransform), typeof(Text));
            nameLabelGo.transform.SetParent(root.transform, false);
            var nameRect = nameLabelGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0, 44);
            nameRect.sizeDelta = new Vector2(240, 30);
            var nameText = nameLabelGo.GetComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontSize = isBoss ? 24 : 20;
            nameText.fontStyle = isBoss ? FontStyle.Bold : FontStyle.Normal;
            nameText.color = isBoss ? new Color(1f, 0.85f, 0.3f) : Color.white;
            nameText.raycastTarget = false;

            var enemy = root.GetComponent<Enemy>();
            enemy.AssignReferences(fillImage, nameText);

            return enemy;
        }
    }
}
