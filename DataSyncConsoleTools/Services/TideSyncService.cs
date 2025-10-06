using DataSyncConsoleTools.Utilites;
using Lib.EFCore;
using Lib.Models;
using Lib.Utilites;
using Microsoft.EntityFrameworkCore;

namespace DataSyncConsoleTools.Services
{
    internal class TideSyncService
    {
        private readonly AppSettings _config;

        public TideSyncService(AppSettings config)
        {
            _config = config;
        }

        /// <summary>
        /// 處理潮汐資料同步
        /// </summary>
        public void ProcessTideData()
        {
            var options = new DbContextOptionsBuilder<EarthquakeDbContext>()
                .UseSqlite(_config.SQLitePath)
                .Options;

            using var dbContext = new EarthquakeDbContext(options);
            dbContext.Database.EnsureCreated();
            dbContext.InitJournalMode();

            // 潮汐地點及其時區偏移量（相對於台北時間 GMT+8）
            var tideLocations = new Dictionary<string, (string url, double timezone)>
            {
                { TideLocationType.Sydney.ToString(), ("https://zh.tideschart.com/Australia/New-South-Wales/Sydney", +10.5) }, // GMT+11，因為澳洲有冬令和夏令時間，這邊直接取平均
                { TideLocationType.Chennai.ToString(), ("https://zh.tideschart.com/India/Tamil-Nadu/Chennai", +5.5) }, // GMT+5:30 
                { TideLocationType.IndianOcean.ToString(), ("https://zh.tideschart.com/British-Indian-Ocean-Territory", +6) }, // GMT+6 
                { TideLocationType.Tokyo.ToString(), ("https://zh.tideschart.com/Japan/Tokyo", +9) } // GMT+9
            };

            foreach (var location in tideLocations)
            {
                var locationName = location.Key;
                var url = location.Value.url;
                var timezone = location.Value.timezone;

                UtilityHelper.ConsoleWriteLine($"開始處理 {locationName} 的潮汐資料...");

                var tideDataList = TideHelper.GetTideDataFromUrl(url, locationName, timezone);

                // 找出 tideDataList 同一天的資料
                var groupedByDate = tideDataList.GroupBy(t => t.Date.Date).ToList();
                foreach (var group in groupedByDate)
                {
                    var data = tideDataList.Where(t => t.Date.Date == group.Key).ToList();

                    UtilityHelper.ConsoleWriteLine($"處理 {locationName} 的 {group.Key:yyyy-MM-dd} 潮汐資料，共 {data.Count} 筆");
                    dbContext.AddOrUpdateTideEntity(data);
                }
            }

            UtilityHelper.ConsoleWriteLine("潮汐資料同步完成");
        }
    }
}