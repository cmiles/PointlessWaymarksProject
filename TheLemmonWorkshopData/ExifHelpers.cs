using System;

namespace TheLemmonWorkshopData
{
    public static class ExifHelpers
    {
        public static string ShutterSpeedToHumanReadableString(int numerator, int denominator)
        {
            return numerator < 0
                ? Math.Round(Math.Pow(2, (double) -1 * numerator / denominator), 1).ToString("N1")
                : $"1/{Math.Round(Math.Pow(2, (double) numerator / denominator), 1):N0}";
        }
    }
}