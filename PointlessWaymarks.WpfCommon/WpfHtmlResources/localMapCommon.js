let map;
let mapLayers = [];
let newLayerAutoClose = false;

let pointCircleMarkerOrangeOptions = {
    radius: 8,
    fillColor: "#ff7800",
    color: "#000",
    weight: 1,
    opacity: 1,
    fillOpacity: 0.8
};

function broadcastProgress(progress) {
    console.log(progress);
    window.chrome.webview.postMessage( { "messageType": "progress", "message": progress } );
}

function generateBaseMaps(calTopoApiKey, bingApiKey){
    
    let tileLayers = [];
    let layerNames = {};

    if(calTopoApiKey && calTopoApiKey.trim.length > 0){
        let calTopoTopo = L.tileLayer('http://caltopo.com/api/${accessToken}/wmts/tile/t/{z}/{x}/{y}.png', {
            attribution: 'CalTopo',
            maxNativeZoom: 16,
            maxZoom: 24,
            id: 'caltopoT',
            accessToken: calTopoApiKey
        });
        tileLayers.push(calTopoTopo);
        layerNames["CalTopo"] = calTopoTopo;
        
        let calTopoFs = L.tileLayer('http://caltopo.com/api/${accessToken}/wmts/tile/t/{z}/{x}/{y}.png', {
            attribution: 'CalTopo',
            maxNativeZoom: 16,
            maxZoom: 24,
            id: 'caltopoF16a',
            accessToken: calTopoApiKey
        });
        tileLayers.push(calTopoFs);
        layerNames["CalTopo - FS"] = calTopoFs;
        
    }
    
    if(bingApiKey && bingApiKey.trim.length > 0){
        let bingAerial = L.tileLayer.bing({
            bingMapsKey: bingApiKey,
            imagerySet: 'AerialWithLabels',
            maxZoom: 24
        });
        tileLayers.push(bingAerial);
        layerNames["Bing - Aerial"] = bingAerial;
        

        let bingRoad = L.tileLayer.bing({
            bingMapsKey: bingApiKey,
            imagerySet: 'RoadOnDemand',
            maxZoom: 24
        });
        tileLayers.push(bingRoad);
        layerNames["Bing - Road"] = bingRoad;
        
    }

    let openTopo = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
        maxNativeZoom: 17,
        maxZoom: 24,
        id: 'osmTopo',
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    });
    tileLayers.push(openTopo);
    layerNames["OpenTopo"] = openTopo;

    let tnmImagery = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}',
        {
            maxNativeZoom: 16,
            maxZoom: 22,
            id: 'tnmImageTopo',
            attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
        });
    tileLayers.push(tnmImagery);
    layerNames["TNM - Image"] = tnmImagery;
    
    let tnmTopo = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}',
        {
            maxNativeZoom: 16,
            maxZoom: 22,
            id: 'tnmTopo',
            attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
        });
    tileLayers.push(tnmTopo);
    layerNames["TNM - Topo"] = tnmTopo;
    
    return [tileLayers, layerNames];
}

function onMapMoveEnd(e){
    window.chrome.webview.postMessage( { "messageType": "mapBoundsChange", "bounds": map.getBounds() } );
}

function geoJsonLayerStyle(feature) {
    //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0
    let newStyle = {};

    if (feature.properties.hasOwnProperty("stroke")) newStyle.color = feature.properties["stroke"];
    if (feature.properties.hasOwnProperty("stroke-width")) newStyle.weight = feature.properties["stroke-width"];
    if (feature.properties.hasOwnProperty("stroke-opacity")) newStyle.opacity = feature.properties["stroke-opacity"];
    if (feature.properties.hasOwnProperty("fill")) newStyle.fillColor = feature.properties["fill"];
    if (feature.properties.hasOwnProperty("fill-opacity")) newStyle.fillOpacity = feature.properties["fill-opacity"];

    return newStyle;
}

