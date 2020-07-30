using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Database
{
    public class EventLogContext : DbContext
    {
        public EventLogContext(DbContextOptions<EventLogContext> options) : base(options)
        {
        }

        public DbSet<EventLog> EventLogs { get; set; }

        public static async Task DeleteLogEntriesMoreThanMonthsOld(int numberOfMonths)
        {
            var cutoff = DateTime.Now.AddMonths(-1 * Math.Abs(numberOfMonths));

            var db = await Db.Log();

            var toRemove = db.EventLogs.Where(x => x.RecordedOn < cutoff);

            db.EventLogs.RemoveRange(toRemove);

            await db.SaveChangesAsync(true);
        }

        public static async Task TryWriteDiagnosticMessageToLog(string message, string sender)
        {
            await TryWriteMessageToLog("Diagnostic", message, sender);
        }

        public static async Task TryWriteExceptionToLog(Exception ex, string sender, string additionLogInfo)
        {
            try
            {
                var informationBuilder = new StringBuilder();
                informationBuilder.AppendLine($"Local Time: {DateTime.Now:F}");

                if (ex != null)
                {
                    informationBuilder.AppendLine($"Top Exception Message: {ex.Message}");

                    informationBuilder.AppendLine("Full Exception:");

                    informationBuilder.AppendLine(ex.ToString());

                    var currentException = ex;

                    while (currentException.InnerException != null)
                    {
                        informationBuilder.AppendLine();
                        informationBuilder.AppendLine(currentException.ToString());
                        currentException = currentException.InnerException;
                    }
                }
                else
                {
                    informationBuilder.AppendLine("Exception is Null?");
                }

                informationBuilder.AppendLine();

                if (!string.IsNullOrWhiteSpace(additionLogInfo)) informationBuilder.AppendLine(additionLogInfo.Trim());

                var log = await Db.Log();
                await log.EventLogs.AddAsync(new EventLog
                {
                    Category = "Exception",
                    Sender = sender,
                    Information = informationBuilder.ToString(),
                    RecordedOn = DateTime.UtcNow
                });

                await log.SaveChangesAsync(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void TryWriteExceptionToLogBlocking(Exception ex, string sender, string additionLogInfo)
        {
            try
            {
                var informationBuilder = new StringBuilder();
                informationBuilder.AppendLine($"Local Time: {DateTime.Now:F}");

                if (ex != null)
                {
                    informationBuilder.AppendLine($"Top Exception Message: {ex.Message}");

                    informationBuilder.AppendLine("Full Exception:");

                    informationBuilder.AppendLine(ex.ToString());

                    var currentException = ex;

                    while (currentException.InnerException != null)
                    {
                        informationBuilder.AppendLine();
                        informationBuilder.AppendLine(currentException.ToString());
                        currentException = currentException.InnerException;
                    }
                }
                else
                {
                    informationBuilder.AppendLine("Exception is Null?");
                }

                informationBuilder.AppendLine();

                if (!string.IsNullOrWhiteSpace(additionLogInfo)) informationBuilder.AppendLine(additionLogInfo.Trim());

                var log = Db.Log().Result;
                log.EventLogs.Add(new EventLog
                {
                    Category = "Exception",
                    Sender = sender,
                    Information = informationBuilder.ToString(),
                    RecordedOn = DateTime.UtcNow
                });

                log.SaveChanges(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task TryWriteGenerationEventToLog(string message, string sender)
        {
            await TryWriteMessageToLog("Generation", message, sender);
        }

        private static async Task TryWriteMessageToLog(string category, string message, string sender)
        {
            try
            {
                var log = await Db.Log();
                if (!await log.Database.CanConnectAsync()) return;

                await log.EventLogs.AddAsync(new EventLog
                {
                    Category = category,
                    Sender = sender,
                    Information = message ?? string.Empty,
                    RecordedOn = DateTime.UtcNow
                });

                await log.SaveChangesAsync(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task TryWriteStartupMessageToLog(string message, string sender)
        {
            await TryWriteMessageToLog("Startup", message, sender);
        }
    }
}