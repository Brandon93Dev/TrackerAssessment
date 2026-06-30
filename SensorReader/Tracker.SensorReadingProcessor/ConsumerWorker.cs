using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Tracker.SensorReadingProcessor.Models;
using Tracker.SensorReadingProcessor.Repositories.Interfaces;
using Tracker.SensorReadingProcessor.Services;

[assembly: InternalsVisibleTo("Tracker.SensorReadingProcessor.Tests")]

namespace Tracker.SensorReadingProcessor;

public class ConsumerWorker : BackgroundService
{
    private readonly ILogger<ConsumerWorker> _logger;
    private readonly ITelemetryClient _tClient;
    private readonly ConcurrentQueue<SensorReading> _queue = new();
    private readonly IServiceProvider _serviceProvider;

    //configurable poll interval
    private readonly int _intervalInMs;
    private readonly int _startAnalysisAt;
    private readonly int _backoffPeriodInMs = 60000;
    private readonly int _maxRetries = 5;

    private readonly int _movingAverageWindow = 5;
    private readonly Queue<double> _movingWindow = new();

    public ConsumerWorker(ILogger<ConsumerWorker> logger, ITelemetryClient tClient, IConfiguration config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _tClient = tClient;
        _intervalInMs = config.GetValue<int>("PollIntervalMs", 2000);
        _startAnalysisAt = config.GetValue<int>("AnalysisTrigger", 10);
        _serviceProvider = serviceProvider;
        _movingWindow.EnsureCapacity(_movingAverageWindow);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Consumer worker started at {Time}.", DateTimeOffset.UtcNow);

        //Run fetch task in background to constantly fill queue            
        var fetchTask = Task.Run(() => FetchReadingAsync(ct), ct);

        //Process loop runs in background thread
        await ProcessReadingAsync(ct);

        await fetchTask;

        _logger.LogInformation("Consumer worker execution terminated at {Time}.", DateTimeOffset.UtcNow);
    }

    internal async Task FetchReadingAsync(CancellationToken ct)
    {
        //Terminate task if cancellation is requested
        while (!ct.IsCancellationRequested)
        {
            try
            {
                int attemptCount = 0;
                int backoffTime = _backoffPeriodInMs;
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var reading = await _tClient.GetNextReadingAsync(ct);

                        if (reading != null)
                        {
                            _queue.Enqueue(reading);
                            _logger.LogInformation("Fetched reading {ID} at timestamp at {Timestamp}", reading.ID, reading.Timestamp);
                        }
                        else
                            _logger.LogInformation("No readings available at time {Time}.", DateTimeOffset.UtcNow);

                        //Exits retry loop as success is reached here
                        break;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        //cancellation has been requested, exit the retry as well
                        break;
                    }
                    catch (Exception ex)
                    {
                        attemptCount++;
                        _logger.LogWarning(ex, "Transient error fetching telemetry (attempt {Attempt}). Retrying after {Backoff}ms.", attemptCount, backoffTime);

                        if (attemptCount >= _maxRetries)
                        {
                            _logger.LogError(ex, "Max retry attempts reached fetching telemetry. Will resume polling and try again later.");
                            break;
                        }

                        await Task.Delay(backoffTime, ct);

                        backoffTime += _backoffPeriodInMs;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Telemetry Service offline or unavailable at {DateTime.UtcNow}");

            }

            await Task.Delay(_intervalInMs, ct);
        }
    }

    internal async Task ProcessReadingAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // efficiency change, exit process early if there arent enough items to process
            if (_queue.Count < _startAnalysisAt)
            {
                try { await Task.Delay(_intervalInMs, ct); } catch (TaskCanceledException) { }
                continue;
            }

            var snapshot = new List<SensorReading>();
            for (int i = 0; i < _startAnalysisAt; i++)
            {
                if (!_queue.TryDequeue(out var item))
                    break;
                snapshot.Add(item);
            }

