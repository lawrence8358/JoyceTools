/**
 * storage.js - 資料儲存管理
 * 
 * 用途：處理 chrome.storage 相關操作，包括：
 * - 保存擷取的資料
 * - 恢復擷取的資料
 * - 保存和恢復 API URL 設定
 * - 管理擷取進度狀態
 */

/**
 * 保存擷取的資料到 chrome.storage
 * @param {Object} extractedData - 擷取的潮汐資料
 * @param {string} apiUrl - API 網址
 */
async function saveState(extractedData, apiUrl) {
    await chrome.storage.local.set({
        extractedData: extractedData,
        apiUrl: apiUrl,
        savedTime: new Date().toISOString()
    });
}

/**
 * 保存擷取進度狀態（用於處理 popup 關閉的情況）
 * @param {boolean} isExtracting - 是否正在擷取
 * @param {Array<string>} pendingLocations - 待處理的地點
 * @param {number} currentIndex - 當前處理到的索引
 */
async function saveExtractionState(isExtracting, pendingLocations, currentIndex) {
    await chrome.storage.local.set({
        isExtracting: isExtracting,
        pendingLocations: pendingLocations,
        currentIndex: currentIndex
    });
}

/**
 * 從 chrome.storage 恢復狀態
 * @returns {Promise<Object>} 包含 extractedData, apiUrl, savedTime 等資訊
 */
async function restoreState() {
    const result = await chrome.storage.local.get([
        'extractedData',
        'apiUrl',
        'savedTime',
        'isExtracting',
        'pendingLocations',
        'currentIndex'
    ]);

    // 檢查資料是否過期（超過 2 小時）
    if (result.extractedData && result.savedTime) {
        const savedTime = new Date(result.savedTime);
        const now = new Date();
        const hoursDiff = (now - savedTime) / (1000 * 60 * 60);

        if (hoursDiff >= 2) {
            // 資料過期，清除
            await chrome.storage.local.remove(['extractedData', 'savedTime']);
            result.extractedData = null;
            result.isExpired = true;
        }
    }

    // 檢查是否有未完成的擷取任務
    if (result.isExtracting) {
        await chrome.storage.local.remove(['isExtracting', 'pendingLocations', 'currentIndex']);
        result.hasUnfinishedTask = true;
    }

    return result;
}
