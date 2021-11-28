using System.ComponentModel;
using System.Web;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;

[ObservableObject]
public partial class UpdateNotesEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private IUpdateNotes _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private Command _refreshPreviewCommand;


    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _updateNotes = string.Empty;
    [ObservableProperty] private ContentFormatChooserContext _updateNotesFormat;
    [ObservableProperty] private bool _updateNotesHasChanges;
    [ObservableProperty] private string _updateNotesHtmlOutput;

    private UpdateNotesEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;
    }

    public void CheckForChangesAndValidationIssues()
    {
        UpdateNotesHasChanges = !StringHelpers.AreEqual((DbEntry?.UpdateNotes).TrimNullToEmpty(), UpdateNotes);

        HasChanges = UpdateNotesHasChanges || PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<UpdateNotesEditorContext> CreateInstance(StatusControlContext statusContext,
        IUpdateNotes dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new UpdateNotesEditorContext(statusContext);
        newContext.UpdateNotesFormat = ContentFormatChooserContext.CreateInstance(newContext.StatusContext);

        await newContext.LoadData(dbEntry);

        return newContext;
    }

    public async Task LoadData(IUpdateNotes toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = toLoad;

        RefreshPreviewCommand = StatusContext.RunBlockingTaskCommand(UpdateUpdateNotesContentHtml);

        UpdateNotesFormat.InitialValue = DbEntry?.UpdateNotesFormat;

        if (toLoad == null || string.IsNullOrWhiteSpace(toLoad.UpdateNotesFormat))
        {
            UpdateNotes = string.Empty;
            UpdateNotesFormat.SelectedContentFormat = UpdateNotesFormat.ContentFormatChoices.First();
            return;
        }

        UpdateNotes = toLoad.UpdateNotes;
        var setUpdateFormatOk = await UpdateNotesFormat.TrySelectContentChoice(toLoad.UpdateNotesFormat);

        if (!setUpdateFormatOk) StatusContext.ToastWarning("Trouble loading Format from Db...");

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(UpdateNotes))
            UpdateNotesHasChanges = !StringHelpers.AreEqual(DbEntry.UpdateNotes, UpdateNotes);
        if (e.PropertyName == nameof(UpdateNotesFormat))
            StatusContext.RunFireAndForgetNonBlockingTask(UpdateUpdateNotesContentHtml);
    }

    public async Task UpdateUpdateNotesContentHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            var preprocessResults =
                await BracketCodeCommon.ProcessCodesForLocalDisplay(UpdateNotes, StatusContext.ProgressTracker());
            var processResults =
                ContentProcessing.ProcessContent(preprocessResults, UpdateNotesFormat.SelectedContentFormat);
            UpdateNotesHtmlOutput = processResults.ToHtmlDocument("Update Notes", string.Empty);
        }
        catch (Exception e)
        {
            UpdateNotesHtmlOutput =
                $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocument("Invalid",
                    string.Empty);
        }
    }
}