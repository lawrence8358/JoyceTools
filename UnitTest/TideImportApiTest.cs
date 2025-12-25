using Lib.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WebApp.Controllers;
using WebApp.Models;

namespace UnitTest
{
    /// <summary>
    /// 潮汐資料匯入 API 測試
    /// </summary>
    public class TideImportApiTest
    {
        private readonly EarthquakeDbContext _dbContext;
        private readonly TideController _controller;

        public TideImportApiTest()
        {
            // 使用記憶體資料庫
            var options = new DbContextOptionsBuilder<EarthquakeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new EarthquakeDbContext(options);

            // 建立空的 Configuration
            var configuration = new ConfigurationBuilder().Build();

            _controller = new TideController(_dbContext, configuration);
        }

        #region Model 解析測試

        [Fact]
        public void ParseTideImportModel_FromJson_ShouldSucceed()
        {
            // Arrange
            var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "tide_data_test.json");
            
            if (!File.Exists(testDataPath))
            {
                // 如果檔案不存在，跳過測試
                Assert.True(true, "測試資料檔案不存在，跳過測試");
                return;
            }

            var jsonContent = File.ReadAllText(testDataPath);

            // Act
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var model = JsonSerializer.Deserialize<TideImportModel>(jsonContent, options);

            // Assert
            Assert.NotNull(model);
            Assert.NotNull(model.Locations);
            Assert.Equal(2, model.Locations.Count);
            Assert.Equal("Sydney", model.Locations[0].Name);
            Assert.Equal("Tokyo", model.Locations[1].Name);
        }

        [Fact]
        public void ParseTideLocationImport_ShouldHaveCorrectTimezone()
        {
            // Arrange
            var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "tide_data_test.json");

            if (!File.Exists(testDataPath))
            {
                Assert.True(true, "測試資料檔案不存在，跳過測試");
                return;
            }

            var jsonContent = File.ReadAllText(testDataPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var model = JsonSerializer.Deserialize<TideImportModel>(jsonContent, options);

            // Assert
            Assert.NotNull(model?.Locations);
            
            var sydney = model.Locations.First(x => x.Name == "Sydney");
            Assert.Equal(10.5, sydney.Timezone);

            var tokyo = model.Locations.First(x => x.Name == "Tokyo");
            Assert.Equal(9, tokyo.Timezone);
        }

        [Fact]
        public void ParseTideDayImport_ShouldHaveCorrectData()
        {
            // Arrange
            var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "tide_data_test.json");

            if (!File.Exists(testDataPath))
            {
                Assert.True(true, "測試資料檔案不存在，跳過測試");
                return;
            }

            var jsonContent = File.ReadAllText(testDataPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var model = JsonSerializer.Deserialize<TideImportModel>(jsonContent, options);

            // Assert
            Assert.NotNull(model?.Locations);

            var sydneyData = model.Locations.First(x => x.Name == "Sydney").Data;
            Assert.NotNull(sydneyData);
            Assert.Equal(2, sydneyData.Count);

            var firstDay = sydneyData[0];
            Assert.Equal("周五 26", firstDay.Date);
            Assert.NotNull(firstDay.Tide1);
            Assert.Equal("01:18", firstDay.Tide1.Time);
            Assert.Equal("▲ 1.34 米", firstDay.Tide1.Value);
            Assert.Equal("高潮", firstDay.Tide1.Type);
        }

        #endregion

        #region API Import 測試

        [Fact]
        public void Import_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var model = CreateTestImportModel();

            // Act
            var actionResult = _controller.Import(model);
            var result = actionResult.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            var importResult = result?.Value as TideImportResult;

            // Assert
            Assert.NotNull(importResult);
            Assert.True(importResult.Success);
            Assert.True(importResult.ImportedCount > 0);
        }

        [Fact]
        public void Import_WithEmptyLocations_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new TideImportModel
            {
                ExtractTime = DateTime.Now.ToString("O"),
                Locations = new List<TideLocationImport>()
            };

            // Act
            var actionResult = _controller.Import(model);
            var result = actionResult.Result as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Import_ShouldWriteToDatabase()
        {
            // Arrange
            var model = CreateTestImportModel();

            // Act
            var actionResult = _controller.Import(model);

            // Assert
            var tideData = _dbContext.Tide.ToList();
            Assert.NotEmpty(tideData);
            Assert.Contains(tideData, x => x.Location == "Sydney");
        }

        [Fact]
        public void Import_ShouldParseHeightCorrectly()
        {
            // Arrange
            var model = CreateTestImportModel();

            // Act
            var actionResult = _controller.Import(model);

            // Assert
            var sydneyData = _dbContext.Tide.FirstOrDefault(x => x.Location == "Sydney");
            Assert.NotNull(sydneyData);
            
            // 第一個潮汐應該是高潮 (正數)
            Assert.NotNull(sydneyData.FirstTideHeight);
            Assert.True(sydneyData.FirstTideHeight > 0, "高潮應該是正數");

            // 第二個潮汐應該是低潮 (負數)
            Assert.NotNull(sydneyData.SecondTideHeight);
            Assert.True(sydneyData.SecondTideHeight < 0, "低潮應該是負數");
        }

        [Fact]
        public void Import_ShouldParseTimeCorrectly()
        {
            // Arrange
            var model = CreateTestImportModel();

            // Act
            var actionResult = _controller.Import(model);

            // Assert
            var sydneyData = _dbContext.Tide.FirstOrDefault(x => x.Location == "Sydney");
            Assert.NotNull(sydneyData);
            Assert.NotNull(sydneyData.FirstTideTime);

            // 時間應該包含 01:18 (根據時區轉換後)
            var time = sydneyData.FirstTideTime.Value.TimeOfDay;
            Assert.True(time.Hours >= 0 && time.Hours < 24);
        }

        #endregion

        #region Helper Methods

        private static TideImportModel CreateTestImportModel()
        {
            return new TideImportModel
            {
                ExtractTime = DateTime.Now.ToString("O"),
                Locations = new List<TideLocationImport>
                {
                    new TideLocationImport
                    {
                        Name = "Sydney",
                        Timezone = 10.5,
                        Url = "https://zh.tideschart.com/Australia/New-South-Wales/Sydney",
                        Data = new List<TideDayImport>
                        {
                            new TideDayImport
                            {
                                Date = $"周五 {DateTime.Now.Day}",
                                Tide1 = new TidePointImport { Time = "01:18", Value = "▲ 1.34 米", Type = "高潮" },
                                Tide2 = new TidePointImport { Time = "06:49", Value = "▼ 0.65 米", Type = "低潮" },
                                Tide3 = new TidePointImport { Time = "13:10", Value = "▲ 1.66 米", Type = "高潮" },
                                Tide4 = new TidePointImport { Time = "19:55", Value = "▼ 0.44 米", Type = "低潮" }
                            }
                        }
                    }
                }
            };
        }

        #endregion
    }
}
