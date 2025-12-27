/**
 * scheduler.js - 排程管理核心模組
 * 
 * 用途：處理自動排程相關功能，包括：
 * - Cron 表達式解析和驗證
 * - 排程任務的 CRUD 操作
 * - 計算下次執行時間
 * - 執行歷史記錄管理
 * 
 * 支援功能：
 * - 多筆排程設定
 * - Cron 格式或簡易週間+時間格式
 * - 排程啟用/停用
 * - 執行歷史記錄（最近5筆）
 */

// ==================== 常數定義 ====================

const STORAGE_KEYS = {
    SCHEDULES: 'schedules',
    SCHEDULE_ENABLED: 'scheduleEnabled',
    EXECUTION_HISTORY: 'executionHistory'
};

const MAX_HISTORY_RECORDS = 5;

// ==================== Cron 解析器 ====================

/**
 * 解析 Cron 表達式（支援標準 5 或 6 欄位格式）
 * 格式: 分 時 日 月 週
 * 範例: "0 9 * * 1-5" = 週一到週五早上9點
 * 
 * @param {string} cronExpression - Cron 表達式
 * @returns {Object|null} 解析結果 { minute, hour, dayOfMonth, month, dayOfWeek } 或 null
 */
function parseCronExpression(cronExpression) {
    if (!cronExpression || typeof cronExpression !== 'string') {
        return null;
    }

    const parts = cronExpression.trim().split(/\s+/);
    
    // 支援 5 欄位格式 (分 時 日 月 週)
    if (parts.length !== 5) {
        return null;
    }

    return {
        minute: parts[0],      // 0-59
        hour: parts[1],        // 0-23
        dayOfMonth: parts[2],  // 1-31
        month: parts[3],       // 1-12
        dayOfWeek: parts[4]    // 0-6 (0=週日)
    };
}

/**
 * 驗證 Cron 表達式是否有效
 * @param {string} cronExpression - Cron 表達式
 * @returns {boolean} 是否有效
 */
function validateCronExpression(cronExpression) {
    const parsed = parseCronExpression(cronExpression);
    if (!parsed) return false;

    // 驗證分鐘 (0-59 或 *)
    if (!validateCronField(parsed.minute, 0, 59)) return false;
    
    // 驗證小時 (0-23 或 *)
    if (!validateCronField(parsed.hour, 0, 23)) return false;
    
    // 驗證日 (1-31 或 *)
    if (!validateCronField(parsed.dayOfMonth, 1, 31)) return false;
    
    // 驗證月 (1-12 或 *)
    if (!validateCronField(parsed.month, 1, 12)) return false;
    
    // 驗證週 (0-6 或 *)
    if (!validateCronField(parsed.dayOfWeek, 0, 6)) return false;

    return true;
}

/**
 * 驗證單一 Cron 欄位
 * @param {string} field - 欄位值
 * @param {number} min - 最小值
 * @param {number} max - 最大值
 * @returns {boolean} 是否有效
 */
function validateCronField(field, min, max) {
    // 允許 *
    if (field === '*') return true;
    
    // 允許數字
    if (/^\d+$/.test(field)) {
        const num = parseInt(field, 10);
        return num >= min && num <= max;
    }
    
    // 允許範圍 (如 1-5)
    if (/^\d+-\d+$/.test(field)) {
        const [start, end] = field.split('-').map(n => parseInt(n, 10));
        return start >= min && end <= max && start <= end;
    }
    
    // 允許列表 (如 1,3,5)
    if (/^\d+(,\d+)+$/.test(field)) {
        const numbers = field.split(',').map(n => parseInt(n, 10));
        return numbers.every(n => n >= min && n <= max);
    }
    
    // 允許步進 (如 */5)
    if (/^\*\/\d+$/.test(field)) {
        const step = parseInt(field.split('/')[1], 10);
        return step > 0 && step <= max;
    }
    
    return false;
}

/**
 * 計算 Cron 排程的下次執行時間
 * @param {string} cronExpression - Cron 表達式
 * @param {Date} fromDate - 起始時間（預設為當前時間）
 * @returns {Date|null} 下次執行時間或 null
 */
function getNextCronExecution(cronExpression, fromDate = new Date()) {
    const parsed = parseCronExpression(cronExpression);
    if (!parsed) return null;

    let nextTime = new Date(fromDate);
    nextTime.setSeconds(0);
    nextTime.setMilliseconds(0);
    
    // 最多嘗試 366 天（一年）
    for (let i = 0; i < 366 * 24 * 60; i++) {
        nextTime.setMinutes(nextTime.getMinutes() + 1);
        
        if (matchesCronExpression(nextTime, parsed)) {
            return nextTime;
        }
    }
    
    return null;
}

/**
 * 檢查時間是否符合 Cron 表達式
 * @param {Date} date - 要檢查的時間
 * @param {Object} parsed - 解析後的 Cron 物件
 * @returns {boolean} 是否符合
 */
