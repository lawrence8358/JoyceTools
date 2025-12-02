using DataSyncConsoleTools.Twitter;
using Lib.Models;
using Lib.Utilites;

namespace DataSyncConsoleTools.Utilites
{
    public static class TwitterDownloadHelper
    {
        public static async Task DownloadProfilesAsync(AppSettings config)
        {
            if (string.IsNullOrEmpty(config.TwitterCookie))
            {
                UtilityHelper.ConsoleError("Twitter Cookie is missing in configuration.");
                return;
            }

            if (string.IsNullOrEmpty(config.TwitterUserAccount))
            {
                UtilityHelper.ConsoleError("Twitter User List is missing in configuration.");
                return;
            }

            var users = config.TwitterUserAccount.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var savePath = config.TwitterDownloadDir;
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(Directory.GetCurrentDirectory(), "TwitterPost");
            }

            var downloader = new TwitterMediaDownloader(
                config.TwitterCookie, 
                config.TwitterDownloadHasVideo, 
                config.LogOutput
            );

            foreach (var user in users)
            {
                var screenName = user.Trim();
                UtilityHelper.ConsoleWriteLine($"\n正在獲取用戶: {screenName}");
                
                // Get user info
                var userInfo = await downloader.GetUserInfoAsync(screenName);
                if (userInfo == null)
                {
                    UtilityHelper.ConsoleError($"Failed to get user info for {screenName}");
                    continue;
                }

                // Print user info
                PrintUserInfo(userInfo.Value.name, screenName, userInfo.Value.restId, userInfo.Value.statusesCount, userInfo.Value.mediaCount);

                var userPath = Path.Combine(savePath, screenName);
                
                // 建立 CSV Generator (使用 1990-2030 時間範圍涵蓋所有推文)
                using var csvGenerator = new CsvGenerator(userPath, userInfo.Value.name, screenName, "1990-01-01:2030-01-01");
                
                // Download Media with CSV and cache support
                var downloadCount = await downloader.DownloadUserMediaAsync(
                    screenName, 
                    userInfo.Value.restId, 
                    userPath,
                    userInfo.Value.name,
                    useCache: true,
                    csvGenerator: csvGenerator
                );
                
                // 如果沒有下載的項目，刪除 csv
                if(downloadCount == 0)
                {
                    var csvFilePath = csvGenerator.FilePath; 
                    csvGenerator.Dispose();
                    if (File.Exists(csvFilePath)) File.Delete(csvFilePath); 
                    UtilityHelper.ConsoleWriteLine("沒有下載任何新媒體，已刪除空的 CSV 檔案。");
                }

                UtilityHelper.ConsoleWriteLine($"\n下載完成: {downloadCount} 個檔案");
            }
            
            UtilityHelper.ConsoleWriteLine($"\n總 API 請求次數: {downloader.RequestCount}");
            UtilityHelper.ConsoleWriteLine($"總下載次數: {downloader.DownCount}");
        }

        private static void PrintUserInfo(string name, string screenName, string restId, int statusesCount, int mediaCount)
        {
            UtilityHelper.ConsoleWriteLine($@"
        <======基本信息=====>
        昵稱:{name}
        用戶名:{screenName}
        數字ID:{restId}
        總推數(含轉推):{statusesCount}
        含圖片/影片/音頻推數(不含轉推):{mediaCount}
        <==================>
        開始爬取...");
        }
    }
}
