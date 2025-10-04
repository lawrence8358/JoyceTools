using DataSyncConsoleTools.Utilites;
using Lib.EFCore;
using Lib.Models;
using Lib.Utilites;
using Microsoft.EntityFrameworkCore;

namespace DataSyncConsoleTools.Services
{
    internal class EarthquakeSyncService
    {
        private readonly AppSettings _config;

        public EarthquakeSyncService(AppSettings config)
        {
            _config = config;
        }

        /// <summary>
        /// 處理地震 OCR 資料並儲存到資料庫
        /// </summary>
        public void ProcessEarthquakeOCRAndSaveToDb()
        {
            var options = new DbContextOptionsBuilder<EarthquakeDbContext>()
                .UseSqlite(_config.SQLitePath)
                .Options;

            using var dbContext = new EarthquakeDbContext(options);
            dbContext.Database.EnsureCreated();
            dbContext.InitJournalMode();

            var lastPostDate = dbContext.GetEarthquakePostDate();
            var downloadDir = _config.TwitterDownloadDir;

            // 找資料夾內檔案時間最新的 CSV 檔案
            var csvFiles = Directory.GetFiles(downloadDir, "*.csv");

            if (csvFiles.Length == 0)
            {
                UtilityHelper.ConsoleError($"資料夾 {downloadDir} 中沒有 CSV 檔案");
                return;
            }

            var dataSource = TwitterOcrHelper.GetCsvDataSource(csvFiles, lastPostDate);
            UtilityHelper.ConsoleWriteLine($"資料筆數: {dataSource.Count}");

            foreach (var data in dataSource)
            {
                var date = data.Date;
                var linkUrl = data.LinkUrl;

                foreach (var fileName in data.FileNames)
                {
                    var filePath = $"{downloadDir}\\{fileName}";
                    var result = TwitterOcrHelper.GetEarthquakeInfo(filePath, date, linkUrl);

                    ShowResult(result, filePath);
                    dbContext.AddOrUpdateEarthquakeEntity(result);
                }
            }

            // 將已完成的 CSV 檔案移動到已處理資料夾
            var processedDir = Path.Combine(downloadDir, "Processed");
            if (!Directory.Exists(processedDir)) Directory.CreateDirectory(processedDir);

            foreach (var csvFile in csvFiles)
            {
                var fileName = Path.GetFileName(csvFile);
                var destPath = Path.Combine(processedDir, fileName);
                File.Move(csvFile, destPath, true);
            }
        }

        /// <summary>
        /// 除錯用：測試地震 OCR 功能
        /// </summary>
        public void DebugEarthquakeOCR()
        {
            var debugFiles = new List<string>
            {
                // $"{_config.TwitterDownloadDir}\\2025-03-05 00-57-img_2532.jpg"
                // "App_Data\\sample\\test.jpg",
                // "App_Data\\sample\\test2.jpg",
                "App_Data\\sample\\zero.jpg",
            };

            foreach (var filePath in debugFiles)
            {
                var result = TwitterOcrHelper.GetEarthquakeInfo(filePath, DateTime.Now, "");
                ShowResult(result, filePath);
            }
        }

        private static void ShowResult(EarthquakeDtoModel? result, string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            if (result == null)
                throw new Exception($"檔案【{fileName}】，無法辨識圖片中的資料");

            if (result.Type == 0)
            {
                Console.WriteLine($"檔案【{fileName}】，經度: {result.Longitude}, 緯度: {result.Latitude}, 深度: {result.MaxDepth}");

                if (result.Longitude == null || result.Latitude == null || result.MaxDepth == null)
                    throw new Exception($"檔案【{fileName}】的經緯度或深度無法轉換成數字");
            }
            else if (result.Type == 1)
            {
                decimal? magnitude = result.Magnitude;
                Console.WriteLine($"檔案【{fileName}】，震度規模: {magnitude}");

                if (magnitude == null)
                    throw new Exception($"檔案【{fileName}】的震度規模無法轉換成數字");
            }
            else
            {
                Console.WriteLine($"檔案【{fileName}】，無法辨識圖片中的資料");
            }
        }
    }
}