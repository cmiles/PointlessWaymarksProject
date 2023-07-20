# Pointless Waymarks - Cloud Backup

The details vary but no one reading this needs an explanation of what a backup program is - starting with 'why' is probably more relevant...

There is some absurdity in writing a backup program - there are so many backup and sync programs that it feels both pointless and overwhelming to even try to make a list of examples. That said I find joy in having software that works the way I want it to, love coding and am a fan of a certain level of absurdity!

Goals/Reasons/Features/Ideas:
- Backup to a Cloud Service that is widely accessible - in an unhappy situation you should be able to access your backup with a wide variety of tools, platforms and programs.
- Backup to an Easily Browsable File System - the backup should be directly viewable and browsable without any special software.
- Data Focused - No special consideration to backing up (or restoring) systems/OSes/VMs...
- Minimal Reliance on a Database/Saved Backup Information - with large backups often taking days or weeks to complete you should be able to re-enter a reasonable amount of information about a backup and have it resume even if the backup database is deleted/lost/corrupt.
- No Sync Functionality - the process is only one way, no amount of (mis)configuration can result in local files being accidentally deleted because of a sync with the cloud.
- Max Runtime Limit - in many situations it is better not to try to run backup uploads while doing all the online life things (video calls, remote work, streaming, ...) and that needs to be an option in the scheduling of the backup runs.

For me this program is basically a companion to the Pointless Waymarks CMS. The CMS has given me a place to store the information I want to save in either a public or private way and uploading the sites to Amazon S3 provides a good level of 'backup'. But what the CMS is not designed to do is work with RAW photographs or large chunks of data that aren't imported/categorized/tagged. After trying a few backup and sync solutions I decided that what I wanted to do was simple enough that it would be reasonable to write my own backup program!

The Pointless Waymarks Cloud Backup consists of a GUI Program (this project) to create, update and edit Backup Jobs and a console program to run the jobs.

The target of the backup is currently Amazon S3. Files are stored on S3 as a mirror of the local file system - there is no attempt to de-duplicate data, store incremental changes or otherwise complicate the storage - the backup is easily viewable in the AWS S3 console or any program that can access S3.

Files are compared by an MD5 hash (both stored as Metadata on S3) - the file comparison happens every run of the program. The database holds information about the backup runs including uploads, downloads, errors, etc... Because the backup is a simple mirror and a full comparison is done each run only the job information is 'needed' in the database - ie a lost/deleted/corrupt database is only a minimal hassle in resuming a backup.

*At this point there are no public installers/releases from the Pointless Waymarks Project although the projects include code and scripts to easily create published versions - the code is MIT Licensed made public on GitHub to share with friends, colleagues and anyone who finds the code interesting or useful. This project is probably only suitable for use if you enjoy debugging and working on code!*

## Cloud Backup Editor

There are two parts to the Cloud Backup project - the Cloud Backup editor is a GUI editor for creating, monitoring and reporting on Cloud Backup Jobs.

## Cloud Backup Runner

The Cloud Backup Runner is a commandline program for running Cloud Backup Jobs.