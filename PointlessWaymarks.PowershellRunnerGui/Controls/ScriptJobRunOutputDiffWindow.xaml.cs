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
    private string _databaseFile = string.Empty;

    private Guid _dbId = Guid.Empty;
    private string _key = string.Empty;

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

    public static async Task CreateInstance(Guid scriptJobRunId, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var run = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == scriptJobRunId);
        var jobId = run?.ScriptJobPersistentId ?? Guid.Empty;

        var allRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobPersistentId == jobId)
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
                PersistentId = loopRun.PersistentId,
                RunType = loopRun.RunType,
                Script = loopRun.Script,
                StartedOnUtc = loopRun.StartedOnUtc,
                StartedOn = loopRun.StartedOnUtc.ToLocalTime(),
                ScriptJobId = loopRun.ScriptJobPersistentId,
                TranslatedOutput = loopRun.Output.Decrypt(key),
                TranslatedScript = loopRun.Script.Decrypt(key)
            };

            toAdd.TranslatedOutput = Regex.Replace(toAdd.TranslatedOutput,
                @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                string.Empty, RegexOptions.Multiline);

            allRunsTranslated.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryLeftRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);
        var factoryRightRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);

        var factorySelectedLeftRun = factoryLeftRuns.FirstOrDefault(x => x.PersistentId == jobId);
        var factorySelectedRightRun = factoryRightRuns.FirstOrDefault(x => x.PersistentId == jobId);
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
            SelectedRightRun = factorySelectedRightRun,
            _key = key,
            _databaseFile = databaseFile,
            _dbId = dbId
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