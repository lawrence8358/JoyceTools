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
        [Option('o', "type1", Required = false, HelpText = "地震資訊圖片的 OCR 辨識")]
        public bool IsProcessEarthquakeOCRA { get; set; } 
    }
}
