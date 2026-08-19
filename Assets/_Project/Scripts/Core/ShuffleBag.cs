using System.Collections.Generic;

namespace TapLegends.Core
{
    /// <summary>
    /// Devuelve indices [0, Count) en orden aleatorio, sin repetir ninguno hasta haber
    /// devuelto todos — recien ahi vuelve a barajar. Pedido explicito del usuario para los
    /// fondos de escenario: "que uno no vuelva a salir hasta que ya hayan salido todos,
    /// para mantener variedad". Clase pura (sin UnityEngine.Random ni MonoBehaviour) a
    /// proposito, para poder testear el algoritmo sin instanciar nada de Unity.
    /// </summary>
    public class ShuffleBag
    {
        private readonly List<int> order = new List<int>();

        public int Count { get; }
        public int Position { get; private set; } = -1;

        /// <summary>Ultimo indice devuelto por Next() — -1 si Next() todavia no se llamo nunca.</summary>
        public int Current => Position >= 0 && Position < order.Count ? order[Position] : -1;

        public ShuffleBag(int count, System.Random rng = null)
        {
            Count = count;
            Reshuffle(rng);
        }

        public int Next(System.Random rng = null)
        {
            Position++;
            if (Position >= order.Count)
            {
                Reshuffle(rng);
                Position = 0;
            }
            return order[Position];
        }

        private void Reshuffle(System.Random rng)
        {
            rng ??= new System.Random();
            order.Clear();
            for (int i = 0; i < Count; i++) order.Add(i);

            // Fisher-Yates.
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        public List<int> GetOrderSnapshot() => new List<int>(order);

        /// <summary>Restaura un guardado. Si el tamaño no coincide (Count cambio — se
        /// agregaron o sacaron fondos entre versiones) lo ignora y se queda con el barajo
        /// nuevo que ya se genero en el constructor, en vez de tirar una excepcion.</summary>
        public void Restore(List<int> savedOrder, int savedPosition)
        {
            if (savedOrder == null || savedOrder.Count != Count) return;
            order.Clear();
            order.AddRange(savedOrder);
            Position = savedPosition;
        }
    }
}
