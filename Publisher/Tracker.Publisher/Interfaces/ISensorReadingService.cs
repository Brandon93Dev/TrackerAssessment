namespace Tracker.Publisher.Interfaces;

using Tracker.Publisher.Models;

public interface ISensorReadingService
{
    Task<SensorReading?> GetNextReadingAsync(CancellationToken ct = default);
    Task<(int depth, int totalGenerated, int totalPersisted)> GetQueueDetailsAsync(CancellationToken ct);
}
