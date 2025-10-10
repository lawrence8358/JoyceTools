namespace Lib.Models
{
    public class KmlModel
    {
        public DateTimeOffset Date { get; set; }

        public string SunLocation { get; set; } = "";

        public string MoonLocation { get; set; } = "";

        public string SunData { get; set; } = "";

        public string MoonData { get; set; } = "";

        public string Url { get; set; } = "";
    }
}
