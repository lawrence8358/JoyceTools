/**
 * config.js - 設定檔
 * 
 * 用途：管理所有靜態設定資料，包括：
 * - 預設 API URL
 * - 支援的地點清單和時區
 * - 地點對應的網址
 */

// 預設 API URL
const DEFAULT_API_URL = 'http://localhost:5000';

// 地點和時區映射
const locationTimezones = {
    'Sydney': 10.5, // GMT+11，因為澳洲有冬令和夏令時間，這邊直接取平均
    'Chennai': 5.5, // GMT+5:30 
    'IndianOcean': 6, // GMT+6 
    'Tokyo': 9 // GMT+9
};

// 地點和 URL 映射
const locationUrls = {
    'Sydney': 'https://zh.tideschart.com/Australia/New-South-Wales/Sydney',
    'Chennai': 'https://zh.tideschart.com/India/Tamil-Nadu/Chennai',
    'IndianOcean': 'https://zh.tideschart.com/British-Indian-Ocean-Territory',
    'Tokyo': 'https://zh.tideschart.com/Japan/Tokyo'
};
