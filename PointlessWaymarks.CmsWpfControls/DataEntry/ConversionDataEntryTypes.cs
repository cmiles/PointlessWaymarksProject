using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.DataEntry
{
    public static class ConversionDataEntryTypes
    {
        public static (bool passed, string conversionMessage, Guid? value) GuidNullableAndBracketCodeConversion(
            string userText)
        {
            var cleanedUserText = userText.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

            if (cleanedUserText.Contains("{"))
            {
                var possibleBracketGuid = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(cleanedUserText);

                if (possibleBracketGuid != null)
                    return (true, $"Extracted {possibleBracketGuid} from {cleanedUserText}", possibleBracketGuid);
            }

            return Guid.TryParse(cleanedUserText, out var parsedValue)
                ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
                : (false, $"Could not convert {userText} into an valid Content Id?", null);
        }
    }
}
