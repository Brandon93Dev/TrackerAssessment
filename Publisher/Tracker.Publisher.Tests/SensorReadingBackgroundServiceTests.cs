using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.Publisher.Interfaces;
using Tracker.Publisher.Models;
using Tracker.Publisher.Services;

namespace Tracker.Publisher.Tests;

public class SensorReadingBackgroundServiceTests
{
    [Fact]
    public async Task GenerateAndQueueReadingAsync_QueueReturnsValueExistsAsync()
    {
        //Arrange
        var mockRepo = new Mock<ISensorRepository>();
        SensorReading readingToPersist = new();

        mockRepo.Setup(r => r.SavePendingReadingAsync(It.IsAny<SensorReading>(), CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback<SensorReading, CancellationToken>((reading, ct) =>
            {
                readingToPersist = reading;
            });

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider.GetService(typeof(ISensorRepository)))
                 .Returns(mockRepo.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                    .Returns(mockScopeFactory.Object);

        var mockLogger = new Mock<ILogger<SensorReadingBackgroundService>>();

        var service = new SensorReadingBackgroundService(mockProvider.Object, mockLogger.Object);

        //Act - we run this under 5 seconds so should not be persisted in the db
        var (persisted, reading, queue) = await service.GenerateAndQueueReadingAsync(CancellationToken.None);

        //Assert
        Assert.NotEqual(readingToPersist.Value, reading.Value);
        Assert.Equal("CoordinatesValue", reading.SensorType);
        Assert.False(persisted);
    }


    [Fact]
    public async Task GenerateAndQueueReadingAsync_Multiple_QueueReturnsDataPersisted()
    {
        //Arrange
        var mockRepo = new Mock<ISensorRepository>();
        List<SensorReading> sensorReadingQueue = new();
        List<SensorReading> readingsToPersist = new();

        mockRepo.Setup(r => r.SavePendingReadingAsync(It.IsAny<SensorReading>(), CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback<SensorReading, CancellationToken>((reading, ct) =>
            {
                readingsToPersist.Add(reading);
            });

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider.GetService(typeof(ISensorRepository)))
                 .Returns(mockRepo.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                    .Returns(mockScopeFactory.Object);

        var mockLogger = new Mock<ILogger<SensorReadingBackgroundService>>();

        var service = new SensorReadingBackgroundService(mockProvider.Object, mockLogger.Object);

        //Act - we call the service a single time, and then call it > 5 seconds later to trigger the persist functionality
        var (persisted, reading, queue) = await service.GenerateAndQueueReadingAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(7), CancellationToken.None);
        var (persisted2, reading2, queue2) = await service.GenerateAndQueueReadingAsync(CancellationToken.None);

        sensorReadingQueue.AddRange(queue2);

        //Assert
        //Ensure the first record is not persisted
        Assert.False(persisted);
        //But ensure that the first record is persisted by the time the second record is generated (5 seconds later)
        Assert.True(persisted2);

        //Ensure only the second reading is in the queue
        Assert.Equal(reading2, sensorReadingQueue.First());
        Assert.Equal(1, sensorReadingQueue.Count());
    }
}
