using DataSyncConsoleTools.Twitter;
using Lib.Utilites;

namespace TwitterDownloader.Tests;

public class TwitterApiTest
{
    private const string Cookie = "auth_token=AUTH_TOKEN; ct0=COOKIE_VALUE;";
    private const string ScreenName = "cwaeew84024";

    [Fact]
    public async Task GetUserInfo_ShouldReturnValidUserData()
    {
        // Arrange
        var downloader = new TwitterMediaDownloader(Cookie);
        
        // Act
        var result = await downloader.GetUserInfoAsync(ScreenName);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Value.restId);
        Assert.NotEmpty(result.Value.name);
        
        Console.WriteLine($"User: {result.Value.name}");
        Console.WriteLine($"RestId: {result.Value.restId}");
        Console.WriteLine($"Statuses: {result.Value.statusesCount}");
        Console.WriteLine($"Media: {result.Value.mediaCount}");
    }

    [Fact]
    public async Task DownloadUserMedia_ShouldDownloadAtLeastOneImage()
    {
        // Arrange
        var savePath = Path.Combine(Path.GetTempPath(), "TwitterTestRun");
        if (Directory.Exists(savePath))
        {
            Directory.Delete(savePath, true);
        }
        
        var downloader = new TwitterMediaDownloader(Cookie, hasVideo: false, logOutput: true);
        
        // Act - Get user info first
        var userInfo = await downloader.GetUserInfoAsync(ScreenName);
        Assert.NotNull(userInfo);
        
        Console.WriteLine($"Downloading media for user: {userInfo.Value.name}");
        
        int downloadCount;
        // Create CSV generator for test (in its own scope to ensure disposal)
        using (var csvGenerator = new CsvGenerator(savePath, userInfo.Value.name, ScreenName, "1990-01-01:2030-01-01"))
        {
            // Act - Download media with CSV and cache
            downloadCount = await downloader.DownloadUserMediaAsync(
                ScreenName, 
                userInfo.Value.restId, 
                savePath,
                userInfo.Value.name,
                useCache: true,
                csvGenerator: csvGenerator
            );
        }
        
        // Assert
        Console.WriteLine($"Downloaded {downloadCount} files");
        Assert.True(downloadCount > 0, $"Expected at least 1 download, but got {downloadCount}");
        
        var files = Directory.GetFiles(savePath);
        Assert.NotEmpty(files);
        
        // Check that CSV was created (filename includes timestamp)
        var csvFiles = Directory.GetFiles(savePath, $"{ScreenName}-*.csv");
        Assert.NotEmpty(csvFiles);
        
        // Check that cache was created
        var cacheFilePath = Path.Combine(savePath, "cache_data.json");
        Assert.True(File.Exists(cacheFilePath), "Cache file should have been created");
        
        foreach (var file in files.Take(5))
        {
            var fileInfo = new FileInfo(file);
            Console.WriteLine($"Downloaded: {fileInfo.Name} ({fileInfo.Length} bytes)");
        }
        
        // Cleanup
        if (Directory.Exists(savePath))
        {
            Directory.Delete(savePath, true);
        }
    }
}
