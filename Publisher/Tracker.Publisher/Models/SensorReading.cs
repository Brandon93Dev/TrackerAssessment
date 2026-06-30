using System.ComponentModel.DataAnnotations;

namespace Tracker.Publisher.Models
{
    public class SensorReading
    {     
        [Key]
        public Guid ID { get; set; }

        //UTC timestamp of the reading
        public DateTimeOffset Timestamp { get; set; }

        public double Value { get; set; }

        public string SensorType { get; set; } = string.Empty;
    }
}
