# Pointless Waymarks GeoTools

The Pointless Waymarks GeoTools are part of the [Pointless Waymarks Project](https://github.com/cmiles/PointlessWaymarksProject). These tools provide some useful functionality for GeoTagging photographs, tagging photographs from GeoJson data and working with [Garmin Connect](https://connect.garmin.com/).

*At this point there are no public installers/releases from the Pointless Waymarks Project - the code is MIT Licensed made public on GitHub to share with friends, colleagues and anyone who finds the code interesting or useful. This project is probably only suitable for use if you enjoy debugging and working on code!*

## GeoTagging - GPX File Based

This allows you to select GPX Files, select files to Tag, get a preview and write the location to the files. For this to work you need to include GPX files in your selection that cover the time the photograph was taken - and the program must be able to determine the time the photograph was taken. This is a nice option for GeoTagging photos when you have all your location data on your local system and the program will happily work with a large number of GPX files and a large number of photos.

## GeoTagging - Garmin Connect Based

Garmin Connect is an incredible free service that many devices and other services will sync to with minimal effort. The program will look for Garmin Activities that match the times of the files you want to GeoTag, download GPX files and, if possible, use that information to GeoTag your files. This can save you some effort downloading and picking the correct GPX Files/Activities to GeoTag your photos with.

![Files To Tag](../PointlessWaymarks.GeoToolsScreenShots/ConnectGeoTaggingFilesToTag.jpg "Garmin Connect GeoTagging - Files To Tag")
![GeoTag Settings](../PointlessWaymarks.GeoToolsScreenShots/ConnectGeoTaggingSettings.jpg "Garmin Connect GeoTagging - Settings")
![GeoTag Results](../PointlessWaymarks.GeoToolsScreenShots/ConnectGeoTaggingPreview.jpg "Garmin Connect GeoTagging - Results Preview")

### Why GeoTagging?

Several good questions about the functionality above:
 - Why? Don't Lightroom, Exiftools, GeoSetter, your phone... already offer excellent ways GeoTag? There are already many fantastic programs/ways to GeoTag photographs! Nice details this program offers: a link to Garmin Connect, ability to work with a large number of GPX and files to GeoTag and the convenience of GeoTagging and then immediately Feature Intersecting Tagging (see below).
 - Isn't GeoTagging and revealing locations a dubious goal for landscape photography? GeoTagging public photographs is an important and interesting question - if you haven't thought about this issue before I recommend searching the internet for 'should you geotag landscape photography'. Regardless of what you think about GeoTagging publicly released photographs there can be huge value in GeoTagging your personal collection. It is easy, even with places you care about and photos you love, for memories to fade over time - in my experience decades later where a photograph was taken can become a complete mystery without a GeoTag... If you GeoTag your personal photographs it is easy with most editors and digital asset management tools to remove location Metadata when exporting. However - don't think that removing location metadata makes your location a secret - please share responsibly! 

## Feature Intersect Tags

With information published about the boundaries of parks, forests, monuments, etc. why not take advantage of that information to automatically tag you photos? Sometimes it is easy enough to create these tags by hand - but with complex boundaries, unfamiliar terrain and County/State/Federal land types all involved it has become clear to me that this is work better done by the computer...

This program can compare the location of a photograph with PAD-US data and other GeoJson files to create tags from the intersections.

You will have to download and setup data for this program to use - this takes some work, time and attention to detail but it also means that once you have the data setup it is available fully offline! The program contains help and instructions for getting and setting up the data. Brief notes about the three basic data sources that can be used are below - see the help in the program or the [README](../PointlessWaymarks.FeatureIntersectionTags/README.md) for more information.

### PAD-US

The PAD-US may be all you need inside the USA to get a good selection of tags about the type and ownership of the land you are on. From the [USGS PAD-US Data Overview](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview):

> PAD-US is America’s official national inventory of U.S. terrestrial and marine protected areas that are dedicated to the preservation of biological diversity and to other natural, recreation and cultural uses, managed for these purposes through legal or other effective means. PAD-US also includes the best available aggregation of federal land and marine areas provided directly by managing agencies, coordinated through the Federal Geographic Data Committee Federal Lands Working Group.

### GeoJson Reference Files

GeoJson files are available for a large number of boundaries - from administrative to scientific and more! Some examples:
- [USDA Forest Service FSGeodata Clearinghouse - FSGeodata Clearinghouse](https://data.fs.usda.gov/geodata/)
- [National Park Service](https://public-nps.opendata.arcgis.com/)
- State Land Departments - In some western states State Land, or State Trust Land, is an important land ownership category - for Arizona: [AZGeo Data](https://azgeo-open-data-agic.hub.arcgis.com/)
- [GIS and GPS Downloadable Data - Wilderness Connect](https://wilderness.net/visit-wilderness/gis-gps.php)
- [Census Mapping Files](https://www.census.gov/geographies/mapping-files.html) - great for US state and county data
- [BLM GBP Hub](https://gbp-blm-egis.hub.arcgis.com/)

### Your Own GeoJson Files!

It is likely that there are local names, landmarks of personal significance and regions of specific interest that you might want tags for but that don't - and will not ever - appear in any published data... In some cases one simple solution is to just create your own reference GeoJson files for the program to use! You will need to make sure to create data that the program to read - but [geojson.io](https://geojson.io/) is a pretty easy way to get started...

![PAD-US Settings](../PointlessWaymarks.GeoToolsScreenShots/FeatureIntersectPadUsSettings.jpg "Feature Intersect Tagging - PAD-US Settings and Help")
![GeoJson File Settings](../PointlessWaymarks.GeoToolsScreenShots/FeatureIntersectGeoJsonFileSettings.jpg "Feature Intersect Tagging - GeoJson Feature Files Settings and Help")
![PAD-US Settings](../PointlessWaymarks.GeoToolsScreenShots/FeatureIntersectNewTags.jpg "Feature Intersect Tagging - New Tags Preview")

## Garmin Connect Download

Many thanks to Garmin Connect - I have been using this service for over a decade! The Garmin Connect Download makes it easy to download an Activity and GPX file from your account to an Archive Directory on the local system. In addition the interface has some limited search and filter capabilities to make finding what you want easier.

![Connect Download](../PointlessWaymarks.GeoToolsScreenShots/ConnectDownload.jpg "Garmin Connect Download Screen")