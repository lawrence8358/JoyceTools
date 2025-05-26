using System.ComponentModel.DataAnnotations;

namespace Lib.EFCore
{
    public abstract class EarthquakeBaseEntity
    {
        [Key]
        public DateTime PostDate { get; set; }

        /// <summary>
        /// 緯度
        /// </summary>
        public decimal? Latitude { get; set; } = null;

        /// <summary>
        /// 經度
        /// </summary>
        public decimal? Longitude { get; set; } = null;

        /// <summary>
        /// 最大深度
        /// </summary>
        public decimal? MaxDepth { get; set; } = null;

        /// <summary>
        /// 震度規模
        /// </summary>
        public decimal? Magnitude { get; set; } = null;

        /// <summary>
        /// Twitter 連結
        /// </summary>
        public string LinkUrl { get; set; } = string.Empty;

        /// <summary>
        /// 地震時間
        /// </summary>
        public DateTime? EarthquakeDate { get; set; } = null;
    }

    public class EarthquakeEntity : EarthquakeBaseEntity
    { 
        /// <summary>
        /// 座標、深度圖片
        /// </summary>
        public string FileName1 { get; set; } = string.Empty;


        /// <summary>
        /// 規模圖片
        /// </summary>
        public string FileName2 { get; set; } = string.Empty;
    }
}
