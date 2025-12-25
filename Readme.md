# 小工具說明 
這個專案是老婆大人說要研究氣候 & 地震資訊的查詢小工具，目前僅支援功能如下
+ `各地雨量資訊查詢`
+ `台灣微地動查詢`
+ `潮汐查詢`


---

---
## 專案資料夾結構
```
|_ DataSyncConsoleTools  => 各種同步排程工具
    |_ EarthquakeSyncService.cs => 地震資料同步服務
    |_ TideSyncService.cs => 潮汐資料同步服務
|_ Lib       => 共用類別
|_ WebApp   => API 站台
   |_ wwwroot
      |_ index.html => 小工具首頁
      |_ rain => 各地雨量資訊查詢小工具
      |  |_ js
      |     |_ env.js => mapbox 的 access token
      |_ earthquake => 台灣微地動查詢小工具
```



--- 
## 各地雨量資訊查詢
預設顯示幾個主要的地點，也可根據實際的需求點選地圖上的位置，查詢該地點最近三日的雨量資訊。

---
### 相關技術
1. 地圖：[MapBox](https://www.mapbox.com/)
2. 氣象 API：[Open-Meteo.com](https://open-meteo.com/)

---
### 使用方式
此專案不需要依賴後端程式，可以直接執行 `WebApp/wwwroot/rain` 資料夾內的 `index.html` 檔案即可。<br><br>
如果你想要使用這個小工具，請先至 MapBox 註冊帳號，並取得 Access Token 後，到 `WebApp/wwwroot/rain/js/env.js` 中修改以下程式碼：
``` js
const mapAccessToken = 'YOUR_MAPBOX_ACCESS_TOKEN';
```

---
### 功能預覽
![功能主頁](DemoImg/rain_demo1.png?raw=true)
![雨量資訊](DemoImg/rain_demo2.png?raw=true)



--- 
## 台灣微地動查詢
X(Twitter) 上有一個帳號 @cwaeew84024，會不定時發佈台灣三級以下微地動的地震資訊，會把資料爬下來，並透過 OCR 辨識圖片中的經緯度、規模、深度等資訊，並將資料寫入資料庫中，提供給前端查詢使用。

--- 
### 台灣地震資訊抓取流程(DataSyncConsoleTools)
由於該帳號所發佈的貼文的資料除了日期以外，經緯度座標、規模、深度等資訊都是透過圖片提供。<br><br>
另外自從馬克斯蒐購了 Twitter 後，API 改成有限制的使用(基本上沒付費應該可以算是不能使用了，因此必須透過下述流程將資料爬下來使用。
1. 抓取 X 貼文資料：網友 caolvchong-top 寫了一個 [twitter_download](https://github.com/caolvchong-top/twitter_download) 的 Python 專案，可以下載貼文及相關附件，執行前記得先找到 `settings.json` 並調整如下參數。
    ``` json
    {
        "save_path": "D:/Service/TwitterPost",
        "user_lst": "cwaeew84024",
        "cookie": "auth_token=xxxxxxxxxxx; ct0=xxxxxxxxxxx;",
        "down_log": true,
        "has_video": false,
        "log_output": true
    }
    ```
2. `twitter_download` 是運行在 Ptyhon 下面的，我的習慣是產生在虛擬環境下，但由於是執行在 Windows 的環境下，所以請先透過底下語法安裝 Python 環境後執行，完成後會將貼文的內容轉成 CSV 檔案，並將圖片下載到指定的資料夾中。
    ``` bash
    cd D:\Service\TwitterDownload

    # 建立虛擬環境
    python -m venv projectenv

    # 啟動虛擬環境
    projectenv\Scripts\activate

    # 安裝套件
    pip install -r requirements.txt

    # 執行下載
    # 配置 settings.json文件
    python main.py 

    # 離開虛擬環境
    deactivate
    ```
3. 將貼文資料寫入 SQLite 資料庫：DataSyncConsoleTools 這個專案負責將下載的圖片進行 OCR 辨識，並將經緯度、規模、深度等資訊寫入資料庫，一樣使用前記得先調整 `appsettings`。
    ``` json
    {
       "TwitterDownloadDir": "D:/Service/TwitterPost/cwaeew84024",
       "SQLitePath": "Data Source=D:/Service/DataSyncConsoleTools/App_Data/Earthquake.db"
    }
    ```
4. API 網站：最後透過 `WebApp` 專案提供 API 服務，讓前端可以查詢資料，但一樣需要先調整 `appsettings`。
    ``` json
    {
        "TwitterDownloadDir": "D:/Service/TwitterPost/cwaeew84024",
        "ConnectionStrings": {
            "DefaultConnection": "Data Source=D:/Service/DataSyncConsoleTools/App_Data/Earthquake.db"
        }
    }
    ```

---
### 使用方式 
此專案的資料來源依定要先透過 `DataSyncConsoleTools` 完成資料抓取，然後執行 `WebApp` 這個 NetCore 專案，啟動後即可透過瀏覽器執行 `http://localhost:5000/earthquake` 來查詢資料。  

---
### 功能預覽
![功能主頁](DemoImg/earthquake_demo1.png?raw=true)



---
## 潮汐資料同步

從多個全球潮汐網站抓取潮汐資料並儲存到資料庫中。

### 支援的潮汐地點
1. **雪梨** (Sydney, Australia)，因為澳洲有夏令和冬令時間的差異，目前轉成台北時間使用平均，也就是 +2.5 計算。
2. **印度清奈** (Chennai, India) 
3. **印度洋** (British Indian Ocean Territory)
4. **東京** (Tokyo, Japan)

### ⚠️ Cloudflare 防護說明
由於潮汐資料來源網站 (zh.tideschart.com) 啟用了 Cloudflare 防爬蟲驗證，原有的自動爬蟲方案已失效。現改用 **Chrome 擴充功能** 方案手動提取資料。

### 資料同步方式

#### 改用 Chrome 擴充功能 + API 直接上傳
1. **啟動 API 服務**
   ```bash
   cd WebApp
   dotnet run
   ```
   預設執行在 `http://localhost:5000`

2. **安裝 Chrome 擴充功能**
   - 在 Chrome 瀏覽器開啟 `chrome://extensions/`
   - 啟用右上角的「開發人員模式」
   - 點擊「載入未封裝項目」
   - 選擇 `TideDataExtractor` 資料夾

3. **配置 API 網址**（如果使用非預設網址）
   - 點擊擴充功能圖示
   - 在「API 網址」欄位輸入自訂網址（預設為 `http://localhost:5000`）

4. **提取並上傳資料**
   - **自動模式**（推薦）：點擊「📦 一次提取全部地點」，擴充功能會依序開啟各地點頁面並自動提取
   - **手動模式**：
     1. 手動開啟 tideschart.com 的任一地點頁面
     2. 點擊「📝 提取此地點」按鈕
     3. 重複步驟 1-2 完成所有地點
   - 提取完成後點擊「⬆️ 上傳到資料庫」

5. **驗證資料**
   - 瀏覽器開啟 `http://localhost:5000/tide`
   - 檢查是否顯示最新的潮汐資料

#### CORS 安全設定
WebApp 的 CORS 政策已設定為僅允許：
- Chrome 擴充功能（`chrome-extension://`）
- 本機測試（`localhost`）

如需在生產環境使用，請修改 `WebApp/Program.cs` 中的 CORS 設定。

詳細擴充功能說明請參閱 [TideDataExtractor/README.md](TideDataExtractor/README.md)

---
### 使用方式 
執行 `WebApp` 這個 NetCore 專案，啟動後即可透過瀏覽器執行 `http://localhost:5000/tide` 來查詢資料。  

---
### 功能預覽
![功能主頁](DemoImg/tide_demo1.png?raw=true)



---
## DataSyncConsoleTools 命令列工具
DataSyncConsoleTools 是一個命令列工具，支援多種同步任務。目前支援的參數如下：

| 參數 | 完整名稱 | 說明 |
|------|----------|------|
| `-e` | `--earthquakes` | 執行地震資訊圖片的 OCR 辨識並儲存到資料庫 |
| `-t` | `--tide` | 同步潮汐資料到資料庫 | 

使用範例：
``` bash
# 顯示說明
dotnet run --project DataSyncConsoleTools --help

# 執行地震資料同步
dotnet run --project DataSyncConsoleTools -e

# 執行潮汐資料同步
dotnet run --project DataSyncConsoleTools -t
```



---
### License
The MIT license