# SOLUTION.md

## Overview
I was tasked to create 2 seperate solutions, one to generate SensorReadings and one to consume Queued or Persisted SensorReadings and generate analysis on those readings.  
- The **Publisher** generates sensor readings while running (one per second), queues them in memory, and spills them to SQL Server if not consumed in a timely manner.  
- The **Consumer** fetches readings with them, performs complex analysis (5‑point moving average, trend slope, peaks/valleys, averages), and persists results back to SQL Server 
- **Stored procedures** are used with all database interactions apart from actually applying migrations, ensuring security, consistency, and compliance with best practices.  
- The system is resilient, scalable (more info on this below), and designed with separation of concerns where layers are applied.

---

## Key Design Decisions

### Publisher
- **BackgroundService** generates mock sensor readings every second.  
- **Unique IDs (Guids)**: Each reading uses `Guid.NewGuid()` for a globally unique identifier.  
- **UTC Timestamps**: All readings use `DateTimeOffset.UtcNow`, stored as `DATETIMEOFFSET` in SQL, ensuring ISO 8601 UTC compliance as mnetioned in the requirements document.  
- **ConcurrentQueue**: Maintains FIFO ordering in memory.  
- **Spill Logic**:  
  - If queue depth exceeds 10, oldest readings are persisted via `sp_SavePendingReading`.  
  - If a reading is not consumed within 5 seconds, it is persisted to DB.  
- **TelemetryController** exposes endpoints for consumers .  
- **Repository Pattern**: `SensorRepository` encapsulates DB access, using stored procedures for db crud operations.  
- **Resilience**: Readings are persisted if not consumed in time, ensuring no data loss.

### Consumer
- **BackgroundService** fetches readings continuously, separate from publisher.  
- **ConcurrentQueue** ensures FIFO ordering across fetch and process boundaries.  
- **Batch Processing**: Analysis is triggered when `_startAnalysisAt` items are available.  
- **Complex Analysis**:  
  - 5‑point moving average .  
  - Trend slope (linear repesentation of data).  
  - Peaks and valleys detection.  
  - Average calculation.  
- **Persistence**: Results are saved via `sp_InsertAnalysisResults`.  
- **Resilience**:  
  - Retry with backoff when publisher is offline.  
  - Errors logged at appropriate levels.  
  - Snapshots re‑enqueued if persistence fails, preventing data loss.

### Database Layer
- **Tables**:  
  - `pendingreadings` for unconsumed publisher data.  
  - `timeseriesanalysis` for processed consumer results.  
- **Stored Procedures**:  
  - `sp_SavePendingReading` → inserts pending readings.  
  - `sp_GetOldestPending` → retrieves oldest pending reading.  
  - `sp_InsertAnalysisResults` → inserts analysis results.  
- **Best Practices**:  
  - Parameterized queries prevent injection.  
  - Transactions ensure on failure rollback is performed operations is reversed.  
  - FIFO ordering enforced via `ORDER BY Timestamp ASC` in databse.

---

## Scalability

- **Multiple Consumers**:  
  - Each consumer can safely fetch readings from the publisher or DB.  
  - FIFO ordering ensures consistent processing across consumers.  
- **Higher Throughput**:  
  - Queue thresholds (`max size`, `expiry time`) can be tuned (appsettings available).  
  - Batch size (`_startAnalysisAt`) balances throughput vs latency.  
- **Additional Sensor Types**:  
  - `SensorType` field supports extensibility.  
  - New sensors can be added without schema changes. (for now i just added one sensorreading type)  
- **Message Queues**:  
  - For very high throughput, replace in‑memory queue with distributed queues (Kafka, Azure Service Bus).  
  - Enables horizontal scaling across multiple publishers and consumers.  
- **Resilience at Scale**:  
  - Retry with backoff prevents overload when publisher is offline.  
  - Spill logic ensures no data loss under heavy load.

---

## Testing & Validation

- **Unit Tests**:  
  - Publisher tests validate spill logic (readings older than 5s or exceeding queue size are persisted).  
  - FIFO ordering confirmed by ensuring only newest readings remain in queue after persistance.  
  - Repository mocked with Moq to verify persistence calls without hitting DB.  
---

## Conclusion
This system is designed with **robustness, scalability, and maintainability** in mind.  
- Publisher and Consumer are decoupled (seperation of concerns, both in projects as well as seprate modules in each project), enabling independent scaling.  
- Stored procedures enforce security and consistency.  
- FIFO ordering and stable IDs guarantee correctness.  
- Resilience mechanisms ensure graceful handling of offline states.  
- The architecture is extensible to support new sensor types and message queues (we could add functionality to use services like RabbitMQ, Kafka or MSMQ).
