
using Microsoft.EntityFrameworkCore;
using Cremory.API.Hubs;
using Cremory.API.Models;
using Cremory.API.Services;

namespace Cremory.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var port = Environment.GetEnvironmentVariable("PORT") ?? "5105";
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? builder.Configuration.GetConnectionString("PostgresConnection");

            builder.Services.AddControllers();
            builder.Services.AddDbContext<Cremory.API.Data.CremoryDbContext>(options =>
                options.UseNpgsql(connStr));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();
            builder.Services.Configure<OrderParserOptions>(
                builder.Configuration.GetSection(OrderParserOptions.SectionName));
builder.Services.Configure<MessengerOptions>(options =>
{
    options.VerifyToken = Environment.GetEnvironmentVariable("MESSENGER_VERIFY_TOKEN")
        ?? builder.Configuration.GetSection(MessengerOptions.SectionName)["VerifyToken"] ?? "";
    options.PageAccessToken = Environment.GetEnvironmentVariable("MESSENGER_PAGE_ACCESS_TOKEN")
        ?? builder.Configuration.GetSection(MessengerOptions.SectionName)["PageAccessToken"] ?? "";
    options.AppSecret = Environment.GetEnvironmentVariable("MESSENGER_APP_SECRET")
        ?? builder.Configuration.GetSection(MessengerOptions.SectionName)["AppSecret"] ?? "";
});
            builder.Services.AddScoped<OrderParserService>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<OrderHub>("/hubs/orders");

            app.Run();
        }
    }
}
