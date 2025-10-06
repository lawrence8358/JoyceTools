using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lib.EFCore
{
    [PrimaryKey(nameof(Date), nameof(Location))]
    public class TideEntity
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 地點
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 第一潮水高度 (漲潮為正數，退潮為負數)
        /// </summary>
        public decimal? FirstTideHeight { get; set; }

        /// <summary>
        /// 第一潮水時間
        /// </summary>
        public DateTime? FirstTideTime { get; set; }

        /// <summary>
        /// 第二潮水高度 (漲潮為正數，退潮為負數)
        /// </summary>
        public decimal? SecondTideHeight { get; set; }

        /// <summary>
        /// 第二潮水時間
        /// </summary>
        public DateTime? SecondTideTime { get; set; }

        /// <summary>
        /// 第三潮水高度 (漲潮為正數，退潮為負數)
        /// </summary>
        public decimal? ThirdTideHeight { get; set; }

        /// <summary>
        /// 第三潮水時間
        /// </summary>
        public DateTime? ThirdTideTime { get; set; }

        /// <summary>
        /// 第四潮水高度 (漲潮為正數，退潮為負數)
        /// </summary>
        public decimal? FourthTideHeight { get; set; }

        /// <summary>
        /// 第四潮水時間
        /// </summary>
        public DateTime? FourthTideTime { get; set; }

        /// <summary>
        /// 建立日期時間
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// 修改日期時間
        /// </summary>
        public DateTimeOffset ModifiedDate { get; set; } = DateTimeOffset.Now;
    }
}