namespace DataSyncConsoleTools.Models
{
    internal class EarthquakeTempModel
    {
        public DateTime Date { get; set; }

        public string Content { get; set; } = string.Empty;

        public string LinkUrl { get; set; } = string.Empty;

        public List<string> FileNames { get; set; } = [];
    }
}
