using ManufacturingScheduler.Application.Services;
using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Infrastructure.AI;
using ManufacturingScheduler.Infrastructure.Data;
using ManufacturingScheduler.Infrastructure.Data.Repositories;
using ManufacturingScheduler.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManufacturingScheduler.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            // Database context (optional - using file-based repos for now)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? ""));

            // Register repositories
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IMachineRepository, MachineRepository>();
            builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();

            // Register scheduling algorithm
            builder.Services.AddScoped<ISchedulingAlgorithm, OptimizedSchedulingAlgorithm>();

            // Register ChatGPT service
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IChatGptService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? "demo-key";
                var endpoint = builder.Configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
                var logger = provider.GetRequiredService<ILogger<ChatGptService>>();
                return new ChatGptService(httpClient, apiKey, endpoint, logger);
            });

            // Register application services
            builder.Services.AddScoped<SchedulingService>();

            var app = builder.Build();

            //Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                 }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        public class SimpleDateTimeConverter : JsonConverter<DateTime>
        {
            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTime.Parse(reader.GetString()!);
            }
        }

    }
}
