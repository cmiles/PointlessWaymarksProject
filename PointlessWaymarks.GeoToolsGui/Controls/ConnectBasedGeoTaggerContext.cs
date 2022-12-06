using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[ObservableObject]
public partial class ConnectBasedGeoTaggerContext
{
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    public ConnectBasedGeoTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;
    }

    public static async Task<ConnectBasedGeoTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus windowStatus)
    {
        var control = new ConnectBasedGeoTaggerContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    public async System.Threading.Tasks.Task Load()
    {
    }
}