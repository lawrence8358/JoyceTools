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

        #endregion

        #region Constructor

        public TideController(EarthquakeDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        #endregion

        #region Public Methods

        [HttpPost("Query")]
        public IEnumerable<TideViewModel> GetDataSource(TideQueryModel model)
        {
            var query = _dbContext.Tide.AsQueryable();

            // 如果有指定開始日期，則加入開始日期篩選條件
            if (model.Sdate.HasValue)
            {
                var sdate = model.Sdate.Value.ToLocalTime().Date;
                query = query.Where(x => x.Date >= sdate);
            }

            // 如果有指定結束日期，則加入結束日期篩選條件
            if (model.Edate.HasValue)
            {
                var edate = model.Edate.Value.ToLocalTime().Date.AddDays(1);
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
    }
}