using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentGeneration;

public static class TagExclusionGenerator
{
    /// <summary>
    ///     Callers must check the generationReturn for success or failure!
    /// </summary>
    /// <param name="toSave"></param>
    /// <returns></returns>
    public static async Task<(GenerationReturn generationReturn, TagExclusion? returnContent)> Save(
        TagExclusion toSave)
    {
        var validationResult = await Validate(toSave).ConfigureAwait(false);
        if (validationResult.HasError) return (validationResult, null);

        TagExclusion toModify;

        try
        {
            var db = await Db.Context().ConfigureAwait(false);

            if (toSave.Id < 1)
            {
                toSave.Tag = Db.TagListItemCleanup(toSave.Tag);
                toSave.ContentVersion = DateTime.Now.ToUniversalTime().TrimDateTimeToSeconds();

                await db.AddAsync(toSave).ConfigureAwait(false);
                await db.SaveChangesAsync(true).ConfigureAwait(false);

                DataNotifications.PublishDataNotification("Tag Exclusion Generator",
                    DataNotificationContentType.TagExclusion,
                    DataNotificationUpdateType.New, null);

                return (GenerationReturn.Success("Tag Exclusion Saved"), toSave);
            }

            toModify = await db.TagExclusions.SingleAsync(x => x.Id == toSave.Id).ConfigureAwait(false);

            toModify.Tag = Db.TagListItemCleanup(toSave.Tag);
            toModify.ContentVersion = DateTime.Now.ToUniversalTime().TrimDateTimeToSeconds();

            await db.SaveChangesAsync(true).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return (
                GenerationReturn.Error(
                    $"Error with Tag Exclusion {toSave.Tag}",
                    null,
                    e), null);
        }

        DataNotifications.PublishDataNotification("Tag Exclusion Generator",
            DataNotificationContentType.TagExclusion,
            DataNotificationUpdateType.Update, null);

        return (GenerationReturn.Success("Tag Exclusion Saved"), toModify);
    }

    public static async Task<GenerationReturn> Validate(TagExclusion toValidate)
    {
        if (string.IsNullOrWhiteSpace(toValidate.Tag))
            return GenerationReturn.Error("Excluded Tag can not be blank");

        var validationResult = CommonContentValidation.ValidateTags(toValidate.Tag.TrimNullToEmpty());
        if (!validationResult.Valid) return GenerationReturn.Error(validationResult.Explanation);

        var cleanedTag = Db.TagListItemCleanup(toValidate.Tag);

        var db = await Db.Context().ConfigureAwait(false);
        if (db.TagExclusions.Any(x => x.Id != toValidate.Id && x.Tag == cleanedTag))
            return GenerationReturn.Error(
                $"It appears that the tag '{cleanedTag}' is already in the Exclusion List?");

        return GenerationReturn.Success("Tag Exclusion is Valid");
    }
}