using Microsoft.EntityFrameworkCore;
using Tracker.SensorReadingProcessor.Data;
using Tracker.SensorReadingProcessor.Models;
using Tracker.SensorReadingProcessor.Repositories.Interfaces;

namespace Tracker.SensorReadingProcessor.Repositories;

public class SensorReadingAnalysisRepository : ISensorReadingAnalysisRepository
{
    private readonly AnalysisDbContext _analysisDbContext;
    private readonly ILogger<SensorReadingAnalysisRepository> _logger;

    public SensorReadingAnalysisRepository(AnalysisDbContext analysisDbContext, ILogger<SensorReadingAnalysisRepository> logger)
    {
        _analysisDbContext = analysisDbContext;
        _logger = logger;    
    }

    public async Task InsertAnalisysResultsAsync(TimeSeriesAnalysisResult result, CancellationToken ct)
    {
        await using var transaction = await _analysisDbContext.Database.BeginTransactionAsync();
        try
        {
            await _analysisDbContext.Database.ExecuteSqlRawAsync(
                @"EXEC sp_InsertAnalysisResults
                    @Timestamp = {0},
                    @Value = {1},
                    @TrendSlope = {2},
                    @PeakValue = {3},
                    @ValleyValue = {4},
                    @Average = {5}",
                new object[]
                {
                    result.Timestamp,
                    result.Value,
                    result.TrendSlope,
                    result.PeakValue,
                    result.ValleyValue,
                    result.Average
                },
                ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occured while attempting to store analysis result: {result.Id} could not be logged");
            await transaction.RollbackAsync(ct);
            throw;
        }

    }
}