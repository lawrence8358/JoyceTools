# 潮汐資料提取器 Chrome 擴充功能

## 📋 專案概述

由於潮汐資料網站 (zh.tideschart.com) 啟用了 Cloudflare 防爬蟲驗證機制，原有的自動爬蟲方案失效。本專案提供了一個 Chrome 擴充功能解決方案，允許使用者手動訪問網站並提取潮汐資料，然後透過 **直接 API 上傳** 或 JSON 檔案匯入到資料庫中。

## 🎯 功能特點

- ✅ 繞過 Cloudflare 人機驗證
- ✅ 友好的圖形介面操作
- ✅ 自動識別當前訪問的地點
- ✅ 智慧表格識別與資料提取
- ✅ **直接上傳到遠端資料庫** (v1.5.0 新功能)
- ✅ **API 網址可自訂設定** (v1.5.0 新功能)
- ✅ **擷取狀態自動保留** (v1.5.0 新功能)
- ✅ **背景自動排程執行** (v1.7.0 新功能)
- ✅ **支援 Cron 表達式和簡易模式** (v1.7.0 新功能)
- ✅ **執行歷史記錄追蹤** (v1.7.0 新功能)
 - ✅ **直接上傳到遠端資料庫**
 - ✅ **API 網址可自訂設定**
 - ✅ **擷取狀態自動保留**
 - ✅ **背景自動排程執行**
 - ✅ **支援 Cron 表達式和簡易模式**
 - ✅ **執行歷史記錄追蹤**
- ✅ 一鍵匯出標準 JSON 格式
- ✅ 批次匯入資料庫
- ✅ 基本錯誤提示與日誌

## 📁 專案結構

```
TideDataExtractor/             # Chrome 擴充功能專案
├── manifest.json              # 擴充功能設定檔（Manifest V3）
├── popup.html                 # 擴充功能彈出介面
├── popup.css                  # 擴充功能樣式表
├── popup.js                   # 主程式入口（事件協調）
├── popup-scheduler.js         # 排程設定頁面互動邏輯
├── config.js                  # 設定管理（預設 API、地點清單）
├── storage.js                 # Chrome Storage 管理（狀態持久化）
├── extractor.js               # 資料提取邏輯（單一/批次提取）
├── api.js                     # API 通訊模組
├── ui.js                      # UI 互動管理
├── scheduler.js               # 排程管理核心模組（Cron 解析、排程 CRUD）
├── background.js              # 背景服務腳本（Alarms 管理、自動執行）
├── content.js                 # 頁面內容提取腳本（注入頁面）
├── cors-test.html             # CORS 測試工具（獨立網頁）
├── Example/                   # 範例 JSON 檔案
│   └── tide_data_all_20251225.json
└── icons/                     # 擴充功能圖示
    ├── icon16.png
    ├── icon48.png
    └── icon128.png

WebApp/
├── Controllers/
│   └── TideController.cs      # 潮汐 API 控制器（含 Import 端點）
├── Models/
│   ├── TideModel.cs           # 查詢模型
│   └── TideImportModel.cs     # 匯入模型
└── Program.cs                 # CORS 設定（僅允許 Chrome 擴充和 localhost）


UnitTest/
├── TideImportApiTest.cs       # API 匯入單元測試
├── CorsTest.cs                # CORS 政策單元測試
└── TestData/
    └── tide_data_test.json    # 測試用 JSON 資料
```

## 🚀 快速開始

### 步驟 1：安裝 Chrome 擴充功能

1. 開啟 Chrome 瀏覽器
2. 訪問 `chrome://extensions/`
3. 開啟右上角的「開發人員模式」
4. 點擊「載入未封裝項目」
5. 選擇 `TideDataExtractor` 資料夾
6. 擴充功能安裝成功後會顯示在工具列

**注意**：如果看到 CSP 錯誤，請確認 TideDataExtractor/vendor 目錄中已放置必要的第三方資源（Bootstrap、Font Awesome）。

### 步驟 3：啟動 WebApp API 服務（如需直接上傳）

```bash
cd WebApp
dotnet run
```

預設會在 `http://localhost:5000` 啟動服務

### 步驟 3：提取並上傳潮汐資料

#### 方法 A：一次提取全部地點並直接上傳（推薦）

