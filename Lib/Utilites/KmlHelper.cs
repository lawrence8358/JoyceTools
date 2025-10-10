using Lib.Models;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Utilites
{
    public class KmlHelper
    {
        #region Public Methods 

        public static byte[] GenerateKml(List<KmlModel> dataSources)
        {
            UtilityHelper.ConsoleWriteLine("產生 KML 檔案，預計筆數: " + dataSources.Count);

            var document = new Document();
            var kml = new Kml();

            Style markerRedStyle = GetMarkerStyle(iconId: "icon_red", iconUrl: "https://earth.google.com/earth/document/icon?color=d32f2f&id=2000&scale=4", lineColor: "ff2dc0fb", polygonColor: "40ffffff");
            Style markerYellowStyle = GetMarkerStyle(iconId: "icon_yellow", iconUrl: "https://earth.google.com/earth/document/icon?color=fbc02d&id=2000&scale=4", lineColor: "ff2dc0fb", polygonColor: "40ffffff");
            document.AddStyle(markerRedStyle);
            document.AddStyle(markerYellowStyle);


            //var result1 = GPSToLatLng("22°36'25\"N,120°18'38\"E"); // (22.6069444, 120.3105556) 
            //Console.WriteLine($"Lat: {result1.lat}, Lng: {result1.lng}");

            //Placemark placemark1 = GetMarker(name: "沱江街", iconId: "icon_red", location: new Vector(22.6071862, 120.3107958));
            //Placemark placemark2 = GetMarker(name: "八五大樓", iconId: "icon_yellow", location: new Vector(22.6120559, 120.2972703));
            //document.AddFeature(placemark1);
            //document.AddFeature(placemark2);

            foreach (var item in dataSources)
            {
                var sunLocation = GPSToLatLng(item.SunLocation);
                var moonLocation = GPSToLatLng(item.MoonLocation);

                Placemark sunMarker = GetMarker(
                    name: item.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " (日)",
                    iconId: "icon_red",
                    location: new Vector(sunLocation.lat, sunLocation.lng)
                );
                Placemark moonMarker = GetMarker(
                    name: item.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " (月)",
                    iconId: "icon_yellow",
                    location: new Vector(moonLocation.lat, moonLocation.lng)
                );

                document.AddFeature(sunMarker);
                document.AddFeature(moonMarker);
            }

            kml.Feature = document;
            KmlFile kmlFile = KmlFile.Create(kml, false);

            using MemoryStream memStream = new();
            kmlFile.Save(memStream);

            var data = memStream.ToArray();
            return data;
        }

        // 透過 Google Driver 讀取 JSON 檔案 
        public static List<KmlModel> LoadDataSourceFromJsonUrl(string url)
        {
            try
            {
                using HttpClient client = new();
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    UtilityHelper.ConsoleError($"讀取 KML 資料檔案失敗: {url}，HTTP 狀態碼: {response.StatusCode}");
                    return new List<KmlModel>();
                }

                var json = response.Content.ReadAsStringAsync().Result;
                var list = System.Text.Json.JsonSerializer.Deserialize<List<KmlModel>>(json);
                Utilites.UtilityHelper.ConsoleWriteLine($"讀取 KML 資料檔案: {url}，筆數: {list?.Count ?? 0}");
                return list ?? new List<KmlModel>();
            }
            catch (Exception ex)
            {
                UtilityHelper.ConsoleError($"讀取 KML 資料檔案失敗: {url}，錯誤訊息: {ex.Message}");
                return new List<KmlModel>();
            }
        }

        public static List<KmlModel> LoadDataSourceFromJsonFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                UtilityHelper.ConsoleError($"找不到 KML 資料檔案: {filePath}");
                return new List<KmlModel>();
            }

            var json = File.ReadAllText(filePath);
            var list = System.Text.Json.JsonSerializer.Deserialize<List<KmlModel>>(json);

            Utilites.UtilityHelper.ConsoleWriteLine($"讀取 KML 資料檔案: {filePath}，筆數: {list?.Count ?? 0}");
            return list ?? new List<KmlModel>();
        }

        public static List<KmlModel> GetMockData()
        {
            var result = @"[
    {
        ""Date"": ""2024-09-30T16:00:00.000Z"",
        ""SunLocation"": ""3°10'S,62°33'W"",
        ""MoonLocation"": ""7°41'N,82°54'W"",
        ""SunData"": ""On Monday, 30 September 2024, 16:00:00 UTC the Sun is at its zenith at Latitude:\\n3° 10' South, Longitude: 62° 33' West"",
        ""MoonData"": ""On Monday, 30 September 2024, 16:00:00 UTC the Moon is at its zenith at\\nLatitude: 7° 41' North, Longitude: 82° 54' West"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=1&month=10&year=2024&hour=0&min=0&sec=0&n=241&ntxt=Taipei&earth=0""
    },
    {
        ""Date"": ""2024-10-01T04:00:00.000Z"",
        ""SunLocation"": ""3°22'S,117°24'E"",
        ""MoonLocation"": ""4°54'N,101°53'E"",
        ""SunData"": ""On Tuesday, 1 October 2024, 04:00:00 UTC the Sun is at its zenith at Latitude:\\n3° 22' South, Longitude: 117° 24' East"",
        ""MoonData"": ""On Tuesday, 1 October 2024, 04:00:00 UTC the Moon is at its zenith at Latitude:\\n4° 54' North, Longitude: 101° 53' East"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=1&month=10&year=2024&hour=12&min=0&sec=0&n=241&ntxt=Taipei&earth=0""
    },
    {
        ""Date"": ""2024-10-01T16:00:00.000Z"",
        ""SumData"": ""On Tuesday, 1 October 2024, 16:00:00 UTC the Sun is at its zenith at Latitude:\n3° 33' South, Longitude: 62° 38' West"",
        ""MoonData: "": ""On Tuesday, 1 October 2024, 16:00:00 UTC the Moon is at its zenith at Latitude:\n2° 04' North, Longitude: 73° 23' West"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=2&month=10&year=2024&hour=0&min=0&sec=0&n=241&ntxt=Taipei&earth=0"",
        ""SunLocation"": ""3°33' S,62°38' W"",
        ""MoonLocation"": ""2°04' N,73°23' W""
    },
    {
        ""Date"": ""2024-10-02T04:00:00.000Z"",
        ""SumData"": ""On Wednesday, 2 October 2024, 04:00:00 UTC the Sun is at its zenith at Latitude:\n3° 45' South, Longitude: 117° 19' East"",
        ""MoonData: "": ""On Wednesday, 2 October 2024, 04:00:00 UTC the Moon is at its zenith at\nLatitude: 0° 47' South, Longitude: 111° 19' East"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=2&month=10&year=2024&hour=12&min=0&sec=0&n=241&ntxt=Taipei&earth=0"",
        ""SunLocation"": ""3°45' S,117°19' E"",
        ""MoonLocation"": ""0°47' S,111°19' E""
    },
    {
        ""Date"": ""2024-10-02T16:00:00.000Z"",
        ""SunLocation"": ""3°57' S,62°43' W"",
        ""MoonLocation"": ""3°37' S,63°58' W"",
        ""SunData"": ""On Wednesday, 2 October 2024, 16:00:00 UTC the Sun is at its zenith at Latitude:\n3° 57' South, Longitude: 62° 43' West"",
        ""MoonData"": ""On Wednesday, 2 October 2024, 16:00:00 UTC the Moon is at its zenith at\nLatitude: 3° 37' South, Longitude: 63° 58' West"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=3&month=10&year=2024&hour=0&min=0&sec=0&n=241&ntxt=Taipei&earth=0""
    },
    {
        ""Date"": ""2024-10-03T04:00:00.000Z"",
        ""SunLocation"": ""4°08' S,117°15' E"",
        ""MoonLocation"": ""6°25' S,120°46' E"",
        ""SunData"": ""On Thursday, 3 October 2024, 04:00:00 UTC the Sun is at its zenith at Latitude:\n4° 08' South, Longitude: 117° 15' East"",
        ""MoonData"": ""On Thursday, 3 October 2024, 04:00:00 UTC the Moon is at its zenith at Latitude:\n6° 25' South, Longitude: 120° 46' East"",
        ""Url"": ""https://www.timeanddate.com/worldclock/sunearth.html?day=3&month=10&year=2024&hour=12&min=0&sec=0&n=241&ntxt=Taipei&earth=0""
    }
]";

            var list = System.Text.Json.JsonSerializer.Deserialize<List<KmlModel>>(result);
            return list ?? new List<KmlModel>();
        }

        #endregion

        #region Private Methods

        private static Placemark GetMarker(string name, string iconId, Vector location)
        {
            var placemark = new Placemark();
            placemark.Name = name;
            placemark.StyleUrl = new Uri($"#{iconId}", UriKind.Relative);
            placemark.Geometry = new SharpKml.Dom.Point
            {
                Coordinate = location
            };
            return placemark;
        }

        private static Style GetMarkerStyle(string iconId, string iconUrl, string lineColor, string polygonColor)
        {
            return new Style
            {
                Id = iconId,
                Icon = new IconStyle
                {
                    Scale = 2.4,
                    Icon = new IconStyle.IconLink(new Uri(iconUrl)),
                    Hotspot = new Hotspot
                    {
                        X = 64,
                        Y = 128,
                        XUnits = Unit.Pixel,
                        YUnits = Unit.InsetPixel
                    }
                },
                Line = new LineStyle
                {
                    Color = Color32.Parse(lineColor),
                    Width = 4
                },
                Polygon = new PolygonStyle
                {
                    Color = Color32.Parse(polygonColor)
                },
                Balloon = new BalloonStyle
                {
                    DisplayMode = DisplayMode.Hide
                }
            };
        }

        private static (double lat, double lng) GPSToLatLng(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (0, 0);

            text = text.Trim().ToUpper(); // 統一大小寫
            text = text.Replace(" ", ""); // 移除空白

            // 拆成 緯度、經度
            string[] parts = text.Split(',');
            if (parts.Length != 2)
                return (0, 0);

            double latitude = ConvertToDecimal(parts[0]);
            double longitude = ConvertToDecimal(parts[1]);

            return (latitude, longitude);
        }

        private static double ConvertToDecimal(string part)
        {
            double degree = 0, minute = 0, second = 0;
            bool isSouthOrWest = part.EndsWith("S") || part.EndsWith("W");

            // 移除 N/E/S/W
            part = part.TrimEnd('N', 'S', 'E', 'W');

            // 取得度
            int degreeIndex = part.IndexOf('°');
            if (degreeIndex == -1) return 0;
            double.TryParse(part.Substring(0, degreeIndex), out degree);

            // 取得分
            int minuteIndex = part.IndexOf('\'');
            if (minuteIndex > degreeIndex)
            {
                double.TryParse(part.Substring(degreeIndex + 1, minuteIndex - degreeIndex - 1), out minute);
            }

            // 取得秒（有可能沒有）
            int secondIndex = part.IndexOf('\"');
            if (secondIndex > minuteIndex)
            {
                double.TryParse(part.Substring(minuteIndex + 1, secondIndex - minuteIndex - 1), out second);
            }

            //x度 y分 z秒 = x + y/60 + z/3600 度              
            double decimalValue = degree + (minute / 60.0) + (second / 3600.0);

            if (isSouthOrWest)
                decimalValue *= -1;

            return decimalValue;
        }

        #endregion
    }
}
