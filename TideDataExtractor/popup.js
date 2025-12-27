/**
 * popup.js - 主要入口點和事件處理
 * 
 * 用途：整合所有模組並處理使用者事件，包括：
 * - 初始化擴充功能
 * - 處理按鈕點擊事件
 * - 協調各模組之間的互動
 * 
 * 依賴模組：
 * - config.js: 設定資料
 * - storage.js: 資料儲存
 * - extractor.js: 資料提取
 * - api.js: API 通訊
 * - ui.js: UI 管理
 */

// 全域變數
let extractedData = null;
let isExtracting = false;

// 頁面載入完成後初始化
document.addEventListener('DOMContentLoaded', async function () {
    // 取得 DOM 元素
    const extractBtn = document.getElementById('extractBtn');
    const extractAllBtn = document.getElementById('extractAllBtn');
    const downloadBtn = document.getElementById('downloadBtn');
    const uploadBtn = document.getElementById('uploadBtn');
    const locationSelect = document.getElementById('location');
    const currentUrlSpan = document.getElementById('currentUrl');
    const apiUrlInput = document.getElementById('apiUrl');

    // 初始化：從 storage 恢復狀態
    await initializeState();

    // 初始化：取得並顯示當前分頁 URL
    initializeCurrentUrl();

    // 註冊所有事件監聽器
    registerEventListeners();

    /**
     * 初始化狀態（從 storage 恢復）
     */
    async function initializeState() {
        const state = await restoreState();

        // 恢復 API URL
        apiUrlInput.value = state.apiUrl || DEFAULT_API_URL;

        // 恢復擷取的資料
        if (state.extractedData && !state.isExpired) {
            extractedData = state.extractedData;
            downloadBtn.disabled = false;
            uploadBtn.disabled = false;

            const locationCount = extractedData.locations?.length || 0;
            const savedTime = new Date(state.savedTime);
            const savedTimeStr = savedTime.toLocaleTimeString('zh-TW');
            showStatus(`📋 已恢復先前擷取的資料 (${locationCount} 個地點，${savedTimeStr} 擷取)`, 'info');
        } else if (state.isExpired) {
            showStatus('⏰ 先前擷取的資料已過期（超過2小時）', 'info');
        }

        // 檢查未完成的擷取任務
        if (state.hasUnfinishedTask) {
            showStatus('⚠️ 偵測到未完成的擷取任務，請重新點擊「一次提取全部地點」', 'info');
        }
    }

    /**
     * 初始化當前 URL 顯示和地點選擇
     */
    function initializeCurrentUrl() {
        chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            if (tabs[0]) {
                const url = tabs[0].url;
                currentUrlSpan.textContent = url;

                // 根據 URL 自動選擇地點
                for (const [location, expectedUrl] of Object.entries(locationUrls)) {
                    if (url.includes(expectedUrl.split('.com/')[1])) {
                        locationSelect.value = location;
                        break;
                    }
                }
            }
        });
    }

    /**
     * 註冊所有事件監聽器
     */
    function registerEventListeners() {
        // 前往網站按鈕
        document.getElementById('goToUrlBtn').addEventListener('click', handleGoToUrl);

        // 複製網址按鈕
        document.getElementById('copyUrlBtn').addEventListener('click', handleCopyUrl);

        // 重新整理頁面按鈕
        document.getElementById('reloadPageBtn').addEventListener('click', handleReloadPage);

        // 提取單一地點資料按鈕
        extractBtn.addEventListener('click', handleExtractSingle);

        // 提取所有地點資料按鈕
        extractAllBtn.addEventListener('click', handleExtractAll);

        // 下載按鈕
        downloadBtn.addEventListener('click', handleDownload);

        // 上傳按鈕
        uploadBtn.addEventListener('click', handleUpload);

        // API URL 變更時自動保存
        apiUrlInput.addEventListener('change', handleApiUrlChange); 

        // 標籤頁切換處理 (使用 Bootstrap)
        const tabButtons = document.querySelectorAll('[data-bs-toggle="tab"]');
        tabButtons.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const tab = new bootstrap.Tab(this);
                tab.show();
            });
        });
    }

    /**
     * 開始一個需要鎖定其他按鈕的動作
     */
    function beginAction() {
        // Disable auxiliary and action buttons while an operation is running
        document.getElementById('goToUrlBtn').disabled = true;
        document.getElementById('copyUrlBtn').disabled = true;
        document.getElementById('extractAllBtn').disabled = true;
        document.getElementById('extractBtn').disabled = true;
        document.getElementById('uploadBtn').disabled = true;
        document.getElementById('downloadBtn').disabled = true;
        isExtracting = true;
    }

    /**
     * 結束動作，依據目前狀態還原按鈕
     */
    function endAction() {
        isExtracting = false;
        // Re-enable auxiliary buttons
        document.getElementById('goToUrlBtn').disabled = false;
        document.getElementById('copyUrlBtn').disabled = false;

        // Re-enable extract buttons
        document.getElementById('extractAllBtn').disabled = false;
        document.getElementById('extractBtn').disabled = false;

        // Enable download/upload only when we have extracted data
        const hasData = !!extractedData;
        document.getElementById('downloadBtn').disabled = !hasData;
        document.getElementById('uploadBtn').disabled = !hasData;
    }

    /**
     * 處理「前往網站」按鈕點擊
     */
    function handleGoToUrl() {
        const location = locationSelect.value;
        const url = locationUrls[location];
        chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            chrome.tabs.update(tabs[0].id, { url: url });
        });
    }

    /**
     * 處理「複製網址」按鈕點擊
     */
    function handleCopyUrl() {
        const location = locationSelect.value;
        const url = locationUrls[location];
        navigator.clipboard.writeText(url).then(() => {
            showStatus(`網址已複製: ${url}`, 'success');
        }).catch(err => {
            showStatus('複製失敗', 'error');
        });
    }

    /**
     * 處理「重新整理頁面」按鈕點擊
     */
    function handleReloadPage() {
        chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            chrome.tabs.reload(tabs[0].id);
            showStatus('✅ 頁面正在重新整理...', 'info');
            setTimeout(() => {
                showStatus('✅ 請等待頁面完全載入後再次使用擴充功能', 'success');
            }, 1000);
        });
    }

    /**
     * 處理「提取當前頁面資料」按鈕點擊
     */
    function handleExtractSingle() {
        const location = locationSelect.value;
        const timezone = locationTimezones[location];
        showStatus('正在提取資料...', 'info');
        beginAction();

        chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            const currentTab = tabs[0];
            
            if (!currentTab.url.includes('zh.tideschart.com')) {
                extractBtn.disabled = false;
                showStatus('錯誤: 請在潮汐網站 (zh.tideschart.com) 上使用此擴充功能', 'error');
                return;
            }

            extractDataFromCurrentPage(location, timezone, async function(success, data, error) {
                endAction();
                
                if (success) {
                    extractedData = {
                        extractTime: new Date().toISOString(),
                        url: currentTab.url,
                        locations: [{
                            name: location,
                            timezone: timezone,
                            data: data
                        }]
                    };

                    // 保存到 storage
                    await saveState(extractedData, apiUrlInput.value);

                    const totalDays = data.length;
                    showStatus(`✅ 成功提取 ${location} 的 ${totalDays} 天潮汐資料`, 'success');
                    downloadBtn.disabled = false;
                    uploadBtn.disabled = false;
                } else {
                    showStatus('❌ ' + error, 'error');
                }
            });
        });
    }

    /**
     * 處理「一次提取全部地點」按鈕點擊
     */
    async function handleExtractAll() {
        const locations = ['Sydney', 'Chennai', 'IndianOcean', 'Tokyo'];

        beginAction();
        await saveExtractionState(true, locations, 0);
        showStatus('準備提取所有地點的資料...', 'info');

        // 使用 extractor.js 的批次提取功能
        await extractAllLocations(
            locations,
            // 進度回調
            (location, status, message) => {
                showStatus(message, status);
            },
            // 完成回調
            async (allData, completedCount, failedCount) => {
                endAction();
                await saveExtractionState(false, [], 0);

                if (completedCount > 0) {
                    extractedData = allData;
                    await saveState(extractedData, apiUrlInput.value);
                    downloadBtn.disabled = false;
                    uploadBtn.disabled = false;
                    showStatus(`✅ 全部完成！成功: ${completedCount}, 失敗: ${failedCount}`, 'success');
                } else {
                    showStatus(`❌ 所有地點提取失敗，請檢查網路連線或重新載入擴充功能`, 'error');
                }
            }
        );
    }

    /**
     * 處理「下載 JSON 檔案」按鈕點擊
     */
    function handleDownload() {
        // disable other actions while download is prepared
        beginAction();
        try {
            downloadJsonFile(extractedData);
        } finally {
            // downloadJsonFile triggers a file save quickly; restore button states
            endAction();
        }
    }

    /**
     * 處理「上傳到資料庫」按鈕點擊
     */
    async function handleUpload() {
        beginAction();
        showStatus('⏳ 正在上傳資料...', 'info');

        try {
            const result = await uploadToApi(extractedData, apiUrlInput.value);
            showStatus(`✅ 上傳成功！已匯入 ${result.importedCount} 筆資料`, 'success');

            // 保存 API URL
            await saveState(extractedData, apiUrlInput.value);
        } catch (error) {
            alert(`上傳失敗: ${JSON.stringify(error)}`);
            showStatus(`❌ 上傳失敗: ${error.message}`, 'error');
        } finally {
            endAction();
        }
    }

    /**
     * 處理 API URL 變更
     */
    async function handleApiUrlChange() {
        await saveState(extractedData, apiUrlInput.value);
    }
});
