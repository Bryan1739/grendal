using System.Collections;
using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Pool de numeros de daño flotantes. Generar/destruir GameObjects en cada tap
    /// mataria el rendimiento (ver Etapa 12 del plan) — por eso se reutilizan.
    /// Construido 100% por codigo, sin depender de un prefab.
    /// </summary>
    public class DamageNumberPool : MonoBehaviour
    {
        public static DamageNumberPool Instance { get; private set; }

        [SerializeField] private int prewarmCount = 12;
        [SerializeField] private float riseDistance = 60f;
        [SerializeField] private float lifetime = 0.6f;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private RectTransform layer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Init(RectTransform popupLayer)
        {
            layer = popupLayer;
            for (int i = 0; i < prewarmCount; i++)
                pool.Enqueue(CreatePopupObject());
        }

        public void Spawn(double amount, Vector2 anchoredPosition, bool isCrit = false)
        {
            if (layer == null) return;

            var go = pool.Count > 0 ? pool.Dequeue() : CreatePopupObject();
            go.transform.SetParent(layer, false);
            go.SetActive(true);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;

            var text = go.GetComponent<Text>();
            text.text = NumberFormatter.Format(amount);
            text.color = isCrit ? new Color(1f, 0.85f, 0.2f) : Color.white;
            text.fontSize = isCrit ? 34 : 26;

            var canvasGroup = go.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            StartCoroutine(AnimateAndReturn(go, rect, canvasGroup));
        }

        private IEnumerator AnimateAndReturn(GameObject go, RectTransform rect, CanvasGroup canvasGroup)
        {
            float t = 0f;
            Vector2 start = rect.anchoredPosition;
            Vector2 end = start + new Vector2(Random.Range(-15f, 15f), riseDistance);

            while (t < lifetime)
            {
                t += Time.deltaTime;
                float p = t / lifetime;
                rect.anchoredPosition = Vector2.Lerp(start, end, p);
                canvasGroup.alpha = 1f - p;
                yield return null;
            }

            go.SetActive(false);
            pool.Enqueue(go);
        }

        private GameObject CreatePopupObject()
        {
            var go = new GameObject("DamagePopup", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
            go.transform.SetParent(layer != null ? layer : transform, false);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 26;
            text.color = Color.white;
            text.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            // Ancla y pivote en el centro: anchoredPosition debe medirse desde el centro
            // del layer padre, que es justo lo que devuelve ScreenPointToLocalPointInRectangle
            // en Enemy.cs. Sin esto el popup aparece desplazado (anclado por defecto a la esquina).
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160, 50);

            go.SetActive(false);
            return go;
        }
    }
}
