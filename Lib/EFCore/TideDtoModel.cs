namespace Lib.EFCore
{
    public enum TideLocationType
    {
        Tokyo = 1,
        Sydney = 2,
        Chennai = 3,
        IndianOcean = 4
    };

    public class TideDtoModel
    {
        /// <summary>
        /// 潮水時間 
        /// </summary>
        public DateTimeOffset Date { get; set; }

        /// <summary>
        /// 地點
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 潮水高度 (漲潮為正數，退潮為負數)
        /// </summary>
        public decimal TideHeight { get; set; }
    }
}