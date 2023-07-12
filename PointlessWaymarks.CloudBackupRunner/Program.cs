using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;

if (args.Length < 1)
{
    Console.WriteLine("To list the Jobs in a Database Specify the Filename");
    Console.WriteLine("To run a job specify the Database name and Job Id");
}

if (args.Length == 1)
{
    var db = await CloudBackupContext.TryCreateInstance(args[0]);

    if (!db.success)
    {
        Console.WriteLine("Connecting to the database failed:");
        Console.WriteLine(db.message);
        return;
    }

    var jobs = await db.context!.BackupJobs.ToListAsync();

    foreach (var loopJob in jobs)
        Console.WriteLine(
            $"{loopJob.Id}  {loopJob.Name}: {loopJob.LocalDirectory} to {loopJob.CloudBucket}:{loopJob.CloudDirectory}");
}