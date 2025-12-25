namespace WebApp.Models
{
    /// <summary>
    /// 從 Chrome 擴充功能匯入的潮汐資料格式
    /// </summary>
    public class TideImportModel
    {
        /// <summary>
        /// 資料擷取時間 (ISO 格式)
        /// </summary>
        public string? ExtractTime { get; set; }

        /// <summary>
        /// 擷取來源 URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 各地點的潮汐資料
        /// </summary>
        public List<TideLocationImport>? Locations { get; set; }
    }

    /// <summary>
    /// 單一地點的潮汐資料
    /// </summary>
    public class TideLocationImport
    {
        /// <summary>
        /// 地點名稱 (Sydney, Chennai, IndianOcean, Tokyo)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 時區偏移 (例如: 10.5 代表 UTC+10:30)
        /// </summary>
        public double Timezone { get; set; }

        /// <summary>
        /// 來源 URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 該地點的每日潮汐資料
        /// </summary>
        public List<TideDayImport>? Data { get; set; }
    }

    /// <summary>
    /// 單日的潮汐資料
    /// </summary>
    public class TideDayImport
    {
        /// <summary>
        /// 日期文字 (例如: "周五 26")
        /// </summary>
        public string? Date { get; set; }

        /// <summary>
        /// 第一個潮汐資料
        /// </summary>
        public TidePointImport? Tide1 { get; set; }

        /// <summary>
        /// 第二個潮汐資料
        /// </summary>
        public TidePointImport? Tide2 { get; set; }

        /// <summary>
        /// 第三個潮汐資料
        /// </summary>
        public TidePointImport? Tide3 { get; set; }

        /// <summary>
        /// 第四個潮汐資料
        /// </summary>
        public TidePointImport? Tide4 { get; set; }
    }

    /// <summary>
    /// 單個潮汐時間點資料
    /// </summary>
    public class TidePointImport
    {
        /// <summary>
        /// 時間 (例如: "03:44")
        /// </summary>
        public string? Time { get; set; }

        /// <summary>
        /// 潮高值 (例如: "▲ 1.34 米" 或 "▼ 0.22 米")
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// 潮汐類型 (高潮/低潮/未知)
        /// </summary>
        public string? Type { get; set; }
    }

    /// <summary>
    /// 匯入結果回應
    /// </summary>
    public class TideImportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 匯入的資料筆數
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 詳細資訊（每個地點的匯入結果）
        /// </summary>
        public List<string>? Details { get; set; }
    }
}
