using NUnit.Framework;
using TapLegends.Core;

namespace TapLegends.Tests
{
    public class NumberFormatterTests
    {
        [TestCase(0d, "0.0")]
        [TestCase(5.5, "5.5")]
        [TestCase(9.94, "9.9")] // <10 usa ToString("0.0"), que SI redondea (a diferencia de Math.Floor de la rama >=10)
        [TestCase(999d, "999")]
        [TestCase(123.9, "123")] // Math.Floor: nunca muestra un nivel que todavia no se alcanzo
        public void Format_NumerosChicos_SinSufijo(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        [Test]
        public void Format_Mil_UsaSufijoK()
        {
            Assert.That(NumberFormatter.Format(1000d), Is.EqualTo("1.00K"));
        }

        [Test]
        public void Format_UnMillon_UsaSufijoM()
        {
            Assert.That(NumberFormatter.Format(1_000_000d), Is.EqualTo("1.00M"));
        }

        [Test]
        public void Format_MilQuinientos_RedondeaADosDecimales()
        {
            Assert.That(NumberFormatter.Format(1500d), Is.EqualTo("1.50K"));
        }

        [Test]
        public void Format_MasAlaDeTrillones_UsaSufijoDeLetras()
        {
            // 1e15 = 1 mas alla de T (10^12) -> primer escalon de letras: "aa"
            string result = NumberFormatter.Format(1e15);
            Assert.That(result, Does.EndWith("aa"));
        }

        [Test]
        public void Format_Negativo_MantieneElSigno()
        {
            Assert.That(NumberFormatter.Format(-50d), Is.EqualTo("-50"));
        }

        [Test]
        public void Format_NaN_DevuelveInfinito()
        {
            Assert.That(NumberFormatter.Format(double.NaN), Is.EqualTo("∞"));
        }

        [Test]
        public void Format_Infinito_DevuelveInfinito()
        {
            Assert.That(NumberFormatter.Format(double.PositiveInfinity), Is.EqualTo("∞"));
        }
    }
}
