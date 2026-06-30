using Microsoft.EntityFrameworkCore;
using Tracker.Publisher.Data;
using Tracker.Publisher.Interfaces;
using Tracker.Publisher.Models;

namespace Tracker.Publisher.Services.Repositories
{
    public class SensorRepository : ISensorRepository
    {
        private readonly TelemetryDbCtx _db;

        public SensorRepository(TelemetryDbCtx db)
        {
            _db = db;
        }

        public async Task<SensorReading?> GetNextReadingAsync(CancellationToken ct = default)
        {
            return await _db.PendingReadings
                .FromSqlRaw("EXEC sp_GetOldestPending")
                //Performance enhancement to not track pending changes and perofm task faster
                .AsNoTracking()           
                .FirstOrDefaultAsync(ct);
        }

        public async Task SavePendingReadingAsync(SensorReading reading, CancellationToken ct = default)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC sp_SavePendingReading @Timestamp = {0}, @Value = {1}, @SensorType = {2}",
                parameters: new object[] { reading.Timestamp, reading.Value, reading.SensorType },
                cancellationToken: ct
            );
        }
    }
}