            //fallback if not enough items were found, return the items to the concurrentqueue
            if (snapshot.Count < _startAnalysisAt)
            {
                foreach (var s in snapshot) _queue.Enqueue(s);
                try { await Task.Delay(_intervalInMs, ct); } catch (TaskCanceledException) { }
                continue;
            }

            try
            {
                //Add Item to movingWindow
                _movingWindow.Enqueue(snapshot.Last().Value);

                // Keep only the last 5
                if (_movingWindow.Count > _movingAverageWindow)
                    _movingWindow.Dequeue();

                //Calc 5 point moving average if items == 5
                if (_movingWindow.Count == _movingAverageWindow)
                {
                    var avgMw = _movingWindow.Average();
                    _logger.LogInformation($"---moving average for last 5 values {avgMw}---");
                }

                // Determine trend slope
                var slope = CalculateTrendSlope(snapshot.Select(r => (r.Timestamp, r.Value)).ToList());

                // Determine peaks and valleys
                var peaksValleys = DeterminePeaksAndValleys(snapshot.Select(r => (r.Timestamp, r.Value)).ToList());

                // Average between values
                var avg = AverageBetweenValues(snapshot.Select(r => r.Value).ToList());

                var lastReading = snapshot.Last();
                var resultPersistt = new TimeSeriesAnalysisResult
                {
                    Id = Guid.NewGuid(),
                    Timestamp = lastReading.Timestamp,
                    Value = lastReading.Value,
                    TrendSlope = slope,
                    PeakValue = peaksValleys.Any() ? peaksValleys.Max(x => x.Value) : lastReading.Value,
                    ValleyValue = peaksValleys.Any() ? peaksValleys.Min(x => x.Value) : lastReading.Value,
                    Average = avg
                };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<ISensorReadingAnalysisRepository>();
                    await repo.InsertAnalisysResultsAsync(resultPersistt, ct);
                }

                _logger.LogInformation("Persisted analysis result {Id} at {Timestamp}", resultPersistt.Id, resultPersistt.Timestamp);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reading");
                // On error re-enqueu the snapshot so data is not lost
                foreach (var s in snapshot) _queue.Enqueue(s);
            }
        }
    }


    internal static double CalculateTrendSlope(List<(DateTimeOffset Timestamp, double Value)> readings)
    {
        if (readings == null || readings.Count < 2)
            return 0;

        var xs = readings.Select(r => r.Timestamp.ToUnixTimeSeconds()).ToArray();
        var ys = readings.Select(r => r.Value).ToArray();

        double xMean = xs.Average();
        double yMean = ys.Average();

        double numerator = xs.Zip(ys, (x, y) => (x - xMean) * (y - yMean)).Sum();
        double denominator = xs.Sum(x => Math.Pow(x - xMean, 2));

        if (Math.Abs(denominator) < double.Epsilon)
            return 0;

        return numerator / denominator;
    }

    public static List<(DateTimeOffset Timestamp, double Value, string Type)> DeterminePeaksAndValleys(
        List<(DateTimeOffset Timestamp, double Value)> readings)
    {
        var results = new List<(DateTimeOffset, double, string)>();

        for (int i = 1; i < readings.Count - 1; i++)
        {
            var prev = readings[i - 1].Value;
            var curr = readings[i].Value;
            var next = readings[i + 1].Value;

            if (curr > prev && curr > next)
                results.Add((readings[i].Timestamp, curr, "Peak"));
            else if (curr < prev && curr < next)
                results.Add((readings[i].Timestamp, curr, "Valley"));
        }

        return results;
    }

    public double AverageBetweenValues(List<double> values)
    {
        if (values == null || values.Count == 0)
            return 0;

        int amountValues = values.Count;
        double total = 0;

        foreach (var v in values)
            total += v;

        return total / amountValues;
    }

    internal void AddReading(SensorReading reading) => _queue.Enqueue(reading);
}