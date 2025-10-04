using CommandLine;
using DataSyncConsoleTools.Models;
using DataSyncConsoleTools.Services; 
using Lib.Utilites;

namespace DataSyncConsoleTools
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var config = UtilityHelper.GetConfig();
            
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed((Options opts) =>
                {
                    if (opts.IsProcessEarthquakeOCRA)
                    {
                        var earthquakeService = new EarthquakeSyncService(config);
                        earthquakeService.ProcessEarthquakeOCRAndSaveToDb();
                        // earthquakeService.DebugEarthquakeOCR(); 
                    }
                });
        }

    }
}
