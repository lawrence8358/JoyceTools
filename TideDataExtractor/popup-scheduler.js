/**
 * popup-scheduler.js - 排程設定頁面互動邏輯
 * 
 * 用途：處理排程設定頁面的 UI 互動，包括：
 * - 排程列表的顯示和管理
 * - 新增/編輯/刪除排程
 * - 切換簡易模式和 Cron 模式
 * - 執行歷史顯示
 * - 排程總開關
 */

// ==================== 全域變數 ====================

let editingScheduleId = null; // 正在編輯的排程 ID

// ==================== 初始化 ====================

document.addEventListener('DOMContentLoaded', async function () {
    // 確保 scheduler.js 已載入
    if (typeof getAllSchedules === 'undefined') {
        console.error('scheduler.js 未正確載入');
        return;
    }

    // 初始化排程設定頁面
    await initializeSchedulerUI();

    // 註冊事件監聽器
    registerSchedulerEventListeners();
});

/**
 * 初始化排程 UI
 */
async function initializeSchedulerUI() {
    try {
        // 載入排程總開關狀態
        const enabled = await isScheduleEnabled();
        document.getElementById('scheduleMainSwitch').checked = enabled;

        // 載入排程列表
        await refreshScheduleList();

        // 載入執行歷史
        await refreshExecutionHistory();
    } catch (error) {
        console.error('初始化排程 UI 失敗:', error);
    }
}

/**
 * 註冊排程相關事件監聽器
 */
function registerSchedulerEventListeners() {
    // 排程總開關
    document.getElementById('scheduleMainSwitch').addEventListener('change', handleMainSwitchChange);

    // 新增排程按鈕
    document.getElementById('addScheduleBtn').addEventListener('click', handleAddScheduleClick);

    // 儲存排程按鈕
    document.getElementById('saveScheduleBtn').addEventListener('click', handleSaveScheduleClick);

    // 取消按鈕
    document.getElementById('cancelScheduleBtn').addEventListener('click', handleCancelScheduleClick);

    // 清除歷史按鈕
    document.getElementById('clearHistoryBtn').addEventListener('click', handleClearHistoryClick);

    // 模式切換
    document.querySelectorAll('input[name="scheduleMode"]').forEach(radio => {
        radio.addEventListener('change', handleScheduleModeChange);
    });

    // Cron 表達式即時驗證
    document.getElementById('cronExpression').addEventListener('input', handleCronExpressionInput);
}

// ==================== 排程總開關 ====================

/**
 * 處理排程總開關變更
 */
async function handleMainSwitchChange(e) {
    const enabled = e.target.checked;
    
    try {
        await setScheduleEnabled(enabled);
        showStatus(enabled ? '✅ 自動排程已啟用' : '⏸️ 自動排程已停用', 'success');
    } catch (error) {
        console.error('切換排程狀態失敗:', error);
        showStatus('❌ 切換失敗: ' + error.message, 'error');
        // 復原開關狀態
        e.target.checked = !enabled;
    }
}

// ==================== 排程列表 ====================

/**
 * 重新整理排程列表
 */
async function refreshScheduleList() {
    try {
        const schedules = await getAllSchedules();
        const container = document.getElementById('scheduleList');

        if (schedules.length === 0) {
            container.innerHTML = '<div class="text-muted small text-center py-2">尚未設定任何排程</div>';
            return;
        }

        container.innerHTML = schedules.map(schedule => createScheduleItemHTML(schedule)).join('');

        // 為每個排程項目註冊事件
        schedules.forEach(schedule => {
            // 啟用/停用開關
            const toggleSwitch = document.getElementById(`schedule-toggle-${schedule.id}`);
            if (toggleSwitch) {
                toggleSwitch.addEventListener('change', (e) => handleScheduleToggle(schedule.id, e.target.checked));
            }

            // 編輯按鈕
            const editBtn = document.getElementById(`schedule-edit-${schedule.id}`);
            if (editBtn) {
                editBtn.addEventListener('click', () => handleEditScheduleClick(schedule.id));
            }

            // 刪除按鈕
            const deleteBtn = document.getElementById(`schedule-delete-${schedule.id}`);
            if (deleteBtn) {
                deleteBtn.addEventListener('click', () => handleDeleteScheduleClick(schedule.id));
            }

            // 測試按鈕
            const testBtn = document.getElementById(`schedule-test-${schedule.id}`);
            if (testBtn) {
                testBtn.addEventListener('click', () => handleTestScheduleClick(schedule.id));
            }
        });
    } catch (error) {
        console.error('載入排程列表失敗:', error);
    }
}

