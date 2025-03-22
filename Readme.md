# 查詢氣候的小工具 
這個老婆大人說要研究各地下雨量寫的小工具，目前僅支援 `查詢近兩日雨量資訊`，之後等候聖旨後再進行擴充。

---
## 相關技術
1. 地圖：MapBox
2. 氣象 API：Open-Meteo.com

---
## 使用方式
如果你想要使用這個小工具，請先至 MapBox 註冊帳號，並取得 Access Token 後，到 js/env.js 中修改以下程式碼：
``` js
const mapAccessToken = 'YOUR_MAPBOX_ACCESS_TOKEN';
```

---
### 功能預覽
![功能主頁](img/demo1.png?raw=true)
![雨量資訊](img/demo2.png?raw=true)


---
### License
The MIT license