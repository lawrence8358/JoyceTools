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

            // 取得 Configuration 

            builder.Services.AddDbContext<EarthquakeDbContext>(optionsBuilder =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlite(connectionString);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

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
