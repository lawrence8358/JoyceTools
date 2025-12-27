// 載入共用模組（必須在所有程式碼之前）
importScripts('config.js', 'scheduler.js');

/**
 * background.js - 背景服務腳本
 * 
 * 用途：管理背景執行的排程任務，包括：
 * - 監聽 alarms 事件並觸發排程任務
 * - 執行自動提取和上傳流程
 * - 管理排程狀態和執行歷史
 * - 處理來自 popup 的訊息
 */

// ==================== 全域變數 ====================

let isExecuting = false;
let currentAlarms = new Map(); // 記錄當前設定的 alarms

// ==================== 初始化 ====================

// 擴充功能安裝或更新時初始化
chrome.runtime.onInstalled.addListener(() => {
    console.log('潮汐資料提取器已安裝/更新');
    initializeSchedules();
});

// 擴充功能啟動時初始化
chrome.runtime.onStartup.addListener(() => {
    console.log('潮汐資料提取器已啟動');
    initializeSchedules();
});

/**
 * 初始化排程
 */
async function initializeSchedules() {
    try {
        // 清除所有現有的 alarms
        await chrome.alarms.clearAll();
        currentAlarms.clear();
        
        // 檢查排程是否啟用
        const enabled = await isScheduleEnabled();
        if (!enabled) {
            console.log('排程功能已停用');
            return;
        }
        
        // 載入所有排程並設定 alarms
        const schedules = await getAllSchedules();
        const activeSchedules = schedules.filter(s => s.enabled);
        
        console.log(`載入 ${activeSchedules.length} 個啟用的排程`);
        
        for (const schedule of activeSchedules) {
            await setupAlarmForSchedule(schedule);
        }
    } catch (error) {
        console.error('初始化排程失敗:', error);
    }
}

/**
 * 為排程設定 alarm
 * @param {Object} schedule - 排程物件
 */
async function setupAlarmForSchedule(schedule) {
    try {
        const nextTime = getNextCronExecution(schedule.cronExpression);
        
        if (!nextTime) {
            console.error(`無法計算排程 ${schedule.name} 的下次執行時間`);
            return;
        }
        
        const alarmName = `schedule_${schedule.id}`;
        const whenMs = nextTime.getTime();
        
        // 設定 alarm
        await chrome.alarms.create(alarmName, {
            when: whenMs
        });
        
        currentAlarms.set(schedule.id, {
            alarmName,
            nextTime: nextTime.toISOString()
        });
        
        console.log(`已設定排程 ${schedule.name}，下次執行: ${formatLocalTime(nextTime)}`);
    } catch (error) {
        console.error(`設定排程 ${schedule.name} 失敗:`, error);
    }
}

// ==================== Alarm 處理 ====================

/**
 * 監聽 alarm 觸發事件
 */
chrome.alarms.onAlarm.addListener(async (alarm) => {
    console.log(`Alarm 觸發: ${alarm.name}`);
    
    // 檢查是否為排程 alarm
    if (!alarm.name.startsWith('schedule_')) {
        return;
    }
    
    const scheduleId = alarm.name.replace('schedule_', '');
    await executeScheduledTask(scheduleId);
});

/**
 * 執行排程任務
 * @param {string} scheduleId - 排程 ID
 */
