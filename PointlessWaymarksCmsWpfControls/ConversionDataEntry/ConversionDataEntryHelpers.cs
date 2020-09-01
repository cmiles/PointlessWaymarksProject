using System;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.ConversionDataEntry
{
    public static class ConversionDataEntryHelpers
    {
        public static (bool passed, string conversionMessage, DateTime value) DateTimeConversion(string userText)
        {
            var cleanedUserText = userText.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedUserText))
                return (false, "Please enter a valid number", DateTime.MinValue);

            return DateTime.TryParse(userText, out var parsedValue)
                ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
                : (false, $"Could not convert {userText} into an Date/Time?", DateTime.MinValue);
        }

        public static (bool passed, string conversionMessage, int value) IntConversion(string userText)
        {
            var cleanedUserText = userText.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedUserText)) return (false, "Please enter a valid number", 0);

            return int.TryParse(userText, out var parsedValue)
                ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
                : (false, $"Could not convert {userText} into an Integer?", 0);
        }

        public static (bool passed, string conversionMessage, DateTime? value) NullableDateTimeConversion(
            string userText)
        {
            var cleanedUserText = userText.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

            return DateTime.TryParse(userText, out var parsedValue)
                ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
                : (false, $"Could not convert {userText} into an Date/Time?", DateTime.MinValue);
        }

        public static (bool passed, string conversionMessage, int? value) NullableIntConversion(string userText)
        {
            var cleanedUserText = userText.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

            return int.TryParse(userText, out var parsedValue)
                ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
                : (false, $"Could not convert {userText} into an Integer?", 0);
        }
    }
}