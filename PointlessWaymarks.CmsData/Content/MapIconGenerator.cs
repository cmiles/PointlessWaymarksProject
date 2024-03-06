using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Content;

public static class MapIconGenerator
{
    public static async Task GenerateMapIconsFile()
    {
        var mapIconsFile = UserSettingsSingleton.CurrentSettings().LocalSiteMapIconsDataFile();

        if (mapIconsFile.Exists)
        {
            mapIconsFile.Delete();
            mapIconsFile.Refresh();
        }

        var serializedIcons = await SerializedMapIcons();

        await FileManagement.WriteAllTextToFileAndLogAsync(mapIconsFile.FullName, serializedIcons)
            .ConfigureAwait(false);
    }

    public static async Task<OneOf<Success, Error<string>>> SaveMapIconAndGenerateMapIconsJson(MapIcon toSave)
    {
        var validation = await ValidateMapIcon(toSave);

        if (!validation.Valid) return new Error<string>(validation.Explanation);

        await Db.SaveMapIcon(toSave);

        await GenerateMapIconsFile();

        return new Success();
    }

    public static async Task<string> SerializedMapIcons()
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allMapIcons = await db.MapIcons.OrderBy(x => x.IconName).ToListAsync();

        return JsonSerializer.Serialize(allMapIcons, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }

    public static async Task<IsValid> ValidateMapIcon(MapIcon toValidate)
    {
        var isValid = true;
        var errorMessage = new List<string>();

        if (toValidate.ContentId == Guid.Empty)
        {
            isValid = false;
            errorMessage.Add("Content ID is Empty");
        }

        var nameValidation =
            await CommonContentValidation.ValidateMapIconName(toValidate.IconName, toValidate.ContentId);
        if (!nameValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(nameValidation.Explanation);
        }

        var svgValidation = await CommonContentValidation.ValidateSvgTag(toValidate.IconSvg);
        if (!svgValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(svgValidation.Explanation);
        }

        if (toValidate.ContentVersion == DateTime.MinValue)
        {
            isValid = false;
            errorMessage.Add($"Content Version of {toValidate.ContentVersion} is not valid.");
        }

        return new IsValid(isValid, string.Join(Environment.NewLine, errorMessage));
    }
}