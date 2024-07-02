using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptJobRunOutputDiffWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobRunOutputDiffWindow
{
    private static string _databaseFile = string.Empty;
    private static string _key = string.Empty;

    public ScriptJobRunOutputDiffWindow()
    {
        InitializeComponent();

        DataContext = this;

        PropertyChanged += OnPropertyChanged;
    }

    public required ObservableCollection<ScriptJobRunGuiView> LeftRuns { get; set; }
    public bool RemoveOutputTimeStamp { get; set; } = true;
    public required ObservableCollection<ScriptJobRunGuiView> RightRuns { get; set; }
    public ScriptJobRunGuiView? SelectedLeftRun { get; set; }
    public ScriptJobRunGuiView? SelectedRightRun { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task CreateInstance(int scriptJobRunId, string databaseFileName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        _databaseFile = databaseFileName;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile, false);
        _key = await ObfuscationKeyHelpers.GetObfuscationKey(_databaseFile);
        var run = await db.ScriptJobRuns.FindAsync(scriptJobRunId);
        var jobId = run?.ScriptJobId ?? -1;

        var allRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobId == jobId)
            .OrderByDescending(x => x.CompletedOnUtc).AsNoTracking().ToListAsync();

        var allRunsTranslated = new List<ScriptJobRunGuiView>();

        foreach (var loopRun in allRuns)
        {
            var toAdd = new ScriptJobRunGuiView
            {
                Id = loopRun.Id,
                CompletedOnUtc = loopRun.CompletedOnUtc,
                CompletedOn = loopRun.CompletedOnUtc?.ToLocalTime(),
                Errors = loopRun.Errors,
                Output = loopRun.Output,
                RunType = loopRun.RunType,
                Script = loopRun.Script,
                StartedOnUtc = loopRun.StartedOnUtc,
                StartedOn = loopRun.StartedOnUtc.ToLocalTime(),
                ScriptJobId = loopRun.ScriptJobId,
                TranslatedOutput = loopRun.Output.Decrypt(_key),
                TranslatedScript = loopRun.Script.Decrypt(_key)
            };

            toAdd.TranslatedOutput = Regex.Replace(toAdd.TranslatedOutput,
                @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                string.Empty, RegexOptions.Multiline);

            allRunsTranslated.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryLeftRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);
        var factoryRightRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);

        var factorySelectedLeftRun = factoryLeftRuns.FirstOrDefault(x => x.Id == jobId);
        var factorySelectedRightRun = factoryRightRuns.FirstOrDefault(x => x.Id == jobId);
        if (factorySelectedRightRun != null)
        {
            var initialIndex = factoryRightRuns.IndexOf(factorySelectedRightRun);

            if (factoryRightRuns.Count == 1) factorySelectedRightRun = factoryRightRuns.First();
            else if (factorySelectedRightRun == factoryRightRuns.Last())
                factorySelectedRightRun = factoryRightRuns[^1];
            else factorySelectedRightRun = factoryRightRuns[initialIndex + 1];
        }

        var window = new ScriptJobRunOutputDiffWindow
        {
            StatusContext = new StatusControlContext(),
            LeftRuns = factoryLeftRuns,
            RightRuns = factoryRightRuns,
            SelectedLeftRun = factorySelectedLeftRun,
            SelectedRightRun = factorySelectedRightRun
        };

        await window.PositionWindowAndShowOnUiThread();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(RemoveOutputTimeStamp)))
            StatusContext.RunNonBlockingTask(ProcessTimeStampInclusionChange);
    }

    private async Task ProcessTimeStampInclusionChange()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopRun in LeftRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput,
                    @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                    string.Empty, RegexOptions.Multiline);
        }

        foreach (var loopRun in RightRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput,
                    @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                    string.Empty, RegexOptions.Multiline);
        }
    }
}