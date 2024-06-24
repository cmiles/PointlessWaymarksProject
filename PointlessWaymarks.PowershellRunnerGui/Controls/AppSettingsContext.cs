using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class AppSettingsContext
{
    public AppSettingsContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
    }

    public StatusControlContext StatusContext { get; set; }

    public static async Task<AppSettingsContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new AppSettingsContext(statusContext ?? new StatusControlContext());
    }
}