using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowershellRunnerGui.Controls;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowershellRunner;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        DataContext = this;

        StatusContext.RunFireAndForgetWithToastOnError(Setup);
    }

    public ScriptRunnerContext? ScriptRunnerContext { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public async Task Setup()
    {
        ScriptRunnerContext = await ScriptRunnerContext.Create(null);
    }
}