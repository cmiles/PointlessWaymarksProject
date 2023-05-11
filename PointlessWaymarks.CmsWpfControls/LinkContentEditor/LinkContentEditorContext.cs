﻿using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LinkContentEditor;

public partial class LinkContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private StringDataEntryContext? _authorEntry;
    [ObservableProperty] private StringDataEntryContext? _commentsEntry;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext? _createdUpdatedDisplay;
    [ObservableProperty] private LinkContent _dbEntry;
    [ObservableProperty] private StringDataEntryContext? _descriptionEntry;
    [ObservableProperty] private RelayCommand _extractDataCommand;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext? _helpContext;
    [ObservableProperty] private ConversionDataEntryContext<DateTime?>? _linkDateTimeEntry;
    [ObservableProperty] private StringDataEntryContext? _linkUrlEntry;
    [ObservableProperty] private RelayCommand _openUrlInBrowserCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private BoolDataEntryContext? _showInLinkRssEntry;
    [ObservableProperty] private StringDataEntryContext? _siteEntry;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext? _tagEdit;
    [ObservableProperty] private StringDataEntryContext? _titleEntry;

    public EventHandler? RequestContentEditorWindowClose;

    private LinkContentEditorContext(StatusControlContext statusContext, LinkContent dbEntry)
    {
        _statusContext = statusContext;

        PropertyChanged += OnPropertyChanged;

        _saveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
        _saveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
        _extractDataCommand = StatusContext.RunBlockingTaskCommand(ExtractDataFromLink);
        _openUrlInBrowserCommand = StatusContext.RunNonBlockingActionCommand(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LinkUrlEntry!.UserValue)) StatusContext.ToastWarning("Link is Blank?");
                var ps = new ProcessStartInfo(LinkUrlEntry.UserValue) { UseShellExecute = true, Verb = "open" };
                Process.Start(ps);
            }
            catch (Exception e)
            {
                StatusContext.ToastWarning($"Trouble opening link - {e.Message}");
            }
        });

        _dbEntry = dbEntry;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<LinkContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        LinkContent? linkContent = null, bool extractDataOnLoad = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new LinkContentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializeLinkContent(linkContent));
        await newControl.LoadData(linkContent, extractDataOnLoad);
        return newControl;
    }

    private LinkContent CurrentStateToLinkContent()
    {
        var newEntry = LinkContent.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.Tags = TagEdit!.TagListString();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.Comments = CommentsEntry!.UserValue.TrimNullToEmpty();
        newEntry.Url = LinkUrlEntry!.UserValue.TrimNullToEmpty();
        newEntry.Title = TitleEntry!.UserValue.TrimNullToEmpty();
        newEntry.Site = SiteEntry!.UserValue.TrimNullToEmpty();
        newEntry.Author = AuthorEntry!.UserValue.TrimNullToEmpty();
        newEntry.Description = DescriptionEntry!.UserValue.TrimNullToEmpty();
        newEntry.LinkDate = LinkDateTimeEntry!.UserValue;
        newEntry.ShowInLinkRss = ShowInLinkRssEntry!.UserValue;

        return newEntry;
    }

    private async Task ExtractDataFromLink()
    {
        var (generationReturn, linkMetadata) =
            await LinkGenerator.LinkMetadataFromUrl(LinkUrlEntry!.UserValue, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            StatusContext.ToastError(generationReturn.GenerationNote);
            return;
        }

        if (linkMetadata == null)
        {
            StatusContext.ToastError("No Link Data?");
            return;
        }

        if (!string.IsNullOrWhiteSpace(linkMetadata.Title))
            TitleEntry!.UserValue = linkMetadata.Title.TrimNullToEmpty();
        if (!string.IsNullOrWhiteSpace(linkMetadata.Author))
            AuthorEntry!.UserValue = linkMetadata.Author.TrimNullToEmpty();
        if (!string.IsNullOrWhiteSpace(linkMetadata.Description))
            DescriptionEntry!.UserValue = linkMetadata.Description.TrimNullToEmpty();
        if (!string.IsNullOrWhiteSpace(linkMetadata.Site))
            SiteEntry!.UserValue = linkMetadata.Site.TrimNullToEmpty();
        if (linkMetadata.LinkDate != null)
            LinkDateTimeEntry!.UserText = linkMetadata.LinkDate == null
                ? string.Empty
                : linkMetadata.LinkDate.Value.ToString("M/d/yyyy h:mm:ss tt");
    }

    private async Task LoadData(LinkContent? toLoad, bool extractDataOnLoad = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeLinkContent(toLoad);

        LinkUrlEntry = StringDataEntryContext.CreateInstance();
        LinkUrlEntry.Title = "URL";
        LinkUrlEntry.HelpText = "Link address";
        LinkUrlEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>> { ValidateUrl };
        LinkUrlEntry.ReferenceValue = DbEntry.Url.TrimNullToEmpty();
        LinkUrlEntry.UserValue = DbEntry.Url.TrimNullToEmpty();

        CommentsEntry = StringDataEntryContext.CreateInstance();
        CommentsEntry.Title = "Comments";
        CommentsEntry.HelpText = "Comments on the Linked Contents";
        CommentsEntry.ReferenceValue = DbEntry.Comments.TrimNullToEmpty();
        CommentsEntry.UserValue = DbEntry.Comments.TrimNullToEmpty();

        TitleEntry = StringDataEntryContext.CreateInstance();
        TitleEntry.Title = "Title";
        TitleEntry.HelpText = "Title Text";
        TitleEntry.ReferenceValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.UserValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateTitle };

        SiteEntry = StringDataEntryContext.CreateInstance();
        SiteEntry.Title = "Site";
        SiteEntry.HelpText = "Name of the Site";
        SiteEntry.ReferenceValue = DbEntry.Site.TrimNullToEmpty();
        SiteEntry.UserValue = DbEntry.Site.TrimNullToEmpty();

        AuthorEntry = StringDataEntryContext.CreateInstance();
        AuthorEntry.Title = "Author";
        AuthorEntry.HelpText = "Author of the linked content";
        AuthorEntry.ReferenceValue = DbEntry.Author.TrimNullToEmpty();
        AuthorEntry.UserValue = DbEntry.Author.TrimNullToEmpty();

        DescriptionEntry = StringDataEntryContext.CreateInstance();
        DescriptionEntry.Title = "Description";
        DescriptionEntry.HelpText = "Description of the linked content";
        DescriptionEntry.ReferenceValue = DbEntry.Description.TrimNullToEmpty();
        DescriptionEntry.UserValue = DbEntry.Description.TrimNullToEmpty();

        ShowInLinkRssEntry = await BoolDataEntryContext.CreateInstance();
        ShowInLinkRssEntry.Title = "Show in Link RSS Feed";
        ShowInLinkRssEntry.HelpText = "If checked the link will appear in the site's Link RSS Feed";
        ShowInLinkRssEntry.ReferenceValue = DbEntry.ShowInLinkRss;
        ShowInLinkRssEntry.UserValue = DbEntry.ShowInLinkRss;

        LinkDateTimeEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        LinkDateTimeEntry.Title = "Link Date";
        LinkDateTimeEntry.HelpText = "Date the Link Content was Created or Updated";
        LinkDateTimeEntry.ReferenceValue = DbEntry.LinkDate;
        LinkDateTimeEntry.UserText = DbEntry.LinkDate == null
            ? string.Empty
            : DbEntry.LinkDate.Value.ToString("M/d/yyyy h:mm:ss tt");

        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        if (extractDataOnLoad) await ExtractDataFromLink();

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await LinkGenerator.SaveAndGenerateHtml(CurrentStateToLinkContent(),
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

    public async Task<IsValid> ValidateUrl(string? linkUrl)
    {
        return await CommonContentValidation.ValidateLinkContentLinkUrl(linkUrl, DbEntry.ContentId);
    }
}