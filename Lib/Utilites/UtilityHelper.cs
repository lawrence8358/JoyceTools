using Lib.Models;
using Microsoft.Extensions.Configuration;

namespace Lib.Utilites
{
    public static class UtilityHelper
    {
        public static AppSettings GetConfig()
        {
            Console.WriteLine("GetConfig.");

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Console.WriteLine("GetConfig done.\n");

            var appSetting = new AppSettings();
            config.Bind(appSetting);
            return appSetting;
        }

        public static void ConsoleWriteLine(string message, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void ConsoleError(string message)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
