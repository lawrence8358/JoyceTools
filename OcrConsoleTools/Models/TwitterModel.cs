namespace OcrConsoleTools.Models
{
    internal class TwitterInfo
    {
        public string TweetDate { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string TweetUrl { get; set; } = string.Empty;

        public string MediaType { get; set; } = string.Empty;

        public string MediaUrl { get; set; } = string.Empty;

        public string SavedFilename { get; set; } = string.Empty;

        public string TweetContent { get; set; } = string.Empty;

        public int FavoriteCount { get; set; } = 0;

        public int RetweetCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;
    }
}