1. 點擊擴充功能圖示
2. 確認 API 網址設定正確（預設: `http://localhost:5000`）
3. 點擊「📦 一次提取全部地點」按鈕
4. 系統會自動：
   - 開啟 Sydney、Chennai、IndianOcean、Tokyo 四個網站
   - 依序提取每個地點的資料
   - 顯示進度（約需 30-40 秒）
5. 完成後點擊「⬆️ 上傳到資料庫」直接寫入遠端資料庫
   - 或點擊「⬇️ 下載 JSON 檔案」保存為本地檔案

> **提示**: 擷取的資料會自動保存，即使關閉 popup 視窗也不會遺失（2 小時內有效）

#### 方法 B：手動提取單一地點

##### 支援的地點
| 地點 | 時區 | URL |
|------|------|-----|
| Sydney (雪梨) | UTC+10:30 | https://zh.tideschart.com/Australia/New-South-Wales/Sydney |
| Chennai (清奈) | UTC+5:30 | https://zh.tideschart.com/India/Tamil-Nadu/Chennai |
| Indian Ocean (印度洋) | UTC+6 | https://zh.tideschart.com/British-Indian-Ocean-Territory |
| Tokyo (東京) | UTC+9 | https://zh.tideschart.com/Japan/Tokyo |

##### 操作步驟
1. 點擊擴充功能圖示
2. 選擇目標地點（或使用「🔗 前往網站」按鈕直接跳轉）
3. 等待頁面完全載入（完成 Cloudflare 驗證）
4. 點擊「📄 提取當前頁面資料」按鈕
5. 點擊「⬆️ 上傳到資料庫」或「⬇️ 下載 JSON 檔案」

### 步驟 5：設定自動排程（可選）

#### 🔧 排程功能說明

本擴充功能支援背景自動排程，可設定定期自動執行「提取全部地點 + 上傳到資料庫」流程，無需手動操作。

#### 📅 排程設定方式

1. 點擊擴充功能圖示，切換到「⚙️ 設定」分頁
2. 開啟「啟用自動排程」開關
3. 點擊「+ 新增排程」按鈕

**模式一：簡易模式（推薦）**
- 選擇執行時間（如 09:00）
- 勾選執行日期（週一到週日）
- 範例：每週一至週五早上 9:00 執行

**模式二：Cron 模式（進階）**
- 支援標準 Cron 表達式（5 欄位格式）
- 格式：`分 時 日 月 週`
- 範例：
  - `0 9 * * 1-5` = 週一至週五 9:00
  - `0 */6 * * *` = 每 6 小時執行一次
  - `30 8 * * 0,6` = 每週六、日 8:30

4. 輸入排程名稱（如「每日定時提取」）
5. 點擊「儲存」

#### 🎛️ 排程管理

- **啟用/停用個別排程**：使用排程列表中的開關
- **編輯排程**：點擊「編輯」按鈕修改設定
- **刪除排程**：點擊「刪除」按鈕移除排程
- **測試排程**：點擊「測試」按鈕立即執行一次（不影響排程時間）
- **查看執行記錄**：在「最近執行記錄」區域查看最近 5 次執行結果

#### ⚠️ 重要提示

- **API 服務必須持續運行**：排程執行時會連線到設定的 API 網址上傳資料
- **Chrome 必須保持運行**：背景排程需要瀏覽器在運作中
- **網路連線**：確保執行時有穩定的網路連線
- **時間基準**：所有時間皆使用**使用者本地時間**
- **多筆排程**：可設定多個不同時間和條件的排程

#### 📊 排程執行流程

```
背景服務 (background.js)
  ↓ 時間到達 (Chrome Alarms API)
  ↓ 自動開啟 4 個地點分頁
  ↓ 依序提取資料 (8秒等待 + 智能表格檢測)
  ↓ 關閉分頁
  ↓ 上傳到 API
  ↓ 記錄執行結果
  ↓ 設定下次執行時間
```



### POST /api/Tide/Import

從 Chrome 擴充功能匯入潮汐資料

#### CORS 政策
WebApp 的 CORS 政策已設定為僅允許：
- **Chrome 擴充功能**: `chrome-extension://`
- **本地開發**: `http://localhost`, `https://localhost`

