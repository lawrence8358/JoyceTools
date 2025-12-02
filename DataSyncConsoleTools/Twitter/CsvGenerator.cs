using System.Text;

namespace DataSyncConsoleTools.Twitter
{
    /// <summary>
    /// CSV 生成器（對應 Python 的 csv_gen）
    /// </summary>
    public class CsvGenerator : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly string _filePath;

        public string FilePath
        {
            get { return _filePath; }
        }

        public CsvGenerator(string savePath, string userName, string screenName, string tweetRange)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _filePath = Path.Combine(savePath, $"{screenName}-{timestamp}.csv");

            // 確保目錄存在
            Directory.CreateDirectory(savePath);

            // 使用 UTF-8 with BOM（對應 Python 的 utf-8-sig）
            _writer = new StreamWriter(_filePath, false, new UTF8Encoding(true));

            // 初始化 CSV 標頭
            WriteRow(new[] { userName, screenName });
            WriteRow(new[] { $"Tweet Range : {tweetRange}" });
            WriteRow(new[] { $"Save Path : {savePath}" });
            WriteRow(new[] {
                "Tweet Date", "Display Name", "User Name", "Tweet URL", "Media Type",
                "Media URL", "Saved Filename", "Tweet Content", "Favorite Count",
                "Retweet Count", "Reply Count"
            });
        }

        public void WriteMediaRecord(
            long tweetMsecs,
            string displayName,
            string userName,
            string tweetUrl,
            string mediaType,
            string mediaUrl,
            string savedFilename,
            string tweetContent,
            int favoriteCount,
            int retweetCount,
            int replyCount)
        {
            var tweetDate = DateTimeOffset.FromUnixTimeMilliseconds(tweetMsecs).DateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            WriteRow(new[] {
                tweetDate,
                displayName,
                userName,
                tweetUrl,
                mediaType,
                mediaUrl,
                savedFilename,
                EscapeCsvField(tweetContent),
                favoriteCount.ToString(),
                retweetCount.ToString(),
                replyCount.ToString()
            });
        }

        private void WriteRow(string[] fields)
        {
            var line = string.Join(",", fields.Select(f => $"\"{f?.Replace("\"", "\"\"")}\""));
            _writer.WriteLine(line);
            _writer.Flush(); // 即時寫入
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // 移除換行符號和特殊字元
            return field.Replace("\r", "").Replace("\n", " ").Trim();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
