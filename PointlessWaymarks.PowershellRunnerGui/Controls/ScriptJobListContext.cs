using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DocumentFormat.OpenXml.Math;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls
{
    [NotifyPropertyChanged]
    [GenerateStatusCommands]
    public partial class ScriptJobListContext
    {
        public required string CurrentDatabase { get; set; }
        public required ObservableCollection<ScriptJobListItem> Items { get; set; }
        public ScriptJobListItem? SelectedItem { get; set; }
        public List<ScriptJobListItem> SelectedItems { get; set; } = [];
        public required StatusControlContext StatusContext { get; set; }
        public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

            var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
                null,
                x =>
                {
                    StatusContext.ToastError(x.ErrorMessage);
                    Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                        x.ErrorMessage,
                        StatusContext.StatusControlContextId);
                    return Task.CompletedTask;
                }
            );

            if (toRun is not null) await toRun;
        }

        [BlockingCommand]
        public async Task DeleteJob(ScriptJobListItem? toDelete)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (toDelete == null)
            {
                StatusContext.ToastWarning("Nothing Selected to Delete?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            if (MessageBox.Show(
                    "Deleting a Script Job is permanent - Continue?",
                    "Delete Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await PowerShellRunnerContext.CreateInstance();
            var currentItem = await db.ScriptJobs.SingleAsync(x => x.Id == toDelete.DbEntry.Id);
            var currentRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobId == currentItem.Id).ExecuteDeleteAsync();

            db.ScriptJobs.Remove(currentItem);
            await db.SaveChangesAsync();

            await RefreshList();
        }

        [NonBlockingCommand]
        public async Task EditJob(ScriptJobListItem? toEdit)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (toEdit == null)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var window = await ScriptJobEditorWindow.CreateInstance(toEdit.DbEntry, CurrentDatabase);
            window.PositionWindowAndShow();
        }


        [NonBlockingCommand]
        public async Task EditSelectedJob()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedItem == null)
            {
                StatusContext.ToastWarning("Nothing Selected to Edit?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var window = await ScriptJobEditorWindow.CreateInstance(SelectedItem.DbEntry, CurrentDatabase);
            window.PositionWindowAndShow();
        }

        [NonBlockingCommand]
        public async Task NewJob()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newJob = new ScriptJob()
            {
                Name = "New Script Job",
                LastEditOn = DateTime.Now
            };

            await ThreadSwitcher.ResumeForegroundAsync();

            var window = await ScriptJobEditorWindow.CreateInstance(newJob, CurrentDatabase);
            window.PositionWindowAndShow();
        }

        private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
        {
            DataNotificationsProcessor?.Enqueue(e);
        }

        private async Task ProcessDataUpdateNotification(
            DataNotifications.InterProcessDataNotification interProcessUpdateNotification)
        {
            if (interProcessUpdateNotification is
                {
                    ContentType: DataNotifications.DataNotificationContentType.ScriptJob,
                    UpdateType: DataNotifications.DataNotificationUpdateType.Delete
                })
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var toRemove = Items.Where(x => x.DbEntry.Id == interProcessUpdateNotification.Id)
                    .ToList();
                toRemove.ForEach(x => Items.Remove(x));
                return;
            }

            if (interProcessUpdateNotification is
                {
                    ContentType: DataNotifications.DataNotificationContentType.ScriptJob,
                    UpdateType: DataNotifications.DataNotificationUpdateType.Update
                    or DataNotifications.DataNotificationUpdateType.New
                })
            {
                var listItem =
                    Items.SingleOrDefault(x => x.DbEntry.Id == interProcessUpdateNotification.Id);
                var db = await PowerShellRunnerContext.CreateInstance();
                var dbItem =
                    db.ScriptJobs.SingleOrDefault(x =>
                        x.Id == interProcessUpdateNotification.Id);

                if (dbItem == null) return;

                if (listItem != null)
                {
                    listItem.DbEntry = dbItem;
                    return;
                }

                var toAdd = await ScriptJobListItem.CreateInstance(dbItem);

                await ThreadSwitcher.ResumeForegroundAsync();

                Items.Add(toAdd);
            }
        }

        [BlockingCommand]
        public async Task RefreshList()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            var db = await PowerShellRunnerContext.CreateInstance();

            var jobs = await db.ScriptJobs.ToListAsync();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Clear();

            foreach (var x in jobs) Items.Add(await ScriptJobListItem.CreateInstance(x));

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        [NonBlockingCommand]
        public async Task RunJob(ScriptJobListItem? toRun)
        {
        }
    }
}