function matchesCronExpression(date, parsed) {
    return matchesField(date.getMinutes(), parsed.minute, 0, 59) &&
           matchesField(date.getHours(), parsed.hour, 0, 23) &&
           matchesField(date.getDate(), parsed.dayOfMonth, 1, 31) &&
           matchesField(date.getMonth() + 1, parsed.month, 1, 12) &&
           matchesField(date.getDay(), parsed.dayOfWeek, 0, 6);
}

/**
 * 檢查值是否符合 Cron 欄位
 * @param {number} value - 要檢查的值
 * @param {string} field - Cron 欄位
 * @param {number} min - 最小值
 * @param {number} max - 最大值
 * @returns {boolean} 是否符合
 */
function matchesField(value, field, min, max) {
    if (field === '*') return true;
    
    if (/^\d+$/.test(field)) {
        return value === parseInt(field, 10);
    }
    
    if (/^\d+-\d+$/.test(field)) {
        const [start, end] = field.split('-').map(n => parseInt(n, 10));
        return value >= start && value <= end;
    }
    
    if (/^\d+(,\d+)+$/.test(field)) {
        const numbers = field.split(',').map(n => parseInt(n, 10));
        return numbers.includes(value);
    }
    
    if (/^\*\/\d+$/.test(field)) {
        const step = parseInt(field.split('/')[1], 10);
        return value % step === 0;
    }
    
    return false;
}

// ==================== 簡易排程格式 ====================

/**
 * 從簡易格式（週間+時間）轉換為 Cron 表達式
 * @param {Array<number>} daysOfWeek - 週間陣列 [0-6]，0=週日
 * @param {string} time - 時間字串 "HH:MM"
 * @returns {string} Cron 表達式
 */
function convertSimpleScheduleToCron(daysOfWeek, time) {
    if (!time || !time.match(/^\d{1,2}:\d{2}$/)) {
        throw new Error('時間格式錯誤，應為 HH:MM');
    }
    
    const [hour, minute] = time.split(':').map(n => parseInt(n, 10));
    
    if (hour < 0 || hour > 23 || minute < 0 || minute > 59) {
        throw new Error('時間超出範圍');
    }
    
    // 如果沒有選擇任何天，預設為每天
    let dayOfWeekStr = '*';
    if (daysOfWeek && daysOfWeek.length > 0) {
        dayOfWeekStr = daysOfWeek.sort((a, b) => a - b).join(',');
    }
    
    return `${minute} ${hour} * * ${dayOfWeekStr}`;
}

/**
 * 從 Cron 表達式轉換為簡易格式
 * @param {string} cronExpression - Cron 表達式
 * @returns {Object|null} { daysOfWeek: Array<number>, time: string } 或 null
 */
