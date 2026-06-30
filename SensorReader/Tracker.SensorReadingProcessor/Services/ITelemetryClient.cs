using Tracker.SensorReadingProcessor.Models;

namespace Tracker.SensorReadingProcessor.Services;

public interface ITelemetryClient
{
    Task<SensorReading?> GetNextReadingAsync(CancellationToken ct);
}
