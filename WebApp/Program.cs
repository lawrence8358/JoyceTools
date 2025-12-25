using Lib.EFCore; 
using Microsoft.EntityFrameworkCore;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // 設定 CORS (僅允許特定來源)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ChromeExtensionPolicy", policy =>
                {
                    // 允許的來源
                    policy.SetIsOriginAllowed(origin =>
                    {
                        // 允許 Chrome 擴充功能 (chrome-extension://...)
                        if (origin.StartsWith("chrome-extension://"))
                            return true;
                        
                        // 允許本機測試
                        if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                            return true;
                        
                        return false;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            // 取得 Configuration 

            builder.Services.AddDbContext<EarthquakeDbContext>(optionsBuilder =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlite(connectionString);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            // 啟用路由
            app.UseRouting();

            // 啟用 CORS（必須在 UseRouting 之後、UseAuthorization 之前）
            app.UseCors("ChromeExtensionPolicy");

            app.UseAuthorization();

            // 啟用預設檔案（如 index.html）
            app.UseDefaultFiles();

            // 啟用靜態檔案服務
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
