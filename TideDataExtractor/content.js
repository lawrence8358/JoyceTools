// content.js - 在潮汐網站頁面中執行的腳本
// 負責從頁面中提取潮汐資料

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
    const tideTable = document.querySelector('table.tidechart');
    const exists = tideTable !== null && tideTable.querySelectorAll('tbody tr').length > 0;
    console.log('檢查表格:', exists ? '已找到' : '未找到');
    sendResponse({ exists: exists });
  }
  return true; // 保持訊息通道開啟
});


/**
 * 從頁面中提取潮汐資料（新版：保留原始HTML格式）
 * @param {string} location - 地點名稱
 * @param {number} timezone - 時區偏移量
 * @returns {Array} 潮汐資料陣列
 */
function extractTideDataFromPage(location, timezone) {
  const tideData = [];
  
  // 查找潮汐表格（class="tidechart"）
  const tideTable = document.querySelector('table.tidechart');
  
  if (!tideTable) {
    throw new Error('未找到潮汐資料表格（class="tidechart"）。請確認頁面已完全載入。');
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
    const cells = row.querySelectorAll('td');
    
    if (cells.length < 5) {
      console.log(`第 ${i + 1} 行：列數不足 (${cells.length})，跳過`);
      continue;
    }

    try {
      // 第1列：日期（如「周五 26」）
      const dateCell = cells[0];
      const date = dateCell.textContent.trim();
      
      // 第2-5列：四個潮汐時間和高度
      const tides = [];
      for (let j = 1; j <= 4; j++) {
        const cell = cells[j];
        if (!cell) continue;
        
        // 獲取時間（直接文本節點）
        const timeText = cell.childNodes[0]?.textContent?.trim() || '';
        
        // 獲取高度（在 div > i 和文本中）
        const heightDiv = cell.querySelector('div');
        const heightText = heightDiv ? heightDiv.textContent.trim() : '';
        
        // 判斷潮汐類型（根據 class）
        const tideType = cell.classList.contains('tide-u') ? '高潮' : 
                        cell.classList.contains('tide-d') ? '低潮' : '未知';
        
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
 * 從文本中解析日期時間（舊版函數，新版不再使用）
 * @param {string} text - 日期時間文本
 * @returns {string|null} ISO 格式的日期時間字串
 * @deprecated
 */
function parseDateTimeFromText(text) {
  try {
    // 移除多餘的空格
    text = text.trim().replace(/\s+/g, ' ');
    
    console.log(`嘗試解析日期: "${text}"`);

    // 嘗試直接解析 ISO 格式或標準格式
    let date = new Date(text);
    if (!isNaN(date.getTime())) {
      console.log(`直接解析成功: ${date.toISOString()}`);
      return date.toISOString();
    }

    // 嘗試中文格式: "1月15日 14:30" 或 "12月25日 上午6:30"
    let chineseMatch = text.match(/(\d+)月(\d+)日\s+(?:上午|下午)?(\d+):(\d+)/);
    if (chineseMatch) {
      const now = new Date();
      const month = parseInt(chineseMatch[1]) - 1;
      const day = parseInt(chineseMatch[2]);
      let hour = parseInt(chineseMatch[3]);
      const minute = parseInt(chineseMatch[4]);
      
      // 處理上午/下午
      if (text.includes('下午') && hour < 12) {
        hour += 12;
      } else if (text.includes('上午') && hour === 12) {
        hour = 0;
      }
      
      date = new Date(now.getFullYear(), month, day, hour, minute);
      console.log(`中文格式解析成功: ${date.toISOString()}`);
      return date.toISOString();
    }

    // 嘗試 "週X X日 XX:XX" 格式
    chineseMatch = text.match(/[週周]\w+\s+(\d+)日?\s+(\d+):(\d+)/);
    if (chineseMatch) {
      const now = new Date();
      const day = parseInt(chineseMatch[1]);
      const hour = parseInt(chineseMatch[2]);
      const minute = parseInt(chineseMatch[3]);
      
      date = new Date(now.getFullYear(), now.getMonth(), day, hour, minute);
      console.log(`週X日格式解析成功: ${date.toISOString()}`);
      return date.toISOString();
    }

    // 嘗試 "DD/MM/YYYY HH:MM" 或 "MM/DD/YYYY HH:MM"
    const slashMatch = text.match(/(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2})/);
    if (slashMatch) {
      // 假設是 DD/MM/YYYY 格式（歐洲格式）
      const day = parseInt(slashMatch[1]);
      const month = parseInt(slashMatch[2]) - 1;
      const year = parseInt(slashMatch[3]);
      const hour = parseInt(slashMatch[4]);
      const minute = parseInt(slashMatch[5]);
      
      date = new Date(year, month, day, hour, minute);
      if (!isNaN(date.getTime())) {
        console.log(`斜線格式解析成功: ${date.toISOString()}`);
        return date.toISOString();
      }
    }

    // 嘗試 "YYYY-MM-DD HH:MM"
    const dashMatch = text.match(/(\d{4})-(\d{1,2})-(\d{1,2})\s+(\d{1,2}):(\d{2})/);
    if (dashMatch) {
      const year = parseInt(dashMatch[1]);
      const month = parseInt(dashMatch[2]) - 1;
      const day = parseInt(dashMatch[3]);
      const hour = parseInt(dashMatch[4]);
      const minute = parseInt(dashMatch[5]);
      
      date = new Date(year, month, day, hour, minute);
      console.log(`橫線格式解析成功: ${date.toISOString()}`);
      return date.toISOString();
    }

    // 嘗試只有時間 "HH:MM" 或 "H:MM AM/PM"
    const timeMatch = text.match(/(\d{1,2}):(\d{2})\s*(AM|PM|am|pm)?/);
    if (timeMatch) {
      const now = new Date();
      let hour = parseInt(timeMatch[1]);
      const minute = parseInt(timeMatch[2]);
      const ampm = timeMatch[3];
      
      if (ampm) {
        if (ampm.toLowerCase() === 'pm' && hour < 12) {
          hour += 12;
        } else if (ampm.toLowerCase() === 'am' && hour === 12) {
          hour = 0;
        }
      }
      
      date = new Date(now.getFullYear(), now.getMonth(), now.getDate(), hour, minute);
      console.log(`時間格式解析成功: ${date.toISOString()}`);
      return date.toISOString();
    }

    console.log('所有日期格式解析均失敗');
    return null;
  } catch (error) {
    console.error('解析日期失敗:', error);
    return null;
  }
}

/**
 * 判斷潮汐類型 (高潮/低潮)（舊版函數，新版不再使用）
 * @param {string} typeText - 類型文本
 * @param {number} height - 高度值
 * @returns {string} "High" 或 "Low"
 * @deprecated
 */
function determineType(typeText, height) {
  const lowerText = typeText.toLowerCase();
  
  if (lowerText.includes('high') || lowerText.includes('高') || lowerText.includes('滿')) {
    return 'High';
  }
  if (lowerText.includes('low') || lowerText.includes('低') || lowerText.includes('乾')) {
    return 'Low';
  }
  
  // 如果沒有明確標記，可以基於高度判斷（這個邏輯可能需要調整）
  return height > 1.5 ? 'High' : 'Low';
}
