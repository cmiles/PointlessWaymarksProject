using System.ComponentModel;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class UpdateNotesEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation,
    IWebViewMessenger
{
    private UpdateNotesEditorContext(StatusControlContext statusContext, IUpdateNotes dbEntry,
        ContentFormatChooserContext contentChooser)
    {
        StatusContext = statusContext;

        BuildCommands();

        UpdateNotesFormat = contentChooser;

        DbEntry = dbEntry;

        UserValue = DbEntry.UpdateNotes ?? string.Empty;

        UpdateNotesFormat = contentChooser;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public IUpdateNotes DbEntry { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public string HtmlPreview { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    public ContentFormatChooserContext UpdateNotesFormat { get; set; }
    public bool UpdateNotesHasChanges { get; set; }
    public string? UpdateNotesHtmlOutput { get; set; }
    public string UserValue { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        UpdateNotesHasChanges = !StringTools.AreEqual(DbEntry.UpdateNotes.TrimNullToEmpty(), UserValue);

        HasChanges = UpdateNotesHasChanges || PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

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

        if (e.PropertyName == nameof(UserValue))
            UpdateNotesHasChanges = !StringTools.AreEqual(DbEntry.UpdateNotes, UserValue);
        if (e.PropertyName == nameof(UpdateNotesFormat))
            StatusContext.RunFireAndForgetNonBlockingTask(RefreshPreview);
    }

    private Task ProcessFromWebView(FromWebViewMessage arg)
    {
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public async Task RefreshPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Building HTML");

        var preprocessResults =
            await BracketCodeCommon.ProcessCodesForSite(UserValue, StatusContext.ProgressTracker());
        var processResults =
            ContentProcessing.ProcessContent(preprocessResults, UpdateNotesFormat.SelectedContentFormat);

        HtmlPreview = processResults;
    }
}