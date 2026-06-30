namespace Tracker.SensorReadingProcessor.Models;

public class TimeSeriesAnalysisResult
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double Value { get; set; }

    public double TrendSlope { get; set; }

    public double PeakValue { get; set; }

    public double ValleyValue { get; set; }

    public double Average { get; set; }
}
