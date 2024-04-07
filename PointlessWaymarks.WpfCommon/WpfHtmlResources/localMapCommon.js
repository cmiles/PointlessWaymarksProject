let map;
let mapLayers = [];
let newLayerAutoClose = false;
let lineElevationChart;
let lineElevationChartMapMarker;
let lineElevationData;
let pointContentMarker;
let useCircleMarkerStyle = false;
let mapIcons;

let pointCircleMarkerOrangeOptions = {
    radius: 8,
    fillColor: "#00b6ff",
    color: "#000",
    weight: 1,
    opacity: 1,
    fillOpacity: 0.8
};

/*Source: https://raw.githubusercontent.com/nationalparkservice/symbol-library/gh-pages/src/standalone/photography-black-30.svg8*/
const pointlessWaymarksCameraIcon = `<svg version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" 
    xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px"
	 viewBox="0 0 30 30" enable-background="new 0 0 30 30" xml:space="preserve">
        <rect x="4" y="4" width="6" height="2"/>
        <circle cx="10.5" cy="16.5" r="3.5"/>
        <path d="M27,7H3c-1.7,0-3,1.3-3,3v13c0,1.6,1.3,3,3,3h24c1.7,0,3-1.4,3-3V10C30,8.3,28.7,7,27,7z M10.5,23.5c-3.9,0-7-3.1-7-7
	        c0-3.9,3.1-7,7-7s7,3.1,7,7C17.5,20.4,14.4,23.5,10.5,23.5z M26,12h-3c-0.5,0-1-0.5-1-1s0.5-1,1-1h3c0.5,0,1,0.5,1,1S26.5,12,26,12z"/>
</svg>`;

/*Source: https://raw.githubusercontent.com/nationalparkservice/symbol-library/gh-pages/src/standalone/dot-black-30.svg*/
const pointlessWaymarksDotIcon = `<svg version="1.1" id="Layer_1" xmlns:sketch="http://www.bohemiancoding.com/sketch/ns"
	 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" 
	 viewBox="0 0 30 30" style="enable-background:new 0 0 30 30;" xml:space="preserve">
    <circle  id="Oval-3-Copy-2" sketch:type="MSShapeGroup" cx="15" cy="15" r="8">
    </circle>
</svg>`;

const mapIconColors = ['red', 'darkred', 'lightred', 'orange', 'beige', 'green', 'darkgreen', 'lightgreen', 'blue', 'darkblue', 'lightblue', 'purple', 'darkpurple', 'pink', 'cadetblue', 'white', 'gray', 'lightgray', 'black'];

function broadcastProgress(progress) {
    console.log(progress);
    window.chrome.webview.postMessage( { "messageType": "progress", "message": progress } );
}

function initialDocumentLoad() {
    broadcastProgress('Initial Document Load');
    window.chrome.webview.postMessage({ "messageType": "scriptFinished" });
}

/**
 * Loads a map to the 'mainMap' div
 * @param {number} initialLatitude
 * @param {number} initialLongitude
 * @param {string} calTopoApiKey
 * @param {string} bingApiKey
 * @param {boolean} circleMarkerStyle
 * @param autoClosePopups
 */
async function initialMapLoad(initialLatitude, initialLongitude, calTopoApiKey, bingApiKey, circleMarkerStyle, autoClosePopups = true) {
    broadcastProgress(`Initial Map Load - ${initialLatitude}, ${initialLongitude}`);

    newLayerAutoClose = autoClosePopups;
    useCircleMarkerStyle = circleMarkerStyle;

    try {
        let response = await fetch("pwMapSvgIcons.json");
        mapIcons = await response.json();
    }
    catch { }


    let [baseMaps, baseMapNames] = generateBaseMaps(calTopoApiKey, bingApiKey);

    map = L.map('mainMap', {
        center: { lat: initialLatitude, lng: initialLongitude },
        zoom: 13,
        layers: baseMaps,
        doubleClickZoom: false,
        closePopupOnClick: false
        });

    L.control.layers(baseMapNames).addTo(map);

    if (document.getElementById('mainElevationChart')) await singleLineChartInit();

    map.on('moveend', onMapMoveEnd);

    window.chrome.webview.addEventListener('message', processMapMessage);

    window.chrome.webview.postMessage({ "messageType": "scriptFinished" });

    return true;
}

