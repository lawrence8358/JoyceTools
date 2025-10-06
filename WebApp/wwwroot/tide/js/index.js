function init() {
    // 使用台北時間 (UTC+8)
    const now = new Date();
    // 取得台北時間的今天日期
    const taipeiOffset = 8 * 60; // UTC+8 轉換為分鐘
    const taipeiTime = new Date(now.getTime() + (taipeiOffset * 60 * 1000));
    
    // 結束日期：明天 (例如今天6號，結束7號)
    const endDate = new Date(taipeiTime);
    endDate.setDate(taipeiTime.getDate() + 1);
    const endDateStr = endDate.toISOString().split('T')[0];
    
    // 開始日期：前天 (例如今天6號，開始4號)
    const startDate = new Date(taipeiTime);
    startDate.setDate(taipeiTime.getDate() - 2);
    const startDateStr = startDate.toISOString().split('T')[0];

    $('#startDate').val(startDateStr);
    $('#endDate').val(endDateStr);
}

async function queryData() {
    $('#div_error').hide();

    const start = $('#startDate').val();
    const end = $('#endDate').val();

    if (!start || !end) {
        $('#div_error').text('請選擇開始和結束日期').show();
        return;
    }

    try {
        const data = await getApiData(start, end);
        let html = `<div class="table-responsive">
                        <table class="table table-bordered table-hover mt-3 bg-white">
                        <thead class="table-light">
                            <tr>
                            <th>日期</th>
                            <th class="location-header"><a href="https://zh.tideschart.com/Japan/Tokyo" target="_blank">東京</a></th>
                            <th class="location-header"><a href="https://zh.tideschart.com/Australia/New-South-Wales/Sydney" target="_blank">雪梨</a></th>
                            <th class="location-header"><a href="https://zh.tideschart.com/India/Tamil-Nadu/Chennai" target="_blank">印度清奈</a></th>
                            <th class="location-header"><a href="https://zh.tideschart.com/British-Indian-Ocean-Territory" target="_blank">印度洋</a></th>
                            </tr>
                        </thead>
                    <tbody>`;

        if (data.length === 0) {
            html += '<tr><td colspan="5" class="text-center">查無資料</td></tr>';
        }
        else {
            // 使用台北時間來判斷今天
            const now = new Date();
            const taipeiOffset = 8 * 60; // UTC+8 轉換為分鐘
            const taipeiToday = new Date(now.getTime() + (taipeiOffset * 60 * 1000));
            const todayDateStr = taipeiToday.toDateString();
            
            for (const row of data) {
                // 格式化日期
                const rowDate = new Date(row.date);
                const date = rowDate.toLocaleDateString('zh-TW', {
                    year: 'numeric',
                    month: '2-digit',
                    day: '2-digit'
                });

                // 檢查是否為今天，如果是則添加特殊樣式
                const isToday = rowDate.toDateString() === todayDateStr;
                const rowClass = isToday ? 'class="table-warning"' : '';

                html += `<tr ${rowClass}>
                        <td class="align-middle fw-bold">${date}</td>
                        <td class="align-middle">${formatLocationTideInfo(row.tokyo)}</td>
                        <td class="align-middle">${formatLocationTideInfo(row.sydney)}</td>
                        <td class="align-middle">${formatLocationTideInfo(row.chennai)}</td>
                        <td class="align-middle">${formatLocationTideInfo(row.indianOcean)}</td>
                    </tr>`;
            }
        }

        html += `       </tbody>
                    </table>
                </div>`;

        $('#div_result').html(html);
    } catch (error) {
        $('#div_error').text('API 呼叫失敗：' + error).show();
        return;
    }
}

function formatLocationTideInfo(locationData) {
    if (!locationData) {
        return '<span class="no-data">無資料</span>';
    }

    let html = '<div class="tide-info">';
    
    if (locationData.firstHighTideHeight && locationData.firstHighTideTime) {
        const time1 = new Date(locationData.firstHighTideTime).toLocaleTimeString('zh-TW', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
        html += `<div><span class="tide-time">${time1}</span> <span class="tide-height">${locationData.firstHighTideHeight}m</span></div>`;
    }
    
    if (locationData.secondHighTideHeight && locationData.secondHighTideTime) {
        const time2 = new Date(locationData.secondHighTideTime).toLocaleTimeString('zh-TW', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
        html += `<div><span class="tide-time">${time2}</span> <span class="tide-height">${locationData.secondHighTideHeight}m</span></div>`;
    }

    // 如果沒有任何漲潮資料
    if (!locationData.firstHighTideHeight && !locationData.secondHighTideHeight) {
        html += '<span class="no-data">無漲潮資料</span>';
    }
    
    html += '</div>';
    return html;
}

function getApiData(sdate, edate) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/api/Tide/Query',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ sdate, edate }),
            success: function (data) {
                resolve(data);
            },
            error: function (xhr, status, error) {
                reject(error);
            }
        });
    });
}