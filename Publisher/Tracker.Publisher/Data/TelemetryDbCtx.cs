using Microsoft.EntityFrameworkCore;
using Tracker.Publisher.Models;

namespace Tracker.Publisher.Data;

public class TelemetryDbCtx : DbContext
{
    public TelemetryDbCtx(DbContextOptions<TelemetryDbCtx> options)
        : base(options) { }

    //Table references
    public DbSet<SensorReading> PendingReadings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region Pending Readings
        //Sql table name mappingg
        modelBuilder.Entity<SensorReading>().ToTable("pendingreadings");

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Timestamp).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.SensorType).IsRequired().HasMaxLength(50);
        });
        #endregion Pending Readings
    }
}