/**
 * 建立排程項目 HTML
 */
function createScheduleItemHTML(schedule) {
    const description = describeCronExpression(schedule.cronExpression);
    const nextTime = getNextCronExecution(schedule.cronExpression);
    const nextTimeStr = nextTime ? formatLocalTime(nextTime) : '無法計算';
    const lastExecutedStr = schedule.lastExecutedAt ? formatLocalTime(schedule.lastExecutedAt) : '尚未執行';

    return `
        <div class="schedule-item">
            <div class="schedule-header">
                <div class="schedule-info">
                    <strong>${escapeHtml(schedule.name)}</strong>
                    <div class="small text-muted">${escapeHtml(description)}</div>
                </div>
                <div class="form-check form-switch">
                    <input class="form-check-input" type="checkbox" id="schedule-toggle-${schedule.id}" ${schedule.enabled ? 'checked' : ''}>
                </div>
            </div>
            <div class="schedule-details">
                <div class="small text-muted">
                    <div>📅 下次執行: ${escapeHtml(nextTimeStr)}</div>
                    <div>🕐 最後執行: ${escapeHtml(lastExecutedStr)}</div>
                </div>
            </div>
            <div class="schedule-actions">
                <button id="schedule-edit-${schedule.id}" class="btn btn-sm btn-aux">編輯</button>
                <button id="schedule-test-${schedule.id}" class="btn btn-sm btn-aux">測試</button>
                <button id="schedule-delete-${schedule.id}" class="btn btn-sm btn-aux">刪除</button>
            </div>
        </div>
    `;
}

/**
 * 處理排程啟用/停用切換
 */
async function handleScheduleToggle(scheduleId, enabled) {
    try {
        await updateSchedule(scheduleId, { enabled: enabled });
        showStatus(enabled ? '✅ 排程已啟用' : '⏸️ 排程已停用', 'success');
        await refreshScheduleList();
    } catch (error) {
        console.error('切換排程狀態失敗:', error);
        showStatus('❌ 切換失敗: ' + error.message, 'error');
        await refreshScheduleList(); // 復原 UI
    }
}

/**
 * 處理編輯排程
 */
async function handleEditScheduleClick(scheduleId) {
    try {
        const schedules = await getAllSchedules();
        const schedule = schedules.find(s => s.id === scheduleId);

        if (!schedule) {
            showStatus('❌ 找不到排程', 'error');
            return;
        }

        editingScheduleId = scheduleId;

        // 填入表單
        document.getElementById('scheduleName').value = schedule.name;

        // 嘗試轉換為簡易格式
        const simple = convertCronToSimpleSchedule(schedule.cronExpression);

        if (simple) {
            // 使用簡易模式
            document.getElementById('modeSimple').checked = true;
            document.getElementById('scheduleTime').value = simple.time;

            // 清除所有日期勾選
            for (let i = 0; i <= 6; i++) {
                document.getElementById(`day${i}`).checked = false;
            }

            // 勾選對應的日期
            simple.daysOfWeek.forEach(day => {
                document.getElementById(`day${day}`).checked = true;
            });

            handleScheduleModeChange({ target: { value: 'simple' } });
        } else {
            // 使用 Cron 模式
            document.getElementById('modeCron').checked = true;
            document.getElementById('cronExpression').value = schedule.cronExpression;
            handleScheduleModeChange({ target: { value: 'cron' } });
            handleCronExpressionInput({ target: { value: schedule.cronExpression } });
        }

        // 顯示表單
        document.getElementById('scheduleForm').style.display = 'block';
        document.getElementById('scheduleName').focus();

    } catch (error) {
        console.error('載入排程資料失敗:', error);
        showStatus('❌ 載入失敗: ' + error.message, 'error');
    }
}

