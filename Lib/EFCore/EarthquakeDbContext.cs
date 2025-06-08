using Microsoft.EntityFrameworkCore;

namespace Lib.EFCore
{
    public class EarthquakeDbContext : DbContext
    {
        public DbSet<EarthquakeEntity> Earthquake { get; set; }

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
    }
}
