using ABCBabyShop_2.Services;
using System.Globalization;

namespace ABCBabyShop_2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllersWithViews();

            // Register Azure services
            builder.Services.AddSingleton<AzureTableService>();
            builder.Services.AddSingleton<AzureBlobService>();
            builder.Services.AddSingleton<AzureQueueService>();
            builder.Services.AddSingleton<AzureFileService>();
            builder.Services.AddHttpClient();
            

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Product}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
