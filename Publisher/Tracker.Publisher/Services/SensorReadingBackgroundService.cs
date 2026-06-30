using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Tracker.Publisher.Interfaces;
using Tracker.Publisher.Models;

[assembly: InternalsVisibleTo("Tracker.Publisher.Tests")]
namespace Tracker.Publisher.Services;

public class SensorReadingBackgroundService : BackgroundService, ISensorReadingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SensorReadingBackgroundService> _logger;
    private readonly ConcurrentQueue<SensorReading> _queue = new();
    private readonly Random _r = new();
    private int totalSensorReadingsGenerated = 0;
    private int totalPersistedToDb = 0;

    public SensorReadingBackgroundService(IServiceProvider serviceProvider,
        ILogger<SensorReadingBackgroundService> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("SensorReadingBackgroundService is starting.");
                      
        //continue to run until stopped
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (persistedToDb, reading,_) = await GenerateAndQueueReadingAsync(ct);  

                _logger.LogInformation("Generating Mock Sensor Reading at: {time}", DateTimeOffset.Now);

                _logger.LogInformation("Generated Sensor Reading: {ID} at time: {time}", reading.ID.ToString(), reading.Timestamp);
                if (persistedToDb)
                    totalPersistedToDb++;

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while mocking sensor readings.");
            }
        }

        _logger.LogInformation("SensorReadingBackgroundService stopping.");
    }

    public bool TryPullNext(out SensorReading? reading)
    {
        return _queue.TryDequeue(out reading);
    }

    //Generate reading and decide if to persist  
    internal async Task<(bool persisted, SensorReading reading, List<SensorReading> queue)> 
        GenerateAndQueueReadingAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var _sensorRepo = scope.ServiceProvider.GetRequiredService<ISensorRepository>();

        var reading = new SensorReading
        {
            ID = Guid.NewGuid(),
            Value = _r.NextDouble() * 100,
            Timestamp = DateTimeOffset.UtcNow,
            SensorType = "CoordinatesValue"
        };

        _queue.Enqueue(reading);
        totalSensorReadingsGenerated++;
        bool persistedToDb = false;

        //we dont want to keep mopre than 10 readings in the ConcurrentQueue, if we see that we canrrently have 10 and persist a new one,
        // we remove the oldest reading and persist it to the database
        while (_queue.Count > 10)
        {
            if (_queue.TryDequeue(out SensorReading? spilledReading))
            {
                _logger.LogInformation("Reading {ID} not consumed in a timely manner and has been persisted to DB", spilledReading.ID.ToString());
                await _sensorRepo.SavePendingReadingAsync(spilledReading, ct);
                persistedToDb = true;
            }
        }

        //Check for oldest reading in the queue and if it has been there for more than 5 seconds, persist it to the database
        if (_queue.TryPeek(out SensorReading oldestReading))
        {
            if ((DateTimeOffset.UtcNow - oldestReading.Timestamp).TotalSeconds > 5)
            {
                if (_queue.TryDequeue(out SensorReading? expiredReading))
                {
                    _logger.LogInformation("Reading {ID} has expired and has been persisted to DB", expiredReading.ID.ToString());
                    await _sensorRepo.SavePendingReadingAsync(expiredReading, ct);
                    persistedToDb = true;
                }
            }
        }
        var queue = _queue.ToArray().ToList();

        return (persistedToDb, reading, queue);
    }


    //Slight inefficientcy haviong to resolvescope everyime
    public async Task<SensorReading?> GetNextReadingAsync(CancellationToken ct = default)
    {
        // Try from queue first
        if (TryPullNext(out var reading))
            return reading;

        using var scope = _serviceProvider.CreateScope();
        var _sensorRepo = scope.ServiceProvider.GetRequiredService<ISensorRepository>();

        // Fallback to DB
        return await _sensorRepo.GetNextReadingAsync(ct);
    }

    public async Task<(int depth, int totalGenerated, int totalPersisted)> GetQueueDetailsAsync(CancellationToken ct)
    {
        // total in queue
        var queueDepth = _queue.Count;

        //total generated
        var totalGenerated = totalSensorReadingsGenerated;

        //total perstedToDb
        var totalPersisted = totalPersistedToDb;

        return (queueDepth, totalGenerated, totalPersisted);
    }


}
