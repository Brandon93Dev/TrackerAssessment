using System.Runtime.CompilerServices;
using Tracker.SensorReadingProcessor.Models;

namespace Tracker.SensorReadingProcessor.Repositories.Interfaces;

public interface ISensorReadingAnalysisRepository
{
    Task InsertAnalisysResultsAsync(TimeSeriesAnalysisResult result, CancellationToken ct);
}