/**
 * Loads a map to the 'mainMap' div
 * @param {number} initialLatitude
 * @param {number} initialLongitude
 * @param {string} calTopoApiKey
 * @param {string} bingApiKey
 */
async function initialMapLoadWithUserPointChooser(initialLatitude, initialLongitude, calTopoApiKey, bingApiKey) {
    broadcastProgress(`Initial Map with User Point Load - ${initialLatitude}, ${initialLongitude}`);
    newLayerAutoClose = true;
    useCircleMarkerStyle = true;

    try {
        let response = await fetch("pwMapSvgIcons.json");
        mapIcons = await response.json();
    }
    catch { }

    let [baseMaps, baseMapNames] = generateBaseMaps(calTopoApiKey, bingApiKey);

    map = L.map('mainMap', {
        center: { lat: initialLatitude, lng: initialLongitude },
        zoom: 13,
        layers: baseMaps,
        doubleClickZoom: false,
        closePopupOnClick: false
    });

    L.control.layers(baseMapNames).addTo(map);

    if (document.getElementById('mainElevationChart')) await singleLineChartInit();

    map.on('moveend', onMapMoveEnd);

    map.on('dblclick', function (e) {
        console.log(e);
        pointContentMarker.setLatLng(e.latlng);
        window.chrome.webview.postMessage({ messageType: 'userSelectedLatitudeLongitudeChanged', latitude: e.latlng.lat, longitude: e.latlng.lng });
    });

    pointContentMarker = new L.marker([initialLatitude, initialLongitude], {
        draggable: true,
        autoPan: true
    }).addTo(map);

    pointContentMarker.on('dragend', function (e) {
        console.log(e);
        window.chrome.webview.postMessage({ messageType: 'userSelectedLatitudeLongitudeChanged', latitude: e.target._latlng.lat, longitude: e.target._latlng.lng });
    });

    window.chrome.webview.addEventListener('message', processMapMessage);

    window.chrome.webview.postMessage({ "messageType": "scriptFinished" });

    return true;
}

/**
 * @param {string} calTopoApiKey
 * @param {string} bingApiKey
 */
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

function createPoints(useCircleMarkers) {
    return function pointToLayer(feature, latlng) {
        let popupHtml = "";

        if (feature.properties?.title) {
            popupHtml += feature.properties.title;
        }

        if (feature.properties?.description) {
            popupHtml += `<p style="text-align: center;">${feature.properties.description}</p>`;
        }

        if (feature.properties?.mapLabel) {

            let labelMarker = L.circleMarker(latlng,
                { radius: 1, color: "blue", fillColor: "blue", fillOpacity: .5 });

            let labelMarkerLayer = labelMarker.addTo(map);

            //Because we aren't returning this layer add it to the mapLayer here 
            //and give it an identifier.
            labelMarkerLayer.displayId = feature.properties?.displayId;
            mapLayers.push(labelMarkerLayer);

            return L.marker(latlng,
                {
                    icon: L.divIcon({
                        className: 'point-map-label',
                        html: `<p style="font-size: 24px;font-weight: bold; height: auto !important;width: max-content !important;">${feature.properties.mapLabel}</p>`,
                        iconAnchor: [-6, 48]
                    })
                });
        }

        if (feature.properties?.mapIconName || feature.properties?.mapMarkerColor) {
            return L.marker(latlng, { icon: L.AwesomeSVGMarkers.icon({ svgIcon: `data:image/svg+xml;utf8,${getMapIconSvg(feature.properties?.mapIcon)}`, markerColor: getMapMarkerColor(feature.properties?.mapMarkerColor), iconColor: '#000000' }) });
        }

        if (useCircleMarkers) {
            return L.circleMarker(latlng, pointCircleMarkerOrangeOptions)
        }

        return L.marker(latlng);
    }

}

function getMapMarkerColor(iconName) {
    if (!iconName) return 'blue';
    if (mapIconColors.includes(iconName)) return iconName;
    return 'blue';
}