async function executeScheduledTask(scheduleId) {
    // 防止重複執行
    if (isExecuting) {
        console.log('任務執行中，跳過本次排程');
        return;
    }
    
    isExecuting = true;
    
    try {
        // 取得排程資訊
        const schedules = await getAllSchedules();
        const schedule = schedules.find(s => s.id === scheduleId);
        
        if (!schedule) {
            console.error(`找不到排程 ID: ${scheduleId}`);
            return;
        }
        
        if (!schedule.enabled) {
            console.log(`排程 ${schedule.name} 已停用，跳過執行`);
            return;
        }
        
        console.log(`開始執行排程: ${schedule.name}`);
        
        // 執行提取和上傳流程
        const result = await executeExtractAndUpload();
        
        // 記錄執行結果
        await addExecutionRecord({
            scheduleName: schedule.name,
            success: result.success,
            message: result.message,
            executedAt: new Date().toISOString()
        });
        
        // 更新排程的最後執行時間
        await updateSchedule(scheduleId, {
            lastExecutedAt: new Date().toISOString()
        });
        
        console.log(`排程 ${schedule.name} 執行完成:`, result.message);
        
    } catch (error) {
        console.error('執行排程任務失敗:', error);
        
        // 記錄錯誤
        await addExecutionRecord({
            scheduleName: '未知排程',
            success: false,
            message: `執行失敗: ${error.message}`,
            executedAt: new Date().toISOString()
        });
    } finally {
        isExecuting = false;
        
        // 重新設定下次執行時間
        await setupNextExecution(scheduleId);
    }
}

/**
 * 設定下次執行時間
 * @param {string} scheduleId - 排程 ID
 */
async function setupNextExecution(scheduleId) {
    try {
        const schedules = await getAllSchedules();
        const schedule = schedules.find(s => s.id === scheduleId);
        
        if (schedule && schedule.enabled) {
            await setupAlarmForSchedule(schedule);
        }
    } catch (error) {
        console.error('設定下次執行時間失敗:', error);
    }
}

// ==================== 提取和上傳流程 ====================

/**
 * 執行提取和上傳流程（背景執行版本）
 * @returns {Promise<Object>} { success: boolean, message: string }
 */
