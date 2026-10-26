using System;
using System.Globalization;

namespace StellarIdle.Core
{
    /// <summary>Idle-game number formatting: 1.23K, 45.6M, 7.89aa ...</summary>
    public static class NumberFormat
    {
        static readonly string[] Short = { "", "K", "M", "B", "T" };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";
            if (value < 0) return "-" + Format(-value);
            if (value < 1000)
                return value < 10 && value % 1 != 0
                    ? Truncate(value, 1).ToString("0.#", CultureInfo.InvariantCulture)
                    : Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double mantissa = value / Math.Pow(10, tier * 3);
            // Floating point can produce 999.9999 or 1000 at tier boundaries.
            if (mantissa >= 1000) { mantissa /= 1000; tier++; }
            if (mantissa < 1) { mantissa *= 1000; tier--; }

            string digits = mantissa < 10 ? Truncate(mantissa, 2).ToString("0.00", CultureInfo.InvariantCulture)
                : mantissa < 100 ? Truncate(mantissa, 1).ToString("0.0", CultureInfo.InvariantCulture)
                : Math.Floor(mantissa).ToString("0", CultureInfo.InvariantCulture);
            return digits + Suffix(tier);
        }

        public static string Suffix(int tier)
        {
            if (tier < Short.Length) return Short[tier];
            int n = tier - Short.Length;
            return new string(new[] { (char)('a' + n / 26 % 26), (char)('a' + n % 26) });
        }

        /// <summary>"2h 05m", "12m 30s", "45s".</summary>
        public static string Duration(double seconds)
        {
            long s = (long)Math.Max(0, Math.Ceiling(seconds));
            long h = s / 3600, m = s % 3600 / 60, sec = s % 60;
            if (h > 0) return $"{h}h {m:00}m";
            if (m > 0) return $"{m}m {sec:00}s";
            return $"{sec}s";
        }

        static double Truncate(double v, int decimals)
        {
            double f = Math.Pow(10, decimals);
            return Math.Floor(v * f + 1e-9) / f;
        }
    }
}