**生產環境部署時請務必修改** `WebApp/Program.cs` 中的 CORS 設定：

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChromeExtensionPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
            origin.StartsWith("chrome-extension://") ||    // Chrome 擴充功能
            origin.StartsWith("http://localhost") ||       // 本地測試
            origin.StartsWith("https://localhost") ||
            origin.StartsWith("https://your-domain.com")   // 加入您的網域
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});
```

#### CORS 測試工具
使用 `TideDataExtractor/cors-test.html` 測試 CORS 設定：
1. 啟動 WebApp 服務
2. 在瀏覽器中開啟 `cors-test.html`
3. 輸入 API 網址（預設 `http://localhost:5000`）
4. 點擊測試按鈕驗證 CORS 是否正確設定

**請求格式:**
```json
{
  "extractTime": "2025-12-25T13:30:00.000Z",
  "locations": [
    {
      "name": "Sydney",
      "timezone": 10.5,
      "url": "https://zh.tideschart.com/...",
      "data": [
        {
          "date": "周五 26",
          "tide1": { "time": "01:18", "value": "▲ 1.34 米", "type": "高潮" },
          "tide2": { "time": "06:49", "value": "▼ 0.65 米", "type": "低潮" },
          "tide3": { "time": "13:10", "value": "▲ 1.66 米", "type": "高潮" },
          "tide4": { "time": "19:55", "value": "▼ 0.44 米", "type": "低潮" }
        }
      ]
    }
  ]
}
```

**回應格式:**
```json
{
  "success": true,
  "importedCount": 7,
  "message": "匯入完成，共 7 筆資料",
  "details": [
    "✅ Sydney: 匯入 7 筆",
    "✅ Tokyo: 匯入 7 筆"
  ]
}
```

### POST /api/Tide/Query

查詢已儲存的潮汐資料

**請求格式:**
```json
{
  "sdate": "2025-12-01T00:00:00Z",
  "edate": "2025-12-31T23:59:59Z"
}
```

## 📊 資料格式

### JSON 檔案結構

擴充功能會保留 HTML 原始格式，讓後端處理更靈活：

```json
{
  "extractTime": "2025-12-25T13:30:00.000Z",
  "locations": [
    {
      "name": "Sydney",
      "timezone": 10.5,
      "url": "https://zh.tideschart.com/Australia/New-South-Wales/Sydney",
      "data": [
        {
          "date": "周五 26",
          "tide1": {
            "time": "01:18",
            "value": "▲ 1.34 米",
            "type": "高潮"
          },
          "tide2": {
            "time": "06:49",
            "value": "▼ 0.65 米",
            "type": "低潮"
          },
          "tide3": {
            "time": "13:10",
            "value": "▲ 1.66 米",
            "type": "高潮"
          },
          "tide4": {
            "time": "19:55",
            "value": "▼ 0.44 米",
            "type": "低潮"
          }
        }
      ]
    }
  ]
}
```

**資料說明：**
| 欄位 | 說明 |
|------|------|
| `extractTime` | 提取時間戳 (ISO 格式) |
| `locations[]` | 可包含多個地點的資料 |
| `name` | 地點名稱（Sydney, Chennai, IndianOcean, Tokyo） |
| `timezone` | 時區偏移量 |
| `url` | 來源網址 |
| `data[]` | 該地點的每日潮汐資料 |
| `date` | 日期（保留原始格式，如"周五 26"） |
| `tide1-4` | 四個潮汐時段 |
| `time` | 時間（如"01:18"） |
| `value` | 高度（保留原始格式，如"▲ 1.34 米"） |
| `type` | 潮汐類型（"高潮"或"低潮"） |

### 資料庫結構 (TideEntity)

