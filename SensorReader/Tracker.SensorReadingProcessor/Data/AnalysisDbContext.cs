using Microsoft.EntityFrameworkCore;
using Tracker.SensorReadingProcessor.Models;

namespace Tracker.SensorReadingProcessor.Data;

public class AnalysisDbContext : DbContext
{

    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region Analysis Results
        modelBuilder.Entity<TimeSeriesAnalysisResult>().ToTable("timeseriesanalysis");

        modelBuilder.Entity<TimeSeriesAnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Timestamp)
                .HasColumnType("datetimeoffset")
                .IsRequired();

            entity.Property(e => e.Value).IsRequired();
        });
        #endregion Analysis Results
    }
}
