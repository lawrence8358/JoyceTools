let center = [120.31655645579866, 22.63170098476671];
let defaultLocations = [
    { name: '達沃', location: [7.073100413386779, 125.6101188863405] },
    { name: '馬拉威', location: [8.011122088147808, 124.29910025623845] },
    { name: 'Polomolok', location: [6.224143960583939, 125.06120996085342] },
    { name: 'Prosperidad', location: [8.604465887322705, 125.87170942354645] },
]
let _marker = null;
let _map = null;

function initMap() {
    // localStorage 取得上次瀏覽的 center
    if (localStorage.getItem(STORAGE_CENTER)) {
        center = JSON.parse(localStorage.getItem(STORAGE_CENTER));
    }

    // localStorage 設定上一次選擇的 API Type
    if (localStorage.getItem(STORAGE_API_TYPE)) {
        DomElm.apiType.value = localStorage.getItem(STORAGE_API_TYPE);
    }

    // 設定上一次選擇的 zoom level
    let zoom = 6;
    if (localStorage.getItem(STORAGE_ZOOM)) {
        zoom = localStorage.getItem(STORAGE_ZOOM);
    }

    _initLocationList();

    const map = new mapboxgl.Map({
        container: 'map',
        accessToken: mapAccessToken,
        style: 'mapbox://styles/mapbox/streets-v12',
        center: center,
        zoom: zoom
    });

    // 禁用雙擊放大地圖(不然在手機版本上會和雙擊顯示雨量資訊衝突)
    map.doubleClickZoom.disable();

    const language = new MapboxLanguage({ defaultLanguage: 'zh-Hant' });
    map.addControl(language);

    _marker = _queryMarker(map);
    _addMapEvent(map);
    _addDefaultMarkers(map);

    _map = map;
}

function _initLocationList() {
    // 將預設的地點加入選單
    defaultLocations.forEach((item) => {
        const option = document.createElement('option');
        option.value = item.location;
        option.text = item.name;
        DomElm.location.appendChild(option);
    });
}

function onLocationChanged() {
    const location = DomElm.location.value?.split(',');
    if (location.length !== 2) return;

    _moveTo(_map, location[1], location[0]);
}

function _queryMarker(map) {
    let html = '<div class="marker-popup"><b>拖曳此標記</b>或<b>雙擊地圖</b>，以查詢<span class="color-danger"> 雨量 </span>資訊';
    html += '<br>查詢的結果請點又上方的<strong class="color-danger"> 看圖表 </strong>按鈕</div>';
    const popup = new mapboxgl.Popup({ offset: 25 })
        .setHTML(html)
        .setMaxWidth('300px');


    const marker = new mapboxgl
        .Marker({
            draggable: true,
            element: _createCustomMarkerElement('img/icon_umbrella.png'),
            offset: [0, -15] // X, Y 偏移量
        })
        .setLngLat(center)
        .setPopup(popup)
        .addTo(map);

    if (!localStorage.getItem(STORAGE_FIRST_LOAD)) {
        marker.togglePopup();
    }
    localStorage.setItem(STORAGE_FIRST_LOAD, 'true');

    marker.on('dragend', () => {
        DomElm.location.selectedIndex = 0;
        const coords = marker.getLngLat();
        _callApiAndDisplayCoordinates(coords.lat, coords.lng);
    });

    return marker;
}

function _addDefaultMarkers(map) {
    defaultLocations.forEach((item) => {
        const marker = new mapboxgl
            .Marker({ color: '#842B00' })
            .setLngLat([item.location[1], item.location[0]])
            .addTo(map);

        marker.getElement().addEventListener('click', () => {
            _moveTo(map, item.location[1], item.location[0]);
        });
    });
}

function _createCustomMarkerElement(imagePath) {
    const el = document.createElement('div');
    const width = 30;
    const height = 30;
    el.className = 'marker';
    el.style.backgroundImage = 'url(' + imagePath + ')';
    el.style.width = `${width}px`;
    el.style.height = `${height}px`;
    el.style.backgroundSize = '100%';
    el.style.backgroundRepeat = 'no-repeat';
    el.style.zIndex = '1';

    return el;
}

function _addMapEvent(map) {
    map.on('load', () => {
        // 載入後直接呼叫一次 _callApiAndDisplayCoordinates，顯示目前座標的雨量資訊
        _callApiAndDisplayCoordinates(center[1], center[0]);
    });

    map.on('zoom', () => {
        const currentZoom = map.getZoom();
        localStorage.setItem(STORAGE_ZOOM, currentZoom);
    });

    map.on('dblclick', (e) => {
        e.preventDefault();

        const coords = e.lngLat;
        _moveTo(map, coords.lng, coords.lat);
    });
}

function _moveTo(map, lng, lat) {
    _marker.setLngLat([lng, lat]);
    map.flyTo({ center: [lng, lat] });
    _callApiAndDisplayCoordinates(lat, lng);
}

function _callApiAndDisplayCoordinates(lat, lng) {
    DomElm.coordinates.style.display = 'block';
    DomElm.coordinates.innerHTML = `Latitude: ${lat}<br/>Longitude: ${lng}`;

    callWeatherApi(lat, lng);

    // localStorage 記錄本次瀏覽的 center
    localStorage.setItem(STORAGE_CENTER, JSON.stringify([lng, lat]));
}
