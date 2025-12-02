using Lib.EFCore;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EarthquakeController : ControllerBase
    {
        #region Members

        private readonly EarthquakeDbContext _dbContext;
        private readonly IConfiguration _configuration;

        #endregion

        #region Constructor

        public EarthquakeController(EarthquakeDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        #endregion

        #region Public Methods

        [HttpPost("Query")]
        public IEnumerable<EarthquakeViewModel> GetDataSource(EarthquakeQueryModel model)
        {
            var sdate = model.Sdate.ToLocalTime().Date;
            var edate = model.Edate.ToLocalTime().Date.AddDays(1);

            var list = _dbContext.Earthquake
                .Where(x => x.EarthquakeDate != null && x.EarthquakeDate >= sdate && x.EarthquakeDate < edate)
                .Select(x => new EarthquakeViewModel
                {
                    EarthquakeDate = x.EarthquakeDate!.Value,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Magnitude = x.Magnitude,
                    MaxDepth = x.MaxDepth,
                    LinkUrl = x.LinkUrl,
                    ImageFileName = x.FileName2
                })
                .OrderByDescending(x => x.EarthquakeDate)
                .ToArray();

            return list;
        }

        [HttpGet("Image/Thumb/{fileName}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult GetThumbnail(string fileName)
        {
            var imagePath = GetImageFilePath(fileName);

            if (!System.IO.File.Exists(imagePath))
                return NotFound();

            var ms = GetThumbnailStream(imagePath);

            return File(ms, "image/jpeg");
        }

        [HttpGet("Image/Original/{fileName}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult GetOriginalImage(string fileName)
        {
            var imagePath = GetImageFilePath(fileName);
            if (!System.IO.File.Exists(imagePath))
                return NotFound();

            var fileInfo = new FileInfo(imagePath);
            return PhysicalFile(fileInfo.FullName, "image/jpeg");
        }

        #endregion

        #region Private Methods

        private static MemoryStream GetThumbnailStream(string imagePath)
        {
            // 縮圖
            using var image = Image.Load(imagePath);
            int maxWidth = 50;
            float ratio = (float)maxWidth / image.Width;
            int newWidth = maxWidth;
            int newHeight = (int)(image.Height * ratio);

            // 調整大小
            image.Mutate(x => x.Resize(newWidth, newHeight));

            var ms = new MemoryStream();
            // 編碼成 JPEG
            var encoder = new JpegEncoder { Quality = 90 };
            image.Save(ms, encoder);
            ms.Position = 0;
            return ms;
        }

        private string GetImageFilePath(string fileName)
        {
            var dir = _configuration.GetValue<string>("TwitterDownloadDir")!;
            return Path.Combine(dir, fileName);
        }

        #endregion
    }
}
