using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class TideQueryModel
    {
        /// <summary>
        /// 開始日期 (UTC from frontend, 可選)
        /// </summary>
        public DateTimeOffset? Sdate { get; set; }

        /// <summary>
        /// 結束日期 (UTC from frontend, 可選)
        /// </summary>
        public DateTimeOffset? Edate { get; set; }
    }

    public class LocationTideInfo
    {
        /// <summary>
        /// 第一次漲潮高度
        /// </summary>
        public decimal? FirstHighTideHeight { get; set; }

        /// <summary>
        /// 第一次漲潮時間
        /// </summary>
        public DateTime? FirstHighTideTime { get; set; }

        /// <summary>
        /// 第二次漲潮高度
        /// </summary>
        public decimal? SecondHighTideHeight { get; set; }

        /// <summary>
        /// 第二次漲潮時間
        /// </summary>
        public DateTime? SecondHighTideTime { get; set; }
    }

    public class TideViewModel
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 東京潮汐資訊
        /// </summary>
        public LocationTideInfo? Tokyo { get; set; }

        /// <summary>
        /// 雪梨潮汐資訊
        /// </summary>
        public LocationTideInfo? Sydney { get; set; }

        /// <summary>
        /// 印度清奈潮汐資訊
        /// </summary>
        public LocationTideInfo? Chennai { get; set; }

        /// <summary>
        /// 印度洋潮汐資訊
        /// </summary>
        public LocationTideInfo? IndianOcean { get; set; }
    }
}