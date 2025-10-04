# 小工具說明 
這個專案是老婆大人說要研究氣候 & 地震資訊的查詢小工具，目前僅支援功能如下
+ `各地雨量資訊查詢`
+ `台灣微地動查詢`


---
## DataSyncConsoleTools 架構說明

### 模組化設計
DataSyncConsoleTools 採用模組化架構設計，方便擴展不同的同步任務：

- **Services** - 各種同步服務的具體實作
- **Models** - 資料模型和命令列參數定義
- **Utilites** - 共用的工具類別

### 擴展新的同步任務
如果要新增其他同步任務，可以按照以下步驟：

1. 在 `Services/` 資料夾下新增服務類別
2. 在 `Models/Options.cs` 中新增對應的命令列參數
3. 在 `Program.cs` 中新增參數處理邏輯

範例：
``` csharp
// 1. 新增服務類別 Services/WeatherSyncService.cs
internal class WeatherSyncService 
{
    public void ProcessWeatherData() { /* 實作邏輯 */ }
}

// 2. 在 Options.cs 新增參數
[Option('w', "weather", Required = false, HelpText = "同步氣象資料")]
public bool IsProcessWeather { get; set; }

// 3. 在 Program.cs 新增處理邏輯
if (opts.IsProcessWeather)
{
    var weatherService = new WeatherSyncService(config);
    weatherService.ProcessWeatherData();
}
```

---
## 專案資料夾結構
```
|_ DataSyncConsoleTools  => 各種同步排程工具
   |_ Services => 各種同步服務
      |_ EarthquakeSyncService.cs => 地震資料同步服務
   |_ Models => 資料模型
      |_ Options.cs => 命令列參數選項
      |_ EarthquakeTempModel.cs => 地震暫存資料模型
      |_ TwitterModel.cs => Twitter 資料模型
   |_ Utilites => 工具類別
      |_ TwitterOcrHelper.cs => OCR 辨識工具
   |_ App_Data => 應用程式資料
      |_ sample => 測試圖片範例
      |_ tessdata => OCR 訓練資料
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
    
    執行地震資料同步：
    ``` bash
    # 執行地震 OCR 辨識並儲存到資料庫
    dotnet run --project DataSyncConsoleTools -o
    
    # 或使用完整參數名稱
    dotnet run --project DataSyncConsoleTools --type1
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

#### DataSyncConsoleTools 命令列工具
DataSyncConsoleTools 是一個命令列工具，支援多種同步任務。目前支援的參數如下：

| 參數 | 完整名稱 | 說明 |
|------|----------|------|
| `-o` | `--type1` | 執行地震資訊圖片的 OCR 辨識並儲存到資料庫 |

使用範例：
``` bash
# 顯示說明
dotnet run --project DataSyncConsoleTools --help

# 執行地震資料同步
dotnet run --project DataSyncConsoleTools -o
```

#### 完整執行流程
此專案的資料來源依定要先透過上述 `DataSyncConsoleTools` 完成資料抓取，然後執行 `WebApp` 這個 NetCore 專案，啟動後即可透過瀏覽器執行 `http://localhost:5000\earthquake` 來查詢資料。

完整步驟：
1. 使用 Python 工具下載 Twitter 資料
2. 執行 `DataSyncConsoleTools -o` 進行 OCR 辨識
3. 啟動 `WebApp` 提供 API 服務
4. 開啟瀏覽器查看結果  


---
### 功能預覽
![功能主頁](DemoImg/earthquake_demo1.png?raw=true)


---
### License
The MIT license