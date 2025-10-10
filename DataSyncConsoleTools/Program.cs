using CommandLine;
using DataSyncConsoleTools.Models;
using DataSyncConsoleTools.Services;
using Lib.Utilites;
using SharpKml.Engine;

namespace DataSyncConsoleTools
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var config = UtilityHelper.GetConfig();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(async (Options opts) =>
                {
                    if (opts.IsProcessEarthquakeOCRA)
                    {
                        var earthquakeService = new EarthquakeSyncService(config);
                        earthquakeService.ProcessEarthquakeOCRAndSaveToDb();
                        // earthquakeService.DebugEarthquakeOCR(); 
                    }

                    if (opts.IsProcessTideSync)
                    {
                        var tideService = new TideSyncService(config);
                        tideService.ProcessTideData();
                    }

                    if (opts.IsGenerateKML)
                    {
                        var kmlJsonUrl = @"https://drive.google.com/uc?export=view&id=1jtTuL2ZQPw4eePq-33y91uSUYKhTTgFu";
                        var saveKmlFile = @"D:\KMLDemo.kml";

                        // var kmlDataSource = KmlHelper.GetMockData();
                        var kmlDataSource = KmlHelper.LoadDataSourceFromJsonUrl(kmlJsonUrl);

                        var bytes = KmlHelper.GenerateKml(kmlDataSource);
                        await System.IO.File.WriteAllBytesAsync(saveKmlFile, bytes);
                        UtilityHelper.ConsoleWriteLine($"KML 檔案已產生，請查看 {saveKmlFile}");
                    }
                });
        }

    }
}