/**
 * 處理刪除排程
 */
async function handleDeleteScheduleClick(scheduleId) {
    const confirmed = confirm('確定要刪除此排程嗎？');

    if (!confirmed) {
        return;
    }

    try {
        await deleteSchedule(scheduleId);
        showStatus('✅ 排程已刪除', 'success');
        await refreshScheduleList();
    } catch (error) {
        console.error('刪除排程失敗:', error);
        showStatus('❌ 刪除失敗: ' + error.message, 'error');
    }
}

/**
 * 處理測試排程
 */
async function handleTestScheduleClick(scheduleId) {
    const confirmed = confirm('確定要立即執行此排程嗎？這將會開始提取資料並上傳。');

    if (!confirmed) {
        return;
    }

    showStatus('⏳ 正在執行排程任務...', 'info');

    try {
        const response = await chrome.runtime.sendMessage({
            action: 'testSchedule',
            scheduleId: scheduleId
        });

        if (response && response.success) {
            showStatus('✅ 排程執行完成', 'success');
            await refreshExecutionHistory();
        } else {
            showStatus('❌ 執行失敗: ' + (response?.error || '未知錯誤'), 'error');
        }
    } catch (error) {
        console.error('測試排程失敗:', error);
        showStatus('❌ 執行失敗: ' + error.message, 'error');
    }
}

// ==================== 新增/編輯排程 ====================

/**
 * 處理新增排程按鈕點擊
 */
function handleAddScheduleClick() {
    editingScheduleId = null;

    // 重置表單
    document.getElementById('scheduleName').value = '';
    document.getElementById('modeSimple').checked = true;
    document.getElementById('scheduleTime').value = '09:00';

    // 預設勾選週一到週五
    for (let i = 0; i <= 6; i++) {
        document.getElementById(`day${i}`).checked = (i >= 1 && i <= 5);
    }

    document.getElementById('cronExpression').value = '';
    document.getElementById('cronDescription').textContent = '';

    handleScheduleModeChange({ target: { value: 'simple' } });

    // 顯示表單
    document.getElementById('scheduleForm').style.display = 'block';
    document.getElementById('scheduleName').focus();
}

/**
 * 處理儲存排程按鈕點擊
 */
async function handleSaveScheduleClick() {
    try {
        // 取得表單資料
        const name = document.getElementById('scheduleName').value.trim();

        if (!name) {
            showStatus('❌ 請輸入排程名稱', 'error');
            return;
        }

        // 取得 Cron 表達式
        let cronExpression;
        const mode = document.querySelector('input[name="scheduleMode"]:checked').value;

        if (mode === 'simple') {
            // 簡易模式
            const time = document.getElementById('scheduleTime').value;
            const daysOfWeek = [];

            for (let i = 0; i <= 6; i++) {
                if (document.getElementById(`day${i}`).checked) {
                    daysOfWeek.push(i);
                }
            }

            if (daysOfWeek.length === 0) {
                showStatus('❌ 請至少選擇一天', 'error');
                return;
            }

            cronExpression = convertSimpleScheduleToCron(daysOfWeek, time);
        } else {
            // Cron 模式
            cronExpression = document.getElementById('cronExpression').value.trim();

            if (!validateCronExpression(cronExpression)) {
                showStatus('❌ Cron 表達式格式錯誤', 'error');
                return;
            }
        }

        // 新增或更新排程
        if (editingScheduleId) {
            await updateSchedule(editingScheduleId, {
                name: name,
                cronExpression: cronExpression
            });
            showStatus('✅ 排程已更新', 'success');
        } else {
            await addSchedule({
                name: name,
                cronExpression: cronExpression,
                enabled: true
            });
            showStatus('✅ 排程已新增', 'success');
        }

        // 隱藏表單
        document.getElementById('scheduleForm').style.display = 'none';
        editingScheduleId = null;

        // 重新整理列表
        await refreshScheduleList();

    } catch (error) {
        console.error('儲存排程失敗:', error);
        showStatus('❌ 儲存失敗: ' + error.message, 'error');
    }
}

