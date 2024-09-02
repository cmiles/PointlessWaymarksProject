using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PostContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PostContentEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;
    
    private PostContentEditorContext(StatusControlContext statusContext, PostContent dbEntry)
    {
        StatusContext = statusContext;
        
        BuildCommands();
        
        DbEntry = dbEntry;
        
        PropertyChanged += OnPropertyChanged;
    }
    
    public BodyContentEditorContext? BodyContent { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public PostContent DbEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public OptionalLocationEntryContext? OptionalLocationEntry { get; set; }
    
    public string PostEditorHelpText =>
        @"
### Post Content

Posts are the most 'generic' content type and will, by default, be included on the main page of the site in chronological order. One of the most useful functions of a Post is to combine other pieces and types of content.

For the most part you can think of a Post the same way you would in any other blog/web publishing system. Some systems differentiate between Posts and Pages - the Pointless Waymarks CMS does not and only has Posts.

If your intent is just to put a single piece of content onto the main page of the site you may not need to create a post... Most Content Types can also appear on the main page of the site and include a Title, Body, Tags, etc.

";
    
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }
    
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    
    [BlockingCommand]
    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var possibleTags = await OptionalLocationEntry!.GetFeatureIntersectTagsWithUiAlerts();
        
        if (possibleTags.Any())
            TagEdit!.Tags =
                $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }
    
    public static async Task<PostContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        PostContent? toLoad = null)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newContext = new PostContentEditorContext(factoryStatusContext,
            NewContentModels.InitializePostContent(toLoad));
        await newContext.LoadData(toLoad);
        return newContext;
    }
    
    private PostContent CurrentStateToPostContent()
    {
        var newEntry = PostContent.CreateInstance();
        
        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }
        
        newEntry.Folder = TitleSummarySlugFolder!.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed!.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.Latitude = OptionalLocationEntry!.LatitudeEntry!.UserValue;
        newEntry.Longitude = OptionalLocationEntry.LongitudeEntry!.UserValue;
        newEntry.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;
        newEntry.ShowLocation = OptionalLocationEntry.ShowLocationEntry!.UserValue;
        
        return newEntry;
    }
    
    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.UserValue} {UpdateNotes!.UserValue}",
            StatusContext.ProgressTracker());
    }
    
    
    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }
        
        var linkString = BracketCodePosts.Create(DbEntry);
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Clipboard.SetText(linkString);
        
        await StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }
    
    public async Task LoadData(PostContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DbEntry = NewContentModels.InitializePostContent(toLoad);
        
        TitleSummarySlugFolder =
            await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, null, null, null);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        
        OptionalLocationEntry = await OptionalLocationEntryContext.CreateInstance(StatusContext, DbEntry);
        
        HelpContext = new HelpDisplayContext([
            PostEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        ]);
        
        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
    
    [BlockingCommand]
    private async Task PointFromLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("The Photo must be saved before creating a Point.");
            return;
        }
        
        if (OptionalLocationEntry!.LatitudeEntry!.UserValue == null ||
            OptionalLocationEntry.LongitudeEntry!.UserValue == null)
        {
            await StatusContext.ToastError("Latitude or Longitude is missing?");
            return;
        }
        
        var latitudeValidation =
            await CommonContentValidation.LatitudeValidation(OptionalLocationEntry.LatitudeEntry.UserValue.Value);
        var longitudeValidation =
            await CommonContentValidation.LongitudeValidation(OptionalLocationEntry.LongitudeEntry.UserValue.Value);
        
        if (!latitudeValidation.Valid || !longitudeValidation.Valid)
        {
            await StatusContext.ToastError("Latitude/Longitude is not valid?");
            return;
        }
        
        var frozenNow = DateTime.Now;
        
        var newPartialPoint = PointContent.CreateInstance();
        
        newPartialPoint.CreatedOn = frozenNow;
        newPartialPoint.FeedOn = frozenNow;
        newPartialPoint.BodyContent = BracketCodePosts.Create(DbEntry);
        newPartialPoint.Title = $"Point From {TitleSummarySlugFolder!.TitleEntry.UserValue}";
        newPartialPoint.Tags = TagEdit!.TagListString();
        newPartialPoint.Slug = SlugTools.CreateSlug(true, newPartialPoint.Title);
        newPartialPoint.Latitude = OptionalLocationEntry.LatitudeEntry.UserValue.Value;
        newPartialPoint.Longitude = OptionalLocationEntry.LongitudeEntry.UserValue.Value;
        newPartialPoint.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;
        
        var pointWindow = await PointContentEditorWindow.CreateInstance(newPartialPoint);
        
        await pointWindow.PositionWindowAndShowOnUiThread();
    }
    
    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(false);
    }
    
    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true);
    }
    
    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var (generationReturn, newContent) = await PostGenerator.SaveAndGenerateHtml(CurrentStateToPostContent(),
            null, StatusContext.ProgressTracker());
        
        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }
        
        await LoadData(newContent);
        
        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }
    
    [BlockingCommand]
    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Please save the content first...");
            return;
        }
        
        var settings = UserSettingsSingleton.CurrentSettings();
        
        var url = $"{settings.PostPageUrl(DbEntry)}";
        
        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}