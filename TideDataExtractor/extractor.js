/**
 * extractor.js - 資料提取核心邏輯
 * 
 * 用途：處理從網頁提取潮汐資料的核心功能，包括：
 * - 單一地點資料提取
 * - 批次提取所有地點
 * - 智能等待和重試機制
 * - 與 content.js 通訊
 */

/**
 * 從當前頁面提取資料
 * @param {string} location - 地點名稱
 * @param {number} timezone - 時區偏移
 * @param {Function} callback - 回調函數 (success, data, error)
 */
function extractDataFromCurrentPage(location, timezone, callback) {
    chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
        const currentTab = tabs[0];

        chrome.tabs.sendMessage(
            currentTab.id,
            { action: 'extractTideData', location: location, timezone: timezone },
            function (response) {
                if (chrome.runtime.lastError) {
                    const shouldReload = confirm(
                        '無法連接到頁面內容腳本。\n\n' +
                        '這通常是因為：\n' +
                        '1. 擴充功能在頁面開啟後才安裝/更新\n' +
                        '2. 頁面需要重新載入\n\n' +
                        '是否要現在重新整理頁面？\n' +
                        '（重新整理後請再次點擊擴充功能圖標）'
                    );

                    if (shouldReload) {
                        chrome.tabs.reload(currentTab.id);
                        callback(false, null, '頁面正在重新整理...請等待完成後再試');
                    } else {
                        callback(false, null, '請手動按 F5 重新整理頁面後再試');
                    }
                } else if (response && response.success) {
                    callback(true, response.data, null);
                } else {
                    callback(false, null, response?.error || '未知錯誤');
                }
            }
        );
    });
}

/**
 * 批次提取所有地點的資料
 * @param {Array<string>} locations - 地點清單
 * @param {Function} onProgress - 進度回調 (location, status, message)
 * @param {Function} onComplete - 完成回調 (allData, completedCount, failedCount)
 */
async function extractAllLocations(locations, onProgress, onComplete) {
    let allData = {
        extractTime: new Date().toISOString(),
        locations: []
    };

    let completedCount = 0;
    let failedCount = 0;

    for (const location of locations) {
        const url = locationUrls[location];
        const timezone = locationTimezones[location];
        
        onProgress(location, 'processing', `正在處理 ${location} (${completedCount + failedCount + 1}/${locations.length})...`);

        try {
            // 在新分頁開啟並等待載入
            const tab = await new Promise((resolve, reject) => {
                chrome.tabs.create({ url: url, active: false }, function (tab) {
                    if (chrome.runtime.lastError) {
                        reject(chrome.runtime.lastError);
                    } else {
                        resolve(tab);
                    }
                });
            });

            // 等待頁面基本載入（8秒，給 Cloudflare 驗證足夠時間）
            await new Promise(resolve => setTimeout(resolve, 8000));

            // 智能等待：檢查表格是否已載入（最多重試5次，每次2秒）
            let tableFound = false;
            for (let retry = 0; retry < 5; retry++) {
                const checkResult = await new Promise((resolve) => {
                    chrome.tabs.sendMessage(
                        tab.id,
                        { action: 'checkTableExists' },
                        function (response) {
                            if (chrome.runtime.lastError) {
                                resolve({ exists: false });
                            } else {
                                resolve(response || { exists: false });
                            }
                        }
                    );
                });

                if (checkResult.exists) {
                    tableFound = true;
                    console.log(`${location}: 表格已載入`);
                    break;
                } else {
                    console.log(`${location}: 等待表格載入... (${retry + 1}/5)`);
                    await new Promise(resolve => setTimeout(resolve, 2000));
                }
            }

            if (!tableFound) {
                throw new Error('等待超時：表格未載入。可能 Cloudflare 驗證未通過。');
            }

            // 提取資料
            const result = await new Promise((resolve) => {
                chrome.tabs.sendMessage(
                    tab.id,
                    { action: 'extractTideData', location: location, timezone: timezone },
                    function (response) {
                        if (chrome.runtime.lastError || !response || !response.success) {
                            resolve({ success: false, error: response?.error || chrome.runtime.lastError?.message });
                        } else {
                            resolve({ success: true, data: response.data });
                        }
                    }
                );
            });

            // 關閉分頁
            chrome.tabs.remove(tab.id);

            if (result.success) {
                allData.locations.push({
                    name: location,
                    timezone: timezone,
                    url: url,
                    data: result.data
                });
                completedCount++;
                onProgress(location, 'success', `✅ ${location} 完成 (${result.data.length} 天) | 進度: ${completedCount + failedCount}/${locations.length}`);
            } else {
                failedCount++;
                console.error(`${location} 提取失敗:`, result.error);
                onProgress(location, 'error', `❌ ${location} 失敗: ${result.error} | 進度: ${completedCount + failedCount}/${locations.length}`);
            }

            // 等待1秒再處理下一個
            await new Promise(resolve => setTimeout(resolve, 1000));

        } catch (error) {
            failedCount++;
            console.error(`${location} 處理失敗:`, error);
            onProgress(location, 'error', `❌ ${location} 處理失敗 | 進度: ${completedCount + failedCount}/${locations.length}`);
        }
    }

    onComplete(allData, completedCount, failedCount);
}
