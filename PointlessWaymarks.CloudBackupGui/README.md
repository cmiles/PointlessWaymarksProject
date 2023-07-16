## Cloud Backup Runner

This command-line program is used to run Cloud Backup Jobs created in the Pointless Waymarks Cloud Backup Gui. If can be used in conjunction with the Windows Task Scheduler to backup nightly/weekly/etc...

### Arguments:

The first argument to the program should be the full name of the database file. By default this will be in the \Pointless Waymarks Cms\Cloud Backup directory of a Users Documents directory (but it can be located anywhere the program can access).

If only the database name is provided the program will list the Ids and Names of the Backup Jobs in the database. This is useful because to run a backup job you must provide the Id of the Job you want to run.

To run a Backup Job provide the Id of the job after the name of the database.