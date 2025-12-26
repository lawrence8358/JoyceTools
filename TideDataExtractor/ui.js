/**
 * ui.js - UI 互動管理
 * 
 * 用途：處理使用者介面相關功能，包括：
 * - 顯示狀態訊息
 * - 更新按鈕狀態
 * - 下載 JSON 檔案
 * - 顯示除錯資訊
 */

/**
 * 顯示狀態訊息
 * @param {string} message - 訊息內容
 * @param {string} type - 訊息類型 (success/error/info)
 */
function showStatus(message, type) {
    const statusDiv = document.getElementById('status');
    // Map our types to Bootstrap alert classes
    const typeMap = {
        success: 'alert alert-success',
        error: 'alert alert-danger',
        info: 'alert alert-info',
        processing: 'alert alert-dark'
    };

    const cls = typeMap[type] || 'alert alert-secondary';
    statusDiv.innerHTML = `<div class="${cls} mb-0">${message}</div>`;
    statusDiv.style.display = 'block';

    if (type === 'success' && !message.includes('進度')) {
        setTimeout(() => {
            statusDiv.style.display = 'none';
        }, 3000);
    }
}

/**
 * 下載 JSON 檔案
 * @param {Object} data - 要下載的資料
 * @param {string} filename - 檔案名稱（可選）
 */
function downloadJsonFile(data, filename) {
    if (!data) {
        showStatus('沒有可下載的資料', 'error');
        return;
    }

    if (!filename) {
        const dateStr = new Date().toISOString().split('T')[0].replace(/-/g, '');
        filename = `tide_data_all_${dateStr}.json`;
    }

    const jsonStr = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);

    chrome.downloads.download({
        url: url,
        filename: filename,
        saveAs: true
    }, function (downloadId) {
        if (chrome.runtime.lastError) {
            showStatus('下載失敗: ' + chrome.runtime.lastError.message, 'error');
        } else {
            showStatus('✅ 檔案已開始下載', 'success');
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    });
}

/* debug UI removed */
