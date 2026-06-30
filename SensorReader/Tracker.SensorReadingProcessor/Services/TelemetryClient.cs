using System.Net.Http.Json;
using Tracker.SensorReadingProcessor.Models;

namespace Tracker.SensorReadingProcessor.Services;

public class TelemetryClient : ITelemetryClient
{
    private readonly HttpClient _httpClient;

    public TelemetryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SensorReading?> GetNextReadingAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<SensorReading>("api/Telemetry/next", ct);
    }
}
