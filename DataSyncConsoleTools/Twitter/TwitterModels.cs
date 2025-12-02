using System.Text.Json.Serialization;

namespace DataSyncConsoleTools.Twitter
{
    /// <summary>
    /// Twitter GraphQL API 使用者回應模型
    /// </summary>
    public class TwitterGraphQLUserResponse
    {
        [JsonPropertyName("data")]
        public TwitterGraphQLData? Data { get; set; }
    }

    public class TwitterGraphQLData
    {
        [JsonPropertyName("user")]
        public TwitterGraphQLUser? User { get; set; }
    }

    public class TwitterGraphQLUser
    {
        [JsonPropertyName("result")]
        public TwitterGraphQLResult? Result { get; set; }
    }

    public class TwitterGraphQLResult
    {
        [JsonPropertyName("rest_id")]
        public string? RestId { get; set; }

        [JsonPropertyName("legacy")]
        public TwitterGraphQLLegacy? Legacy { get; set; }

        [JsonPropertyName("timeline_v2")]
        public TwitterGraphQLTimelineV2? TimelineV2 { get; set; }
    }

    public class TwitterGraphQLLegacy
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("screen_name")]
        public string? ScreenName { get; set; }

        [JsonPropertyName("statuses_count")]
        public int StatusesCount { get; set; }

        [JsonPropertyName("media_count")]
        public int MediaCount { get; set; }
    }

    public class TwitterGraphQLTimelineV2
    {
        [JsonPropertyName("timeline")]
        public TwitterGraphQLTimeline? Timeline { get; set; }
    }

    public class TwitterGraphQLTimeline
    {
        [JsonPropertyName("instructions")]
        public List<object>? Instructions { get; set; }
    }
}
