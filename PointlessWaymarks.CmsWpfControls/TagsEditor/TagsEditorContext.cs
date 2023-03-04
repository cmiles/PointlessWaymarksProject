using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.TagsEditor;

public partial class TagsEditorContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private ITag _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _tags = string.Empty;
    [ObservableProperty] private string _tagsValidationMessage;

    private TagsEditorContext(StatusControlContext statusContext, ITag dbEntry)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;

        DbEntry = dbEntry;
        HelpText =
            "Comma separated tags - only a-z 0-9 _ - [space] are valid, each tag must be less than 200 characters long.";
        Tags = dbEntry?.Tags ?? string.Empty;
        Tags = TagListString();
    }

    public void CheckForChangesAndValidationIssues()
    {
        Tags = SlugTools.CreateRelaxedInputSpacedString(true, Tags, new List<char> { ',', ' ', '-', '_' }).ToLower();

        HasChanges = !TagSlugList().SequenceEqual(DbTagList());

        var tagValidation = CommonContentValidation.ValidateTags(Tags);

        HasValidationIssues = !tagValidation.Valid;
        TagsValidationMessage = tagValidation.Explanation;
    }

    public static TagsEditorContext CreateInstance(StatusControlContext statusContext, ITag dbEntry)
    {
        return new TagsEditorContext(statusContext, dbEntry);
    }

    private List<string> DbTagList()
    {
        return string.IsNullOrWhiteSpace(DbEntry?.Tags) ? new List<string>() : Db.TagListParseToSlugs(DbEntry, false);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
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