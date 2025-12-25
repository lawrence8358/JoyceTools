/**
 * api.js - API 通訊模組
 * 
 * 用途：處理與後端 API 的通訊，包括：
 * - 上傳資料到 API
 * - 處理 API 回應
 * - 錯誤處理
 */

/**
 * 上傳資料到 API
 * @param {Object} extractedData - 要上傳的潮汐資料
 * @param {string} apiUrl - API 基礎網址
 * @returns {Promise<Object>} API 回應結果
 */
async function uploadToApi(extractedData, apiUrl) {
    if (!extractedData) {
        throw new Error('沒有可上傳的資料');
    }

    const response = await fetch(`${apiUrl}/api/Tide/Import`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(extractedData)
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return await response.json();
}
