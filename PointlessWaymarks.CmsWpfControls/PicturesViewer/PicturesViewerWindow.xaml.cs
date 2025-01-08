using System.Text;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.CmsWpfControls.PicturesViewer;

/// <summary>
///     Interaction logic for PicturesViewerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PicturesViewerWindow
{
    public PicturesViewerWindow(StatusControlContext statusContext, List<Guid> toShow)
    {
        InitializeComponent();

        StatusContext = statusContext;

        BuildCommands();

        PictureGuids = toShow;
        HtmlPreview = string.Empty;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        DataContext = this;
    }

    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public string HtmlPreview { get; set; }
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? JpgScreenshotFunction { get; set; }
    public List<Guid> PictureGuids { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public static async Task<PicturesViewerWindow> CreateInstance(List<Guid> pictureOrVideoGuids)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PicturesViewerWindow(await StatusControlContext.CreateInstance(), pictureOrVideoGuids);

        await window.PositionWindowAndShowOnUiThread();

        window.StatusContext.RunBlockingTask(window.CreatePage);

        return window;
    }

    public async Task CreatePage()
    {
        var db = await Db.Context();

        var markdownDoc = new StringBuilder();

        var photoGuids = await db.PhotoContents.Where(x => PictureGuids.Contains(x.ContentId)).Select(x => x.ContentId)
            .ToListAsync();
        var imageGuids = await db.ImageContents.Where(x => PictureGuids.Contains(x.ContentId)).Select(x => x.ContentId)
            .ToListAsync();
        var videoGuids = await db.VideoContents.Where(x => PictureGuids.Contains(x.ContentId)).Select(x => x.ContentId)
            .ToListAsync();

        foreach (var loopGuid in PictureGuids)
        {
            if (photoGuids.Contains(loopGuid))
                markdownDoc.AppendLine(
                    $"{{{{{BracketCodePhotos.BracketCodeToken} {loopGuid}; PicturesViewerWindow}}}}");
            if (imageGuids.Contains(loopGuid))
                markdownDoc.AppendLine(
                    $"{{{{{BracketCodeImages.BracketCodeToken} {loopGuid}; PicturesViewerWindow}}}}");
            if (videoGuids.Contains(loopGuid))
                markdownDoc.AppendLine(
                    $"{{{{{BracketCodeVideoEmbed.BracketCodeToken} {loopGuid}; PicturesViewerWindow}}}}");
        }

        var preprocessResults =
            await BracketCodeCommon.ProcessCodesForSite(markdownDoc.ToString(), StatusContext.ProgressTracker());
        var processResults =
            ContentProcessing.ProcessContent(preprocessResults, ContentFormatEnum.MarkdigMarkdown01);

        HtmlPreview = processResults;
    }

    [BlockingCommand]
    private async Task JpgScreenshot()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (JpgScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await JpgScreenshotFunction();

        if (screenshotResult.IsT0)
            await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);
        else
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
    }

    private Task ProcessFromWebView(FromWebViewMessage arg)
    {
        return Task.CompletedTask;
    }
}