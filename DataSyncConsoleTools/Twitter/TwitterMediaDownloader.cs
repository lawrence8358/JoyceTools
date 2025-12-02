using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lib.Utilites;

namespace DataSyncConsoleTools.Twitter
{
    /// <summary>
    /// Twitter 媒體下載器（完全依照 Python 版本的邏輯實作）
    /// </summary>
    public class TwitterMediaDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _cookie;
        private readonly string _csrfToken;
        private readonly bool _hasVideo;
        private readonly bool _logOutput;
        private int _requestCount = 0;
        private int _downCount = 0;

        public TwitterMediaDownloader(string cookie, bool hasVideo = false, bool logOutput = true)
        {
            _cookie = cookie;
            _hasVideo = hasVideo;
            _logOutput = logOutput;

            // 從 cookie 中提取 ct0
            var match = Regex.Match(cookie, @"ct0=(.*?);");
            if (!match.Success)
            {
                match = Regex.Match(cookie, @"ct0=(.*)$");
            }
            _csrfToken = match.Success ? match.Groups[1].Value : "";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("authorization", "Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");
            _httpClient.DefaultRequestHeaders.Add("cookie", _cookie);
            _httpClient.DefaultRequestHeaders.Add("x-csrf-token", _csrfToken);
        }

        /// <summary>
        /// 取得使用者資訊（對應 Python 的 get_other_info）
        /// </summary>
        public async Task<(string restId, string name, int statusesCount, int mediaCount)?> GetUserInfoAsync(string screenName)
        {
            var url = $"https://twitter.com/i/api/graphql/xc8f1g7BYqr6VTzTbvNlGw/UserByScreenName?variables={{\"screen_name\":\"{screenName}\",\"withSafetyModeUserFields\":false}}&features={{\"hidden_profile_likes_enabled\":false,\"hidden_profile_subscriptions_enabled\":false,\"responsive_web_graphql_exclude_directive_enabled\":true,\"verified_phone_label_enabled\":false,\"subscriptions_verification_info_verified_since_enabled\":true,\"highlights_tweets_tab_ui_enabled\":true,\"creator_subscriptions_tweet_preview_api_enabled\":true,\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false,\"responsive_web_graphql_timeline_navigation_enabled\":true}}&fieldToggles={{\"withAuxiliaryUserLabels\":false}}";
            url = QuoteUrl(url);

            SetReferer(screenName);

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                _requestCount++;

                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var restId = root.GetProperty("data").GetProperty("user").GetProperty("result").GetProperty("rest_id").GetString();
                var name = root.GetProperty("data").GetProperty("user").GetProperty("result").GetProperty("legacy").GetProperty("name").GetString();
                var statusesCount = root.GetProperty("data").GetProperty("user").GetProperty("result").GetProperty("legacy").GetProperty("statuses_count").GetInt32();
                var mediaCount = root.GetProperty("data").GetProperty("user").GetProperty("result").GetProperty("legacy").GetProperty("media_count").GetInt32();

                return (restId ?? "", name ?? "", statusesCount, mediaCount);
            }
            catch (Exception ex)
            {
                UtilityHelper.ConsoleError($"獲取信息失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 下載使用者媒體（對應 Python 的 get_download_url + download_control）
        /// </summary>
        public async Task<int> DownloadUserMediaAsync(
            string screenName,
            string restId,
            string savePath,
            string userName,
            bool useCache = true,
            CsvGenerator? csvGenerator = null)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            SetReferer(screenName);

            DownloadCache? cache = null;
            if (useCache)
            {
                cache = new DownloadCache(savePath);
            }

            string? cursor = null;
            bool firstPage = true;
            int totalDownloaded = 0;
            int downloadOrder = 0; // 用於檔案編號（對應 Python 的 order）

            try
            {
                while (true)
                {
                    UtilityHelper.ConsoleWriteLine($"已下載圖片/影片:{totalDownloaded}");

                    // 呼叫 UserMedia API
                    var (mediaItems, nextCursor) = await GetDownloadUrlAsync(restId, cursor, firstPage, userName, screenName);
                    if (mediaItems == null || mediaItems.Count == 0)
                    {
                        break;
                    }

                    // 下載所有媒體
                    foreach (var mediaItem in mediaItems)
                    {
                        // 檢查 cache
                        if (cache != null && !cache.IsNew(mediaItem.Url))
                        {
                            if (_logOutput)
                            {
                                UtilityHelper.ConsoleWriteLine($"跳過已下載: {Path.GetFileName(mediaItem.Url)}");
                            }
                            downloadOrder++; // cache 也要計數，保持編號連續
                            continue;
                        }

                        var timeStr = Stamp2Time(mediaItem.TweetMsecs);
                        // 加入 order 編號以處理同一推文多張圖片的情況
                        var fileName = $"{timeStr}-{mediaItem.Prefix}_{downloadOrder}.{(mediaItem.Url.Contains(".mp4") ? "mp4" : "jpg")}";
                        var filePath = Path.Combine(savePath, fileName);

                        if (await DownloadFileAsync(mediaItem.Url, filePath))
                        {
                            totalDownloaded++;
                            _downCount++;

                            // 寫入 CSV
                            csvGenerator?.WriteMediaRecord(
                                mediaItem.TweetMsecs,
                                mediaItem.DisplayName,
                                mediaItem.UserName,
                                mediaItem.TweetUrl,
                                mediaItem.MediaType,
                                mediaItem.Url,
                                fileName,
                                mediaItem.TweetContent,
                                mediaItem.FavoriteCount,
                                mediaItem.RetweetCount,
                                mediaItem.ReplyCount
                            );
                        }

                        // 無論下載成功或失敗，都要增加編號以保持順序
                        downloadOrder++;
                    }

                    if (string.IsNullOrEmpty(nextCursor))
                    {
                        break;
                    }

                    cursor = nextCursor;
                    firstPage = false;
                }
            }
            finally
            {
                cache?.Dispose();
            }

            return totalDownloaded;
        }

        /// <summary>
        /// 取得下載 URL 列表（對應 Python 的 get_download_url）
        /// </summary>
        private async Task<(List<MediaDownloadItem>? mediaItems, string? nextCursor)> GetDownloadUrlAsync(
            string restId,
            string? cursor,
            bool firstPage,
            string userName,
            string screenName)
        {
            // UserMedia API
            var urlTop = $"https://twitter.com/i/api/graphql/Le6KlbilFmSu-5VltFND-Q/UserMedia?variables={{\"userId\":\"{restId}\",\"count\":500,";
            var urlBottom = "\"includePromotedContent\":false,\"withClientEventToken\":false,\"withBirdwatchNotes\":false,\"withVoice\":true,\"withV2Timeline\":true}&features={\"responsive_web_graphql_exclude_directive_enabled\":true,\"verified_phone_label_enabled\":false,\"creator_subscriptions_tweet_preview_api_enabled\":true,\"responsive_web_graphql_timeline_navigation_enabled\":true,\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false,\"tweetypie_unmention_optimization_enabled\":true,\"responsive_web_edit_tweet_api_enabled\":true,\"graphql_is_translatable_rweb_tweet_is_translatable_enabled\":true,\"view_counts_everywhere_api_enabled\":true,\"longform_notetweets_consumption_enabled\":true,\"responsive_web_twitter_article_tweet_consumption_enabled\":false,\"tweet_awards_web_tipping_enabled\":false,\"freedom_of_speech_not_reach_fetch_enabled\":true,\"standardized_nudges_misinfo\":true,\"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled\":true,\"longform_notetweets_rich_text_read_enabled\":true,\"longform_notetweets_inline_media_enabled\":true,\"responsive_web_media_download_video_enabled\":false,\"responsive_web_enhance_cards_enabled\":false}";

            string url;
            if (!string.IsNullOrEmpty(cursor))
            {
                url = urlTop + $"\"cursor\":\"{cursor}\"," + urlBottom;
            }
            else
            {
                url = urlTop + urlBottom;
            }

            url = QuoteUrl(url);

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                _requestCount++;

                var jsonNode = JsonNode.Parse(response);
                var instructions = jsonNode?["data"]?["user"]?["result"]?["timeline_v2"]?["timeline"]?["instructions"] as JsonArray;

                if (instructions == null)
                {
                    return (null, null);
                }

                JsonArray? rawData = null;
                string? nextCursor = null;

                // Python: if not has_retweet and not has_highlights (我們是 UserMedia 模式)
                if (firstPage)
                {
                    // 第一頁特殊處理
                    // Python: raw_data = raw_data[-1]['entries'][0]['content']['items']
                    var lastInst = instructions[instructions.Count - 1];
                    var entries = lastInst?["entries"] as JsonArray;
                    if (entries != null && entries.Count > 0)
                    {
                        // 取第一個 entry 的 content.items
                        rawData = entries[0]?["content"]?["items"] as JsonArray;

                        // 找 cursor-bottom （在 entries 中找）
                        foreach (var entry in entries)
                        {
                            var entryId = entry?["entryId"]?.GetValue<string>();
                            if (entryId != null && entryId.Contains("bottom"))
                            {
                                nextCursor = entry["content"]?["value"]?.GetValue<string>();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // 後續頁面
                    var firstInst = instructions[0];
                    if (firstInst?["moduleItems"] != null)
                    {
                        rawData = firstInst["moduleItems"] as JsonArray;

                        // 找 cursor
                        var lastInst = instructions[instructions.Count - 1];
                        var entries = lastInst?["entries"] as JsonArray;
                        if (entries != null)
                        {
                            foreach (var entry in entries)
                            {
                                var entryId = entry?["entryId"]?.GetValue<string>();
                                if (entryId != null && entryId.Contains("bottom"))
                                {
                                    nextCursor = entry["content"]?["value"]?.GetValue<string>();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 沒有 moduleItems，結束
                        return (null, null);
                    }
                }

                if (rawData == null)
                {
                    return (null, null);
                }

                // 解析 media items
                var mediaItems = GetUrlFromContent(rawData, userName, screenName);
                return (mediaItems, nextCursor);
            }
            catch (Exception ex)
            {
                UtilityHelper.ConsoleError($"獲取推文信息錯誤: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// 從 content 中提取媒體資訊（對應 Python 的 get_url_from_content）
        /// </summary>
        private List<MediaDownloadItem> GetUrlFromContent(JsonArray content, string userName, string screenName)
        {
            var mediaList = new List<MediaDownloadItem>();

            foreach (var item in content)
            {
                try
                {
                    var tweetResults = item?["item"]?["itemContent"]?["tweet_results"]?["result"];
                    if (tweetResults == null) continue;

                    JsonNode? legacy;
                    long tweetMsecs;

                    if (tweetResults["tweet"] != null)
                    {
                        legacy = tweetResults["tweet"]?["legacy"];
                        var msecStr = tweetResults["tweet"]?["edit_control"]?["editable_until_msecs"]?.GetValue<string>();
                        tweetMsecs = string.IsNullOrEmpty(msecStr) ? 0 : long.Parse(msecStr);
                    }
                    else
                    {
                        legacy = tweetResults["legacy"];
                        var msecStr = tweetResults["edit_control"]?["editable_until_msecs"]?.GetValue<string>();
                        tweetMsecs = string.IsNullOrEmpty(msecStr) ? 0 : long.Parse(msecStr);
                    }

                    if (tweetMsecs == 0) continue;

                    tweetMsecs -= 3600000; // 減 1 小時

                    if (legacy == null) continue;

                    var extendedEntities = legacy["extended_entities"];
                    if (extendedEntities == null) continue;

                    var mediaArray = extendedEntities["media"] as JsonArray;
                    if (mediaArray == null) continue;

                    // 取得推文相關資訊
                    var fullText = legacy["full_text"]?.GetValue<string>() ?? "";
                    var favoriteCount = legacy["favorite_count"]?.GetValue<int>() ?? 0;
                    var retweetCount = legacy["retweet_count"]?.GetValue<int>() ?? 0;
                    var replyCount = legacy["reply_count"]?.GetValue<int>() ?? 0;
                    var expandedUrl = mediaArray[0]?["expanded_url"]?.GetValue<string>() ?? "";

                    foreach (var media in mediaArray)
                    {
                        var type = media?["type"]?.GetValue<string>();

                        if (type == "photo")
                        {
                            var mediaUrl = media?["media_url_https"]?.GetValue<string>();
                            if (mediaUrl != null)
                            {
                                mediaList.Add(new MediaDownloadItem
                                {
                                    Url = mediaUrl + "?name=orig",
                                    Prefix = "img",
                                    TweetMsecs = tweetMsecs,
                                    DisplayName = userName,
                                    UserName = $"@{screenName}",
                                    TweetUrl = expandedUrl,
                                    MediaType = "Image",
                                    TweetContent = fullText,
                                    FavoriteCount = favoriteCount,
                                    RetweetCount = retweetCount,
                                    ReplyCount = replyCount
                                });
                            }
                        }
                        else if ((type == "video" || type == "animated_gif") && _hasVideo)
                        {
                            var videoInfo = media?["video_info"];
                            var variants = videoInfo?["variants"] as JsonArray;
                            if (variants != null)
                            {
                                var bestUrl = GetHighestVideoQuality(variants);
                                if (bestUrl != null)
                                {
                                    mediaList.Add(new MediaDownloadItem
                                    {
                                        Url = bestUrl,
                                        Prefix = "vid",
                                        TweetMsecs = tweetMsecs,
                                        DisplayName = userName,
                                        UserName = $"@{screenName}",
                                        TweetUrl = expandedUrl,
                                        MediaType = "Video",
                                        TweetContent = fullText,
                                        FavoriteCount = favoriteCount,
                                        RetweetCount = retweetCount,
                                        ReplyCount = replyCount
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return mediaList;
        }

        /// <summary>
        /// 取得最高畫質影片（對應 Python 的 get_heighest_video_quality）
        /// </summary>
        private string? GetHighestVideoQuality(JsonArray variants)
        {
            if (variants.Count == 1)
            {
                return variants[0]?["url"]?.GetValue<string>();
            }

            int maxBitrate = 0;
            string? highestUrl = null;

            foreach (var variant in variants)
            {
                var bitrateNode = variant?["bitrate"];
                if (bitrateNode != null)
                {
                    var bitrate = bitrateNode.GetValue<int>();
                    if (bitrate > maxBitrate)
                    {
                        maxBitrate = bitrate;
                        highestUrl = variant["url"]?.GetValue<string>();
                    }
                }
            }

            return highestUrl;
        }

        /// <summary>
        /// 下載檔案
        /// </summary>
        private async Task<bool> DownloadFileAsync(string url, string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return false;
                }

                var data = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, data);

                if (_logOutput)
                {
                    UtilityHelper.ConsoleWriteLine($"{Path.GetFileName(filePath)}=====>下載完成");
                }

                return true;
            }
            catch (Exception ex)
            {
                UtilityHelper.ConsoleError($"下載失敗 {url}: {ex.Message}");
                return false;
            }
        }

        private void SetReferer(string screenName)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("referer"))
            {
                _httpClient.DefaultRequestHeaders.Remove("referer");
            }
            _httpClient.DefaultRequestHeaders.Add("referer", $"https://twitter.com/{screenName}");
        }

        private string QuoteUrl(string url)
        {
            return url.Replace("{", "%7B").Replace("}", "%7D");
        }

        private string Stamp2Time(long msecsStamp)
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(msecsStamp).DateTime.ToLocalTime();
            return dateTime.ToString("yyyy-MM-dd HH-mm");
        }

        public int RequestCount => _requestCount;
        public int DownCount => _downCount;
    }
}
