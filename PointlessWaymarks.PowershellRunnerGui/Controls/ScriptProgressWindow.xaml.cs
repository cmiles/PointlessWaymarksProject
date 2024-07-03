using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptProgressWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptProgressWindow
{
    public ScriptProgressWindow()
    {
        InitializeComponent();
    }

    public string FilterDescription { get; set; } = string.Empty;
    public ScriptProgressContext? ProgressContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public async Task<ScriptProgressWindow> CreateInstance(List<Guid> jobIdFilter, List<Guid> runIdFilter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        string filterDescription;
        var db = await PowerShellRunnerDbContext.CreateInstance();

        if (jobIdFilter.Count > 0)
        {
            var possibleJobs = await db.ScriptJobs.Where(x => jobIdFilter.Contains(x.PersistentId)).ToListAsync();
            filterDescription = string.Join(", ", possibleJobs.OrderBy(x => x.Name).Select(x => x.Name));

            if (runIdFilter.Count > 0)
            {
                var possibleRuns = await db.ScriptJobRuns
                    .Where(x => runIdFilter.Contains(x.PersistentId) && jobIdFilter.Contains(x.ScriptJobPersistentId)).ToListAsync();
                filterDescription += " - Runs " + string.Join(", ",
                    possibleRuns.OrderBy(x => x.StartedOnUtc).Select(x => $"Id {x.Id} Started {x.StartedOnUtc}"));
            }
            else
            {
                filterDescription += " - All Runs";
            }
        }
        else
        {
            filterDescription = "All Jobs";

            if (runIdFilter.Count > 0)
            {
                var possibleRuns = await db.ScriptJobRuns.Where(x => runIdFilter.Contains(x.PersistentId)).ToListAsync();
                filterDescription += " - Runs " + string.Join(", ",
                    possibleRuns.OrderBy(x => x.StartedOnUtc).Select(x => $"Id {x.Id} Started {x.StartedOnUtc}"));
            }
            else
            {
                filterDescription += " - All Runs";
            }
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptProgressWindow
            { FilterDescription = filterDescription, StatusContext = new StatusControlContext() };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ProgressContext =
            await ScriptProgressContext.CreateInstance(window.StatusContext, jobIdFilter, runIdFilter);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}