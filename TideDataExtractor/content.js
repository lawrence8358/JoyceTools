// content.js - 在潮汐網站頁面中執行的腳本
// 負責從頁面中提取潮汐資料

// tideschart.com 新版潮汐表格的選擇器
const TIDE_TABLE_SELECTOR = 'table.fv-tide-table';

// 監聽來自 popup 的訊息
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === 'extractTideData') {
    try {
      const tideData = extractTideDataFromPage(request.location, request.timezone);
      sendResponse({ success: true, data: tideData });
    } catch (error) {
      console.error('提取錯誤:', error);
      sendResponse({ success: false, error: error.message, stack: error.stack });
    }
  } else if (request.action === 'checkTableExists') {
    // 檢查表格是否存在
    const tideTable = document.querySelector(TIDE_TABLE_SELECTOR);
    const exists = tideTable !== null && tideTable.querySelector('tbody time.fv-tide-time') !== null;
    console.log('檢查表格:', exists ? '已找到' : '未找到');
    sendResponse({ exists: exists });
  }
  return true; // 保持訊息通道開啟
});


/**
 * 從新版頁面中提取潮汐資料，並轉為舊版 API 輸出格式
 * @param {string} location - 地點名稱
 * @param {number} timezone - 時區偏移量
 * @returns {Array} 潮汐資料陣列
 */
function extractTideDataFromPage(location, timezone) {
  const tideData = [];
  
  // 查找新版潮汐表格
  const tideTable = document.querySelector(TIDE_TABLE_SELECTOR);
  
  if (!tideTable) {
    throw new Error('未找到潮汐資料表格（class="fv-tide-table"）。請確認頁面已完全載入。');
  }

  console.log('找到潮汐表格');
  
  // 找到 tbody 中的所有數據行
  const rows = tideTable.querySelectorAll('tbody tr');
  console.log(`找到 ${rows.length} 行資料`);
  
  if (rows.length === 0) {
    throw new Error('表格中沒有資料行。請確認頁面已完全載入。');
  }

  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const tideCells = getTideCells(row);
    
    if (tideCells.length === 0) {
      console.log(`第 ${i + 1} 行：未找到潮汐欄位，跳過`);
      continue;
    }

    try {
      const dateCell = row.querySelector('.fv-tide-day');
      const date = normalizeDateText(dateCell?.textContent || '');

      if (!date) {
        console.log(`第 ${i + 1} 行：未找到日期，跳過`);
        continue;
      }
      
      // 最多取得四個潮汐時間和高度
      const tides = [];
      for (const cell of tideCells.slice(0, 4)) {
        const timeElement = cell.querySelector('time.fv-tide-time');
        const timeText = timeElement?.getAttribute('datetime')?.trim() ||
          timeElement?.textContent?.trim() || '';

        const heightElement = cell.querySelector('.fv-tide-height');
        const tideType = getTideType(cell);
        const rawHeightText = heightElement?.textContent?.trim() || '';
        const heightText = formatTideValue(rawHeightText, tideType);
        
        if (timeText) {
          tides.push({
            time: timeText,
            value: heightText,
            type: tideType
          });
        }
      }
      
      if (tides.length > 0) {
        const dayData = {
          date: date,
          tide1: tides[0] || null,
          tide2: tides[1] || null,
          tide3: tides[2] || null,
          tide4: tides[3] || null
        };
        
        tideData.push(dayData);
        console.log(`第 ${i + 1} 行成功提取:`, date, `(${tides.length} 個潮汐)`);
      }
    } catch (error) {
      console.error(`第 ${i + 1} 行解析失敗:`, error);
    }
  }

  console.log(`提取完成：共 ${tideData.length} 天的資料`);

  if (tideData.length === 0) {
    throw new Error('未能提取到有效的潮汐資料。請確認頁面已載入並重試。');
  }

  return tideData;
}

/**
 * 取得資料列中的潮汐欄位，排除日期、日月出沒等欄位。
 * @param {HTMLTableRowElement} row
 * @returns {HTMLTableCellElement[]}
 */
function getTideCells(row) {
  return Array.from(row.querySelectorAll('td'))
    .filter(cell => cell.querySelector('time.fv-tide-time'));
}

/**
 * 根據新版圖示的 class 或 aria-label 判斷潮汐類型。
 * @param {HTMLTableCellElement} cell
 * @returns {string}
 */
function getTideType(cell) {
  if (cell.querySelector('.fv-tide-up, .icon-tide-up')) {
    return '高潮';
  }

  if (cell.querySelector('.fv-tide-down, .icon-tide-down')) {
    return '低潮';
  }

  const label = cell.querySelector('[aria-label]')?.getAttribute('aria-label')?.toLowerCase() || '';
  if (label.includes('高潮') || label.includes('high')) return '高潮';
  if (label.includes('低潮') || label.includes('low')) return '低潮';
  return '未知';
}

/**
 * 移除新版日期尾端的「日」，例如「周日 12日」轉為「周日 12」。
 * @param {string} dateText
 * @returns {string}
 */
function normalizeDateText(dateText) {
  return dateText.trim().replace(/(\d+)日$/, '$1');
}

/**
 * 將新版高度轉為舊版 API 格式：補上方向箭頭，並將低潮負值轉為絕對值。
 * @param {string} heightText
 * @param {string} tideType
 * @returns {string}
 */
function formatTideValue(heightText, tideType) {
  if (!heightText) return '';
  if (tideType !== '高潮' && tideType !== '低潮') return heightText.trim();

  let normalizedHeight = heightText.trim().replace(/^[▲▼]\s*/, '');
  if (tideType === '低潮') {
    normalizedHeight = normalizedHeight.replace(/^[-−]\s*/, '');
  }

  const directionSymbol = tideType === '高潮' ? '▲' : '▼';
  return `${directionSymbol} ${normalizedHeight}`;
}
