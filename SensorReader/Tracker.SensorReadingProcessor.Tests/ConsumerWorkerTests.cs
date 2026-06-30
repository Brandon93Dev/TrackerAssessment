using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.SensorReadingProcessor.Models;
using Tracker.SensorReadingProcessor.Repositories.Interfaces;
using Tracker.SensorReadingProcessor.Services;
using Microsoft.Extensions.Configuration;

namespace Tracker.SensorReadingProcessor.Tests;

public class ConsumerWorkerTests
{
    [Fact]
    public async Task ProcessReadingAsync_PersistsAnalysisResults()
    {
        // Arrange
        var mockRepo = new Mock<ISensorReadingAnalysisRepository>();
        TimeSeriesAnalysisResult? persistedResult = null;

        mockRepo.Setup(r => r.InsertAnalisysResultsAsync(It.IsAny<TimeSeriesAnalysisResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<TimeSeriesAnalysisResult, CancellationToken>((result, ct) =>
            {
                persistedResult = result;
            });

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider.GetService(typeof(ISensorReadingAnalysisRepository)))
                 .Returns(mockRepo.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                    .Returns(mockScopeFactory.Object);

        var mockLogger = new Mock<ILogger<ConsumerWorker>>();
        var mockTelemetry = new Mock<ITelemetryClient>();

        var inMemorySettings = new Dictionary<string, string>
        {
            { "PollIntervalMs", "50" },
            { "AnalysisTrigger", "2" }
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var worker = new ConsumerWorker(mockLogger.Object, mockTelemetry.Object, config, mockProvider.Object);
     
        worker.AddReading(new SensorReading { ID = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, Value = 42 });
        worker.AddReading(new SensorReading { ID = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow.AddSeconds(1), Value = 43 });

        // Act: run with a cancellation token that stops after 200ms
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.ProcessReadingAsync(cts.Token);

        // Assert
        Assert.NotNull(persistedResult);
        Assert.InRange(persistedResult.Value, 42, 43);
        Assert.True(persistedResult.Average > 0);
    }
}
