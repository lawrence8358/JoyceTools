using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSyncConsoleTools.Models
{
    internal class Options
    {
        [Option('e', "earthquake", Required = false, HelpText = "地震資訊圖片的 OCR 辨識")]
        public bool IsProcessEarthquakeOCRA { get; set; }

        [Option('t', "tide", Required = false, HelpText = "潮汐資料同步")]
        public bool IsProcessTideSync { get; set; }


        [Option('k', "kml", Required = false, HelpText = "產生 KML 檔案")]
        public bool IsGenerateKML { get; set; }

        [Option('w', "twitter", Required = false, HelpText = "Twitter 資料下載")]
        public bool IsProcessTwitterDownload { get; set; }
    }
}
