using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.S3Deletions;

/// <summary>
///     Interaction logic for S3DeletionsWindow.xaml
/// </summary>
[ObservableObject]
public partial class S3DeletionsWindow
{
    [ObservableProperty] private S3DeletionsContext _deletionContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public S3DeletionsWindow(List<S3DeletionsItem> itemsToDelete)
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();

        DataContext = this;

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            DeletionContext = await S3DeletionsContext.CreateInstance(StatusContext, itemsToDelete);
        });
    }

    private void S3DeletionsWindow_OnClosing(object sender, CancelEventArgs e)
    {
        if (DeletionContext == null || !StatusContext.BlockUi) return;

        e.Cancel = true;
    }
}