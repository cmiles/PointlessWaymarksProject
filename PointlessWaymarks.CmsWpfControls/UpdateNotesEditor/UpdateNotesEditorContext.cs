using System.ComponentModel;
using System.Web;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class UpdateNotesEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    private UpdateNotesEditorContext(StatusControlContext statusContext, IUpdateNotes dbEntry,
        ContentFormatChooserContext contentChooser)
    {
        StatusContext = statusContext;

        BuildCommands();

        UpdateNotesFormat = contentChooser;

        DbEntry = dbEntry;
        UpdateNotes = DbEntry.UpdateNotes ?? string.Empty;

        UpdateNotesFormat = contentChooser;

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public IUpdateNotes DbEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UpdateNotes { get; set; }
    public ContentFormatChooserContext UpdateNotesFormat { get; set; }
    public bool UpdateNotesHasChanges { get; set; }
    public string? UpdateNotesHtmlOutput { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        UpdateNotesHasChanges = !StringTools.AreEqual(DbEntry.UpdateNotes.TrimNullToEmpty(), UpdateNotes);

        HasChanges = UpdateNotesHasChanges || PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static async Task<UpdateNotesEditorContext> CreateInstance(StatusControlContext statusContext,
        IUpdateNotes dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext;

        var factoryFormatChooserContext = await ContentFormatChooserContext.CreateInstance(factoryContext);
        factoryFormatChooserContext.InitialValue = dbEntry.UpdateNotesFormat;

        if (string.IsNullOrWhiteSpace(dbEntry.UpdateNotesFormat))
        {
            factoryFormatChooserContext.SelectedContentFormat =
                factoryFormatChooserContext.ContentFormatChoices.First();
        }
        else
        {
            var setUpdateFormatOk = await factoryFormatChooserContext.TrySelectContentChoice(dbEntry.UpdateNotesFormat);

            if (!setUpdateFormatOk)
            {
                factoryContext.ToastWarning("Trouble loading Format from Db...");
                factoryFormatChooserContext.SelectedContentFormat =
                    factoryFormatChooserContext.ContentFormatChoices.First();
            }
        }

        var newContext = new UpdateNotesEditorContext(factoryContext, dbEntry, factoryFormatChooserContext);

        newContext.CheckForChangesAndValidationIssues();

        return newContext;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(UpdateNotes))
            UpdateNotesHasChanges = !StringTools.AreEqual(DbEntry.UpdateNotes, UpdateNotes);
        if (e.PropertyName == nameof(UpdateNotesFormat))
            StatusContext.RunFireAndForgetNonBlockingTask(RefreshPreview);
    }

    [BlockingCommand]
    public async Task RefreshPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        try
        {
            var preprocessResults =
                await BracketCodeCommon.ProcessCodesForLocalDisplay(UpdateNotes, StatusContext.ProgressTracker());
            var processResults =
                ContentProcessing.ProcessContent(preprocessResults, UpdateNotesFormat.SelectedContentFormat);
            UpdateNotesHtmlOutput = processResults.ToHtmlDocumentWithLeaflet("Update Notes", string.Empty);
        }
        catch (Exception e)
        {
            UpdateNotesHtmlOutput =
                $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocumentWithLeaflet(
                    "Invalid",
                    string.Empty);
        }
    }
}