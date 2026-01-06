using System.Globalization;
using System.Text.RegularExpressions;
using Lib.EFCore;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TideController : ControllerBase
    {
        #region Members

        private readonly EarthquakeDbContext _dbContext;
        private readonly IConfiguration _configuration;

        // 台北時區 UTC+8
        private static readonly TimeSpan TaipeiTimezoneOffset = TimeSpan.FromHours(8);

        #endregion

        #region Constructor

        public TideController(EarthquakeDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 從 Chrome 擴充功能匯入潮汐資料
        /// </summary>
        [HttpPost("Import")]
        public ActionResult<TideImportResult> Import([FromBody] TideImportModel model)
        {
            if (model?.Locations == null || !model.Locations.Any())
            {
                return BadRequest(new TideImportResult
                {
                    Success = false,
                    ImportedCount = 0,
                    Message = "沒有可匯入的資料"
                });
            }

            var details = new List<string>();
            var totalImported = 0;

            foreach (var location in model.Locations)
            {
                try
                {
                    var imported = ProcessLocationData(location);
                    totalImported += imported;
                    details.Add($"✅ {location.Name}: 匯入 {imported} 筆");
                }
                catch (Exception ex)
                {
                    details.Add($"❌ {location.Name}: {ex.Message}");
                }
            }

            return Ok(new TideImportResult
            {
                Success = totalImported > 0,
                ImportedCount = totalImported,
                Message = $"匯入完成，共 {totalImported} 筆資料",
                Details = details
            });
        }

        [HttpPost("Query")]
        public IEnumerable<TideViewModel> GetDataSource(TideQueryModel model)
        {
            var query = _dbContext.Tide.AsQueryable();

            // 資料庫中的時間已經是 UTC+8，直接使用日期部分進行比較
            if (model.Sdate.HasValue)
            {
                var sdate = model.Sdate.Value.Date;
                query = query.Where(x => x.Date >= sdate);
            }

            if (model.Edate.HasValue)
            {
                var edate = model.Edate.Value.Date.AddDays(1);
                query = query.Where(x => x.Date < edate);
            }

            // 只查詢指定的四個地點
            var targetLocations = new[] {
                TideLocationType.Tokyo.ToString(),
                TideLocationType.Sydney.ToString(),
                TideLocationType.Chennai.ToString(),
                TideLocationType.IndianOcean.ToString()
            };
            query = query.Where(x => targetLocations.Contains(x.Location));

            var tideData = query.ToList();

            // 按日期分組並整合同一天的不同地點資料
            var groupedData = tideData
                .GroupBy(x => x.Date.Date)
                .Select(dateGroup => new TideViewModel
                {
                    Date = dateGroup.Key,
                    Tokyo = GetLocationTideInfo(dateGroup.FirstOrDefault(x => x.Location == TideLocationType.Tokyo.ToString())),
                    Sydney = GetLocationTideInfo(dateGroup.FirstOrDefault(x => x.Location == TideLocationType.Sydney.ToString())),
                    Chennai = GetLocationTideInfo(dateGroup.FirstOrDefault(x => x.Location == TideLocationType.Chennai.ToString())),
                    IndianOcean = GetLocationTideInfo(dateGroup.FirstOrDefault(x => x.Location == TideLocationType.IndianOcean.ToString()))
                })
                .OrderBy(x => x.Date)
                .ToArray();

            return groupedData;
        }

        private static LocationTideInfo? GetLocationTideInfo(TideEntity? entity)
        {
            if (entity == null) return null;

            // 收集所有潮汐資料並篩選出漲潮(正數)資料
            var tides = new List<(decimal height, DateTime time)>();

            if (entity.FirstTideHeight.HasValue && entity.FirstTideHeight > 0 && entity.FirstTideTime.HasValue)
                tides.Add((entity.FirstTideHeight.Value, entity.FirstTideTime.Value));

            if (entity.SecondTideHeight.HasValue && entity.SecondTideHeight > 0 && entity.SecondTideTime.HasValue)
                tides.Add((entity.SecondTideHeight.Value, entity.SecondTideTime.Value));

            if (entity.ThirdTideHeight.HasValue && entity.ThirdTideHeight > 0 && entity.ThirdTideTime.HasValue)
                tides.Add((entity.ThirdTideHeight.Value, entity.ThirdTideTime.Value));

            if (entity.FourthTideHeight.HasValue && entity.FourthTideHeight > 0 && entity.FourthTideTime.HasValue)
                tides.Add((entity.FourthTideHeight.Value, entity.FourthTideTime.Value));

            // 按時間排序，只取前兩個漲潮
            var highTides = tides.OrderBy(x => x.time).Take(2).ToList();

            var locationInfo = new LocationTideInfo();

            if (highTides.Count > 0)
            {
                locationInfo.FirstHighTideHeight = highTides[0].height;
                locationInfo.FirstHighTideTime = highTides[0].time;
            }

            if (highTides.Count > 1)
            {
                locationInfo.SecondHighTideHeight = highTides[1].height;
                locationInfo.SecondHighTideTime = highTides[1].time;
            }

            return locationInfo;
        }

        #endregion

        #region Private Methods - Import

        /// <summary>
        /// 處理單一地點的潮汐資料
        /// </summary>
        private int ProcessLocationData(TideLocationImport location)
        {
            if (location.Data == null || !location.Data.Any())
                return 0;

            // 使用台北時區 (UTC+8) 的當前時間，不依賴伺服器本地時區
            var taipeiNow = DateTimeOffset.UtcNow.ToOffset(TaipeiTimezoneOffset);
            var currentYear = taipeiNow.Year;
            var currentMonth = taipeiNow.Month;

            // 先收集所有潮汐資料（已轉換為台北時區）
            var allTideData = new List<TideDtoModel>();

            foreach (var dayData in location.Data)
            {
                try
                {
                    var tides = ParseDayData(dayData, location.Name, location.Timezone, currentYear, currentMonth);
                    allTideData.AddRange(tides);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析 {location.Name} 資料失敗: {ex.Message}");
                }
            }

            // 按台北時間日期重新分組（解決跨日問題）
            var groupedByDate = allTideData
                .GroupBy(t => t.Date.Date)
                .OrderBy(g => g.Key);

            var importedCount = 0;
            foreach (var group in groupedByDate)
            {
                var dayTides = group.OrderBy(t => t.Date).ToList();
                if (dayTides.Any())
                {
                    _dbContext.AddOrUpdateTideEntity(dayTides);
                    importedCount++;
                }
            }

            return importedCount;
        }

        /// <summary>
        /// 解析單日的潮汐資料
        /// </summary>
        private List<TideDtoModel> ParseDayData(TideDayImport dayData, string location, double timezone, int year, int month)
        {
            var result = new List<TideDtoModel>();

            // 解析日期 (例如: "周五 26")
            var date = ParseDateFromText(dayData.Date, year, month);

            // 解析四個潮汐時段
            var tidePoints = new[] { dayData.Tide1, dayData.Tide2, dayData.Tide3, dayData.Tide4 };

            foreach (var tidePoint in tidePoints)
            {
                if (tidePoint == null || string.IsNullOrEmpty(tidePoint.Time))
                    continue;

                try
                {
                    var (height, time) = ParseTidePoint(tidePoint);

                    // 組合日期和時間
                    var dateTime = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified);
                    var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.FromHours(timezone));

                    // 統一轉換到台北時區 (UTC+8)，不依賴伺服器本地時區
                    result.Add(new TideDtoModel
                    {
                        Date = dateTimeOffset.ToOffset(TaipeiTimezoneOffset),
                        Location = location,
                        TideHeight = height
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析潮汐點失敗: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 從文字解析日期 (例如: "周五 26" 或 "周日 5")
        /// </summary>
        private static DateTime ParseDateFromText(string? dateText, int year, int month)
        {
            if (string.IsNullOrEmpty(dateText))
                throw new Exception("日期文字為空");

            var dayMatch = Regex.Match(dateText, @"(\d+)");
            if (!dayMatch.Success)
                throw new Exception($"無法解析日期: '{dateText}'");

            var day = int.Parse(dayMatch.Groups[1].Value);

            // 處理跨月份的情況（使用台北時區的當前日期，而非伺服器本地時間）
            var taipeiToday = DateTimeOffset.UtcNow.ToOffset(TaipeiTimezoneOffset).Date;
            var date = new DateTime(year, month, day);
            if (date < taipeiToday.AddDays(-10))
            {
                if (month == 12)
                    date = new DateTime(year + 1, 1, day);
                else
                    date = new DateTime(year, month + 1, day);
            }

            return date;
        }

        /// <summary>
        /// 解析單個潮汐時間點資料
        /// </summary>
        private static (decimal height, TimeSpan time) ParseTidePoint(TidePointImport point)
        {
            var time = ParseTime(point.Time);
            var height = ParseHeight(point.Value, point.Type);
            return (height, time);
        }

        /// <summary>
        /// 解析時間 (格式: "HH:mm")
        /// </summary>
        private static TimeSpan ParseTime(string? timeText)
        {
            if (string.IsNullOrEmpty(timeText))
                throw new Exception("時間文字為空");

            var timeMatch = Regex.Match(timeText, @"(\d{1,2}):(\d{2})");
            if (!timeMatch.Success)
                throw new Exception($"無法解析時間: '{timeText}'");

            var hours = int.Parse(timeMatch.Groups[1].Value);
            var minutes = int.Parse(timeMatch.Groups[2].Value);

            if (hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59)
                return new TimeSpan(hours, minutes, 0);

            throw new Exception($"時間值超出範圍: '{timeText}'");
        }

        /// <summary>
        /// 解析潮高值 (例如: "▲ 1.34 米" 或 "▼ 0.22 米")
        /// </summary>
        private static decimal ParseHeight(string? valueText, string? typeText)
        {
            if (string.IsNullOrEmpty(valueText))
                throw new Exception("潮高文字為空");

            // 尋找數字 (可能含負號)
            var heightMatch = Regex.Match(valueText, @"(-?\d+\.?\d*)");
            if (!heightMatch.Success)
                throw new Exception($"無法解析潮高: '{valueText}'");

            var heightValue = decimal.Parse(heightMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            // 根據符號或類型判斷漲退潮
            if (valueText.Contains("▲") || typeText == "高潮")
                return Math.Abs(heightValue);
            else if (valueText.Contains("▼") || typeText == "低潮")
                return -Math.Abs(heightValue);
            else
                return heightValue;
        }

        #endregion
    }
}