function onEachMapGeoJsonFeature(feature, layer) {
    if (feature.properties && (feature.properties.title || feature.properties.description)) {
        let popupHtml = "";

        if (feature.properties.title) {
            popupHtml += feature.properties.title;
        }

        if (feature.properties.description) {
            popupHtml += `<p style="text-align: center;">${feature.properties.description}</p>`;
        }

        if(popupHtml !== "") layer.bindPopup(popupHtml, { autoClose: newLayerAutoClose });

        layer.on('click', function (e) {
            console.log(e);
            window.chrome.webview.postMessage({ "messageType": "featureClicked", "data": e.target.feature.properties }); });
    }
}

function postGeoJsonDataHandler(e, clearCurrent, center) {

    if(clearCurrent) {
        broadcastProgress('Clearing Map Layers');
        if(Object.keys(mapLayers).length > 0) {
            mapLayers.forEach(item => map.removeLayer(item));
        }
        mapLayers = [];
    }

    let mapData = e.data;

    if(Object.keys(mapData.GeoJsonLayers).length === 0) return;

    if(center) {
        map.fitBounds([
            [mapData.Bounds.MinLatitude, mapData.Bounds.MinLongitude],
            [mapData.Bounds.MaxLatitude, mapData.Bounds.MaxLongitude]
        ]);
    }

    let newLayerCount = mapData.GeoJsonLayers.length;
    let currentCount = 0;

    mapData.GeoJsonLayers.forEach(item => {
        broadcastProgress(`Adding Layer ${++currentCount} of ${newLayerCount}`);
        let newLayer = new L.geoJSON(item, {onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle});
        mapLayers.push(newLayer);
        map.addLayer(newLayer); });
}

function processMapMessage(e)
{
    console.log(e);

    if(e.data.MessageType === 'NewFeatureCollection') postGeoJsonDataHandler(e, true, false);

    if(e.data.MessageType === 'NewFeatureCollectionAndCenter') postGeoJsonDataHandler(e, true, true);

    if(e.data.MessageType === 'AddFeatureCollection') postGeoJsonDataHandler(e, false, false);

    if(e.data.MessageType === 'CenterFeatureRequest') {
        broadcastProgress('Center Feature Request');
        map.eachLayer(function (l) {
            if (l.feature?.properties?.displayId === e.data.DisplayId) {
                console.log(`l.feature?.geometry?.type ${l.feature?.geometry?.type}`);

                if(l.feature?.geometry?.type === 'Point') {
                    map.flyTo([l.feature.geometry.coordinates[1], l.feature.geometry.coordinates[0]]);
                }

                if(l.feature?.geometry?.type === 'LineString') {
                    map.flyToBounds([[l.feature.bbox[1], l.feature.bbox[0]],
                        [l.feature.bbox[3], l.feature.bbox[2]]]);
                }
                l.openPopup();
            }
        })
    }

    if(e.data.MessageType === 'ShowPopupsFor') {
        broadcastProgress(`Show Popups Request`);
        map.eachLayer(function (l) {
            if(!l.feature?.properties?.displayId) return;
            if (e.data.IdentifierList.includes(l.feature?.properties?.displayId)) {
                console.log(`opening popup for l.feature ${l.feature}`);
                l.openPopup();
            }
            else {
                console.log(`closing popup for l.feature ${l.feature}`);
                l.closePopup();
            }
            console.log(l);
        })
    }

    if(e.data.MessageType === 'CenterCoordinateRequest') {
        broadcastProgress('Center Coordinate Request');
        map.flyTo([e.data.Latitude, e.data.Longitude]);
    }

    if(e.data.MessageType === 'CenterBoundingBoxRequest') {
        broadcastProgress('Center Bounding Box Request');
        map.flyToBounds([[e.data.Bounds.MinLatitude, e.data.Bounds.MinLongitude], [e.data.Bounds.MaxLatitude, e.data.Bounds.MaxLongitude]]);
    }
}