/**
 * 處理取消按鈕點擊
 */
function handleCancelScheduleClick() {
    document.getElementById('scheduleForm').style.display = 'none';
    editingScheduleId = null;
}

/**
 * 處理排程模式切換
 */
function handleScheduleModeChange(e) {
    const mode = e.target.value;

    if (mode === 'simple') {
        document.getElementById('simpleModePanel').style.display = 'block';
        document.getElementById('cronModePanel').style.display = 'none';
    } else {
        document.getElementById('simpleModePanel').style.display = 'none';
        document.getElementById('cronModePanel').style.display = 'block';
    }
}

/**
 * 處理 Cron 表達式輸入
 */
function handleCronExpressionInput(e) {
    const cronExpression = e.target.value.trim();
    const descElement = document.getElementById('cronDescription');

    if (!cronExpression) {
        descElement.textContent = '';
        return;
    }

    if (validateCronExpression(cronExpression)) {
        const description = describeCronExpression(cronExpression);
        const nextTime = getNextCronExecution(cronExpression);
        const nextTimeStr = nextTime ? formatLocalTime(nextTime) : '無法計算';

        descElement.innerHTML = `✅ ${escapeHtml(description)}<br>下次執行: ${escapeHtml(nextTimeStr)}`;
        descElement.className = 'small text-success mb-2';
    } else {
        descElement.textContent = '❌ 格式錯誤';
        descElement.className = 'small text-danger mb-2';
    }
}

// ==================== 執行歷史 ====================

/**
 * 重新整理執行歷史
 */
async function refreshExecutionHistory() {
    try {
        const history = await getExecutionHistory();
        const container = document.getElementById('executionHistory');

        if (history.length === 0) {
            container.innerHTML = '<div class="text-muted small text-center py-2">尚無執行記錄</div>';
            return;
        }

        container.innerHTML = history.map(record => createHistoryItemHTML(record)).join('');
    } catch (error) {
        console.error('載入執行歷史失敗:', error);
    }
}

/**
 * 建立執行歷史項目 HTML
 */
function createHistoryItemHTML(record) {
    const icon = record.success ? '✅' : '❌';
    const statusClass = record.success ? 'text-success' : 'text-danger';
    const timeStr = formatLocalTime(record.executedAt);

    return `
        <div class="history-item">
            <div class="d-flex justify-content-between align-items-start">
                <div class="flex-fill">
                    <div><strong>${icon} ${escapeHtml(record.scheduleName)}</strong></div>
                    <div class="small ${statusClass}">${escapeHtml(record.message)}</div>
                    <div class="small text-muted">🕐 ${escapeHtml(timeStr)}</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * 處理清除歷史按鈕點擊
 */
async function handleClearHistoryClick() {
    const confirmed = confirm('確定要清除所有執行記錄嗎？');

    if (!confirmed) {
        return;
    }

    try {
        await clearExecutionHistory();
        showStatus('✅ 執行記錄已清除', 'success');
        await refreshExecutionHistory();
    } catch (error) {
        console.error('清除執行記錄失敗:', error);
        showStatus('❌ 清除失敗: ' + error.message, 'error');
    }
}

// ==================== 輔助函數 ====================

/**
 * HTML 跳脫
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
