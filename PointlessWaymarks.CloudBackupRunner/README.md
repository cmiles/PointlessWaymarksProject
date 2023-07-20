# Cloud Backup Runner

This command-line program is used to run Cloud Backup Jobs created in the Pointless Waymarks Cloud Backup Editor. If can be used in conjunction with the Windows Task Scheduler to backup nightly/weekly/etc...

## Arguments:

.\PointlessWaymarks.CloudBackupRunner [Db Filename] [optional: Backup Job Id] [optional: Backup Batch Id|last|auto]

The first argument to the program is always the full name of the database file. By default this will be in the \Pointless Waymarks Cms\Cloud Backup directory of a Users Documents directory (but it can be located anywhere the program can access).

If only the database name is provided the program will list the Ids and Names of the Backup Jobs in the database. This is useful because to run a backup job you must provide the Id of the Job you want to run.

To run a Backup Job provide the Id of the job after the name of the database (use the Pointless Waymarks Cloud Backup Editor Gui to create a Job). This will create a new Backup 'Batch' and run it. This is a great way to get start but it can cost significant time to create a new batch - for day to day use you will want to use one of the Batch Arguments.

Batch Arguments: There are 3 ways to specify what Backup Batch to use:
  - auto: this will look for the latest batch in the last two weeks and if that batch is < 95% done and less than 10% of the uploads/deletes have errors it will resume the batch. If those conditions aren't met a new batch will be created. This can be a good option for automated nightly runs if you don't need 'up to the minute' backups. 
  - Batch Id: The specific batch id will be used - a new batch will be created if the Id is not found.
  - last: the last batch - based on the Created On date - will be used. If no batches are found a new one will be created.
