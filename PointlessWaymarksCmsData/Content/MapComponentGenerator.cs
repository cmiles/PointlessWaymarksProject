using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Content
{
    public static class MapComponentGenerator
    {
        public static async Task<GenerationReturn> Validate(MapComponent mapComponent,
            List<MapComponentElement> mapElements)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    mapComponent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateMapComponent(mapComponent, mapElements);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, mapComponent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(mapComponent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, mapComponent.ContentId);

            return await GenerationReturn.Success("GeoJson Content Validation Successful");
        }

        private static async Task<(bool valid, string explanation)> ValidateMapComponent(MapComponent toValidate,
            List<MapComponentElement> elements)
        {
            if (toValidate == null) return (false, "Null Content to Validate");

            var isNewEntry = toValidate.Id < 1;

            var isValid = true;
            var errorMessage = new List<string>();

            if (toValidate.ContentId == Guid.Empty)
            {
                isValid = false;
                errorMessage.Add("Content ID is Empty");
            }

            var summaryValidation = CommonContentValidation.ValidateSummary(toValidate.Summary);

            if (!summaryValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(summaryValidation.explanation);
            }

            var (createdUpdatedIsValid, createdUpdatedExplanation) =
                CommonContentValidation.ValidateCreatedAndUpdatedBy(toValidate, isNewEntry);

            if (!createdUpdatedIsValid)
            {
                isValid = false;
                errorMessage.Add(createdUpdatedExplanation);
            }

            if (elements == null || !elements.Any())
            {
                isValid = false;
                errorMessage.Add("A map must have at least one element");
            }

            if (!isValid) return (isValid, string.Join(Environment.NewLine, errorMessage));

            if (elements.Any(x => x.MapComponentContentId != toValidate.ContentId))
            {
                isValid = false;
                errorMessage.Add("Not all map elements are correctly associated with the map.");
            }

            if (!elements.Any(x => x.InitialFocus))
            {
                isValid = false;
                errorMessage.Add("Please set at least one element as the initial focus.");
            }

            if (!isValid) return (isValid, string.Join(Environment.NewLine, errorMessage));

            foreach (var loopElements in elements)
            {
                if (await Db.ContentIdIsSpatialContentInDatabase(loopElements.MapComponentContentId)) continue;
                isValid = false;
                errorMessage.Add("Could not find all Elements Content Items in Db?");
                break;
            }

            return (isValid, string.Join(Environment.NewLine, errorMessage));
        }
    }
}