function convertCronToSimpleSchedule(cronExpression) {
    const parsed = parseCronExpression(cronExpression);
    if (!parsed) return null;
    
    // 只支援簡單的時間設定（* * * 格式）
    if (parsed.dayOfMonth !== '*' || parsed.month !== '*') {
        return null;
    }
    
    const hour = parsed.hour === '*' ? 0 : parseInt(parsed.hour, 10);
    const minute = parsed.minute === '*' ? 0 : parseInt(parsed.minute, 10);
    const time = `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
    
    let daysOfWeek = [];
    if (parsed.dayOfWeek === '*') {
        daysOfWeek = [0, 1, 2, 3, 4, 5, 6];
    } else if (/^\d+(,\d+)+$/.test(parsed.dayOfWeek) || /^\d+$/.test(parsed.dayOfWeek)) {
        daysOfWeek = parsed.dayOfWeek.split(',').map(n => parseInt(n, 10));
    } else if (/^\d+-\d+$/.test(parsed.dayOfWeek)) {
        const [start, end] = parsed.dayOfWeek.split('-').map(n => parseInt(n, 10));
        for (let i = start; i <= end; i++) {
            daysOfWeek.push(i);
        }
    }
    
    return { daysOfWeek, time };
}

// ==================== 排程管理 ====================

/**
 * 取得所有排程
 * @returns {Promise<Array>} 排程陣列
 */
async function getAllSchedules() {
    const result = await chrome.storage.local.get(STORAGE_KEYS.SCHEDULES);
    return result[STORAGE_KEYS.SCHEDULES] || [];
}

/**
 * 儲存排程
 * @param {Array} schedules - 排程陣列
 */
async function saveSchedules(schedules) {
    await chrome.storage.local.set({ [STORAGE_KEYS.SCHEDULES]: schedules });
}

/**
 * 新增排程
 * @param {Object} schedule - 排程物件 { name, cronExpression, enabled }
 * @returns {Promise<Object>} 新增的排程（含 id）
 */
async function addSchedule(schedule) {
    const schedules = await getAllSchedules();
    const newSchedule = {
        id: Date.now().toString(),
        name: schedule.name || '未命名排程',
        cronExpression: schedule.cronExpression,
        enabled: schedule.enabled !== false,
        createdAt: new Date().toISOString(),
        lastExecutedAt: null
    };
    
    schedules.push(newSchedule);
    await saveSchedules(schedules);
    
    // 通知 background script 更新排程
    chrome.runtime.sendMessage({ action: 'updateSchedules' });
    
    return newSchedule;
}

/**
 * 更新排程
 * @param {string} scheduleId - 排程 ID
 * @param {Object} updates - 要更新的欄位
 */
async function updateSchedule(scheduleId, updates) {
    const schedules = await getAllSchedules();
    const index = schedules.findIndex(s => s.id === scheduleId);
    
    if (index === -1) {
        throw new Error('找不到指定的排程');
    }
    
    schedules[index] = { ...schedules[index], ...updates };
    await saveSchedules(schedules);
    
    // 通知 background script 更新排程
    chrome.runtime.sendMessage({ action: 'updateSchedules' });
}

/**
 * 刪除排程
 * @param {string} scheduleId - 排程 ID
 */
async function deleteSchedule(scheduleId) {
    const schedules = await getAllSchedules();
    const filtered = schedules.filter(s => s.id !== scheduleId);
    await saveSchedules(filtered);
    
    // 通知 background script 更新排程
    chrome.runtime.sendMessage({ action: 'updateSchedules' });
}

/**
 * 取得排程啟用狀態
 * @returns {Promise<boolean>} 是否啟用
 */
async function isScheduleEnabled() {
    const result = await chrome.storage.local.get(STORAGE_KEYS.SCHEDULE_ENABLED);
    return result[STORAGE_KEYS.SCHEDULE_ENABLED] !== false; // 預設為 true
}

/**
 * 設定排程啟用狀態
 * @param {boolean} enabled - 是否啟用
 */
async function setScheduleEnabled(enabled) {
    await chrome.storage.local.set({ [STORAGE_KEYS.SCHEDULE_ENABLED]: enabled });
    
    // 通知 background script 更新排程
    chrome.runtime.sendMessage({ action: 'updateSchedules' });
}

// ==================== 執行歷史 ====================

/**
 * 取得執行歷史
 * @returns {Promise<Array>} 執行歷史陣列（最近的在前面）
 */
async function getExecutionHistory() {
    const result = await chrome.storage.local.get(STORAGE_KEYS.EXECUTION_HISTORY);
    return result[STORAGE_KEYS.EXECUTION_HISTORY] || [];
}

/**
 * 新增執行記錄
 * @param {Object} record - 執行記錄 { scheduleName, success, message, executedAt }
 */
async function addExecutionRecord(record) {
    const history = await getExecutionHistory();
    
    const newRecord = {
        id: Date.now().toString(),
        scheduleName: record.scheduleName || '未命名',
        success: record.success,
        message: record.message || '',
        executedAt: record.executedAt || new Date().toISOString()
    };
    
    // 新記錄放在最前面
    history.unshift(newRecord);
    
    // 只保留最近 N 筆
    const trimmed = history.slice(0, MAX_HISTORY_RECORDS);
    
    await chrome.storage.local.set({ [STORAGE_KEYS.EXECUTION_HISTORY]: trimmed });
}

/**
 * 清除執行歷史
 */
async function clearExecutionHistory() {
    await chrome.storage.local.set({ [STORAGE_KEYS.EXECUTION_HISTORY]: [] });
}

// ==================== 輔助函數 ====================

/**
 * 格式化時間為本地時間字串
 * @param {Date|string} date - 日期物件或 ISO 字串
 * @returns {string} 格式化的時間字串
 */
function formatLocalTime(date) {
    if (typeof date === 'string') {
        date = new Date(date);
    }
    
    if (!(date instanceof Date) || isNaN(date)) {
        return '-';
    }
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hour = String(date.getHours()).padStart(2, '0');
    const minute = String(date.getMinutes()).padStart(2, '0');
    const second = String(date.getSeconds()).padStart(2, '0');
    
    return `${year}-${month}-${day} ${hour}:${minute}:${second}`;
}

/**
 * 取得週間名稱
 * @param {number} dayOfWeek - 週間數字 (0=週日, 1=週一, ...)
 * @returns {string} 週間名稱
 */
function getDayOfWeekName(dayOfWeek) {
    const names = ['週日', '週一', '週二', '週三', '週四', '週五', '週六'];
    return names[dayOfWeek] || '未知';
}

/**
 * 取得 Cron 表達式的可讀描述
 * @param {string} cronExpression - Cron 表達式
 * @returns {string} 可讀描述
 */
function describeCronExpression(cronExpression) {
    const simple = convertCronToSimpleSchedule(cronExpression);
    
    if (simple) {
        const dayNames = simple.daysOfWeek.map(d => getDayOfWeekName(d));
        if (simple.daysOfWeek.length === 7) {
            return `每天 ${simple.time}`;
        } else {
            return `${dayNames.join('、')} ${simple.time}`;
        }
    }
    
    return cronExpression;
}
