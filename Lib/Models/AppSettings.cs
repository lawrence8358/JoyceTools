
namespace Lib.Models
{
    public class AppSettings
    {
        public string TwitterDownloadDir { get; set; } = string.Empty;

        public string SQLitePath { get; set; } = string.Empty;

        public string TwitterCookie { get; set; } = string.Empty;

        public string TwitterUserAccount { get; set; } = string.Empty;

        public bool TwitterDownloadHasVideo { get; set; } = false;

        public bool TwitterDownloadDownLog { get; set; } = true;

        public bool LogOutput { get; set; } = true;
    }
}