```csharp
public class TideEntity
{
    // 主鍵: Date + Location
    public DateTime Date { get; set; }
    public string Location { get; set; }
    
    // 第一至第四潮汐資料
    public decimal? FirstTideHeight { get; set; }   // 漲潮為正，退潮為負
    public DateTime? FirstTideTime { get; set; }
    public decimal? SecondTideHeight { get; set; }
    public DateTime? SecondTideTime { get; set; }
    public decimal? ThirdTideHeight { get; set; }
    public DateTime? ThirdTideTime { get; set; }
    public decimal? FourthTideHeight { get; set; }
    public DateTime? FourthTideTime { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

## 🐛 疑難排解

### 問題 1: "Could not establish connection"

**原因**: 擴充功能在頁面開啟後才安裝/更新，content script 未注入

**解決方案**:
1. 點擊「🔄 重新整理頁面」按鈕（或按 F5）
2. 等待頁面完全載入
3. 重新點擊擴充功能圖示
4. 再次嘗試提取資料

### 問題 2: "未能提取到有效的潮汐資料"

**原因**: 頁面尚未完全載入或表格格式變化

**解決方案**:
1. 等待 8-10 秒讓頁面完全載入
2. 確認找到的表格數量和內容，或使用瀏覽器主控台檢查錯誤
3. 確認找到的表格數量和內容
4. 按 F12 開啟開發者工具，查看主控台訊息

### 問題 3: API 上傳失敗

**檢查項目**:
1. 確認 WebApp 服務已啟動
2. 確認 API 網址設定正確
3. 檢查網路連線
4. 查看瀏覽器開發者工具 Network 分頁

**CORS 設定**: WebApp 已設定允許跨域請求，若仍有問題請確認 Program.cs 中的 CORS 設定

### 問題 4: 擷取中途關閉 popup

**說明**: 系統會自動保存擷取狀態,重新開啟 popup 時會提示恢復之前的資料（2 小時內有效）

### 問題 5: 排程沒有執行

**可能原因**:
1. Chrome 瀏覽器已關閉
2. 排程總開關未啟用
3. 個別排程被停用
4. API 服務未運行

**解決方案**:
1. 確保 Chrome 保持運行（可最小化）
2. 檢查「設定」頁面中的「啟用自動排程」開關
3. 檢查排程列表中各排程的啟用狀態
4. 確認 API 服務正常運作
5. 查看「最近執行記錄」了解失敗原因

### 問題 6: 排程執行失敗

**檢查項目**:
1. 查看執行記錄中的錯誤訊息
2. 確認 API 網址設定正確
3. 測試 API 連線（使用 cors-test.html）
4. 檢查網路連線狀態
5. 確認目標網站可正常訪問

**常見錯誤**:
- "提取失敗" → 網站連線問題或 Cloudflare 阻擋
- "上傳失敗" → API 服務問題或網路連線中斷
- "等待超時" → 網頁載入過慢，可能需要手動執行一次確認



### 自訂 API 網址

1. 開啟擴充功能 popup
2. 在「API 網址」輸入框中輸入您的 API 位址
3. 設定會自動保存到 chrome.storage

### 批次處理舊 JSON 檔案

```csharp
// 使用 TideJsonImportHelper 匯入本地 JSON 檔案
var helper = new TideJsonImportHelper(dbContext);
var importedCount = await helper.ImportFromFolder(
    Path.Combine(AppContext.BaseDirectory, "AppData", "TideData")
);
Console.WriteLine($"批次匯入完成，共 {importedCount} 筆資料");
```

## 📝 技術細節

### Chrome 擴充功能架構

- **Manifest Version**: 3
- **Permissions**: `activeTab`, `downloads`, `storage`
- **Content Scripts**: 在 zh.tideschart.com 上自動注入
- **Message Passing**: popup.js ↔ content.js 通訊
- **Storage**: 使用 chrome.storage.local 保存狀態（2 小時有效期）

#### 模組化架構 (v1.6.0)

為提高程式碼可維護性，popup.js 已重構為模組化架構：

| 模組 | 行數 | 職責 |
|------|------|------|
| **config.js** | ~30 | 集中管理設定（API 網址、地點資訊） |
| **storage.js** | ~70 | Chrome Storage 操作（狀態保存/恢復） |
| **extractor.js** | ~160 | 核心提取邏輯（單一/批次提取） |
| **api.js** | ~30 | API 通訊（POST 請求處理） |
| **ui.js** | ~100 | UI 互動（狀態顯示、下載、除錯） |
| **popup.js** | ~260 | 主程式入口（事件協調） |

**模組依賴順序**: config → storage → ui → api → extractor → popup

#### 排程架構 (v1.7.0)

新增排程功能模組：

| 模組 | 行數 | 職責 |
|------|------|------|
| **scheduler.js** | ~550 | 排程核心（Cron 解析、排程 CRUD、執行歷史） |
| **background.js** | ~450 | 背景服務（Alarms 管理、自動執行流程） |
| **popup-scheduler.js** | ~450 | 排程 UI 互動（表單處理、列表顯示） |

**排程模組依賴**: scheduler → background, popup-scheduler

**載入方式** (`popup.html`):
```html
<script src="config.js"></script>
<script src="storage.js"></script>
<script src="ui.js"></script>
<script src="api.js"></script>
<script src="extractor.js"></script>
<script src="scheduler.js"></script>
<script src="popup.js"></script>
<script src="popup-scheduler.js"></script>
```

### 資料處理流程

```
使用者訪問網站 
→ Cloudflare 驗證（手動完成）
→ 頁面載入完成
→ content.js 注入
→ 提取表格資料（保留原始格式）
→ 組裝 JSON
→ 保存到 chrome.storage.local（2 小時有效）
→ 選擇上傳方式:
    A. API 上傳 → POST /api/Tide/Import → CORS 驗證 → 儲存到 SQLite
    B. 下載 JSON → 手動匯入 → TideJsonImportHelper → SQLite
