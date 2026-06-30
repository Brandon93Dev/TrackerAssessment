using Tracker.Publisher.Models;

namespace Tracker.Publisher.Interfaces
{
    public interface ISensorRepository
    {
        Task SavePendingReadingAsync(SensorReading reading, CancellationToken ct = default);   

        Task<SensorReading?> GetNextReadingAsync(CancellationToken ct = default);     
    }
}
