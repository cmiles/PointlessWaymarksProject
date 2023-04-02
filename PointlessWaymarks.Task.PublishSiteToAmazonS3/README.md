## Publish to Amazon S3 Task

This project creates a command-line program that can be used to automatically generate a site from changes and upload the site to Amazon S3.

Used in conjunction with the Windows Task Scheduler this provides a way to automatically publish a site nightly - or combined with other Pointless Waymarks Tasks and some scripting a way to publish a site after automatically importing content.

### Settings:

In order for this program to work the site must already be configured to use Amazon S3 on the computer/login that this program runs on. The easiest way to make sure that everything is setup correctly is to open the site in the Pointless Waymarks CMS and select Site -> Generate Site and Start Upload - if that succeeds then this program is ready to run, if not hopefully the errors you find what you still need to setup or fix.

The settings file is a single setting - below - and the path to the settings file must be passed an argument to the program.

"pointlessWaymarksSiteSettingsFileFullName": "C:\\Site\\SiteSettings.json"