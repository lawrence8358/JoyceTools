using Microsoft.EntityFrameworkCore;

namespace Lib.EFCore
{
    public class EarthquakeDbContext : DbContext
    {
        public DbSet<EarthquakeEntity> Earthquake { get; set; }
        public DbSet<TideEntity> Tide { get; set; }

        public EarthquakeDbContext(DbContextOptions<EarthquakeDbContext> options) : base(options)
        {
        }


        public void InitJournalMode()
        {
            Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
        }

        /// <summary>
        /// 取得資料庫內最新的地震資訊的貼文資料
        /// </summary>
        public DateTime? GetEarthquakePostDate()
        {
            // 若資料庫有值，則取得 FileName1 或 FileName2 為空，且 PostDate 最小的資料(資料不完整的狀態，因此需重新執行)
            var entity = Earthquake
                .Where(x => string.IsNullOrEmpty(x.FileName1) || string.IsNullOrEmpty(x.FileName2))
                .OrderBy(x => x.PostDate)
                .FirstOrDefault();

            if (entity != null) return entity.PostDate;


            entity = Earthquake
                .OrderByDescending(x => x.PostDate)
                .FirstOrDefault();

            if (entity == null) return null;

            return entity.PostDate;
        }

        /// <summary>
        /// 新增或修改地震資訊
        /// </summary>
        public void AddOrUpdateEarthquakeEntity(EarthquakeDtoModel? result)
        {
            if (result == null) return;
            bool isAdd = false;

            var entity = Earthquake.FirstOrDefault(x => x.PostDate == result.PostDate);
            if (entity == null)
            {
                entity = new EarthquakeEntity { PostDate = result.PostDate };
                isAdd = true;
            }

            if (result.Type == 0)
            {
                entity.Latitude = result.Latitude;
                entity.Longitude = result.Longitude;
                entity.MaxDepth = result.MaxDepth;
                entity.EarthquakeDate = result.EarthquakeDate;
                entity.FileName1 = result.FileName;
            }
            else if (result.Type == 1)
            {
                entity.Magnitude = result.Magnitude;
                entity.FileName2 = result.FileName;
            }

            entity.LinkUrl = result.LinkUrl;

            if (isAdd)
                Earthquake.Add(entity);
            else
                Earthquake.Update(entity);

            SaveChanges();
        }

        /// <summary>
        /// 新增或修改潮汐資訊
        /// </summary>
        public void AddOrUpdateTideEntity(List<TideDtoModel> tideDtoModels)
        {
            if (tideDtoModels == null || !tideDtoModels.Any()) return;

            // 傳入的陣列會是同一天的四個潮汐資料，因此只取第一筆資料的日期與地點來處理
            var tideData = tideDtoModels.First();
            var entity = Tide.FirstOrDefault(x => x.Date.Date == tideData.Date.Date && x.Location == tideData.Location);

            // 其餘依潮汐時間排序來設定，但不保證會有四個潮汐資料
            bool isAdd = entity == null;

            // 如果是修改，但是沒有四個潮汐資料，則不處理，否則一周後會把原本最後一天的資料給清掉(跨時區的關係)
            if (!isAdd && tideDtoModels.Count < 4) return;

            if (entity == null)
            {
                entity = new TideEntity
                {
                    Date = tideData.Date.Date,
                    Location = tideData.Location,
                    CreatedDate = DateTimeOffset.Now.LocalDateTime
                };
            }

            for (int i = 0; i < tideDtoModels.Count && i < 4; i++)
            {
                var tide = tideDtoModels[i];
                switch (i)
                {
                    case 0:
                        entity.FirstTideHeight = tide.TideHeight;
                        entity.FirstTideTime = tide.Date.DateTime;
                        break;
                    case 1:
                        entity.SecondTideHeight = tide.TideHeight;
                        entity.SecondTideTime = tide.Date.DateTime;
                        break;
                    case 2:
                        entity.ThirdTideHeight = tide.TideHeight;
                        entity.ThirdTideTime = tide.Date.DateTime;
                        break;
                    case 3:
                        entity.FourthTideHeight = tide.TideHeight;
                        entity.FourthTideTime = tide.Date.DateTime;
                        break;
                }
            }

            entity.ModifiedDate = DateTimeOffset.Now.LocalDateTime;
            if (isAdd)
                Tide.Add(entity);
            else
                Tide.Update(entity);

            SaveChanges();
        }
    }
}
