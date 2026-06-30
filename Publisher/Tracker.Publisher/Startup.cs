using Tracker.Publisher.Services;
using Microsoft.EntityFrameworkCore;
using Tracker.Publisher.Data;
using Tracker.Publisher.Interfaces;
using Tracker.Publisher.Services.Repositories;

namespace Tracker.Publisher;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Web api conteroller registration.
        services.AddControllers();

        //resgister db congfiguration
        services.AddDbContext<TelemetryDbCtx>(options => 
            options.UseSqlServer(
                Configuration.GetConnectionString("TelemetryDb")));

        //Repository registration 
        services.AddScoped<ISensorRepository, SensorRepository>();

        //Service registration 
        services.AddSingleton<SensorReadingBackgroundService>();
        //Hook the service to run as background service
        services.AddHostedService(provider => provider.GetRequiredService<SensorReadingBackgroundService>());
    
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
