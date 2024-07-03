using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunListContext
{
    private string _databaseFile = string.Empty;
    private string _key = string.Empty;
    public required string FilterDescription { get; set; }

    public required ObservableCollection<ScriptJobRunGuiView> Items { get; set; }
    public List<Guid> JobFilter { get; set; } = [];
    public ScriptJobRunGuiView? SelectedItem { get; set; }
    public List<ScriptJobRunGuiView> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptJobRunListContext> CreateInstance(StatusControlContext? statusContext,
        List<Guid> jobFilter, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);

        var filteredRuns = jobFilter.Any()
            ? await db.ScriptJobRuns.Where(x => jobFilter.Contains(x.ScriptJobPersistentId))
                .OrderByDescending(x => x.StartedOnUtc).AsNoTracking().ToListAsync()
            : await db.ScriptJobRuns.OrderByDescending(x => x.StartedOnUtc).AsNoTracking().ToListAsync();

        string filterDescription;
        if (jobFilter.Any())
        {
            var possibleJobs = await db.ScriptJobs.Where(x => jobFilter.Contains(x.PersistentId)).ToListAsync();
            filterDescription = string.Join(", ", possibleJobs.OrderBy(x => x.Name).Select(x => x.Name));
        }
        else
        {
            filterDescription = "All Jobs";
        }

        var runList = new List<ScriptJobRunGuiView>();

        foreach (var loopRun in filteredRuns)
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
                ScriptJobId = loopRun.ScriptJobPersistentId,
                TranslatedOutput = loopRun.Output.Decrypt(key),
                TranslatedScript = loopRun.Script.Decrypt(key)
            };

            runList.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ScriptJobRunListContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            Items = new ObservableCollection<ScriptJobRunGuiView>(runList),
            JobFilter = jobFilter,
            FilterDescription = filterDescription,
            _key = key,
            _databaseFile = databaseFile
        };

        return toReturn;
    }
}