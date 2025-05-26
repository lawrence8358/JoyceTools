const DomElm = {
    get coordinates() {
        return document.getElementById('coordinates');
    },
    get btnDialog() {
        return document.getElementById('btn-dialog');
    },
    get modal() {
        return document.getElementById('modal-container');
    },
    get modalBody() {
        return document.getElementById('my-modal');
    },
    get modelName() {
        return document.getElementById('model-name');
    },
    get apiType() {
        return document.getElementById('select-api-type');
    },
    get location() {
        return document.getElementById('select-location');
    }
};


const STORAGE_CENTER = 'center';
const STORAGE_API_TYPE = 'apiType';
const STORAGE_ZOOM = 'zoom';
const STORAGE_FIRST_LOAD = 'firstload';

let lastLat = 0, lastLng = 0;


function callWeatherApi(lat, lng) {
    DomElm.btnDialog.style.display = 'none';
    DomElm.modalBody.classList.add('loading');

    const model = DomElm.apiType.value;

    const url = `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lng}&hourly=rain&models=${model}&timezone=Asia%2FSingapore&past_days=1&forecast_days=2`;
    fetch(url)
        .then(response => response.json())
        .then(data => {
            const hourly = data.hourly;

            drawTable(hourly.time, hourly.rain);

            DomElm.btnDialog.style.display = 'block';
            DomElm.modalBody.classList.remove('loading');

            lastLat = lat;
            lastLng = lng;
        });
}


function drawTable(times, rains) {
    const containerElm = document.getElementById('history-container');
    containerElm.innerHTML = "";

    // 依日期分組，key 格式為 "YYYY-MM-DD"
    const groups = {};
    for (let i = 0; i < times.length; i++) {
        const dateObj = new Date(times[i]);
        const year = dateObj.getFullYear();
        const month = (dateObj.getMonth() + 1).toString().padStart(2, '0');
        const day = dateObj.getDate().toString().padStart(2, '0');
        const dateKey = `${year}-${month}-${day}`;
        const hour = dateObj.getHours().toString().padStart(2, '0');

        if (!groups[dateKey]) {
            groups[dateKey] = [];
        }
        groups[dateKey].push({ hour, rain: rains[i] });
    }

    // 為每一天建立一個 table
    for (const dateKey in groups) {
        // 建立 table 元素
        const table = document.createElement("table");
        table.style.marginBottom = "20px"; // 每個 table 間隔一些距離

        // 依日期格式化日期顯示，格式為 MM-dd
        const parts = dateKey.split("-");
        const dateDisplay = `${parts[1]}-${parts[2]}`;

        // 建立時間（header）列
        const timeRow = document.createElement("tr");
        // 第一個單元格顯示日期 MM-dd
        const dateHeader = document.createElement("td");
        dateHeader.textContent = dateDisplay;
        dateHeader.style.fontWeight = "bold";
        timeRow.appendChild(dateHeader);

        // 建立降雨量列
        const rainRow = document.createElement("tr");
        const rainHeader = document.createElement("td");
        rainHeader.textContent = "雨量";
        rainHeader.style.fontWeight = "bold";
        rainRow.appendChild(rainHeader);

        // 取出該天的資料陣列，依序填入剩餘欄位
        const dayData = groups[dateKey];
        dayData.forEach(item => {
            // 時間列僅顯示小時
            const timeCell = document.createElement("td");
            timeCell.textContent = item.hour;
            timeRow.appendChild(timeCell);

            // 降雨量列對應填入數值
            const rainCell = document.createElement("td");
            rainCell.textContent = item.rain;
            rainRow.appendChild(rainCell);
        });

        table.appendChild(timeRow);
        table.appendChild(rainRow);

        // 將 table 加入到容器 div 中
        containerElm.appendChild(table);
    }
}


function openDialog() {
    DomElm.modal.style.opacity = 1;
    DomElm.modal.style.pointerEvents = 'unset';
}


function closeDialog() {
    DomElm.modal.style.opacity = 0;
    DomElm.modal.style.pointerEvents = 'none';
}

function syncApiTypeList() {
    DomElm.modelName.innerHTML = DomElm.apiType.innerHTML;
}

function onApiTypeChanged(type) {
    if (type === 1)
        DomElm.modelName.value = DomElm.apiType.value;
    else
        DomElm.apiType.value = DomElm.modelName.value;

    localStorage.setItem(STORAGE_API_TYPE, DomElm.apiType.value);
    callWeatherApi(lastLat, lastLng);
} 