namespace DataSyncConsoleTools.Twitter
{
    /// <summary>
    /// 媒體下載項目，包含 CSV 所需的完整資訊
    /// </summary>
    public class MediaDownloadItem
    {
        public string Url { get; set; } = "";
        public string Prefix { get; set; } = "";
        public long TweetMsecs { get; set; }
        public string DisplayName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string TweetUrl { get; set; } = "";
        public string MediaType { get; set; } = "";
        public string TweetContent { get; set; } = "";
        public int FavoriteCount { get; set; }
        public int RetweetCount { get; set; }
        public int ReplyCount { get; set; }
    }
}
