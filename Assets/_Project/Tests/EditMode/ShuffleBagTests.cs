using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TapLegends.Core;

namespace TapLegends.Tests
{
    public class ShuffleBagTests
    {
        [Test]
        public void Next_UnaVueltaCompleta_DevuelveCadaIndiceUnaSolaVez()
        {
            var bag = new ShuffleBag(20, new Random(1));

            var seen = new HashSet<int>();
            for (int i = 0; i < 20; i++) seen.Add(bag.Next());

            Assert.That(seen.Count, Is.EqualTo(20));
            Assert.That(seen, Is.EquivalentTo(Enumerable.Range(0, 20)));
        }

        [Test]
        public void Next_SegundaVuelta_TambienDevuelveCadaIndiceUnaSolaVez()
        {
            var bag = new ShuffleBag(20, new Random(2));

            for (int i = 0; i < 20; i++) bag.Next(); // agota la primera vuelta

            var seen = new HashSet<int>();
            for (int i = 0; i < 20; i++) seen.Add(bag.Next());

            Assert.That(seen.Count, Is.EqualTo(20));
            Assert.That(seen, Is.EquivalentTo(Enumerable.Range(0, 20)));
        }

        [Test]
        public void Next_ConUnSoloElemento_SiempreDevuelveEseIndice()
        {
            var bag = new ShuffleBag(1, new Random(3));

            for (int i = 0; i < 5; i++)
                Assert.That(bag.Next(), Is.EqualTo(0));
        }

        [Test]
        public void Current_AntesDeLlamarNext_EsMenosUno()
        {
            var bag = new ShuffleBag(20, new Random(4));
            Assert.That(bag.Current, Is.EqualTo(-1));
        }

        [Test]
        public void Current_DespuesDeNext_CoincideConElUltimoDevuelto()
        {
            var bag = new ShuffleBag(20, new Random(5));
            int last = bag.Next();
            Assert.That(bag.Current, Is.EqualTo(last));
        }

        [Test]
        public void Restore_RetomaDesdeElMismoPuntoSinRepetirNiSaltear()
        {
            var original = new ShuffleBag(20, new Random(6));
            for (int i = 0; i < 7; i++) original.Next(); // avanza a mitad de la primera vuelta

            var snapshot = original.GetOrderSnapshot();
            int position = original.Current;

            var restored = new ShuffleBag(20, new Random(999)); // semilla distinta a proposito
            restored.Restore(snapshot, position);

            // Debe completar la MISMA vuelta que "original" habria completado, sin repetir
            // ninguno de los primeros 7 ya salidos.
            var alreadySeen = new HashSet<int>(snapshot.Take(7));
            var rest = new HashSet<int>();
            for (int i = 0; i < 13; i++) rest.Add(restored.Next());

            Assert.That(rest.Overlaps(alreadySeen), Is.False);
            Assert.That(alreadySeen.Union(rest).Count(), Is.EqualTo(20));
        }

        [Test]
        public void Restore_ConTamañoDistinto_SeIgnoraYSigueConElBarajoNuevo()
        {
            var bag = new ShuffleBag(20, new Random(7));
            var wrongSizeOrder = new List<int> { 0, 1, 2 }; // tamaño 3, no 20

            bag.Restore(wrongSizeOrder, 1);

            // No exploto, y el barajo interno sigue siendo de tamaño 20 (el del constructor).
            var seen = new HashSet<int>();
            for (int i = 0; i < 20; i++) seen.Add(bag.Next());
            Assert.That(seen, Is.EquivalentTo(Enumerable.Range(0, 20)));
        }
    }
}