```

### 智能等待機制

批次提取時使用智能等待：
1. 基本等待 8 秒（Cloudflare 驗證時間）
2. 每 2 秒檢查一次表格是否載入
3. 最多重試 5 次
4. 總計最長等待 18 秒

### 狀態持久化

使用 `chrome.storage.local` 保存：
- 已提取的資料
- 自訂 API 網址
- 批次提取進度
- 排程設定和執行歷史

**過期機制**: 提取資料超過 2 小時自動清除

### 排程機制

**Chrome Alarms API**: 用於設定精確的定時任務
- 支援 Cron 表達式解析（5 欄位格式）
- 自動計算下次執行時間
- 任務完成後自動設定下次執行

**背景執行流程**:
1. Service Worker 監聽 alarm 事件
2. 觸發時開啟分頁進行提取
3. 提取完成後上傳 API
4. 記錄執行結果到 storage
5. 計算並設定下次執行時間

**Cron 表達式範例**:
| 表達式 | 說明 |
|--------|------|
| `0 9 * * *` | 每天 9:00 |
| `0 9 * * 1-5` | 週一至週五 9:00 |
| `0 */6 * * *` | 每 6 小時 |
| `30 8,20 * * *` | 每天 8:30 和 20:30 |
| `0 0 1 * *` | 每月 1 號 0:00 |



### 執行所有測試
```bash
cd UnitTest
dotnet test
```

### 執行特定測試套件

#### 潮汐匯入測試
```bash
dotnet test --filter "FullyQualifiedName~TideImportApiTest"
```

測試項目：
- ✅ JSON 模型解析
- ✅ 時區設定正確性
- ✅ 潮汐資料解析
- ✅ API Import 功能
- ✅ 資料庫寫入
- ✅ 潮高正負號處理
- ✅ 多地點匯入
- ✅ 重複資料更新

#### CORS 政策測試
```bash
dotnet test --filter "FullyQualifiedName~CorsTest"
```

測試項目：
- ✅ Chrome 擴充功能來源 (`chrome-extension://`)
- ✅ Localhost 來源 (`http://localhost`)

**注意**: CORS 測試需要 WebApp 服務運行中，否則測試會跳過

## 📜 版本歷史

| 版本 | 日期 | 更新內容 |
|------|------|----------|
| v1.0.0 | 2024-01 | 初始版本，支援 4 個地點的資料提取 |
| v1.1.0 | 2024-01 | 新增連線錯誤自動重新整理提示 |
| v1.2.0 | 2024-01 | 新增除錯功能和智慧表格識別 |
| v1.3.0 | 2024-01 | 轉換為繁體中文，優化 UI 一致性 |
| v1.4.0 | 2024-01 | 新增當前頁面跳轉功能，整合文件 |
| v1.5.0 | 2024-12 | **新增 API 直接上傳功能、狀態保留、API 網址設定** |
| v1.6.0 | 2025-01 | **重構為模組化架構、縮小 CORS 範圍、新增單元測試** |
| v1.7.0 | 2025-12 | **新增背景自動排程功能、支援 Cron 和簡易模式、執行歷史記錄** |

## 📧 支援與回饋

如遇到問題或有改進建議，請提供：
1. 錯誤訊息截圖
2. 瀏覽器主控台訊息 (F12)
3. 目標網站 URL

## 📄 授權

本專案僅供學習和個人使用。請勿用於商業用途或大量爬取資料。
