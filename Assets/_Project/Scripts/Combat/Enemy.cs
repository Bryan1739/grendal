using System;
using System.Collections;
using TapLegends.Core;
using TapLegends.Progression;
using TapLegends.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapLegends.Combat
{
    /// <summary>
    /// Instancia de combate de un enemigo. Recibe taps directamente (IPointerDownHandler),
    /// lo que da multitouch gratis: el EventSystem de uGUI reparte un puntero por dedo.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class Enemy : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Text nameLabel;

        // Color hacia el que se tiñe el enemigo a medida que pierde vida.
        // Placeholder de "esta a punto de morir" mientras no hay animacion de daño real (Etapa 9).
        private static readonly Color DamagedTint = new Color(0.35f, 0.08f, 0.08f);

        private Image bodyImage;
        private RectTransform rectTransform;
        private double currentHealth;
        private double maxHealth;
        private int level;
        private bool isDead;
        private bool isBoss;
        private Color restingTint = Color.white;

        public event Action<Enemy> OnDied;
        public bool IsBoss => isBoss;

        private void Awake()
        {
            bodyImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>Referencias creadas dinamicamente por EnemySpawner (no hay prefab en la v0.1).</summary>
        public void AssignReferences(Image healthBar, Text label)
        {
            healthBarFill = healthBar;
            nameLabel = label;
        }

        public void Setup(EnemyData data, int enemyLevel)
        {
            isDead = false;
            level = enemyLevel;
            isBoss = data.tier == EnemyTier.Boss;
            bodyImage.sprite = data.Art;
            // Sin SetNativeSize(): el tamaño en pantalla lo fija EnemySpawner (160x160),
            // no el tamaño nativo del sprite (que variaria por nivel/tier). preserveAspect
            // ya esta activado en el Image para que no se deforme.

            maxHealth = GameConstants.EnemyHealthForLevel(level);
            if (isBoss) maxHealth *= GameConstants.BossHealthMultiplier;
            currentHealth = maxHealth;

            if (nameLabel != null) nameLabel.text = data.displayName;
            UpdateHealthVisuals();
            bodyImage.color = restingTint;

            rectTransform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isDead || PlayerStats.Instance == null) return;
            TakeDamage(PlayerStats.Instance.TapDamage, eventData.position);
        }

        /// <summary>Daño sin posicion de toque conocida (DPS pasivo de heroes): usa el centro del propio sprite.</summary>
        public void TakeDamage(double amount)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            TakeDamage(amount, RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position));
        }

        public void TakeDamage(double amount, Vector2 screenPosition)
        {
            if (isDead) return;

            currentHealth -= amount;
            UpdateHealthVisuals();

            if (DamageNumberPool.Instance != null)
            {
                var canvas = rectTransform.GetComponentInParent<Canvas>();
                Vector2 anchoredPos = ScreenToPopupLayerPosition(canvas, screenPosition);
                DamageNumberPool.Instance.Spawn(amount, anchoredPos);
            }

            StopAllCoroutines();
            StartCoroutine(HitJuice());

            if (currentHealth <= 0d) Die();
        }

        private Vector2 ScreenToPopupLayerPosition(Canvas canvas, Vector2 screenPosition)
        {
            var popupLayer = DamageNumberPool.Instance.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupLayer, screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var localPoint);
            return localPoint;
        }

        private void Die()
        {
            isDead = true;
            double gold = GameConstants.EnemyGoldForLevel(level);
            if (isBoss) gold *= GameConstants.BossGoldMultiplier;
            if (PrestigeManager.Instance != null) gold *= PrestigeManager.Instance.GoldMultiplier;
            if (EquipmentManager.Instance != null) gold *= EquipmentManager.Instance.GoldMultiplier;
            if (PetManager.Instance != null) gold *= PetManager.Instance.GoldMultiplier;
            if (SkillManager.Instance != null) gold *= SkillManager.Instance.GoldBuffMultiplier;

            if (GoldWallet.Instance != null) GoldWallet.Instance.Add(gold);

            EventBus.Publish(new EnemyKilledEvent
            {
                EnemyId = name,
                GoldDropped = gold,
                WasBoss = isBoss
            });

            OnDied?.Invoke(this);
        }

        /// <summary>Actualiza la barra de vida y el tinte de daño (mas rojo/oscuro cuanta menos vida queda).</summary>
        private void UpdateHealthVisuals()
        {
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)(currentHealth / maxHealth)) : 0f;

            if (healthBarFill != null) healthBarFill.fillAmount = ratio;

            // Placeholder de "esta a punto de morir" mientras no hay animacion de daño real (Etapa 9).
            restingTint = Color.Lerp(DamagedTint, Color.white, ratio);
        }

        private IEnumerator HitJuice()
        {
            // Squash & stretch + flash blanco. Ver TapLegends-Plan.md: el juice va desde el dia 1.
            var flashColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            bodyImage.color = flashColor;
            Vector3 punch = new Vector3(0.9f, 1.1f, 1f);
            rectTransform.localScale = punch;

            float t = 0f;
            const float duration = 0.12f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                rectTransform.localScale = Vector3.Lerp(punch, Vector3.one, p);
                // Vuelve al tinte de daño actual, no siempre a blanco puro: asi el color
                // "herido" persiste entre golpes en vez de resetearse cada vez.
                bodyImage.color = Color.Lerp(flashColor, restingTint, p);
                yield return null;
            }

            rectTransform.localScale = Vector3.one;
            bodyImage.color = restingTint;
        }
    }
}
