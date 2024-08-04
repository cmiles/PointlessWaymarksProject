using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptProgressWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptProgressWindow
{
    private string _databaseFile = string.Empty;
    // ReSharper disable once NotAccessedField.Local
    private Guid _dbId = Guid.Empty;

    public ScriptProgressWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public string FilterDescription { get; set; } = string.Empty;
    public ScriptProgressContext? ProgressContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task CreateInstance(List<Guid> jobIdFilter, List<Guid> runIdFilter,
        string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        string filterDescription;
        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        if (jobIdFilter.Count > 0)
        {
            var possibleJobs = await db.ScriptJobs.Where(x => jobIdFilter.Contains(x.PersistentId)).ToListAsync();
            filterDescription = string.Join(", ", possibleJobs.OrderBy(x => x.Name).Select(x => x.Name));

            if (runIdFilter.Count > 0)
            {
                var possibleRuns = await db.ScriptJobRuns
                    .Where(x => runIdFilter.Contains(x.PersistentId) && jobIdFilter.Contains(x.ScriptJobPersistentId))
                    .ToListAsync();
                filterDescription += " - Runs " + string.Join(", ",
                    possibleRuns.OrderBy(x => x.StartedOnUtc)
                        .Select(x => $"PersistentId {x.PersistentId} Started {x.StartedOnUtc}"));
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
                var possibleRuns =
                    await db.ScriptJobRuns.Where(x => runIdFilter.Contains(x.PersistentId)).ToListAsync();
                filterDescription += " - Runs " + string.Join(", ",
                    possibleRuns.OrderBy(x => x.StartedOnUtc)
                        .Select(x => $"PersistentId {x.PersistentId} Started {x.StartedOnUtc}"));
            }
            else
            {
                filterDescription += " - All Runs";
            }
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptProgressWindow
        {
            FilterDescription = filterDescription, StatusContext = new StatusControlContext(),
            _databaseFile = databaseFile, _dbId = dbId
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ProgressContext =
            await ScriptProgressContext.CreateInstance(window.StatusContext, jobIdFilter, runIdFilter,
                window._databaseFile);

        await window.PositionWindowAndShowOnUiThread();
    }
}