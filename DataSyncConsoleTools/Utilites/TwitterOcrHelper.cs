using CsvHelper;
using CsvHelper.Configuration;
using Lib.EFCore;
using DataSyncConsoleTools.Models;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;

namespace DataSyncConsoleTools.Utilites
{
    internal static class TwitterOcrHelper
    {
        #region Public Methods

        /// <summary>
        /// 取得 CSV 檔案內的資料來源
        /// </summary>
        public static List<EarthquakeTempModel> GetCsvDataSource(string[] csvFilePaths, DateTime? lastPostDate)
        {
            // 讀取 CSV 檔案，第四列是標題，第五列之後是資料
            // 標題 Tweet Date,Display Name,User Name,Tweet URL,Media Type,Media URL,Saved Filename,Tweet Content,Favorite Count,Retweet Count,Reply Count
            // 資料範例 2025-05-18 21:26,cwaeew,@cwaeew84024,https://x.com/cwaeew84024/status/1924094258329555260/photo/1,Image,https://pbs.twimg.com/tweet_video_thumb/GrPBhyyW0AA1cYV.jpg,2025-05-18 21-26-img_1.jpg,"CWA IPFx Test Report, sent time: 2025-05-18T13:26:29.822269 https://t.co/v1GET7RI6q",0,0,0
            // 資料內容，可能會有包含,的情況，所以不能直接用 Split(',') 來切割
            // 讀取 CSV 檔案，並轉成 List<TweetInfo>

            var result = new List<EarthquakeTempModel>();

            List<TwitterInfo> tweetInfos = new List<TwitterInfo>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            foreach (var csvFilePath in csvFilePaths)
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, config))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        reader.ReadLine();
                    }

                    tweetInfos.AddRange(csv.GetRecords<TwitterInfo>().ToList());
                }
            }

            tweetInfos = [.. tweetInfos
                .Distinct()
                .OrderBy(x => x.TweetDate)
            ];

            // Distinct TweetDate & TweetContent 欄位
            var mainInfos = tweetInfos
                .GroupBy(x => new { x.TweetDate, x.TweetContent })
                .Select(g => new
                {
                    g.Key.TweetDate,
                    g.Key.TweetContent
                })
                .ToList();

            var regexDate = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?");
            var regexUrl = new Regex(@"https?://[^\s]+");

            foreach (var info in mainInfos)
            {
                // 使用正則表達式，抓出 TweetContent 內的日期欄位
                // "CWA IPFx Test Report, sent time: 2025-03-03T20:12:20.514210 https://t.co/o7FWwytFWX" 

                var matchDate = regexDate.Match(info.TweetContent ?? "");
                if (!matchDate.Success) throw new Exception($"無法從 TweetContent 中抓取日期，內容: {info.TweetContent}");

                // 使用正則表達式，抓出 TweetContent 內的超連結欄位
                // "CWA IPFx Test Report, sent time: 2025-03-03T20:12:20.514210 https://t.co/o7FWwytFWX" 

                var matchUrl = regexUrl.Match(info.TweetContent ?? "");
                if (!matchUrl.Success) throw new Exception($"無法從 TweetContent 中抓取超連結，內容: {info.TweetContent}");

                var date = DateTime.Parse(matchDate.Value, new CultureInfo("zh-TW"));
                result.Add(new EarthquakeTempModel
                {
                    Date = date,
                    Content = info.TweetContent ?? "",
                    LinkUrl = matchUrl.Value,
                    FileNames = tweetInfos
                        .Where(x => x.TweetDate == info.TweetDate && x.TweetContent == info.TweetContent)
                        .Select(x => x.SavedFilename)
                        .Distinct()
                        .ToList()
                });
            }

            if (lastPostDate == null) return result;

            // 僅回傳超過資料庫 lastPostDate 的數據
            return result.Where(x => x.Date >= lastPostDate).ToList();
        }

        /// 使用 Tesseract 進行 OCR 辨識，並提取 M= 開頭 和 包含 E= 的資料行
        /// </summary> 
        public static EarthquakeDtoModel? GetEarthquakeInfo(string imagePath, DateTime date, string linkUrl)
        {
            if (!File.Exists(imagePath)) return null;
            var fileName = Path.GetFileName(imagePath);

            var fullTextLocation = ExtractTextFromImage(imagePath, x: 110, y: 30, width: 420, height: 40, "0123456789.,E=");
            var fulltextMagnitude = ExtractTextFromImage(imagePath, x: 250, y: 48, width: 585, height: 40, "0123456789.,+:TMSI=()");

            if (string.IsNullOrEmpty(fullTextLocation) && string.IsNullOrEmpty(fulltextMagnitude)) return null;

            var line = fulltextMagnitude?.Split('\n').FirstOrDefault(x => x.Contains("M="))?.Trim();
            if (!string.IsNullOrEmpty(line)) // 包含 M= 資料行，這個是抓取震度規模的資料行
            {
                // 3:25:00 (+1s) M=1.9 SI=0.0
                // T = 20:02 :33 (+1s) M= 2.1 SI=1.0
                // T = 23:25:56 (+1s) M= ‘1. 9 SI=1.0
                // T=17:58:15(+1S)M= 2. 0SI=1.0
                // T=12:45:52 (+15) M= =1. 8SI=1.0
                // T=18:09:29 (+15) M=1.4 S=0.0
                // 抓取 M= 後面的數字
                var value = line.Split("M=").Skip(1).First().Trim().Split("S")[0].Replace(" ", "").Replace("=", "").Trim();
                return new EarthquakeDtoModel
                {
                    Type = 1,
                    PostDate = date,
                    // 地震規模有出現 -0.0 這樣的資料格式，因此先處理找不到地震規模的一律視為 0 
                    Magnitude = decimal.TryParse(value, out var magnitude) ? magnitude : 0,
                    FileName = fileName,
                    LinkUrl = linkUrl
                };
            }

            line = fullTextLocation?.Split('\n').FirstOrDefault(x => x.Contains("E="))?.Trim();
            if (!string.IsNullOrEmpty(line)) // E= 開頭的資料行，這個是抓取經緯度和深度的資料行
            {
                // 25/05/1618:35:24
                var datetime = ExtractTextFromImage(imagePath, x: 100, y: 370, width: 420, height: 40, "X=0123456789:/");
                datetime = datetime?.Split('=')[1].Replace(" ", "").Trim();

                if (datetime == null || datetime.Length != 16 && !datetime.StartsWith("25/"))
                    throw new Exception($"無法從圖片中抓取日期，內容: {datetime}");

                try
                {
                    // E=121.40, 22.95, 30.3
                    // -E=121.95,23.36,31.1
                    // E=120.52, 23.17, 13.7 Sl=5.0@13:27:48
                    var values = line.Split('=')[1].Replace("=", "").Split(',').Select(x => x.Trim()).ToList();
                    return new EarthquakeDtoModel
                    {
                        Type = 0,
                        PostDate = date,
                        Longitude = decimal.TryParse(values[0].Split(' ')[0], out var longitude) ? longitude : null,
                        Latitude = decimal.TryParse(values[1].Split(' ')[0], out var latitude) ? latitude : null,
                        MaxDepth = decimal.TryParse(values[2].Split(' ')[0], out var maxDepth) ? maxDepth : null,
                        FileName = fileName,
                        LinkUrl = linkUrl,
                        // 25/05/1618:35:24，轉成日期欄位
                        EarthquakeDate = DateTime.ParseExact("20" + datetime, "yyyy/MM/ddHH:mm:ss", CultureInfo.CurrentCulture)
                    };
                }
                catch
                {
                    Console.WriteLine(fileName);
                    throw;
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        private static string? ExtractTextFromImage(string imagePath, int cropHeight, string whitelist)
        {
            var bytes = CropImagePixels(imagePath, cropHeight);
#if DEBUG
            // File.WriteAllBytes(@"D:\debug.jpg", bytes);
#endif 
            // 取得辨識結果
            var text = GetOrcText(bytes, whitelist);

            return text;
        }

        private static string? ExtractTextFromImage(string imagePath, int x, int y, int width, int height, string whitelist)
        {
            var bytes = CropImagePixels(imagePath, x: x, y: y, width: width, height: height);
#if DEBUG
            // File.WriteAllBytes(@"D:\debug.jpg", bytes);
#endif 

            // 取得辨識結果
            var text = GetOrcText(bytes, whitelist);

            return text;
        }


        private static string GetOrcText(byte[] imageBtyes, string whitelist)
        {
            using var engine = new TesseractEngine("App_Data\\tessdata", "eng");

            // 設定可辨識的字元
            engine.SetVariable("tessedit_char_whitelist", whitelist);

            using var img = Pix.LoadFromMemory(imageBtyes);
            using var page = engine.Process(img);

            // 取得辨識結果
            var text = page.GetText();
            return text;
        }

        private static byte[] CropImagePixels(string imagePath, int x, int y, int width, int height)
        {
#pragma warning disable CA1416 // 驗證平台相容性
            using (var original = new Bitmap(imagePath))
            {
                Rectangle cropRect = new Rectangle(x, y, width, height);

                // 建立新圖並裁切
                using (var cropped = original.Clone(cropRect, original.PixelFormat))
                {
                    // 回傳 MemoryStream
                    using (var ms = new MemoryStream())
                    {
                        cropped.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        return ms.ToArray();
                    }
                }
            }
#pragma warning restore CA1416 // 驗證平台相容性
        }

        /// <summary>
        /// 裁切指定範圍的 OCR 圖檔
        /// </summary>
        private static byte[] CropImagePixels(string imagePath, int cropHeight)
        {
#pragma warning disable CA1416 // 驗證平台相容性
            using (var original = new Bitmap(imagePath))
            {
                Rectangle cropRect = new Rectangle(0, 0, original.Width, cropHeight);

                // 建立新圖並裁切
                using (var cropped = original.Clone(cropRect, original.PixelFormat))
                {
                    // 回傳 MemoryStream
                    using var ms = new MemoryStream();
                    cropped.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }
#pragma warning restore CA1416 // 驗證平台相容性
        }

        #endregion
    }
}
