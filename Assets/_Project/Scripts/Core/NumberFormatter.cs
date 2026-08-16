using System;
using System.Text;

namespace TapLegends.Core
{
    /// <summary>
    /// Formatea valores double grandes con notacion abreviada: 1.23K, 4.56M, 7.89B,
    /// y despues de billones pasa a letras: 1.00aa, 1.00ab... (estilo idle game clasico).
    /// Todo valor de juego (oro, daño, vida) debe pasar por aqui antes de mostrarse.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };
        private static readonly StringBuilder Builder = new StringBuilder(16);

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";

            bool negative = value < 0;
            double abs = Math.Abs(value);

            string result;
            if (abs < 1000d)
            {
                result = abs < 10d ? abs.ToString("0.0") : Math.Floor(abs).ToString("0");
            }
            else
            {
                int tier = (int)(Math.Log10(abs) / 3d);
                double scaled = abs / Math.Pow(1000d, tier);

                if (tier < Suffixes.Length)
                {
                    result = scaled.ToString("0.00") + Suffixes[tier];
                }
                else
                {
                    // Mas alla de T: notacion de letras aa, ab, ac... az, ba, bb...
                    int letterIndex = tier - Suffixes.Length;
                    result = scaled.ToString("0.00") + LetterSuffix(letterIndex);
                }
            }

            return negative ? "-" + result : result;
        }

        private static string LetterSuffix(int index)
        {
            Builder.Clear();
            int first = index / 26;
            int second = index % 26;
            Builder.Append((char)('a' + first));
            Builder.Append((char)('a' + second));
            return Builder.ToString();
        }
    }
}
