using Microsoft.AspNetCore.Mvc;
using Tracker.Publisher.Models;
using Tracker.Publisher.Services;

namespace Tracker.Publisher.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryController : ControllerBase
    {
        private readonly SensorReadingBackgroundService _sensorReadingService;

        public TelemetryController(SensorReadingBackgroundService sensorReadingService)
        {
            _sensorReadingService = sensorReadingService;
        }

        [HttpGet("next")]
        public async Task<ActionResult<SensorReading>> GetNext(CancellationToken ct)
        {
            var reading = await _sensorReadingService.GetNextReadingAsync(ct);
            if (reading == null) return NoContent();
            return Ok(reading);
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetTelemetryStats(CancellationToken ct)
        {
            //Pull queue Depth
            var queueStats = await _sensorReadingService.GetQueueDetailsAsync(ct);

            return Ok(queueStats);
        }

        //Endpoint to return publisher state
        [HttpGet("ping")]
        public ActionResult GetPublisherState()
        {
            return Ok(new
            {
                message = "Pong",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