function getMapIconSvg(iconName) {
    if (!iconName) return pointlessWaymarksDotIcon;
    if(iconName === 'camera') return pointlessWaymarksCameraIcon;
    if (iconName === 'dot') return pointlessWaymarksDotIcon;
    if(!mapIcons) return pointlessWaymarksDotIcon;
    var possibleMapJsonIcons = mapIcons.filter(x => x.IconName === iconName);
    if (possibleMapJsonIcons.length === 0) return pointlessWaymarksDotIcon;
    return possibleMapJsonIcons[0].IconSvg;
}


function removeGeoJsonDataHandler(e) {

    if (Object.keys(e.data.IdentifierList).length === 0) return;

    if (Object.keys(mapLayers).length > 0) {

        let toRemove = [];
        map.eachLayer(function (l) {
            if (!l.feature?.properties?.displayId && 'displayId' in l === false) return;
            if (e.data.IdentifierList.includes(l.feature?.properties?.displayId)
                || ('displayId' in l && e.data.IdentifierList.includes(l.displayId))) {
                console.log(`removing l.feature ${l.feature}`);
                map.removeLayer(l);

                let index = mapLayers.indexOf(l);
                if (index !== -1) { mapLayers.splice(index, 1) };
            }
        });

        mapLayers = mapLayers.filter(x => !toRemove.includes(x));
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
        let newLayer = new L.geoJSON(item, { onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle, pointToLayer: createPoints(useCircleMarkerStyle) });
        mapLayers.push(newLayer);
        map.addLayer(newLayer);
    });
}

function processMapMessage(e)
{
    console.log(e);

    if(e.data.MessageType === 'NewFeatureCollection') postGeoJsonDataHandler(e, true, false);

    if (e.data.MessageType === 'NewFeatureCollectionAndCenter') postGeoJsonDataHandler(e, true, true);

    if (e.data.MessageType === 'RemoveFeatures') removeGeoJsonDataHandler(e);

    if(e.data.MessageType === 'AddFeatureCollection') postGeoJsonDataHandler(e, false, false);

    if (e.data.MessageType === 'CenterFeatureRequest') centerFeatureHandler(e);

    if (e.data.MessageType === 'ShowPopupsFor') showPopupsForHandler(e);

    if(e.data.MessageType === 'CenterCoordinateRequest') {
        broadcastProgress('Center Coordinate Request');
        map.flyTo([e.data.Latitude, e.data.Longitude]);
    }

    if(e.data.MessageType === 'CenterBoundingBoxRequest') {
        broadcastProgress('Center Bounding Box Request');
        map.flyToBounds([[e.data.Bounds.MinLatitude, e.data.Bounds.MinLongitude], [e.data.Bounds.MaxLatitude, e.data.Bounds.MaxLongitude]]);
    }

    if (e.data.MessageType === 'LoadElevationChartData') singleLineChartLoadDataHandler(e);

    if (e.data.MessageType === 'MoveUserLocationSelection') {
        broadcastProgress('Mover User Location Selection Request');
        pointContentMarker.setLatLng([e.data.Latitude, e.data.Longitude]);
        map.setView([e.data.Latitude, e.data.Longitude], map.getZoom());
    }
}

function showPopupsForHandler(e) {
    broadcastProgress(`Show Popups Request`);
    map.eachLayer(function (l) {
        if (!l.feature?.properties?.displayId) return;
        if (e.data.IdentifierList.includes(l.feature?.properties?.displayId)) {
            console.log(`opening popup for l.feature ${l.feature}`);
            l.openPopup();
        }
        else {
            console.log(`closing popup for l.feature ${l.feature}`);
            l.closePopup();
        }
        console.log(l);
    });
}

function closeAllPopups() {
    broadcastProgress(`Close All Popups Request`);
    map.eachLayer(function (l) {  l.closePopup(); });
}

function centerFeatureHandler(e) {
    broadcastProgress('Center Feature Request');
    map.eachLayer(function (l) {
        if (l.feature?.properties?.displayId === e.data.DisplayId) {
            console.log(`l.feature?.geometry?.type ${l.feature?.geometry?.type}`);

            if (l.feature?.geometry?.type === 'Point') {
                map.flyTo([l.feature.geometry.coordinates[1], l.feature.geometry.coordinates[0]]);
            }

            if (l.feature?.geometry?.type === 'LineString') {
                map.flyToBounds([[l.feature.bbox[1], l.feature.bbox[0]],
                [l.feature.bbox[3], l.feature.bbox[2]]]);
            }
            l.openPopup();
        }
    })
}