async function executeExtractAndUpload() {
    try {
        // 1. 取得 API URL
        const state = await chrome.storage.local.get(['apiUrl']);
        const apiUrl = state.apiUrl || 'http://localhost:5000';
        
        console.log('步驟 1: 開始提取所有地點資料');
        
        // 2. 提取所有地點資料
        const extractResult = await extractAllLocationsInBackground();
        
        if (!extractResult.success) {
            return {
                success: false,
                message: `提取失敗: ${extractResult.error}`
            };
        }
        
        console.log(`步驟 2: 提取完成，成功 ${extractResult.completedCount} 個，失敗 ${extractResult.failedCount} 個`);
        
        if (extractResult.completedCount === 0) {
            return {
                success: false,
                message: '所有地點提取失敗'
            };
        }
        
        // 3. 上傳資料到 API
        console.log('步驟 3: 開始上傳資料到 API');
        
        try {
            const response = await fetch(`${apiUrl}/api/Tide/Import`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(extractResult.data)
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
            
            const uploadResult = await response.json();
            
            console.log('步驟 4: 上傳完成');
            
            return {
                success: true,
                message: `成功提取 ${extractResult.completedCount} 個地點，已匯入 ${uploadResult.importedCount} 筆資料`
            };
            
        } catch (uploadError) {
            return {
                success: false,
                message: `提取成功但上傳失敗: ${uploadError.message}`
            };
        }
        
    } catch (error) {
        console.error('執行流程失敗:', error);
        return {
            success: false,
            message: `執行失敗: ${error.message}`
        };
    }
}

/**
 * 在背景提取所有地點資料
 * @returns {Promise<Object>} { success, data, completedCount, failedCount, error }
 */
async function extractAllLocationsInBackground() {
    // 使用 config.js 中定義的地點資訊
    const locations = ['Sydney', 'Chennai', 'IndianOcean', 'Tokyo'];
    
    let allData = {
        extractTime: new Date().toISOString(),
        locations: []
    };
    
    let completedCount = 0;
    let failedCount = 0;
    
    for (const location of locations) {
        const url = locationUrls[location];
        const timezone = locationTimezones[location];
        
        console.log(`處理 ${location} (${completedCount + failedCount + 1}/${locations.length})`);
        
        try {
            // 在新分頁開啟
            const tab = await chrome.tabs.create({ url: url, active: false });
            
            // 等待頁面載入
            await new Promise(resolve => setTimeout(resolve, 8000));
            
            // 智能等待表格載入
            let tableFound = false;
            for (let retry = 0; retry < 5; retry++) {
                try {
                    const checkResult = await chrome.tabs.sendMessage(tab.id, { 
                        action: 'checkTableExists' 
                    });
                    
                    if (checkResult && checkResult.exists) {
                        tableFound = true;
                        console.log(`${location}: 表格已載入`);
                        break;
                    }
                } catch (e) {
                    // 忽略錯誤，繼續重試
                }
                
                console.log(`${location}: 等待表格載入... (${retry + 1}/5)`);
                await new Promise(resolve => setTimeout(resolve, 2000));
            }
            
            if (!tableFound) {
                throw new Error('等待超時：表格未載入');
            }
            
            // 提取資料
            const result = await chrome.tabs.sendMessage(tab.id, {
                action: 'extractTideData',
                location: location,
                timezone: timezone
            });
            
            // 關閉分頁
            await chrome.tabs.remove(tab.id);
            
            if (result && result.success) {
                allData.locations.push({
                    name: location,
                    timezone: timezone,
                    url: url,
                    data: result.data
                });
                completedCount++;
                console.log(`✅ ${location} 完成 (${result.data.length} 天)`);
            } else {
                failedCount++;
                console.error(`❌ ${location} 失敗:`, result?.error);
            }
            
            // 等待1秒再處理下一個
            await new Promise(resolve => setTimeout(resolve, 1000));
            
        } catch (error) {
            failedCount++;
            console.error(`❌ ${location} 處理失敗:`, error);
            
            // 嘗試關閉可能殘留的分頁
            try {
                const tabs = await chrome.tabs.query({ url: url });
                for (const tab of tabs) {
                    await chrome.tabs.remove(tab.id);
                }
            } catch (e) {
                // 忽略清理錯誤
            }
        }
    }
    
    if (completedCount === 0) {
        return {
            success: false,
            error: '所有地點提取失敗',
            completedCount: 0,
            failedCount: failedCount
        };
    }
    
    return {
        success: true,
        data: allData,
        completedCount: completedCount,
        failedCount: failedCount
    };
}

// ==================== 訊息處理 ====================

/**
 * 監聽來自 popup 的訊息
 */
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === 'updateSchedules') {
        // 重新初始化排程
        initializeSchedules().then(() => {
            sendResponse({ success: true });
        }).catch(error => {
            sendResponse({ success: false, error: error.message });
        });
        return true; // 保持訊息通道開啟
    }
    
    if (request.action === 'testSchedule') {
        // 測試執行排程
        executeScheduledTask(request.scheduleId).then(() => {
            sendResponse({ success: true });
        }).catch(error => {
            sendResponse({ success: false, error: error.message });
        });
        return true;
    }
    
    if (request.action === 'getScheduleStatus') {
        // 取得排程狀態
        getScheduleStatus().then(status => {
            sendResponse(status);
        }).catch(error => {
            sendResponse({ error: error.message });
        });
        return true;
    }
});

/**
 * 取得排程狀態
 * @returns {Promise<Object>} 排程狀態資訊
 */
async function getScheduleStatus() {
    const schedules = await getAllSchedules();
    const enabled = await isScheduleEnabled();
    
    const status = {
        enabled: enabled,
        totalSchedules: schedules.length,
        activeSchedules: schedules.filter(s => s.enabled).length,
        isExecuting: isExecuting,
        nextExecutions: []
    };
    
    // 取得每個排程的下次執行時間
    for (const schedule of schedules) {
        if (schedule.enabled) {
            const alarm = currentAlarms.get(schedule.id);
            if (alarm) {
                status.nextExecutions.push({
                    scheduleName: schedule.name,
                    nextTime: alarm.nextTime
                });
            }
        }
    }
    
    return status;
}
