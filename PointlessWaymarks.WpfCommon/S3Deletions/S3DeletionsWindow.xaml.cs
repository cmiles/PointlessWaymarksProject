using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

/// <summary>
///     Interaction logic for S3DeletionsWindow.xaml
/// </summary>
[ObservableObject]
public partial class S3DeletionsWindow
{
    [ObservableProperty] private S3DeletionsContext? _deletionContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public S3DeletionsWindow(IS3AccountInformation s3Info, List<S3DeletionsItem> itemsToDelete)
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();

        DataContext = this;

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            DeletionContext = await S3DeletionsContext.CreateInstance(StatusContext, s3Info, itemsToDelete);
        });
    }

    private void S3DeletionsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!StatusContext.BlockUi) return;

        e.Cancel = true;
    }
}