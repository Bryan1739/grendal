using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Oro del jugador. En memoria por ahora — la persistencia llega en la Etapa 4
    /// (ver TapLegends-Plan.md). Nunca usar int/float aqui: siempre double.
    /// </summary>
    public class GoldWallet : MonoBehaviour
    {
        public static GoldWallet Instance { get; private set; }

        [SerializeField] private double startingGold = 0d;

        public double Gold { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Gold = startingGold;
        }

        public void Add(double amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            EventBus.Publish(new GoldChangedEvent { NewTotal = Gold, Delta = amount });
        }

        public bool TrySpend(double amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            EventBus.Publish(new GoldChangedEvent { NewTotal = Gold, Delta = -amount });
            return true;
        }

        /// <summary>Solo para el menu de debug.</summary>
        public void DebugAdd(double amount) => Add(amount);

        /// <summary>Restaura el oro desde un guardado. No pasa por TrySpend/Add: no es una transaccion de juego.</summary>
        public void LoadState(double savedGold)
        {
            Gold = savedGold;
            EventBus.Publish(new GoldChangedEvent { NewTotal = Gold, Delta = 0d });
        }
    }
}
