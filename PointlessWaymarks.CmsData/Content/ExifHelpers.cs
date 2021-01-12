
using System;
using MetadataExtractor;

namespace PointlessWaymarks.CmsData.Content
{
    public static class ExifHelpers
    {
        public static string ShutterSpeedToHumanReadableString(int numerator, int denominator)
        {
            return numerator < 0
                ? Math.Round(Math.Pow(2, (double) -1 * numerator / denominator), 1).ToString("N1")
                : $"1/{Math.Round(Math.Pow(2, (double) numerator / denominator), 1):N0}";
        }

        public static string ShutterSpeedToHumanReadableString(Rational rational)
        {
            return ShutterSpeedToHumanReadableString((Rational?) rational);
        }

        public static string ShutterSpeedToHumanReadableString(Rational? toProcess)
        {
            if (toProcess == null) return string.Empty;

            if (toProcess.Value.Numerator < 0)
                return Math.Round(Math.Pow(2, (double) -1 * toProcess.Value.Numerator / toProcess.Value.Denominator), 1)
                    .ToString("N1");

            return
                $"1/{Math.Round(Math.Pow(2, (double) toProcess.Value.Numerator / toProcess.Value.Denominator), 1):N0}";
        }
    }
}