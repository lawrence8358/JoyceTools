using CommandLine;
using DataSyncConsoleTools.Models;
using DataSyncConsoleTools.Services;
using Lib.Utilites;

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
                });
        }

    }
}
