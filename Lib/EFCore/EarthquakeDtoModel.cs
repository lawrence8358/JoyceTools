namespace Lib.EFCore
{
    public class EarthquakeDtoModel : EarthquakeBaseEntity
    {
        /// <summary>
        /// 類型，0: 經緯度+深度，1: 震度規模
        /// </summary> 
        public int Type { get; set; } = -1;
         
        /// <summary>
        /// 要解析的圖片
        /// </summary>
        public string FileName { get; set; } = string.Empty;

    }
}