function singleLineChartLoadDataHandler(e) {

    lineElevationData = e.data.ElevationData;

    //This code is to help give the charts a slight bit more cross chart comparability - so the
    //charts will always end on a multiple of 5 miles and 5,000' of elevation. This is a compromise
    //because the chart won't fill all available space (show max detail) and charts won't always
    //have the same scale, but having worked with this data for years I think this is a very simple
    //compromise that often works out nicely...
    const maxDistanceInMeters = Math.max(...lineElevationData.map(x => x.AccumulatedDistance));
    const distanceFiveMileUnits = Math.floor((maxDistanceInMeters * 0.0006213711922) / 5);
    const distanceMax = (distanceFiveMileUnits + 1) * 5;

    const maxElevationInMeters = Math.max(...lineElevationData.map(x => x.Elevation));
    const elevationFiveThousandFeetUnits = Math.floor((maxElevationInMeters * 3.280839895) / 5000);
    const elevationMax = (elevationFiveThousandFeetUnits + 1) * 5000;

    //Thank you to https://www.geoapify.com/tutorial/draw-route-elevation-profile-with-chartjs for
    //the starting point on this!

    lineElevationChart.options.scales.x.max = distanceMax;
    lineElevationChart.options.scales.y.max = elevationMax;

    const chartData = {
        labels: lineElevationData.map(x => x.AccumulatedDistance * 0.0006213711922),
        datasets: [{
            data: lineElevationData.map(x => x.Elevation * 3.280839895),
            fill: true,
            borderColor: '#66ccff',
            backgroundColor: '#66ccff66',
            tension: 0.1,
            pointRadius: 0,
            spanGaps: true
        }]
    };

    lineElevationChart.data = chartData;

    lineElevationChart.update();
}

async function singleLineChartInit() {

    const config = {
        type: 'line',
        plugins: [{
            beforeInit: (chart, args, options) => {
                chart.options.scales.x.min = 0;
                chart.options.scales.x.max = 5;
                chart.options.scales.y.min = 0;
                chart.options.scales.y.max = 5000;
            }
        }],
        options: {
            animation: false,
            maintainAspectRatio: false,
            interaction: { intersect: false, mode: 'index' },
            tooltip: { position: 'nearest' },
            scales: {
                x: { type: 'linear' },
                y: { type: 'linear' },
            },
            plugins: {
                title: { align: "center", display: true, text: "Distance: Miles, Elevation: Feet" },
                legend: { display: false },
                tooltip: {
                    displayColors: false,
                    backgroundColor: 'rgba(0, 0, 0, 0.5)',
                    callbacks: {
                        title: (tooltipItems) => {
                            return "Distance: " + parseFloat(tooltipItems[0].label).toFixed(2).toLocaleString() + " miles";
                        },
                        label: (tooltipItem) => {
                            
                            var location = [lineElevationData[tooltipItem.dataIndex].Latitude, lineElevationData[tooltipItem.dataIndex].Longitude];

                            setLineElevationChartMapMarker(location);

                            return ["Elevation: " + Math.floor(tooltipItem.raw).toLocaleString() + " feet",
                                "Accumulated Climb: " + Math.floor(lineElevationData[tooltipItem.dataIndex].AccumulatedClimb).toLocaleString(),
                                "Accumulated Descent: " + Math.floor(lineElevationData[tooltipItem.dataIndex].AccumulatedDescent).toLocaleString()
                            ];
                        },
                    }
                }
            }
        }
    };


    lineElevationChart = new Chart(document.getElementById('mainElevationChart').getContext("2d"), config);

    lineElevationChart.canvas.onclick = (e) => {
        const points = lineElevationChart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
        if (!points?.length) return;

        let location = [lineElevationData[points[0].index].Latitude, lineElevationData[points[0].index].Longitude];
        map.flyTo(location);

        setLineElevationChartMapMarker(location);
    };

}

function setLineElevationChartMapMarker(location) {

    if (!lineElevationChartMapMarker) {
        lineElevationChartMapMarker = L.circle(location, {
            color: '#f03',
            fillColor: '#f03',
            fillOpacity: 0.5,
            radius: 30
        });

        lineElevationChartMapMarker.addTo(map);
    } else {
        lineElevationChartMapMarker.setLatLng(location);
    }
}
