using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class EarthquakeQueryModel
    {
        public DateTimeOffset Sdate { get; set; }

        public DateTimeOffset Edate { get; set; }
    }

    public class EarthquakeViewModel
    {
        public DateTime EarthquakeDate { get; set; }

        public decimal? Latitude { get; set; } = null;

        public decimal? Longitude { get; set; } = null;

        public decimal? MaxDepth { get; set; } = null;

        public decimal? Magnitude { get; set; } = null;

        public string LinkUrl { get; set; } = string.Empty;

        public string ImageFileName { get; set; } = string.Empty;
    }
}
