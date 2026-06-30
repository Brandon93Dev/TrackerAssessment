using Microsoft.EntityFrameworkCore;
using Tracker.SensorReadingProcessor;
using Tracker.SensorReadingProcessor.Data;
using Tracker.SensorReadingProcessor.Repositories;
using Tracker.SensorReadingProcessor.Repositories.Interfaces;
using Tracker.SensorReadingProcessor.Services;

namespace TrackerSensorReadingProcessor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var hst = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var publisherUrl = hostContext.Configuration["PublisherApi:BaseUrl"];
           
                services.AddHttpClient<ITelemetryClient, TelemetryClient>(client =>
                {
                    client.BaseAddress = new Uri(publisherUrl);
                });

                // Database context
                services.AddDbContext<AnalysisDbContext>(options =>
                {
                    options.UseSqlServer(
                        hostContext.Configuration.GetConnectionString("AnalysisDb")
                    );
                });

                // Registerrrepository interface
                services.AddScoped<ISensorReadingAnalysisRepository, SensorReadingAnalysisRepository>();
                
                services.AddHostedService<ConsumerWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        await hst.RunAsync();
    }
}
