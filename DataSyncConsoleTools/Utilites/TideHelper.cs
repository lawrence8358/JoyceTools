using HtmlAgilityPack;
using Lib.EFCore;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataSyncConsoleTools.Utilites
{
    internal static class TideHelper
    {
        #region Members

        private static readonly HttpClient httpClient = new();

        #endregion

        #region Constructor

        static TideHelper()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 從指定 URL 抓取潮汐資料
        /// </summary>
        public static List<TideDtoModel> GetTideDataFromUrl(string url, string location, double timezone)
        {
            var result = new List<TideDtoModel>();

            var html = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 尋找潮汐表格
            var table = doc.DocumentNode
                .SelectSingleNode("//table[contains(@class, 'tidechart')]");

            if (table == null)
            {
                throw new Exception($"在 {url} 中找不到潮汐表格");
            }

            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return result;

            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            foreach (var row in rows)
            {
                var tideData = ParseTideRow(row, location, currentYear, currentMonth, timezone);
                if (tideData == null) continue;

                result.AddRange(tideData);
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 解析潮汐表格的一列資料
        /// </summary>
        private static List<TideDtoModel>? ParseTideRow(HtmlNode row, string location, int year, int month, double timezone)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 5) return null;

            var result = new List<TideDtoModel>();

            // 解析日期 (例如: "周日 5" 或 "周一 6")
            var date = ParseDateFromCells(cells[0].InnerText.Trim(), year, month);

            // 解析四個潮汐時段
            for (int i = 1; i <= 4 && i < cells.Count; i++)
            {
                var cellText = cells[i].InnerText.Trim();
                if (string.IsNullOrEmpty(cellText)) continue;

                var (height, time) = ParseTideCell(cellText);

                // 組合日期和時間，DateTimeKind 設為 Unspecified
                var dateTime = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified);
                // 轉換為 DateTimeOffset，並考慮時區偏移
                var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.FromHours(timezone));

                var tideData = new TideDtoModel
                {
                    Date = dateTimeOffset.ToLocalTime(),
                    Location = location,
                    TideHeight = height
                };

                result.Add(tideData);
            }

            return result;
        }

        /// <summary>
        /// 從表格單元格文字（例如 "周日 5" 或 "周一 6"）解析出正確的日期。
        /// </summary> 
        private static DateTime ParseDateFromCells(string dayText, int year, int month)
        {
            var dayMatch = Regex.Match(dayText, @"(\d+)");
            if (!dayMatch.Success)
                throw new Exception($"無法解析日期，格式錯誤: '{dayText}'");

            var day = int.Parse(dayMatch.Groups[1].Value);

            // 處理跨月份的情況
            var date = new DateTime(year, month, day);
            if (date < DateTime.Now.Date.AddDays(-10)) // 如果是過去超過10天的日期，可能是下個月
            {
                if (month == 12)
                {
                    date = new DateTime(year + 1, 1, day);
                }
                else
                {
                    date = new DateTime(year, month + 1, day);
                }
            }

            return date;
        }

        /// <summary>
        /// 解析潮汐儲存格內容 (例如: "03:44▼ 0.22 米" 或 "07:35▲ 1.49 米")
        /// 處理 HTML 實體編碼: &#x25B2; (▲) 和 &#x25BC; (▼)
        /// </summary>
        private static (decimal height, TimeSpan time) ParseTideCell(string cellText)
        {
            // 記錄原始內容以便除錯，02:06&#x25BC; 0.25 米
            var originalText = cellText.Trim();

            // 將 HTML 實體編碼解碼為實際符號，02:06▼ 0.25 米
            cellText = System.Net.WebUtility.HtmlDecode(originalText);

            // 移除多餘空白但保留結構
            cellText = Regex.Replace(cellText, @"\s+", " ").Trim();

            var time = ParseTime(cellText);
            var height = ParseHeight(cellText);

            return (height, time);
        }

        /// <summary>
        /// 解析時間部分 (格式: HH:mm)
        /// </summary>
        private static TimeSpan ParseTime(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new Exception("時間文字為空");

            var timeMatch = new Regex(@"(\d{1,2}):(\d{2})", RegexOptions.Compiled).Match(text);
            if (!timeMatch.Success)
                throw new Exception($"無法解析時間，格式錯誤: '{text}'");

            var hours = int.Parse(timeMatch.Groups[1].Value);
            var minutes = int.Parse(timeMatch.Groups[2].Value);

            // 驗證時間範圍
            if (hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59)
            {
                return new TimeSpan(hours, minutes, 0);
            }

            throw new Exception($"時間值超出範圍: '{text}'");
        }

        /// <summary>
        /// 解析高度部分 (包含漲退潮符號處理)
        /// </summary>
        private static decimal ParseHeight(string decodedText)
        {
            if (string.IsNullOrEmpty(decodedText))
                throw new Exception("高度文字為空");

            // 尋找符號和數字
            // 正常資料應該 05:44▲ 2.1 米
            // 特殊資料出現 00:30▼ -0.1 米
            var regex = new Regex(@"[▲▼]\s*(\d+\.?\d*)\s*米?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var heightMatch = regex.Match(decodedText);
            if (!heightMatch.Success)
            {
                // 判斷是否有負號的情況
                regex = new Regex(@"[▲▼]\s*(-\d+\.?\d*)\s*米?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                heightMatch = regex.Match(decodedText);
                if (!heightMatch.Success)
                    throw new Exception($"無法解析高度，格式錯誤: '{decodedText}'");
            }

            var heightValue = decimal.Parse(heightMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return DetermineSignFromDecodedText(decodedText, heightValue);
        }

        /// <summary>
        /// 從解碼後的文字判斷漲退潮符號
        /// </summary>
        private static decimal DetermineSignFromDecodedText(string text, decimal value)
        {
            if (text.Contains("▲"))
                return value; // 漲潮為正數
            else if (text.Contains("▼"))
                return -value; // 退潮為負數
            else
                return value; // 預設為正數
        }

        #endregion
    }
}