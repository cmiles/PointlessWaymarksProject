using System.ComponentModel;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

/// <summary>
///     Interaction logic for S3DeletionsWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class S3DeletionsWindow
{
    public S3DeletionsWindow(IS3AccountInformation s3Info, List<S3DeletionsItem> itemsToDelete)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        DataContext = this;

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            DeletionContext = await S3DeletionsContext.CreateInstance(StatusContext, s3Info, itemsToDelete);
        });
    }

    public S3DeletionsContext? DeletionContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    private void S3DeletionsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!StatusContext.BlockUi) return;

        e.Cancel = true;
    }
}