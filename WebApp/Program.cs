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

            // ���o Configuration 

            builder.Services.AddDbContext<EarthquakeDbContext>(optionsBuilder =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlite(connectionString);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            // �ҥιw�]�ɮס]�p index.html�^
            app.UseDefaultFiles();

            // �ҥ��R�A�ɮתA��
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
