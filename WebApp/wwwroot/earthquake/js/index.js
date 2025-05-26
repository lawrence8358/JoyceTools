function init() {
    // 初始化結束日期為今天，開始日期為前 7 天
    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    const startDate = new Date(today);
    startDate.setDate(today.getDate() - 7);
    const startDateStr = startDate.toISOString().split('T')[0];

    $('#startDate').val(startDateStr);
    $('#endDate').val(endDate);
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
                            <th>經緯度</th>
                            <th>規模</th>
                            <th>深度</th>
                            <th>圖片</th>
                            <th>來源</th>
                            </tr>
                        </thead>
                    <tbody>`;

        if (data.length === 0) {
            html += '<tr><td colspan="6" class="text-center">查無資料</td></tr>';
        }
        else {
            for (const row of data) {
                // 2025-05-18T13:25:00 格式轉換為 YYYY-MM-DD HH:mm
                const earthquakeDate = new Date(row.earthquakeDate).toLocaleString('zh-TW', {
                    year: 'numeric',
                    month: '2-digit',
                    day: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    hour12: false
                });

                // 開啟 google map 地圖
                const latlng = `${row.latitude},${row.longitude}`;
                const mapUrl = `https://www.google.com/maps/place/${latlng}/@${latlng},8z`;

                html += `<tr>
                        <td class="align-middle">${earthquakeDate}</td>
                        <td class="align-middle text-center">
                            <a href="${mapUrl}" target="_blank">
                                ${row.latitude}, ${row.longitude}
                            </a>
                        </td>
                        <td class="align-middle text-end">${row.magnitude}</td>
                        <td class="align-middle text-end">${row.maxDepth}</td>
                        <td class="align-middle text-center">
                            <a href="/api/Earthquake/Image/Original/${row.imageFileName}" data-lightbox="image-1" data-title="${earthquakeDate}">
                                <img src="/api/Earthquake/Image/Thumb/${row.imageFileName}" alt="縮圖">
                            </a>
                        </td>
                        <td class="align-middle text-center">
                            <a href="${row.linkUrl}" target="_blank">
                                <img src="img/x.png" alt="@cwaeew84024" >
                            </a>
                        </td>
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

function getApiData(sdate, edate) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/api/Earthquake/Query',
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