using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptViewContext
{
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;
    private string _key = string.Empty;
    public required ScriptJob DbEntry { get; set; }
    public NotificationCatcher? IpcNotifications { get; set; }

    public required StatusControlContext StatusContext { get; set; }

    public string TranslatedScript { get; set; } = string.Empty;

    public static async Task<ScriptViewContext> CreateInstance(StatusControlContext? statusContext,
        Guid jobPersistentId, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        var job = await db.ScriptJobs.SingleAsync(x => x.PersistentId == jobPersistentId);

        var factoryContext = new ScriptViewContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            DbEntry = job,
            TranslatedScript = job.Script.Decrypt(key),
            _key = key,
            _databaseFile = databaseFile,
            _dbId = dbId
        };

        factoryContext.IpcNotifications = new NotificationCatcher
        {
            JobDataNotification = factoryContext.ProcessJobDataUpdateNotification
        };

        return factoryContext;
    }

    private async Task ProcessJobDataUpdateNotification(DataNotifications.InterProcessJobDataNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != _dbId || arg.JobPersistentId != DbEntry.PersistentId) return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var job = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == arg.JobPersistentId);

        if (job == null) return;

        DbEntry = job;
        TranslatedScript = DbEntry.Script.Decrypt(_key);
    }
}