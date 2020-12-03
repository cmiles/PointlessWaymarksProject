const lazyInit = (elementToObserve, fn) => {
    const observer = new IntersectionObserver((entries) => {
        if (entries.some(({ isIntersecting }) => isIntersecting)) {
            observer.disconnect();
            fn();
        }
    });
    observer.observe(elementToObserve);
};

function openTopoMapLayer() {
    return L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
        {
            maxZoom: 17,
            id: 'osmTopo',
            attribution:
                'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });
}

function onEachMapGeoJsonFeature(feature, layer) {
    if (feature.properties && feature.properties.PopupContent) {
        layer.bindPopup(feature.properties.PopupContent);
    }
    if (feature.properties && feature.properties.popupContent) {
        layer.bindPopup(feature.properties.PopupContent);
    }
}

async function singleGeoJsonMapInit(mapElement, contentId) {

    let geoJsonDataResponse = await fetch(`/GeoJson/Data/GeoJson-${contentId}.json`);
    if (!geoJsonDataResponse.ok)
        throw new Error(geoJsonDataResponse.statusText);

    let geoJsonData = await geoJsonDataResponse.json();

    let map = L.map(mapElement,
        {
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true
        });

    map.fitBounds([
        [geoJsonData.Bounds.InitialViewBoundsMinY, geoJsonData.Bounds.InitialViewBoundsMinX],
        [geoJsonData.Bounds.InitialViewBoundsMaxY, geoJsonData.Bounds.InitialViewBoundsMaxX]
    ]);

    let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
        onEachFeature: onEachMapGeoJsonFeature
    });

    newMapLayer.addTo(map);
}

async function mapComponentInit(mapElement, contentId) {

    let mapComponentResponse = await fetch(`/Maps/Data/Map-${contentId}.json`);
    if (!mapComponentResponse.ok)
        throw new Error(mapComponentResponse.statusText);

    let mapComponent = await mapComponentResponse.json();

    let map = L.map(mapElement,
        {
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true
        });

    map.fitBounds([
        [mapComponent.MapComponent.InitialViewBoundsMinY, mapComponent.MapComponent.InitialViewBoundsMinX],
        [mapComponent.MapComponent.InitialViewBoundsMaxY, mapComponent.MapComponent.InitialViewBoundsMaxX]
    ]);

    if (mapComponent.PointGuids?.length) {

        let response = await fetch('/Points/Data/pointdata.json');
        if (!response.ok)
            throw new Error(response.statusText);

        let pointData = await response.json();

        let includedPoints = pointData.filter(x => mapComponent.PointGuids.includes(x.ContentId));

        for (let pagePoint of includedPoints) {
            const pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
                {
                    draggable: false,
                    autoPan: true
                }).addTo(map);

            const pointPopup = L.popup().setContent(`${pagePoint.Title} (${pagePoint.DetailTypeString})`);
            pointPopup.autoClose = false;

            pointContentMarker.bindPopup(pointPopup);

            //if (mapComponent.ShowDetailsGuid.includes(pagePoint)) pointPopup.openPopup();
        };
    }

    if (mapComponent.GeoJsonGuids?.length) {

        for (let loopGeoJson of mapComponent.GeoJsonGuids) {

            let response = await fetch(`/GeoJson/Data/GeoJson-${loopGeoJson}.json`);

            if (!response.ok)
                throw new Error(response.statusText);

            let geoJsonData = await response.json();

            let newMapLayer = new L.geoJSON(geoJsonData.GeoJson, {
                onEachFeature: onEachMapGeoJsonFeature
            });

            newMapLayer.addTo(map);
        };
    }
}

async function singlePointMapInit(mapElement, displayedPointSlug) {

    let response = await fetch('/Points/Data/pointdata.json');
    if (!response.ok)
        throw new Error(response.statusText);

    let pointData = await response.json();

    let pagePoint = pointData.filter(x => x.Slug === displayedPointSlug)[0];

    let map = L.map(mapElement,
        {
            center: { lat: pagePoint.Latitude, lng: pagePoint.Longitude },
            zoom: 13,
            layers: [openTopoMapLayer()],
            doubleClickZoom: false,
            gestureHandling: true
        });

    let pointContentMarker = new L.marker([pagePoint.Latitude, pagePoint.Longitude],
        {
            draggable: false,
            autoPan: true
        }).addTo(map);

    pointContentMarker
        .bindPopup(
            `<p style="margin-top: .5rem; margin-bottom: 0;">${pagePoint.Title}</p><p style="margin-left: .5rem; margin-top: .1rem;">${pagePoint.DetailTypeString}</p>`).openPopup();

    for (let circlePoint of pointData) {
        if (circlePoint.Slug == displayedPointSlug) continue;
        let toAdd = L.circle([circlePoint.Latitude, circlePoint.Longitude],
            60,
            { color: 'gray', fillColor: 'gray', fillOpacity: .5 });
        toAdd.bindTooltip(
            `${circlePoint.Title}(${circlePoint.DetailTypeString})`);
        toAdd.addTo(map).on("click", (e) => window.location.href = circlePoint.PointPageUrl);
    };
}