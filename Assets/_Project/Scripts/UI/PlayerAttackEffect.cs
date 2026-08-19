using System.Collections;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Tajo ancho y curvo que aparece sobre el enemigo en cada tap — puramente decorativo.
    /// Color por skin: morado para Hierro Negra (a tono con su estela de ataque, pedido
    /// explícito), blanco para el resto (antes dorado — "blanco" pedido explícito). Alterna
    /// entre unas pocas variantes de rotación/espejado en cada ataque ("variaciones
    /// intercaladas... para que se vea más genial", pedido explícito) para que no se vea
    /// siempre el mismo corte. Ya NO viaja del héroe al enemigo (esa "estela de viento" se
    /// sacó — el usuario reportó que la línea se veía rara y que el tajo solo ya alcanza
    /// para dar a entender el golpe).
    /// </summary>
    public class PlayerAttackEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform effectOrigin;
        [SerializeField] private RectTransform effectTarget;
        [SerializeField] private Transform effectParent;

        private const float SlashDelay = 0.08f; // sincroniza el tajo con el swing del arte, no aparece instantaneo
        private const float SlashDuration = 0.16f;
        private static readonly Color DefaultColor = Color.white;
        private static readonly Color HierroNegraColor = new Color(0.62f, 0.22f, 0.85f);

        private struct Variant
        {
            public float StartOffset;
            public float EndOffset;
            public bool Mirror;
            public Variant(float start, float end, bool mirror) { StartOffset = start; EndOffset = end; Mirror = mirror; }
        }

        private static readonly Variant[] Variants =
        {
            new Variant(-30f, 18f, false),
            new Variant(30f, -18f, true),
            new Variant(-55f, 35f, false),
        };

        private int variantIndex;

        private void OnEnable() => EventBus.Subscribe<PlayerTappedEvent>(HandleTapped);
        private void OnDisable() => EventBus.Unsubscribe<PlayerTappedEvent>(HandleTapped);

        private void HandleTapped(PlayerTappedEvent evt)
        {
            if (effectOrigin == null || effectTarget == null || effectParent == null) return;
            StartCoroutine(PlayEffect());
        }

        private IEnumerator PlayEffect()
        {
            var skin = WeaponSkinManager.Instance != null ? WeaponSkinManager.Instance.Selected : null;
            var baseColor = skin != null && skin.id == "espada_hierro_negra" ? HierroNegraColor : DefaultColor;

            Vector3 from = effectOrigin.position;
            Vector3 to = effectTarget.position;
            float distance = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var variant = Variants[variantIndex];
            variantIndex = (variantIndex + 1) % Variants.Length;

            if (SlashDelay > 0f) yield return new WaitForSeconds(SlashDelay);
            yield return SlashHit(to, angle, distance, baseColor, variant);
        }

        /// <summary>Tajo ancho y curvo que aparece sobre el objetivo y gira rápido, como un
        /// espadazo conectando.</summary>
        private IEnumerator SlashHit(Vector3 at, float angle, float distance, Color baseColor, Variant variant)
        {
            var go = new GameObject("SlashHit", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(effectParent, false);

            var img = go.GetComponent<Image>();
            img.sprite = PlaceholderArt.SlashArc();
            img.raycastTarget = false;
            img.color = baseColor;

            float width = Mathf.Clamp(distance * 0.65f, 90f, 190f);
            rect.sizeDelta = new Vector2(width, width * 0.42f);
            rect.position = at;

            float startAngle = angle + variant.StartOffset;
            float endAngle = angle + variant.EndOffset;
            float mirrorX = variant.Mirror ? -1f : 1f;

            float t = 0f;
            while (t < SlashDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / SlashDuration);
                float ease = 1f - (1f - p) * (1f - p);

                rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, endAngle, ease));
                float scale = Mathf.Lerp(0.55f, 1.15f, ease);
                rect.localScale = new Vector3(mirrorX * scale, scale, 1f);

                var c = img.color;
                c.a = baseColor.a * (1f - p * p);
                img.color = c;
                yield return null;
            }

            Destroy(go);
        }
    }
}
