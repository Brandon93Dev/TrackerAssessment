namespace Tracker.SensorReadingProcessor.Models;

public class SensorReading
{
    public Guid ID { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double Value { get; set; }
    public string SensorType { get; set; } = string.Empty;
}
