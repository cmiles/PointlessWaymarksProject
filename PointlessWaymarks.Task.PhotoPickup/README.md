## Photo Pickup Task

This project creates a command-line program that can be used to automatically add photographs from a folder to a Pointless Waymarks CMS Based Site.

Used in conjunction with the Windows Task Scheduler this provides a way to have a folder where the photographs are automatically picked up once (or more) a day! And used along with a local folder that syncs to a service like Dropbox it can provide a reasonable way to quickly transfer phone photos to a site.

### Settings:

"pointlessWaymarksSiteSettingsFileFullName": "C:\\Site\\SiteSettings.json" - The settings file a Pointless Waymarks CMS based site.

"photoPickupDirectory": "C:\\TestDirectory" - The photograph pickup directory determines where the program will look for .jpg and .jpeg files to import - subdirectories are not scanned, just the top level directory.

"photoPickupArchiveDirectory": "C:\\TestDirectory\\PickUpArchive" - If the program successfully imports a file it is moved to the Archive directory. This keeps the original photographs safe but makes it easy for both users and the program to know what is new and what has been processed

"renameFileToTitle": false - The Photo Pickup Task was written with mobile devices in mind - in many cases on a mobile device you have limited editing capabilities and while you might prefer nicely organized filenames for your photographs they may be difficult to provide... Setting this option to true will cause the program to attempt to rename the photograph to the title found when scanning the metadata. Note that because of existing files - both in the CMS DB and on the filesystem the program may not always be able to rename your file as expected...

"showInMainSiteFeed": false - Setting this to true will cause the Photo Content to be shown on the main (index) page of the site.