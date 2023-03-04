using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.CmsWpfControls.DataEntry;

public static class StringDataEntryTypes
{
    public static async Task<StringDataEntryContext> CreateSlugInstance(ITitleSummarySlugFolder? dbEntry)
    {
        var newContext = StringDataEntryContext.CreateInstance();

        newContext.Title = "Slug";
        newContext.HelpText = "This will be the Folder and File Name used in URLs - limited to a-z 0-9 _ -";
        newContext.ReferenceValue = dbEntry?.Slug ?? string.Empty;
        newContext.UserValue = StringTools.NullToEmptyTrim(dbEntry?.Slug);
        newContext.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateSlugLocal };

        await newContext.CheckForChangesAndValidationIssues();

        return newContext;
    }

    public static async Task<StringDataEntryContext> CreateSummaryInstance(ITitleSummarySlugFolder? dbEntry)
    {
        var newContext = StringDataEntryContext.CreateInstance();

        newContext.Title = "Summary";
        newContext.HelpText = "A short text entry that will show in Search and short references to the content";
        newContext.ReferenceValue = dbEntry?.Summary ?? string.Empty;
        newContext.UserValue = StringTools.NullToEmptyTrim(dbEntry?.Summary);
        newContext.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateSummary };

        await newContext.CheckForChangesAndValidationIssues();

        return newContext;
    }

    public static async Task<StringDataEntryContext> CreateTitleInstance(ITitleSummarySlugFolder? dbEntry)
    {
        var newContext = StringDataEntryContext.CreateInstance();

        newContext.Title = "Title";
        newContext.HelpText = "Title Text";
        newContext.ReferenceValue = dbEntry?.Title ?? string.Empty;
        newContext.UserValue = StringTools.NullToEmptyTrim(dbEntry?.Title);
        newContext.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateTitle };

        await newContext.CheckForChangesAndValidationIssues();

        return newContext;
    }
}