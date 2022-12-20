PointlessWaymarks.Task.GarminConnectGpxImport

The Pointless Waymarks CMS is not designed as an 'all purpose static site generation solution' but with Landscape and Place the topics that it is designed to address personal location information is an obvious source of interesting material.

This project creates a Console app designed primarily to be run as a periodic 'Task' that will download track information from Garmin Connect and create Pointless Waymarks 'Line' content.

Notes:
  - A JSON settings file controls how the program works and the settings file must be passed on the command line to the program
  - The settings file holds unsername and password information for Garmin Connect IN UNENCRYPTED PLAINTEXT FILES!!! For many uses/computers/locations this may make the program unsuitable and too insecure to use!!
  - This program isn't designed to continually sync changes you make in Garmin Connect into the Pointless Waymarks CMS! You could delete and download and activity again... But there is no sense of syncing changes.
  - The download process will store Activity information in JSON and download the GPX for the activity - this program doesn't make any attempt to 'manage' this archive as a backup but by consistently using the same folder you will generate some level of backup of your data.
  - The Pointless Waymarks CMS and Line Content care about location, not about workout data! The distance, elevation change, start time and end time are preserved - your heart rate, pace and any sensor data are not brought into the Pointless Waymarks database - consider this before using this program!

The settings file is a JSON file - a sample is included with the program.

Required Settings:
 - "connectUserName": "connect@test.com" -> See the note above about security, this information is stored in plain text and if the location of the settings file is not secure you allowing anyone to read your Garmin Connect credentials!
 - "connectPassword": "beCareful" -> Your Garmin Connect password in plain text - this program and setting file do NOTHING to secure this critical piece of information - if you can't store your settings file in a secure location DO NOT USE THIS PROGRAM.
 - "gpxArchiveDirectoryFullName": "C:\\GarminPointlessWaymarksArchive" -> The program will write a JSON file of the data it gets from Garmin and the GPX File download it requests into this folder - the program doesn't back up this directory or check it for missing information but used carefully this folder can provide a nice backup of your Garmin Connect data.

Can be omitted to use a default value:
  - "downloadEndDate": "" -> Default is end of the day yesterday. This setting is useful for testing and also potentially for archiving data from specific date ranges.
  "downloadDaysBack": 1 -> Default is 1. This setting is useful for testing and also can be set to a very large value to get all of your older Activities. When setting this to a larger value try to only set the value to the largest value you need - the program doesn't know and doesn't determine your actual earliest activity and if you request 20 years back but only have 3 years of data the program will (flood) Garmin with requests for the entire time period you specify...
  - "overwriteExistingArchiveDirectoryFiles": false -> This program is NOT designed to sync your data, just to import it. This option gives you a way to re-import by over writing, but if you are importing the activity to a Line you must MANUALLY manage/delete the older version of the Line. The default is false.
  - "importActivitiesToSite": false -> This program can actually be used just to download information from Garmin - you may be better off finding a different tool for this since it is not the programs focus... This is useful for testing. The default is false.
  - "pointlessWaymarksSiteSettingsFileFullName": "" -> Required if have set importActivitiesToSite to true (and ignored if importActivitiesToSite is false). This must point to the .ini file for your Pointless Waymarks CMS site and is how this program will find the database and other resources.
  - "showInMainSiteFeed": true -> The default is false. Set to true to make the new Line show up in the Main Site Feed (ie index and RSS). Ignored if importActivitiesToSite is false.

Optional Settings:
 - "intersectionTagSettings":  "C:\PathToYour\FeatureIntersectionTagsSettings.json" -> See the PointlessWaymarks.FeatureIntersectionTags project for help! This file helps you specify GeoJson files that you want to check the imported Line against to generate tags. This can be useful for automatically generating Tags for National Forest Name, National Forest Ranger District or National Park Name since data for those boundaries is easily available. This library also accepts GeoJson you have created yourself so that you can define areas the you want tagged even if you haven't/can't found a data source that has that information (for example for hikes that start in my neighborhood I use a GeoJson file with a polygon covering my neighborhood so that the neighborhood name gets added to the line automatically).