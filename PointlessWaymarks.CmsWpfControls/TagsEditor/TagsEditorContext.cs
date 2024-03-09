using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.TagsEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TagsEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    private TagsEditorContext(StatusControlContext statusContext, ITag? dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        DbEntry = dbEntry;
        HelpText =
            "Comma separated tags - only a-z 0-9  - [space] are valid, each tag must be less than 200 characters long.";
        Tags = dbEntry?.Tags ?? string.Empty;
        Tags = TagListString();

        PropertyChanged += OnPropertyChanged;
    }

    public ITag? DbEntry { get; set; }
    public string HelpText { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Tags { get; set; }
    public string TagsValidationMessage { get; set; } = string.Empty;

    public void CheckForChangesAndValidationIssues()
    {
        Tags = SlugTools.CreateRelaxedInputSpacedString(true, Tags, new List<char> { ',', ' ', '-', '_' }).ToLower();

        HasChanges = !TagSlugList().SequenceEqual(DbTagList());

        var tagValidation = CommonContentValidation.ValidateTags(Tags);

        HasValidationIssues = !tagValidation.Valid;
        TagsValidationMessage = tagValidation.Explanation;
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static Task<TagsEditorContext> CreateInstance(StatusControlContext? statusContext, ITag? dbEntry)
    {
        var factoryContext = statusContext ?? new StatusControlContext();

        var newItem = new TagsEditorContext(factoryContext, dbEntry);
        newItem.CheckForChangesAndValidationIssues();

        return Task.FromResult(newItem);
    }

    private List<string> DbTagList()
    {
        return string.IsNullOrWhiteSpace(DbEntry?.Tags) ? new List<string>() : Db.TagListParseToSlugs(DbEntry, false);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    public List<string> TagList()
    {
        return string.IsNullOrWhiteSpace(Tags) ? new List<string>() : Db.TagListParse(Tags);
    }

    public string TagListString()
    {
        return string.IsNullOrWhiteSpace(Tags) ? string.Empty : Db.TagListJoin(TagList());
    }

    public List<string> TagSlugList()
    {
        return string.IsNullOrWhiteSpace(Tags) ? new List<string>() : Db.TagListParseToSlugs(Tags, false);
    }
}