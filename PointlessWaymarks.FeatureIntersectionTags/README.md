This library is designed to help you take points/lines/areas and extract Attribute Values from GeoJson Features that intersect with them. To do this the program uses a settings file that defines a list of GeoJson files to use and the Attribute Names that you want to extract. This functionality is all based on local files and works offline.

The intended use is to extract 'Tag' values - for example to automatically tag a run with values like State, County, National Forest, Wilderness Areas -> 'arizona,coronado national forest,garmin connect import,pima county,pusch ridge wilderness,sabino canyon recreation area,santa catalina mountains,santa catalina ranger district'.

There are two possible sources of data that this code will use:
 - [PAD-US Data Overview | U.S. Geological Survey](https://www.usgs.gov/programs/gap-analysis-project/science/pad-us-data-overview) - the US Protected Areas Database is "America’s official national inventory of U.S. terrestrial and marine protected areas that are dedicated to the preservation of biological diversity and to other natural, recreation and cultural uses, managed for these purposes through legal or other effective means. PAD-US also includes the best available aggregation of federal land and marine areas provided directly by managing agencies, coordinated through the Federal Geographic Data Committee Federal Lands Working Group." This is an incredible resource if you are interested in the landscape of the US - National Parks, National Forests, BLM, Convervation Areas, County Parks, etc. are all included. In many cases this might be all you need - but it is also likely it won't have everything you personally care about.
 - GeoJson Files - this program can take advantage of individual GeoJson files - some examples:
	 - [USDA Forest Service FSGeodata Clearinghouse - FSGeodata Clearinghouse](https://data.fs.usda.gov/geodata/) - suggestions: 'Administrative Forest Boundaries' and 'Ranger District Boundaries'
	 - [National Park Service](https://public-nps.opendata.arcgis.com/) - suggestions: NPS - Land Resources Division Boundary and Tract Data Service.
	 - State Land Departments - In some western states State Land, or State Trust Land, is an important land ownership category - for Arizona: [AZGeo Data](https://azgeo-open-data-agic.hub.arcgis.com/)
	 - [GIS and GPS Downloadable Data - Wilderness Connect](https://wilderness.net/visit-wilderness/gis-gps.php) - You can find Wilderness Area information in various Federal Data Sources - however take a look at the excel file linked on the Wilderness Connect GIS page for some perspective about the various agencies involved...
	 - [Census Mapping Files](https://www.census.gov/geographies/mapping-files.html) - great for US state and county data
	 - [BLM GBP Hub](https://gbp-blm-egis.hub.arcgis.com/) - suggestion: BLM Natl NLCS National Monuments National Conservation Areas Polygons
	 - [U.S. Fish & Wildlife Service GIS Data](https://gis-fws.opendata.arcgis.com/) - suggestion: FWS National Realty Boundaries
	 - Regardless of the areas you are interested and the availability of pre-existing data part of the motivation of this library is to include a way to tag based on geographic locations based on your own data! [geojson.io](https://geojson.io/) is one simple way to produce a reference file - you could for example draw a polygon around a local trail area that has a well known local name that doesn't appear on any map and isn't officially recognized by any government agency and into the properties for the polygon "Name": "My Special Trail Area". Official recognition and public data almost certainly don't define everything you care about on the landscape!


PAD-US Setup
The downside to using PAD-US as local files is that the data files are quite large. To deal with this a specific and somewhat extensive setup is needed...
 - On the [U.S. Department of the Interior Unified Interior Regional Boundaries](https://www.doi.gov/employees/reorg/unified-regional-boundaries) find and click the 'hapefiles (for mapping software)' link. Extract the contents of the zip file, use ogr2ogr to convert the data (rough template: \ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}). Put the file somewhere that it can stay permanently - in the settings file you will need to enter this file as "padUsDoiRegionFile".
 - [PAD-US 3.0 Download data by Department of the Interior (DOI) Region GeoJSON - ScienceBase-Catalog](https://www.sciencebase.gov/catalog/item/622256afd34ee0c6b38b6bb7) - from this page download any regions you are interested in.
   - Unzip the files and process them with ogr2ogr - for example \ogr2ogr.exe -f GeoJSON -t_srs crs:84 C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.geojson C:\PointlessWaymarksPadUs\PADUS3_0Combined_Region1.json - it may not still be true but it appears that the files as downloaded will not have the expected coordinate reference system (crs) and will not be read correctly by the program. The files must have a consistent name ending with the DOI region number and a .geojson extension.
 - The DOI Regions file plus the rather specific names of the PAD-US region files allows the program to only open the needed files. Because the geojson files are large this is not a fast process - but it is better than scanning all the files and the data is good enough that it is worth it!
 - See the settings file details below to complete the setup.


If a data source doesn't offer a GeoJson download a Shapefile download will almost certainly be offered. On Windows a great way to deal with this is to:
 - Install [QGIS](https://www.qgis.org/en/site/) - QGIS offers the ability to open a Shapefile as a Vector Layer and then export it as GeoJson
 - Installing QGIS also install a number of other tools and it may be easier to use the commandline to transform a Shapefile to GeoJson - find the QGIS bin folder (for example C:\Program Files\QGIS 3.16\bin\) and then run .\ogr2ogr.exe -f GeoJSON -t_srs crs:84 {path and name for destination GeoJson file} {path and name of the shapefile to convert}
 - If working directly in QGIS be careful of the CRS - best is to convert your layer/project to EPS 4326 before exporting GeoJson.


The settings file is a JSON file - the included sample file may be useful:
 - "padUsDoiRegionFile" - path and filename for the GeoJson DOI Regions file
 - "padUsDirectory" - directory where the GeoJson PAD-US region files are stored
 - "padUsFilePrefix" - what comes before the DOI region number in the PAD-US GeoJson Filenames (for example if your files names were like 'PADUS3_0Combined_Region1.geojson' the "padUsFilePrefix" should be set to "PADUS3_0Combined_Region" - all files must use the same prefix)
 - "padUsAttributesForTags" - an array of the Attribute Names you want extracted. [ "Unit_Nm" ] is a simple way to get useful data.
 - GeoJson files other that the PAD-US data should be listed in"intersectFiles": []
	 - Each intersectFiles element should have:
	   - "source" - can be null but very very useful to record this information
	   - "downloaded" - not required - this should be the Date that the data was downloaded - '2022-06-01' for example.
	   - "name" - a human readable name - can be null but can provide useful logging information
	   - "fileName" - must be the full path and filename of a valid GeoJson file
	   - "attributesForTags" - this is an Array of Strings. This defines the property name(s) that the program will get the values of for any interscting features. This can be an empty array if using 'tagAll'.
	   - "tagAll" - you may find datasets where you don't care about the values of any of the properties and just want any intersections tagged as a value. An example is Arizona State Trust Land - in Arizona it is interesting and useful to know your hike included Arizona State Trust Land but you might not care about any of the specifics (who leases the land, how is the land used, ...) - you could leave the attributesForTags blank and set tagAll to 'Arizona State Trust Land' to deal with that scenario.

