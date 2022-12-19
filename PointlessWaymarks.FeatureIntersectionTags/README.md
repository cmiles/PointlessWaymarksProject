# Feature Intersect Tags

This library is designed to help you take points/lines/areas and extract Attribute Values from GeoJson Features that intersect with them. To do this the program uses a settings file that defines a list of GeoJson files to use and the Attribute Names that you want to extract. This functionality is all based on local files and works offline.

The intended use is to extract 'Tag' values - for example to automatically tag a run with values like State, County, National Forest, Wilderness Areas -> 'arizona,coronado national forest,garmin connect import,pima county,pusch ridge wilderness,sabino canyon recreation area,santa catalina mountains,santa catalina ranger district'.

## Data

There are two possible sources of data that this code will use:

### GeoJson Reference Files

This program can take advantage of individual GeoJson files - some examples:
 - [USDA Forest Service FSGeodata Clearinghouse - FSGeodata Clearinghouse](https://data.fs.usda.gov/geodata/) - suggestions: 'Administrative Forest Boundaries' and 'Ranger District Boundaries'
 - [National Park Service](https://public-nps.opendata.arcgis.com/) - suggestions: NPS - Land Resources Division Boundary and Tract Data Service.
 - State Land Departments - In some western states State Land, or State Trust Land, is an important land ownership category - for Arizona: [AZGeo Data](https://azgeo-open-data-agic.hub.arcgis.com/)
 - [GIS and GPS Downloadable Data - Wilderness Connect](https://wilderness.net/visit-wilderness/gis-gps.php) - You can find Wilderness Area information in various Federal Data Sources - however take a look at the excel file linked on the Wilderness Connect GIS page for some perspective about the various agencies involved...
 - [Census Mapping Files](https://www.census.gov/geographies/mapping-files.html) - great for US state and county data
 - [BLM GBP Hub](https://gbp-blm-egis.hub.arcgis.com/) - suggestion: BLM Natl NLCS National Monuments National Conservation Areas Polygons
 - [U.S. Fish & Wildlife Service GIS Data](https://gis-fws.opendata.arcgis.com/) - suggestion: FWS National Realty Boundaries
 - Regardless of the areas you are interested in and the availability of pre-existing data part of the motivation of this library is to include a way to tag based on geographic locations based on your own data! [geojson.io](https://geojson.io/) is one simple way to produce a reference file - you could for example draw a polygon around a local trail area that has a well known local name that doesn't appear on any map and isn't officially recognized by any government agency and into the properties for the polygon "Name": "My Special Trail Area". Official recognition and public data almost certainly don't define everything you care about on the landscape!

### PAD-US

[PAD-US Data Overview | U.S. Geological Survey](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview) - the US Protected Areas Database is "America’s official national inventory of U.S. terrestrial and marine protected areas that are dedicated to the preservation of biological diversity and to other natural, recreation and cultural uses, managed for these purposes through legal or other effective means. PAD-US also includes the best available aggregation of federal land and marine areas provided directly by managing agencies, coordinated through the Federal Geographic Data Committee Federal Lands Working Group." This is an incredible resource if you are interested in the landscape of the US - National Parks, National Forests, BLM, Convervation Areas, County Parks, etc. are all included. In many cases this might be all you need - but it is also likely it won't have everything you personally care about.

The downside to using PAD-US as local files is that the data files are quite large. To deal with this a specific setup - you will need to create a directory dedicated to the PAD-US data, place the Region Boundaries GeoJson file and Region GeoJson files in this directory and set this directory in the settings as the PadUsDirectory.
  - On the [U.S. Department of the Interior Unified Interior Regional Boundaries](https://www.doi.gov/employees/reorg/unified-regional-boundaries) site find and click the 'shapefiles (for mapping software)' link - this will download a zip file.
  - Extract the contents of the zip file.
  - Use ogr2ogr (see the general help for information on this commandline program) to convert the data to GeoJson (rough template: \ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}).
  - Put the GeoJson output file into your PAD-US data directory
  - [PAD-US 3.0 Download data by Department of the Interior (DOI) Region GeoJSON - ScienceBase-Catalog](https://www.sciencebase.gov/catalog/item/622256afd34ee0c6b38b6bb7) - from this page click the 'Download data by Department of the Interior (DOI) Region GeoJSON' link, this will take you to a page where you can download any regions you are interested in. For each region:
  - Extract the zip file and place the GeoJson file in your PAD-US data directory
  - Ensure that the GeoJson has the expected coordinate reference system and format - for example  \ogr2ogr.exe -f GeoJSON -t_srs crs:84 C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.geojson C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.json.


## Shapefiles

This program works exclusively with GeoJson files - if a data source you are interested in doesn't offer a GeoJson download a Shapefile download will almost certainly be offered. On Windows a great way to deal with this is to:
 - Install [QGIS](https://www.qgis.org/en/site/) - QGIS offers the ability to open a Shapefile as a Vector Layer and then export it as GeoJson
 - Installing QGIS also install a number of other tools and it may be easier to use the commandline to transform a Shapefile to GeoJson - find the QGIS bin folder (for example C:\Program Files\QGIS 3.16\bin\) and then run .\ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}
 - If working directly in QGIS be careful of the CRS - best is to convert your layer/project to EPS 4326 before exporting GeoJson.


## Settings File

The settings file is a JSON file (see the sample included with the program):
 - "padUsDirectory" - directory where the GeoJson PAD-US region files are stored, see the PAD-US notes above for setup - the file names do have to follow some guidlines in order for the program to find them.
 - "padUsAttributesForTags" - an array of the Attribute Names you want extracted. [ "Unit_Nm" ] is a simple way to get useful data.
 - GeoJson files other that the PAD-US data should be listed in"intersectFiles": []
	 - Each intersectFiles element should have:
	   - "source" - can be null but very very useful to record this information
	   - "downloaded" - not required - this should be the Date that the data was downloaded - '2022-06-01' for example.
	   - "name" - a human readable name - can be null but can provide useful logging information
	   - "fileName" - must be the full path and filename of a valid GeoJson file
	   - "attributesForTags" - this is an Array of Strings. This defines the property name(s) that the program will get the values of for any interscting features. This can be an empty array if using 'tagAll'.
	   - "tagAll" - you may find datasets where you don't care about the values of any of the properties and just want any intersections tagged as a value. An example is Arizona State Trust Land - in Arizona it is interesting and useful to know your hike included Arizona State Trust Land but you might not care about any of the specifics (who leases the land, how is the land used, ...) - you could leave the attributesForTags blank and set tagAll to 'Arizona State Trust Land' to deal with that scenario.
