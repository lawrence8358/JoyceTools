using System.Text.Json;

namespace DataSyncConsoleTools.Twitter
{
    /// <summary>
    /// Cache 管理器（對應 Python 的 cache_gen）
    /// 使用 JSON 格式儲存已下載的 URL，避免重複下載
    /// </summary>
    public class DownloadCache : IDisposable
    {
        private readonly string _cachePath;
        private HashSet<string> _cacheData;

        public DownloadCache(string savePath)
        {
            _cachePath = Path.Combine(savePath, "cache_data.json");
            
            // 載入現有的 cache
            if (File.Exists(_cachePath))
            {
                try
                {
                    var json = File.ReadAllText(_cachePath);
                    _cacheData = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
                }
                catch
                {
                    _cacheData = new HashSet<string>();
                }
            }
            else
            {
                _cacheData = new HashSet<string>();
            }
        }

        /// <summary>
        /// 檢查 URL 是否已存在，如果不存在則加入 cache
        /// </summary>
        /// <param name="url">媒體 URL</param>
        /// <returns>true 表示是新的（應該下載），false 表示已存在（跳過）</returns>
        public bool IsNew(string url)
        {
            if (_cacheData.Contains(url))
            {
                return false; // 已存在，不應下載
            }
            else
            {
                _cacheData.Add(url);
                return true; // 是新的，應該下載
            }
        }

        public void Add(string url)
        {
            _cacheData.Add(url);
        }

        public void Dispose()
        {
            // 儲存 cache 到檔案
            try
            {
                var json = JsonSerializer.Serialize(_cacheData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_cachePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"儲存 cache 失敗: {ex.Message}");
            }
        }
    }
}
